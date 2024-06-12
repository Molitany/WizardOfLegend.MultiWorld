using Archipelago.MultiClient.Net.Models;
using System.Collections.Generic;
using System.Linq;

namespace MultiWorld.ArchipelagoClient.Receivers;

public class HintReceiver : IReceiver<Hint[]>
{
    private readonly List<string> hintQueue = new();

    public void ClearQueue()
    {
        hintQueue.Clear();
    }

    public void OnReceive(Hint[] type)
    {
        //lock (ArchipelagoManager.receiverLock)
        //{
        //    foreach (var hint in type)
        //    {
        //        if (hint.Found || hint.FindingPlayer != MultiWorldPlugin.ArchipelagoManager.PlayerSlot)
        //            continue;
        //        if (MultiWorldPlugin.ArchipelagoManager.LocationIdExists(hint.LocationId, out string locationId))
        //        {
        //            hintQueue.Add(locationId);
        //            MultiWorldPlugin.Log.LogMessage($"Hint for location={locationId} queued");
        //        }
        //        else
        //        {
        //            MultiWorldPlugin.Log.LogError($"Invalid hint location={locationId} recieved");
        //        }
        //    }
        //}
    }

    public void Update()
    {
        if (!hintQueue.Any())
            return;

        MultiWorldPlugin.Log.LogWarning("Processing hint queue");

        foreach (string locationId in hintQueue)
        {
            // Show Hint to user
        }
    }

}