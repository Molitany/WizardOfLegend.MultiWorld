using Archipelago.MultiClient.Net.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine.SocialPlatforms.Impl;

namespace MultiWorld.ArchipelagoClient.Receivers;

public class LocationReceiver : IReceiver<ReadOnlyCollection<long>>
{
    private readonly List<string> locationQueue = [];
    public void ClearQueue() => locationQueue.Clear();

    public void OnReceive(ReadOnlyCollection<long> locations)
    {
        lock (ArchipelagoManager.receiverLock)
        {
            foreach (long apId in locations)
            {
                if (MultiWorldPlugin.ArchipelagoManager.LocationIdExists(apId, out string locationId))
                {
                    locationQueue.Add(locationId);
                    MultiWorldPlugin.Log.LogMessage("Queueing check for location: " + locationId);
                }
                else
                {
                    MultiWorldPlugin.Log.LogError("Received invalid checked location: " + apId);
                }
            }
        }
    }

    public void Update()
    {
        if (locationQueue.Count == 0)
            return;

        MultiWorldPlugin.Log.LogWarning("Processing location queue");
        var APManager = MultiWorldPlugin.ArchipelagoManager;
        foreach (string locationId in locationQueue)
        {
            var itemName = MultiWorldPlugin.CleanArchipelagoId(locationId);
            APManager.FoundLocations[APManager.GetGroupFromArchipelagoId(locationId)].Add(itemName);
        }

        ClearQueue();
    }
}