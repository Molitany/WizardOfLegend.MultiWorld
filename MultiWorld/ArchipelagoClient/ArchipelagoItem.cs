namespace MultiWorld.Archipelago;

internal class ArchipelagoItem
{
    private string _name;
    private string _player_name;
    private ItemType _type;

    public enum ItemType
    {
        Basic = 0,
        Progression = 1,
        Useful = 2,
        Trap = 4,
    }

    // Add more WoL item stuff

    public ArchipelagoItem(string name, string player_name, ItemType type)
    {
        _name = name;
        _player_name = player_name;
        _type = type;
    }
}