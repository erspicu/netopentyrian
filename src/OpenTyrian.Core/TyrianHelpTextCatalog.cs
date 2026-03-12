namespace OpenTyrian.Core;

public sealed class TyrianHelpTextCatalog
{
    public TyrianHelpTextCatalog(
        IList<string> helpText,
        IList<string> miscText,
        IList<string> topicNames,
        IList<string> mainMenuHelp,
        IList<string> gameplayNames,
        IList<string> episodeNames,
        IList<string> fullGameMenu,
        IList<ShipDescriptionEntry> shipInfo,
        IList<string> optionsMenu)
    {
        HelpText = helpText;
        MiscText = miscText;
        TopicNames = topicNames;
        MainMenuHelp = mainMenuHelp;
        GameplayNames = gameplayNames;
        EpisodeNames = episodeNames;
        FullGameMenu = fullGameMenu;
        ShipInfo = shipInfo;
        OptionsMenu = optionsMenu;
    }

    public IList<string> HelpText { get; }

    public IList<string> MiscText { get; }

    public IList<string> TopicNames { get; }

    public IList<string> MainMenuHelp { get; }

    public IList<string> GameplayNames { get; }

    public IList<string> EpisodeNames { get; }

    public IList<string> FullGameMenu { get; }

    public IList<ShipDescriptionEntry> ShipInfo { get; }

    public IList<string> OptionsMenu { get; }
}
