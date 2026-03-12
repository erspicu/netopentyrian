namespace OpenTyrian.Core;

public sealed class SaveSlotCatalog
{
    public required string SourcePath { get; init; }

    public required bool HasSaveFile { get; init; }

    public required bool IsValid { get; init; }

    public required IList<SaveSlotInfo> Slots { get; init; }
}
