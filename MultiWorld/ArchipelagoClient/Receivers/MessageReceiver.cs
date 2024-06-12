using Archipelago.MultiClient.Net;

namespace MultiWorld.ArchipelagoClient.Receivers;

public class MessageReceiver : IReceiver<ArchipelagoPacketBase>
{
    public void ClearQueue()
    {
        throw new System.NotImplementedException();
    }

    public void OnReceive(ArchipelagoPacketBase type)
    {
        //TODO: this next, keeps throwing an error because of messages being received, find out what they are and send the to the ChatBox.
        throw new System.NotImplementedException();
    }

    public void Update()
    {
        throw new System.NotImplementedException();
    }
}