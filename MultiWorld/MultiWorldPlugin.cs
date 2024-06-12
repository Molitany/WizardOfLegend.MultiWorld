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
    public Dictionary<string, GameData.StoredItemData> Relics { get; private set; } = [];
    public Dictionary<string, GameData.StoredSkillData> Skills { get; private set; } = [];
    public Dictionary<string, bool> Outfits { get; private set; } = [];
    public static bool InGame => GameController.activePlayers.Any();
    public static int allowedTier = 0;
    public static Player Player;
    public static bool IsConnected;

    private bool UIDisplayed = false;
    public struct ArchipelagoItem
    {
        public string name;
        public string type;
        public string classification;
    }

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
    }

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

    private void NextLevelLoader_LoadNextLevel(On.NextLevelLoader.orig_LoadNextLevel orig, NextLevelLoader self)
    {
        if (Application.loadedLevelName.Contains("BossLevel") && GameController.tierCount + 1 > allowedTier)
        {
            self.nextLevelName = "Hub";
            self.showLevelSummary = true;
        }
        orig(self);

        // possibly use stageCount to cancel the next floor if we dont have that unlock yet, send to "Hub"?
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
        if (GameObject.Find("TitleScreen") && !GameObject.Find("ArchipelagoConnectButtonController") && !UIDisplayed)
        {
            Log.LogMessage($"firing the UI");
            CreateConnectUI();
            UIDisplayed = true;
        }
        if (Player) GameUI.BroadcastNoticeMessage(Player.transform.position.ToString());
        if (InGame && Player == null)
        {
            Relics = GameDataManager.gameData.itemDataDictionary;
            Outfits = GameDataManager.gameData.outfitDataDictionary;
            Player = GameController.activePlayers[0];
            CreateChatBoxtUI();
        }
    }

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
        ChatBox.AddComponent<ChatBoxController>();
    }

    public static void OnConnect(GameSettings settings)
    {
        MultiworldSettings = settings;
    }
}
