using BepInEx;
using BepInEx.Logging;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MultiWorld.UI;
using MultiWorld.ArchipelagoClient;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using System;
using MultiWorld.DeathLink;
using MultiWorld.Notification;

namespace MultiWorld;

[BepInDependency("xyz.yekoc.wizardoflegend.LegendAPI", BepInDependency.DependencyFlags.HardDependency)]
[BepInPlugin("Molitany.Archipelago", "Archipelago", "1.0.0")]
public class MultiWorldPlugin : BaseUnityPlugin
{
    public static readonly ManualLogSource Log = BepInEx.Logging.Logger.CreateLogSource("MultiWorld");
    public static MultiWorldPlugin Instance { get; private set; }
    public static ArchipelagoManager ArchipelagoManager { get; private set; }
    public static DeathLinkManager DeathLinkManager { get; private set; }
    public static GameSettings MultiworldSettings { get; private set; }
    public static NotificationManager NotificationManager { get; private set; }
    public static ChatBoxController ChatBoxController { get; private set; }
    public static List<string> ChatLines = [];
    public static bool InGame => GameController.activePlayers.Any();
    public static int allowedTier = 0;
    public static Player Player;
    public static bool IsConnected;

    private static Dictionary<string, GameData.StoredItemData> Relics { get; set; }
    private static Dictionary<string, Outfit> Outfits { get; set; }
    private static Dictionary<string, Player.SkillState> Skills { get; set; }
    #region Hooks
    private void Player_DeadState_OnEnter(On.Player.DeadState.orig_OnEnter orig, Player.DeadState self)
    {
        orig(self);
        if (DeathLinkManager.CurrentStatus == DeathLinkStatus.Nothing)
            DeathLinkManager.SendDeath();
        else if (DeathLinkManager.CurrentStatus == DeathLinkStatus.Killing)
            DeathLinkManager.CurrentStatus = DeathLinkStatus.Nothing;
    }

    private void TitleScreen_HandleMenuStates(On.TitleScreen.orig_HandleMenuStates orig, TitleScreen self)
    {
        if (ArchipelagoConnectButtonController.IsOpened)
            return;
        orig(self);
    }

    private void Player_RunState_Update(On.Player.RunState.orig_Update orig, Player.RunState self)
    {
        if (ChatBoxController.Writing)
            return;
        orig(self);
    }
    private void GameUI_TogglePause(On.GameUI.orig_TogglePause orig)
    {
        if (ChatBoxController.Writing)
            return;
        orig();
    }

    private void NextLevelLoader_LoadNextLevel(On.NextLevelLoader.orig_LoadNextLevel orig, NextLevelLoader self)
    {
        if (SceneManager.GetActiveScene().name.Contains("BossLevel") && GameController.tierCount + 1 > allowedTier)
        {
            self.nextLevelName = "Hub";
            self.showLevelSummary = true;
        }
        orig(self);
    }

    #endregion

    public void Awake()
    {
        Instance = this;
        ArchipelagoManager = new();
        DeathLinkManager = new();
        NotificationManager = new();
        AssetBundleHelper.LoadBundle();
        CreateArchipelagoInfo();
        On.NextLevelLoader.LoadNextLevel += NextLevelLoader_LoadNextLevel;
        On.TitleScreen.HandleMenuStates += TitleScreen_HandleMenuStates;
        On.Player.DeadState.OnEnter += Player_DeadState_OnEnter;
        On.Player.RunState.Update += Player_RunState_Update;
        On.LoadingScreen.StopLoading += LoadingScreen_StopLoading;
        //On.GameController.OnLevelWasLoaded += GameController_OnLevelWasLoaded;
        On.GameUI.TogglePause += GameUI_TogglePause;
    }

    private void LoadingScreen_StopLoading(On.LoadingScreen.orig_StopLoading orig, LoadingScreen self)
    {
        orig(self);
        if (!GameObject.Find("ChatInput"))
            CreateChatBoxtUI();
    }


