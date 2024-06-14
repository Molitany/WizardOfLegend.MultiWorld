using Archipelago.MultiClient.Net.Helpers;
using Archipelago.MultiClient.Net.Models;
using System.Collections.Generic;
using System.Linq;

namespace MultiWorld.ArchipelagoClient.Receivers;

public class HintReceiver : IReceiver<Hint[]>
{
    private readonly List<Hint> hintQueue = [];


    public void OnReceive(Hint[] type)
    {
        lock (ArchipelagoManager.receiverLock)
        {
            foreach (var hint in type)
            {
                if (hint.Found || hint.FindingPlayer != MultiWorldPlugin.ArchipelagoManager.PlayerSlot)
                    continue;
                if (MultiWorldPlugin.ArchipelagoManager.LocationIdExists(hint.LocationId, out string locationId))
                {
                    hintQueue.Add(hint);
                    MultiWorldPlugin.Log.LogMessage($"Hint queued for: {locationId} ");
                }
                else
                {
                    MultiWorldPlugin.Log.LogError($"Invalid hint recieved: {locationId}");
                }
            }
        }
    }

    public void Update()
    {
        if (!hintQueue.Any())
            return;

        MultiWorldPlugin.Log.LogWarning("Processing hint queue");
        foreach (var hint in hintQueue)
            MultiWorldPlugin.NotificationManager.DisplayText($"{MultiWorldPlugin.ArchipelagoManager.GetItemNameFromId(hint.ItemId)} located at {MultiWorldPlugin.ArchipelagoManager.GetLocationNameFromId(hint.LocationId)}");

        ClearQueue();
    }
    public void ClearQueue() => hintQueue.Clear();

}