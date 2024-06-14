using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MultiWorld.ArchipelagoClient.Receivers;

public class MessageReceiver : IReceiver<ArchipelagoPacketBase>
{
    private readonly Queue<string> messageQueue = new();


    public void OnReceive(ArchipelagoPacketBase packet)
    {
        if (packet.PacketType != ArchipelagoPacketType.PrintJSON)
            return;

        var jsonPacket = packet as PrintJsonPacket;
        var output = new StringBuilder();

        foreach (var messagePart in jsonPacket.Data)
        {
            switch (messagePart.Type)
            {
                case JsonMessagePartType.Text:
                    output.Append("Text: ");
                    break;
                case JsonMessagePartType.PlayerId:
                    output.Append("PlayerId: ");
                    break;
                case JsonMessagePartType.PlayerName:
                    output.Append("PlayerName: ");
                    break;
                case JsonMessagePartType.ItemId:
                    output.Append("ItemId: ");
                    break;
                case JsonMessagePartType.ItemName:
                    output.Append("ItemName: ");
                    break;
                case JsonMessagePartType.LocationId:
                    output.Append("LocationId: ");
                    break;
                case JsonMessagePartType.LocationName:
                    output.Append("LocationName: ");
                    break;
                case JsonMessagePartType.EntranceName:
                    output.Append("EntranceName: ");
                    break;
                case JsonMessagePartType.Color:
                    output.Append("Color: ");
                    break;
                case null:
                    output.Append("Server: ");
                    break;
                default:
                    break;
            }
            output.Append($"{messagePart.Text}\n");
        }

        lock (ArchipelagoManager.receiverLock)
        {
            string message = output.ToString();
            messageQueue.Enqueue(message);
        }

    }
    public void ClearQueue() => messageQueue.Clear();

    public void Update()
    {
        if (messageQueue.Any())
            MultiWorldPlugin.Log.LogMessage(messageQueue.Dequeue());
    }


    private readonly Dictionary<ColorType, string> colorCodes = new()
        {
            { ColorType.ItemProgression, "AF99EF" },
            { ColorType.ItemUseful, "6D8BE8" },
            { ColorType.ItemTrap, "FA8072" },
            { ColorType.ItemBasic, "00EEEE" },
            { ColorType.Location, "00FF7F" },
            { ColorType.PlayerSelf, "EE00EE" },
            { ColorType.PlayerOther, "FAFAD2" },
            { ColorType.Red, "EE0000" },
            { ColorType.Error, "7F7F7F" },
        };

    private enum ColorType
    {
        ItemProgression,
        ItemUseful,
        ItemTrap,
        ItemBasic,
        Location,
        PlayerSelf,
        PlayerOther,
        Red,
        Error,
        NoColor,
    }
}