    // Create a chat to receive and send AP commands
    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            Log.LogInfo("big thing");
            var allGO = GameObject.Find("TitleMenu");
            Log.LogMessage(allGO.transform.parent.name);
            for (int i = 0; i < allGO.transform.childCount; i++)
            {
                Log.LogMessage(allGO.transform.GetChild(i).name);
            }
            Log.LogMessage($"scene: {SceneManager.GetActiveScene().name}");
            CreateConnectUI();
        }
        if (Player)
        {
            DeathLinkManager.Update();
        }
        if (InGame && Player == null)
        {
            Player = GameController.activePlayers[0];
            Relics = GameDataManager.gameData.itemDataDictionary;
            Outfits = Outfit.outfitDict;
            Skills = Player.skillsDict;
            Log.LogMessage($"firing the UI");
            if (!ArchipelagoManager.Connected)
                CreateConnectUI();
        }
        ArchipelagoManager.UpdateAllReceivers();
    }

    #region UI
    private void CreateArchipelagoInfo()
    {
        Log.LogMessage($"ArchipelagoManager && ArchipelagoConnectButtonController");
        ArchipelagoConnectButtonController.OnSlotChanged = (value) => ArchipelagoManager.SlotName = value;
        ArchipelagoConnectButtonController.OnPasswordChanged = (value) => ArchipelagoManager.Password = value;
        ArchipelagoConnectButtonController.OnUrlChanged = (value) => ArchipelagoManager.Url = value;
        ArchipelagoConnectButtonController.OnPortChanged = (value) => ArchipelagoManager.Port = value;
        ArchipelagoConnectButtonController.OnConnectClick = ConnectButton;
    }

    private void ConnectButton()
    {
        var url = $"{ArchipelagoManager.Url}:{ArchipelagoManager.Port}";
        Log.LogInfo($"Server {ArchipelagoManager.Url} Port: {ArchipelagoManager.Port} Slot: {ArchipelagoManager.SlotName} Password: {ArchipelagoManager.Password}");
        Log.LogMessage(ArchipelagoManager.Connect(url, ArchipelagoManager.SlotName, ArchipelagoManager.Password));
    }

    private void CreateConnectUI()
    {
        var ConnectUI = new GameObject("ArchipelagoConnectButtonController");
        ConnectUI.AddComponent<ArchipelagoConnectButtonController>();
    }
    private void CreateChatBoxtUI()
    {
        var ChatBox = new GameObject("ChatBoxController");
        ChatBoxController = ChatBox.AddComponent<ChatBoxController>();
    }
    #endregion

    public static bool CheckItemExists(string item)
    {
        if (Relics.ContainsKey(item))
            return true;
        else if (item.EndsWith("Signature"))
        {
            if (Skills.ContainsKey(item.Replace("Signature", "")))
                return true;
        }
        else if (Skills.ContainsKey(item))
            return true;
        else if (Outfits.ContainsKey(item))
            return true;
        else if (item.Contains("Boss tier"))
            return true;
        return false;
    }

    public static bool AddToInventory(QueuedItem item)
    {
        if (Relics.ContainsKey(item.itemId))
        {
            GameDataManager.gameData.UpdateItemDataEntry(item.itemId);
            return true;
        }
        else if (item.itemId.Contains("Signature"))
        {
            if (Skills.TryGetValue(item.itemId.Replace("Signature", ""), out var skill))
            {
                skill.signatureUnlocked = true;
                GameDataManager.gameData.PushSkillData();
                return true;
            }
        }
        else if (Skills.TryGetValue(item.itemId, out var skill))
        {
            skill.isUnlocked = true;
            GameDataManager.gameData.PushSkillData();
            return true;
        }
        else if (Outfits.TryGetValue(item.itemId, out var outfits))
        {
            outfits.unlocked = true;
            GameDataManager.gameData.PushOutfitData();
            return true;
        }
        else if (item.itemId.Contains("Boss tier"))
        {
            allowedTier++;
            return true;
        }
        return false;
    }

    public static void OnConnect(GameSettings settings)
    {
        MultiworldSettings = settings;
    }
}
