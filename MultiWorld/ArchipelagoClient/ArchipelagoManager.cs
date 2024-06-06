using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using MultiWorld.Archipelago.Receivers;
using MultiWorld.UI;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace MultiWorld.Archipelago;

public class ArchipelagoManager
{
    public static string SlotName;
    public static string Password;
    public static string Url = "archipelago.gg";
    public static string Port = "38281";

    public bool Connected { get; private set; }
    public string ServerAddress => Connected ? _session.Socket.Uri.ToString() : string.Empty;
    public int PlayerSlot => Connected ? _session.ConnectionInfo.Slot : -1;
    public ItemReceiver ItemReceiver => _itemReceiver;
    public MessageReceiver MessageReceiver => _messageReceiver;

    private ArchipelagoSession _session;
    private string _lastServerUrl;
    private string _lastPlayerName;
    private string _lastPassword;

    private readonly Dictionary<string, long> archipelagoLocations = [];
    private readonly List<ArchipelagoItem> archipelagoItems = [];

    private readonly HintReceiver _hintReceiver = new();
    private readonly LocationReceiver _locationReceiver = new();
    private readonly MessageReceiver _messageReceiver = new();
    private readonly ItemReceiver _itemReceiver = new();

    public static readonly object receiverLock = new();

    #region Connection
    public string Connect(string url, string playerName, string password)
    {
        GameUI.BroadcastNoticeMessage($"Connecting to Archipelago at {url}");
        _lastServerUrl = url;
        _lastPlayerName = playerName;
        _lastPassword = password;
        LoginResult result;
        try
        {
            // Log.LogInfo type load error in here somewhere
            _session = ArchipelagoSessionFactory.CreateSession(url);
            _session.Items.ItemReceived += _itemReceiver.OnReceive;
            _session.Socket.PacketReceived += _messageReceiver.OnReceive;
            _session.Locations.CheckedLocationsUpdated += _locationReceiver.OnReceive;
            _session.Socket.SocketClosed += OnDisconnect;
            result = _session.TryConnectAndLogin("Wizard of Legend", playerName, ItemsHandlingFlags.AllItems, new Version(0, 4, 6), null, null, password);
        }
        catch (Exception ex)
        {
            OnDisconnect(ex.Message);
            return $"Failed to connect: {ex.Message}";
        }

        ArchipelagoConnectButtonController.connectPanel.SetActive(false);
        ArchipelagoConnectButtonController.IsOpened = false;
        OnConnect(result as LoginSuccessful, playerName);
        return "Archipelago multiworld connected";
    }

    private void OnConnect(LoginSuccessful loginSuccessful, string playerName)
    {
        GameSettings settings = new()
        {
            Config = ((JObject)loginSuccessful.SlotData["config"]).ToObject<Config>(),
            RequiredEnding = int.Parse(loginSuccessful.SlotData["ending"].ToString()),
            DeathLinkEnabled = bool.Parse(loginSuccessful.SlotData["death_link"].ToString()),
            PlayerName = playerName
        };

        //Deathlink

        //Locations
        ArchipelagoLocation[] locations = ((JArray)loginSuccessful.SlotData["locations"]).ToObject<ArchipelagoLocation[]>();
        Dictionary<string, string> mappedItems = [];
        archipelagoLocations.Clear();
        archipelagoItems.Clear();

        foreach (var location in locations)
        {
            archipelagoLocations.Add(location.id, location.archipelago_id);

            //if (location.player_name == settings.PlayerName)
            //    if (ItemNameExists(location.name, out string itemId))
            //        mappedItems.Add(location.id, itemId);
            //    else
            //    {
            //        MultiWorldPlugin.Log.LogError($"Item {location.name} does not exist");
            //        continue;
            //    }
            //else
            //{
            //    mappedItems.Add(location.id, $"AP{archipelagoItems.Count}");
            //    archipelagoItems.Add(new ArchipelagoItem(location.name, location.player_name, (ArchipelagoItem.ItemType)location.type));
            //}
        }

        _session.DataStorage.TrackHints(_hintReceiver.OnReceive, true);
        // ingame on connect
    }

    public void Disconnect()
    {
        if (Connected)
        {
            _session.Socket.Disconnect();
            Connected = false;
            _session = null;
        }
    }

    private void OnDisconnect(string cause)
    {
        //handle ingame effect aswell
        Connected = false;
        _session = null;
    }

    public void UpdateAllReceivers()
    {
        lock (receiverLock)
        {
            if (MultiWorldPlugin.InGame)
            {
                _hintReceiver.Update();
                _itemReceiver.Update();
                _locationReceiver.Update();
            }
            _messageReceiver.Update(); // Doesn't need to be in game
        }
    }

    public void ClearAllReceivers()
    {
        lock (receiverLock)
        {
            _hintReceiver.ClearQueue();
            _itemReceiver.ClearQueue();
            _locationReceiver.ClearQueue();
            _messageReceiver.ClearQueue();
        }
    }
    #endregion

    //public bool ItemNameExists(string itemName, out string itemId)
    //{
    //    foreach (var item in list of WoL items)
    //    {
    //        if (item.Name == itemName)
    //        {
    //            itemId = item.id;
    //            return true;
    //        }
    //    }
    //    itemId = null;
    //    return false;
    //}

    public bool LocationIdExists(long archpelagoId, out string locationId)
    {
        //consider switching key and value potentially
        foreach (var locationPair in archipelagoLocations)
        {
            if (locationPair.Value == archpelagoId)
            {
                locationId = locationPair.Key;
                return true;
            }
        }

        locationId = null;
        return false;
    }
}