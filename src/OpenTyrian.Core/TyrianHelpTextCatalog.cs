namespace OpenTyrian.Core;

public sealed class TyrianHelpTextCatalog
{
    public TyrianHelpTextCatalog(
        IReadOnlyList<string> mainMenuHelp,
        IReadOnlyList<string> gameplayNames,
        IReadOnlyList<string> episodeNames)
    {
        MainMenuHelp = mainMenuHelp;
        GameplayNames = gameplayNames;
        EpisodeNames = episodeNames;
    }

    public IReadOnlyList<string> MainMenuHelp { get; }

    public IReadOnlyList<string> GameplayNames { get; }

    public IReadOnlyList<string> EpisodeNames { get; }
}
