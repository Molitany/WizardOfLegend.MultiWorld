namespace MultiWorld;
public class GameSettings
{
    public Config Config { get; set; }
    public string PlayerName { get; set; }
    public int RequiredEnding { get; set; }
    public bool DeathLinkEnabled { get; set; }

}

public class Config
{
    // All of the world configurations here
    public string Name { get; set; }
}