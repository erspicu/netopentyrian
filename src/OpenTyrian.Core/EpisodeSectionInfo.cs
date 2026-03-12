namespace OpenTyrian.Core;

public sealed class EpisodeSectionInfo
{
    public required string Label { get; init; }

    public required int StringIndex { get; init; }

    public required long FileOffset { get; init; }

    public required IReadOnlyList<EpisodeCommandInfo> Commands { get; init; }
}
