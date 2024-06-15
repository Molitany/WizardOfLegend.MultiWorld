using Archipelago.MultiClient.Net.Helpers;
using System.Collections.Generic;
using System.Linq;

namespace MultiWorld.ArchipelagoClient.Receivers;

public class ItemReceiver : IReceiver<ReceivedItemsHelper>
{
    public int SaveItemsReceived => itemsReceived;

    private readonly Queue<QueuedItem> itemQueue = new();
    private int itemsReceived;

    public void OnReceive(ReceivedItemsHelper itemHelper)
    {
        lock (ArchipelagoManager.receiverLock)
        {
            var item = itemHelper.DequeueItem();
            var player = item.Player.Name;
            MultiWorldPlugin.Log.LogInfo($"Player: {player}");
            if (player != null || player == string.Empty)
                player = "Server";
            var itemIndex = itemHelper.Index;
            var itemName = item.ItemName;

            if (MultiWorldPlugin.CheckItemExists(itemName))
            {
                itemQueue.Enqueue(new QueuedItem(itemName, itemIndex, player));
                MultiWorldPlugin.Log.LogInfo($"Item queued: {itemName}");
            }
            else
                MultiWorldPlugin.Log.LogError($"Error: {itemName} does not exist!");
        }
    }

    public void Update()
    {
        if (!itemQueue.Any())
            return;

        foreach (var item in itemQueue)
        {
            if (item.index > itemsReceived)
            {
                MultiWorldPlugin.AddToInventory(item);
                MultiWorldPlugin.ArchipelagoManager.DisplayNoticeFromArchipelagoId(item.itemId);
                itemsReceived++;
            }
        }

        ClearQueue();
    }
    public void LoadItemsReceived(int items) => itemsReceived = items;
    public void ResetItemsReceived() => itemsReceived = 0;
    public void ClearQueue() => itemQueue.Clear();
}