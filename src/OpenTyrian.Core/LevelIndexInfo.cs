namespace OpenTyrian.Core;

public sealed class LevelIndexInfo
{
    public required int LevelCount { get; init; }

    public required IList<int> LevelOffsets { get; init; }

    public required int EndOffset { get; init; }
}
