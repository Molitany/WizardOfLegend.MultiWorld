namespace MultiWorld;
public class GameSettings
{
    public string PlayerName { get; set; }
    public bool DeathLinkEnabled { get; set; }
    public bool EarlyChaos { get; set; }
    public Goals Goal { get; set; }

    public enum Goals
    {
        DefeatSura = 0,
        DefeatSuperSura = 1,
    }
}
