namespace OpenTyrian.Core;

public sealed class MainLevelEntry
{
    public required int MainLevelNumber { get; init; }

    public required EpisodeSectionInfo Section { get; init; }

    public IList<EpisodeCommandInfo> Commands => Section.Commands;
}
