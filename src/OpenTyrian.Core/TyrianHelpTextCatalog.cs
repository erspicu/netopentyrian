namespace OpenTyrian.Core;

public sealed class TyrianHelpTextCatalog
{
    public TyrianHelpTextCatalog(
        IList<string> mainMenuHelp,
        IList<string> gameplayNames,
        IList<string> episodeNames,
        IList<string> fullGameMenu)
    {
        MainMenuHelp = mainMenuHelp;
        GameplayNames = gameplayNames;
        EpisodeNames = episodeNames;
        FullGameMenu = fullGameMenu;
    }

    public IList<string> MainMenuHelp { get; }

    public IList<string> GameplayNames { get; }

    public IList<string> EpisodeNames { get; }

    public IList<string> FullGameMenu { get; }
}
