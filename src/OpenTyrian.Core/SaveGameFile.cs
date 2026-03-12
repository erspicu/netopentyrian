namespace OpenTyrian.Core;

public sealed class SaveGameFile
{
    public required string SourcePath { get; set; }

    public required bool HasSaveFile { get; set; }

    public required bool IsValid { get; set; }

    public required IList<SaveSlotRecord> Slots { get; set; }

    public required byte[] ExtraData { get; set; }

    public SaveSlotCatalog ToCatalog()
    {
        List<SaveSlotInfo> slots = new List<SaveSlotInfo>(Slots.Count);
        for (int i = 0; i < Slots.Count; i++)
        {
            slots.Add(Slots[i].ToInfo());
        }

        return new SaveSlotCatalog
        {
            SourcePath = SourcePath,
            HasSaveFile = HasSaveFile,
            IsValid = IsValid,
            Slots = slots,
        };
    }
}
