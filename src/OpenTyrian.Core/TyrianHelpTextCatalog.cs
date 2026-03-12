namespace OpenTyrian.Core;

public sealed class TyrianHelpTextCatalog
{
    public TyrianHelpTextCatalog(
        IList<string> mainMenuHelp,
        IList<string> gameplayNames,
        IList<string> episodeNames)
    {
        MainMenuHelp = mainMenuHelp;
        GameplayNames = gameplayNames;
        EpisodeNames = episodeNames;
    }

    public IList<string> MainMenuHelp { get; }

    public IList<string> GameplayNames { get; }

    public IList<string> EpisodeNames { get; }
}
