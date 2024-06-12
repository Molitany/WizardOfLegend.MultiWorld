using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.BounceFeatures.DeathLink;
using Archipelago.MultiClient.Net.Enums;
using MultiWorld.ArchipelagoClient.Receivers;
using MultiWorld.UI;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace MultiWorld.ArchipelagoClient;

public class ArchipelagoManager
{
    public static string SlotName = "Molitany";
    public static string Password;
    public static string Url = "localhost";
    public static string Port = "61462";

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

    private DeathLinkService deathLink;
    private HintReceiver _hintReceiver = new();
    private LocationReceiver _locationReceiver = new();
    private MessageReceiver _messageReceiver = new();
    private ItemReceiver _itemReceiver = new();
    public static readonly object receiverLock = new();

    #region Connection
    public string Connect(string url, string playerName, string password)
    {
        GameUI.BroadcastNoticeMessage($"Connecting to Archipelago at {url}");
        _lastServerUrl = url;
        _lastPlayerName = playerName;
        _lastPassword = password;
        LoginResult result;
        string resultMessage;
        try
        {
            // Log.LogInfo type load error in here somewhere
            _session = ArchipelagoSessionFactory.CreateSession(url);
            _session.Items.ItemReceived += _itemReceiver.OnReceive;
            _session.Socket.PacketReceived += _messageReceiver.OnReceive;
            _session.Locations.CheckedLocationsUpdated += _locationReceiver.OnReceive;
            _session.Socket.SocketClosed += OnDisconnect;
            result = _session.TryConnectAndLogin("Wizard of Legend", playerName, ItemsHandlingFlags.AllItems, new Version(0, 4, 6), null, null, password);
            MultiWorldPlugin.Log.LogMessage($"{result}");
        }
        catch (Exception ex)
        {
            result = new LoginFailure(ex.GetBaseException().Message);
        }

        if (!result.Successful)
        {
            Connected = false;
            var failure = result as LoginFailure;
            resultMessage = "Archipelago connection failed: ";
            if (failure.Errors.Length > 0)
                resultMessage += failure.Errors[0];
            else
                resultMessage += "Unknown reason";

            return resultMessage;
        }

        Connected = true;
        resultMessage = "Archipelago connection successful";
        ArchipelagoConnectButtonController.ConnectPanel.SetActive(false);
        ArchipelagoConnectButtonController.IsOpened = false;
        OnConnect(result as LoginSuccessful, playerName);
        return resultMessage;
    }

    private void OnConnect(LoginSuccessful loginSuccessful, string playerName)
    {
        GameSettings settings = new()
        {
            //Config = ((JObject)loginSuccessful.SlotData["config"]).ToObject<Config>(),
            //RequiredEnding = int.Parse(loginSuccessful.SlotData["ending"].ToString()),
            DeathLinkEnabled = loginSuccessful.SlotData["deathLink"].ToString() == "1",
            PlayerName = playerName
        };
        //Deathlink
        deathLink = _session.CreateDeathLinkService();
        deathLink.OnDeathLinkReceived += ReceivedDeath;
        EnableDeathLink(settings.DeathLinkEnabled);
        //Locations
        archipelagoLocations.Clear();
        archipelagoItems.Clear();

        _session.DataStorage.TrackHints(_hintReceiver.OnReceive, true);
        MultiWorldPlugin.OnConnect(settings);
    }

    #region Death link

    public void SendDeath()
    {
        if (Connected)
        {
            deathLink.SendDeathLink(new Archipelago.MultiClient.Net.BounceFeatures.DeathLink.DeathLink(MultiWorldPlugin.MultiworldSettings.PlayerName));
        }
    }

    public void EnableDeathLink(bool deathLinkEnabled)
    {
        if (Connected)
        {
            if (deathLinkEnabled)
                deathLink.EnableDeathLink();
            else
                deathLink.DisableDeathLink();
        }
    }

    private void ReceivedDeath(Archipelago.MultiClient.Net.BounceFeatures.DeathLink.DeathLink deathLink)
    {
        MultiWorldPlugin.DeathLinkManager.ReceiveDeath(deathLink.Source);
    }

    #endregion Death link

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

    public void SendMessage(string v)
    {
        throw new NotImplementedException();
    }

}