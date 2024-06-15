using Archipelago.MultiClient.Net.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MultiWorld.Notification;
public class NotificationManager
{
    public enum NoticeType
    {
        Relic,
        Spell,
        Signature,
        Outfit,
        Progression,
    }

    public void DisplayNotification(QueuedItem queuedItem)
    {
        GameUI.BroadcastNoticeMessage($"{queuedItem.player} found: {queuedItem.itemId} ");
    }
    public void DisplayText(string input)
    {
        MultiWorldPlugin.ChatBoxController.WriteToChat(input);
    }

    public void DisplayUnlockNotification(string givenId, NoticeType givenType)
    {
        var unlockNotifier = UnlockNotifier.instance;

        if (unlockNotifier.noticeQueue.Count == 0 && !unlockNotifier.playAnimation)
            SoundManager.PlayAudio("UnlockNotifierIntro", 1f, false, -1f, -1f);
        if (givenType == NoticeType.Spell || givenType == NoticeType.Signature)
        {
            var unlockType = givenType == NoticeType.Spell ? UnlockNotifier.NoticeType.Spell : UnlockNotifier.NoticeType.Signature;
            unlockNotifier.noticeQueue.Enqueue(new UnlockNotifier.NoticeVars(givenId, TextManager.GetSkillName(givenId) + TextManager.GetUIText("unlockNotifier-unlockHeader"), TextManager.GetUIText("unlockNotifier-arcanaInfo"), IconManager.GetSkillIcon(givenId), unlockType));
        }
        else if (givenType == NoticeType.Relic)
            unlockNotifier.noticeQueue.Enqueue(new UnlockNotifier.NoticeVars(givenId, TextManager.GetItemName(givenId) + TextManager.GetUIText("unlockNotifier-unlockHeader"), TextManager.GetUIText("unlockNotifier-relicInfo"), IconManager.GetItemIcon(givenId), UnlockNotifier.NoticeType.Relic));
        else if (givenType == NoticeType.Outfit)
            unlockNotifier.noticeQueue.Enqueue(new UnlockNotifier.NoticeVars(givenId, TextManager.GetOutfitName(givenId) + TextManager.GetUIText("unlockNotifier-unlockHeader"), TextManager.GetUIText("unlockNotifier-outfitInfo"), IconManager.GetItemIcon(givenId), UnlockNotifier.NoticeType.Outfit));
        else if (givenType == NoticeType.Progression)
            unlockNotifier.noticeQueue.Enqueue(new UnlockNotifier.NoticeVars(givenId, givenId + TextManager.GetUIText("unlockNotifier-unlockHeader"), $"Progression to {givenId} unlocked!", IconManager.GetItemIcon(givenId), UnlockNotifier.NoticeType.Relic));
        unlockNotifier.playAnimation = true;
    }
}
