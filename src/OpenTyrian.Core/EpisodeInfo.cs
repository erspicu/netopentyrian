namespace OpenTyrian.Core;

public sealed class EpisodeInfo
{
    public required int EpisodeNumber { get; init; }

    public required string Label { get; init; }

    public required string Description { get; init; }

    public required bool IsAvailable { get; init; }

    public required EpisodeStartInfo StartInfo { get; init; }
}
