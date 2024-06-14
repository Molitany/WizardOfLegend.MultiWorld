using Archipelago.MultiClient.Net.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MultiWorld.Notification;
public class NotificationManager
{
    public void DisplayNotification(QueuedItem queuedItem)
    {
        GameUI.BroadcastNoticeMessage($"{queuedItem.player} found: {queuedItem.itemId} ");
    }
    public void DisplayText(string input)
    {
        MultiWorldPlugin.ChatBoxController.WriteToChatLog(input);
    }
}
