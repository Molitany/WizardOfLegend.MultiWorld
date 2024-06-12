using MultiWorld.Notification;
using System;
using UnityEngine.SocialPlatforms.Impl;

namespace MultiWorld.DeathLink;

public enum DeathLinkStatus
{
    Nothing,
    Queued,
    Killing,
}

public class DeathLinkManager
{
    public DeathLinkStatus CurrentStatus { get; set; }
    public bool DeathLinkEnabled {
        get => MultiWorldPlugin.MultiworldSettings.DeathLinkEnabled;
        set => MultiWorldPlugin.MultiworldSettings.DeathLinkEnabled = value;
    }

    public void Update()
    {
        if (DeathLinkEnabled && CurrentStatus == DeathLinkStatus.Queued && MultiWorldPlugin.InGame)
        {
            CurrentStatus = DeathLinkStatus.Killing;
            var deathLinkAtkInfo = new AttackInfo();
            deathLinkAtkInfo.skillID = "DeathLink";
            deathLinkAtkInfo.damage = 9999;

            foreach (var entityGameObject in GameController.allies)
            {
                if (!(entityGameObject == null))
                {
                    Entity entity = entityGameObject.GetComponent<Entity>();
                    if (entity != null && entity.health == null)
                    {
                        entity.health.TakeDamage(deathLinkAtkInfo);
                    }
                }
            }
        }
    }

    public void SendDeath()
    {
        MultiWorldPlugin.Log.LogMessage(MultiWorldPlugin.MultiworldSettings);
        if (!MultiWorldPlugin.MultiworldSettings.DeathLinkEnabled)
            return;

        MultiWorldPlugin.Log.LogMessage("Sending death link!");
        MultiWorldPlugin.ArchipelagoManager.SendDeath();
    }

    public void ReceiveDeath(string source)
    {
        if (!MultiWorldPlugin.MultiworldSettings.DeathLinkEnabled)
            return;

        MultiWorldPlugin.Log.LogInfo("Received death link!");
        MultiWorldPlugin.NotificationManager.DisplayNotification(new QueuedItem("Death", 0, source));
        CurrentStatus = DeathLinkStatus.Queued;
    }

    public bool ToggleDeathLink()
    {
        bool newDeathLinkEnabled = !DeathLinkEnabled;
        DeathLinkEnabled = newDeathLinkEnabled;
        MultiWorldPlugin.ArchipelagoManager.EnableDeathLink(newDeathLinkEnabled);
        MultiWorldPlugin.Log.LogMessage($"Setting deathlink status to {newDeathLinkEnabled}");
        return newDeathLinkEnabled;
    }
}