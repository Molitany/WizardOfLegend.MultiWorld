using BepInEx;
using BepInEx.Logging;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MultiWorld.UI;
using MultiWorld.Archipelago;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using System;

namespace MultiWorld;

[BepInDependency("xyz.yekoc.wizardoflegend.LegendAPI", BepInDependency.DependencyFlags.HardDependency)]
[BepInPlugin("Molitany.Archipelago", "Archipelago", "1.0.0")]
public class MultiWorldPlugin : BaseUnityPlugin
{
    public static readonly ManualLogSource Log = BepInEx.Logging.Logger.CreateLogSource("MultiWorld");
    public static ArchipelagoManager ArchipelagoManager { get; private set; }
    public static MultiWorldPlugin Instance { get; private set; }
    public Dictionary<string, GameData.StoredItemData> Relics { get; private set; } = [];
    public Dictionary<string, GameData.StoredSkillData> Skills { get; private set; } = [];
    public Dictionary<string, bool> Outfits { get; private set; } = [];
    public static bool InGame => GameController.activePlayers.Any();
    public static Player Player;
    public static bool IsConnected;

    public static GameObject ChatBox { get; private set; }
    public static int allowedTier = 0;

    private bool UIDisplayed = false;

    public static BepInEx.Configuration.ConfigEntry<string> SlotNameEntry { get; set; }
    public static BepInEx.Configuration.ConfigEntry<string> ServerNameEntry { get; set; }
    public static BepInEx.Configuration.ConfigEntry<string> PortEntry { get; set; }
    public static BepInEx.Configuration.ConfigEntry<string> PasswordEntry { get; set; }

    public struct ArchipelagoItem
    {
        public string name;
        public string type;
        public string classification;
    }

    public void Awake()
    {
        Instance = this;
        ArchipelagoManager = new ArchipelagoManager();
        On.NextLevelLoader.LoadNextLevel += NextLevelLoader_LoadNextLevel;
        AssetBundleHelper.LoadBundle();
        CreateArchipelagoInfo();
        On.TitleScreen.HandleMenuStates += TitleScreen_HandleMenuStates;
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
        if (GameObject.Find("TitleScreen") && !UIDisplayed)
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
        var url = ArchipelagoManager.Url + ArchipelagoManager.Port;

        Log.LogInfo($"Server {ArchipelagoManager.Url} Port: {ArchipelagoManager.Port} Slot: {ArchipelagoManager.SlotName} Password: {ArchipelagoManager.Password}");

        Log.LogInfo(ArchipelagoManager.Connect(url, ArchipelagoManager.SlotName, ArchipelagoManager.Password));
    }

    private void CreateConnectUI()
    {
        var ConnectUI = new GameObject("ArchipelagoConnectButtonController");
        ConnectUI.AddComponent<ArchipelagoConnectButtonController>();
    }
}
