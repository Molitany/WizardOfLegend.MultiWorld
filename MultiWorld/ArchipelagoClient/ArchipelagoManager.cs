using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.BounceFeatures.DeathLink;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Models;
using Archipelago.MultiClient.Net.Packets;
using MultiWorld.ArchipelagoClient.Receivers;
using MultiWorld.Notification;
using MultiWorld.UI;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace MultiWorld.ArchipelagoClient;

public class ArchipelagoManager
{
    public static string SlotName = "Molitany";
    public static string Password;
    public static string Url = "localhost";
    public static string Port = "55752";

    public bool Connected { get; private set; }
    public string ServerAddress => Connected ? _session.Socket.Uri.ToString() : string.Empty;
    public int PlayerSlot => Connected ? _session.ConnectionInfo.Slot : -1;
    public ItemReceiver ItemReceiver => _itemReceiver;
    public MessageReceiver MessageReceiver => _messageReceiver;
    public ReadOnlyCollection<ItemInfo> StartingArchipelagoItems { get; private set; }

    private ArchipelagoSession _session;
    private const string _game = "Wizard of Legend";
    private string _lastServerUrl;
    private string _lastPlayerName;
    private string _lastPassword;

    private DeathLinkService _deathLink;
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
        string resultMessage;
        try
        {
            // Log.LogInfo type load error in here somewhere
            _session = ArchipelagoSessionFactory.CreateSession(url);
            _session.Items.ItemReceived += _itemReceiver.OnReceive;
            _session.Socket.PacketReceived += _messageReceiver.OnReceive;
            _session.Locations.CheckedLocationsUpdated += _locationReceiver.OnReceive;
            _session.Socket.SocketClosed += OnDisconnect;
            result = _session.TryConnectAndLogin("Wizard of Legend", playerName, ItemsHandlingFlags.AllItems, new Version(0, 4, 6), password: password);
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
        _deathLink = _session.CreateDeathLinkService();
        _deathLink.OnDeathLinkReceived += ReceivedDeath;
        EnableDeathLink(settings.DeathLinkEnabled);

        //Starting Locations
        StartingArchipelagoItems = _session.Items.AllItemsReceived;


        _session.DataStorage.TrackHints(_hintReceiver.OnReceive, true);
        MultiWorldPlugin.OnConnect(settings);
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
            if (MultiWorldPlugin.Player)
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

    #region Death link

    public void SendDeath()
    {
        if (Connected)
        {
            _deathLink.SendDeathLink(new Archipelago.MultiClient.Net.BounceFeatures.DeathLink.DeathLink(MultiWorldPlugin.MultiworldSettings.PlayerName));
        }
    }

    public void EnableDeathLink(bool deathLinkEnabled)
    {
        if (Connected)
        {
            if (deathLinkEnabled)
                _deathLink.EnableDeathLink();
            else
                _deathLink.DisableDeathLink();
        }
    }

    private void ReceivedDeath(Archipelago.MultiClient.Net.BounceFeatures.DeathLink.DeathLink deathLink)
    {
        MultiWorldPlugin.DeathLinkManager.ReceiveDeath(deathLink.Source);
    }

    #endregion Death link

    public bool LocationIdExists(long archpelagoId, out string locationId)
    {
        locationId = GetLocationNameFromId(archpelagoId);
        return MultiWorldPlugin.CheckItemExists(locationId);
    }

    public void SendMessage(string message)
    {
        if (Connected)
        {
            var packet = new SayPacket
            {
                Text = message
            };
            _session.Socket.SendPacket(packet);
        }
    }

    public string GetItemNameFromId(long itemId) => _session.Items.GetItemName(itemId);

    public string GetLocationNameFromId(long itemId) => _session.Locations.GetLocationNameFromId(itemId);

    public void SendLocation(string locationId)
    {
        if (!Connected)
            return;

        if (MultiWorldPlugin.CheckItemExists(locationId))
        {
            var apId = _session.Locations.GetLocationIdFromName(_game, locationId);
            _session.Locations.CompleteLocationChecks(apId);
        }
        else
            MultiWorldPlugin.Log.LogError($"Location {locationId} does not exist on Archipelago!");
    }

    public void DisplayNoticeFromArchipelagoId(string archipelagoId)
    {
        if (!StartingArchipelagoItems.Any(x => x.ItemName == archipelagoId))
        {
            _session.DataStorage.GetItemNameGroupsAsync(dictionary =>
            {
                var item_group = dictionary.First(group => group.Value.Contains(archipelagoId)).Key;
                var itemName = archipelagoId;
                if (itemName.EndsWith("Signature"))
                    itemName = itemName.Replace("Signature", "");

                var noticeType = item_group switch
                {
                    "relics" => NotificationManager.NoticeType.Relic,
                    "skills_basic" => NotificationManager.NoticeType.Spell,
                    "skills_dash" => NotificationManager.NoticeType.Spell,
                    "skills_optionals" => NotificationManager.NoticeType.Spell,
                    "skills_signatures" => NotificationManager.NoticeType.Signature,
                    "outfits" => NotificationManager.NoticeType.Outfit,
                    "progression" => NotificationManager.NoticeType.Progression,
                    _ => throw new NotImplementedException(),
                };

                MultiWorldPlugin.Log.LogMessage($"{noticeType}: {archipelagoId}");
                MultiWorldPlugin.NotificationManager.DisplayUnlockNotification(itemName, noticeType);
            });
        }
    }
}