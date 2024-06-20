using BepInEx;
using BepInEx.Logging;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MultiWorld.UI;
using MultiWorld.ArchipelagoClient;
using UnityEngine.SceneManagement;
using MultiWorld.DeathLink;
using MultiWorld.Notification;
using System.Text;
using System.Collections.ObjectModel;
using System;

namespace MultiWorld;

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
    public static Player Player;
    public static int allowedTier = 0;
    public static bool IsConnected;

    private static Dictionary<string, GameData.StoredItemData> Relics { get; set; }
    private static Dictionary<string, Outfit> Outfits { get; set; }
    private static Dictionary<string, Player.SkillState> Skills { get; set; }
    private bool FirstTime = true;
    private readonly List<string> possibleItems = [];
    private readonly List<string> possibleSkills = [];
    private readonly List<string> possibleSignatures = [];
    private readonly List<string> possibleOutfits = [];

    #region Hooks
    private void Hook()
    {
        On.NextLevelLoader.LoadNextLevel += NextLevelLoader_LoadNextLevel;
        On.TitleScreen.HandleMenuStates += TitleScreen_HandleMenuStates;
        On.Player.DeadState.OnEnter += Player_DeadState_OnEnter;
        On.Player.RunState.Update += Player_RunState_Update;
        On.LoadingScreen.StopLoading += LoadingScreen_StopLoading;
        On.GameUI.TogglePause += GameUI_TogglePause;
        On.UnlockNotifier.Notify += UnlockNotifier_Notify;
        On.SkillStoreItem.Buy += SkillStoreItem_Buy;
        On.Player.HandleSkillUnlock_string_bool += Player_HandleSkillUnlock;
        On.ItemStoreItem.BuyWithPlat += ItemStoreItem_BuyWithPlat;
        On.Item.IsUnlocked += Item_IsUnlocked;
        On.OutfitStoreItem.Buy += OutfitStoreItem_Buy;
        On.Outfit.UnlockOutfit += Outfit_UnlockOutfit;
        On.GameController.Awake += GameController_Awake;
        On.OutfitMerchantNpc.CreateOutfitStoreItem += OutfitMerchantNpc_CreateOutfitStoreItem;
    }

    private void OutfitMerchantNpc_CreateOutfitStoreItem(On.OutfitMerchantNpc.orig_CreateOutfitStoreItem orig, OutfitMerchantNpc self, Vector2 givenPosition, string givenID)
    {
        if (possibleOutfits.Count > 0)
        {
            var text = possibleOutfits[UnityEngine.Random.Range(0, possibleOutfits.Count)];
            possibleOutfits.Remove(text);
            orig(self, givenPosition, text);
        }
    }

    private void GameController_Awake(On.GameController.orig_Awake orig, GameController self)
    {
        if (FirstTime)
        {
            On.LootManager.GetSkillID += LootManager_GetSkillID;
            On.LootManager.GetLockedItemID += LootManager_GetLockedItemID;
            FirstTime = false;
        }

        if (Player)
        {
            possibleSkills.AddRange(LootManager.completeSkillList.Except(ArchipelagoManager.FoundLocations[NotificationManager.NoticeType.Spell])
                                                                 .Except(Globals.startingSkillIDList)
                                                                 .Except(Globals.startingOverdriveIDList));

            possibleSignatures.AddRange(LootManager.completeSkillList.Except(ArchipelagoManager.FoundLocations[NotificationManager.NoticeType.Signature])
                                                                     .Except(Globals.startingSkillIDList)
                                                                     .Except(Globals.startingOverdriveIDList)
                                                                     .Where(skill => Player.skillsDict[skill].hasSignatureVariant));

            List<string> startingItems = [PlayerStartItem.staticID, BuffWithFriendship.staticID, WaterChargeFamiliarItem.staticID];
            possibleItems.AddRange(LootManager.completeItemDict.Keys.Except(ArchipelagoManager.FoundLocations[NotificationManager.NoticeType.Relic])
                                                                    .Except(startingItems));

            List<string> startingOutfits = ["Hope", "Patience"];
            possibleOutfits.AddRange(Outfit.outfitList.Select(outfit => outfit.outfitID)
                                                      .Except(ArchipelagoManager.FoundLocations[NotificationManager.NoticeType.Outfit])
                                                      .Except(startingOutfits));
        }
        orig(self);
    }

    private string LootManager_GetLockedItemID(On.LootManager.orig_GetLockedItemID orig)
    {
        string text = string.Empty;
        if (possibleItems.Count > 0)
        {
            text = possibleItems[UnityEngine.Random.Range(0, possibleItems.Count)];
            possibleItems.Remove(text);
        }
        return text;
    }

    private string LootManager_GetSkillID(On.LootManager.orig_GetSkillID orig, bool locked, bool signature)
    {
        string text = string.Empty;
        if (signature)
        {
            Log.LogMessage(possibleSignatures.Count);
            if (possibleSignatures.Count > 0)
                text = possibleSignatures[UnityEngine.Random.Range(0, possibleSignatures.Count)];
        }
        else if (locked)
        {
            if (possibleSkills.Count > 0)
                text = possibleSkills[UnityEngine.Random.Range(0, possibleSkills.Count)];
        }
        else if (LootManager.availableSkillList.Count > 0)
        {
            text = LootManager.availableSkillList[UnityEngine.Random.Range(0, LootManager.availableSkillList.Count)];
        }
        if (text != string.Empty)
        {
            LootManager.lockedSigList.Remove(text);
            LootManager.lockedSkillList.Remove(text);
            LootManager.availableSkillList.Remove(text);
            possibleSkills.Remove(text);
            possibleSignatures.Remove(text);
        }
        return text;
    }

    public static string CleanArchipelagoId(string archipelagoId)
    {
        var itemName = archipelagoId;
        if (itemName.EndsWith("Signature"))
            itemName = itemName.Replace("Signature", "");
        return itemName;
    }

    public string GetDisplayNameFromArchipelagoId(string archipelagoId)
    {
        string itemName = CleanArchipelagoId(archipelagoId);

        return ArchipelagoManager.GetGroupFromArchipelagoId(archipelagoId) switch
        {
            NotificationManager.NoticeType.Relic => TextManager.GetItemName(itemName),
            NotificationManager.NoticeType.Spell => TextManager.GetSkillName(itemName),
            NotificationManager.NoticeType.Signature => TextManager.GetSkillName(itemName),
            NotificationManager.NoticeType.Outfit => TextManager.GetOutfitName(itemName),
            NotificationManager.NoticeType.Progression => itemName,
            _ => itemName,
        };
    }

    private void UnlockNotifier_Notify(On.UnlockNotifier.orig_Notify orig, UnlockNotifier self, string givenID, UnlockNotifier.NoticeType givenType)
    {
        // Disable normal unlock notifications
    }

    private bool ItemStoreItem_BuyWithPlat(On.ItemStoreItem.orig_BuyWithPlat orig, ItemStoreItem self)
    {
        var result = orig(self);
        if (result && self.usePlatinumCost && !self.cursedOnly)
            ArchipelagoManager.SendLocation(self.itemID);
        return result;
    }

    private bool Item_IsUnlocked(On.Item.orig_IsUnlocked orig, string givenID, bool setUnlocked)
    {
        // Disable natural relic unlocking
        if (!InGame)
            return orig(givenID, false);
        return orig(givenID, setUnlocked);
    }
    private bool OutfitStoreItem_Buy(On.OutfitStoreItem.orig_Buy orig, OutfitStoreItem self, Player player)
    {
        var result = orig(self, player);
        if (result && self.usePlatinumCost)
            ArchipelagoManager.SendLocation(self.outfitID);
        return result;
    }

    private void Outfit_UnlockOutfit(On.Outfit.orig_UnlockOutfit orig, string givenName, bool pushData)
    {
        // Disable unlocking outfits by default
    }

    private bool SkillStoreItem_Buy(On.SkillStoreItem.orig_Buy orig, SkillStoreItem self, Player player)
    {
        var result = orig(self, player);
        if (result && self.usePlatinumCost)
        {
            if (self.signatureOnly)
                ArchipelagoManager.SendLocation(self.skillID + "Signature");
            else
                ArchipelagoManager.SendLocation(self.skillID);
        }
        return result;
    }

    private void Player_HandleSkillUnlock(On.Player.orig_HandleSkillUnlock_string_bool orig, Player self, string givenID, bool isSignature)
    {
        // Disable the unlocking of skill naturally
    }

    private void LoadingScreen_StopLoading(On.LoadingScreen.orig_StopLoading orig, LoadingScreen self)
    {
        orig(self);
        if (!GameObject.Find("ChatInput"))
            CreateChatBoxtUI();
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

    private void Player_RunState_Update(On.Player.RunState.orig_Update orig, Player.RunState self)
    {
        if (ChatBoxController.Writing || ArchipelagoConnectButtonController.IsOpened)
            return;
        orig(self);
    }
    private void GameUI_TogglePause(On.GameUI.orig_TogglePause orig)
    {
        if (ChatBoxController.Writing || ArchipelagoConnectButtonController.IsOpened)
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
        Hook();
    }



    // Create a chat to receive and send AP commands
    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            Log.LogMessage($"scene: {SceneManager.GetActiveScene().name}");
            GameObject[] rootObjects = SceneManager.GetActiveScene().GetRootGameObjects();
            foreach (var rootObject in rootObjects)
            {
                StringBuilder sb = new($"{rootObject.transform.name}: ");
                Component[] components = rootObject.GetComponents(typeof(Component));
                foreach (var obj in components)
                {
                    sb.Append(obj.ToString());
                }
                Log.LogMessage(sb);
                LogobjectsRecursively(rootObject.transform, 1);
            }
            Log.LogMessage(string.Join(", ", ItemMerchant.PrefabDict.Select(pair => $"{pair.Key} => {pair.Value}").ToArray()));
        }
        else if (Input.GetKeyDown(KeyCode.F2))
        {
            StringBuilder sb = new($"Locked item id list: ");
            foreach (var item in LootManager.lockedItemIDList)
            {
                sb.Append($"{TextManager.GetItemName(item)}, ");
            }
            Log.LogMessage(sb);

            sb = new($"Locked skill id list: ");
            foreach (var item in LootManager.lockedSkillList)
            {
                sb.Append($"{TextManager.GetSkillName(item)}, ");
            }
            Log.LogMessage(sb);

            sb = new($"Available skill id list: ");
            foreach (var item in LootManager.availableSkillList)
            {
                sb.Append($"{TextManager.GetSkillName(item)}, ");
            }
            Log.LogMessage(sb);

            sb = new($"Complete skill id list: ");
            foreach (var item in LootManager.completeSkillList)
            {
                sb.Append($"{TextManager.GetSkillName(item)}, ");
            }
            Log.LogMessage(sb);

            sb = new($"Locked signature id list: ");
            foreach (var item in LootManager.lockedSigList)
            {
                sb.Append($"{TextManager.GetSkillName(item)}, ");
            }
            Log.LogMessage(sb);
        }
        else if (Input.GetKeyDown(KeyCode.F3))
        {
            Player.platWallet.balance = 99999;
        }

        if (Player)
        {
            DeathLinkManager.Update();
        }
        else if (InGame && Player == null)
        {
            Player = GameController.activePlayers[0];
            Relics = GameDataManager.gameData.itemDataDictionary;
            Outfits = Outfit.outfitDict;
            Skills = Player.skillsDict;
            Log.LogMessage($"firing the UI");
            if (!ArchipelagoManager.Connected)
                CreateConnectUI();
            if (!GameObject.Find("ChatInput"))
                CreateChatBoxtUI();
        }
        ArchipelagoManager.UpdateAllReceivers();
    }

    private static void LogobjectsRecursively(Transform root, int layer)
    {
        if (root.childCount == 0)
            return;
        for (int i = 0; i < root.childCount; i++)
        {
            var child = root.GetChild(i);
            StringBuilder sb = new($"{string.Concat(Enumerable.Repeat("--", layer).ToArray())} {child.name}: ");
            foreach (var obj in child.GetComponents<Component>())
            {
                sb.Append($"{obj}, ");
            }
            Log.LogMessage(sb);
            LogobjectsRecursively(child, layer + 1);
        }
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
            var itemName = item.itemId.Replace("Signature", "");
            if (Skills.ContainsKey(itemName))
            {
                if (Player.skillsDict.TryGetValue(itemName, out var skillState))
                    skillState.signatureUnlocked = true;
                GameDataManager.gameData.PushSkillData();
                return true;
            }
        }
        else if (Skills.ContainsKey(item.itemId))
        {
            if (Player.skillsDict.TryGetValue(item.itemId, out var skillState))
                skillState.isUnlocked = true;
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
            if (int.TryParse(item.itemId.Replace("Boss tier ", ""), out int tier))
                allowedTier = Mathf.Max(tier, allowedTier);
            return true;
        }
        return false;
    }

    public static void OnConnect(GameSettings settings)
    {
        MultiworldSettings = settings;
    }

}
