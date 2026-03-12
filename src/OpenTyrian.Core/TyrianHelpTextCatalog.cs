namespace OpenTyrian.Core;

public sealed class TyrianHelpTextCatalog
{
    public TyrianHelpTextCatalog(
        IList<string> mainMenuHelp,
        IList<string> gameplayNames,
        IList<string> episodeNames,
        IList<string> fullGameMenu,
        IList<ShipDescriptionEntry> shipInfo)
    {
        MainMenuHelp = mainMenuHelp;
        GameplayNames = gameplayNames;
        EpisodeNames = episodeNames;
        FullGameMenu = fullGameMenu;
        ShipInfo = shipInfo;
    }

    public IList<string> MainMenuHelp { get; }

    public IList<string> GameplayNames { get; }

    public IList<string> EpisodeNames { get; }

    public IList<string> FullGameMenu { get; }

    public IList<ShipDescriptionEntry> ShipInfo { get; }
}
