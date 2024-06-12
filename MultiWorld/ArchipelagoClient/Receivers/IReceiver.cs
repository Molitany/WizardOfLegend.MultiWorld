namespace MultiWorld.ArchipelagoClient.Receivers;
internal interface IReceiver<T>
{
    public void OnReceive(T type);
    public void Update();
    public void ClearQueue();
}
