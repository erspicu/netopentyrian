namespace OpenTyrian.Core;

public sealed class EpisodeCommandInfo
{
    public required EpisodeCommandKind Kind { get; init; }

    public required string RawText { get; init; }

    public required int StringIndex { get; init; }

    public required long FileOffset { get; init; }

    public int? TargetMainLevel { get; init; }

    public IReadOnlyList<string>? BlockLines { get; init; }

    public ItemAvailabilityInfo? ItemAvailability { get; init; }
}
