namespace OpenTyrian.Core;

public sealed class SaveSlotInfo
{
    public required int SlotIndex { get; init; }

    public required int PageIndex { get; init; }

    public required bool IsEmpty { get; init; }

    public required string Name { get; init; }

    public required string LevelName { get; init; }

    public required int LevelNumber { get; init; }

    public required int EpisodeNumber { get; init; }

    public required int CubeCount { get; init; }

    public required int Cash { get; init; }

    public required int Cash2 { get; init; }
}
