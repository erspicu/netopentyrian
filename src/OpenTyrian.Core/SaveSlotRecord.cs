namespace OpenTyrian.Core;

public sealed class SaveSlotRecord
{
    public required int SlotIndex { get; set; }

    public required int PageIndex { get; set; }

    public ushort Encode { get; set; }

    public ushort LevelNumber { get; set; }

    public required byte[] Items { get; set; }

    public int Cash { get; set; }

    public int Cash2 { get; set; }

    public required string LevelName { get; set; }

    public required string Name { get; set; }

    public byte CubeCount { get; set; }

    public required byte[] WeaponPowers { get; set; }

    public byte EpisodeNumber { get; set; }

    public required byte[] LastItems { get; set; }

    public byte Difficulty { get; set; }

    public byte SecretHint { get; set; }

    public byte Input1 { get; set; }

    public byte Input2 { get; set; }

    public bool GameHasRepeated { get; set; }

    public byte InitialDifficulty { get; set; }

    public int HighScore1 { get; set; }

    public int HighScore2 { get; set; }

    public required string HighScoreName { get; set; }

    public byte HighScoreDiff { get; set; }

    public bool IsEmpty
    {
        get { return LevelNumber == 0; }
    }

    public SaveSlotInfo ToInfo()
    {
        bool empty = IsEmpty;
        return new SaveSlotInfo
        {
            SlotIndex = SlotIndex,
            PageIndex = PageIndex,
            IsEmpty = empty,
            Name = empty ? "EMPTY SLOT" : Name,
            LevelName = empty ? "-----" : LevelName,
            LevelNumber = LevelNumber,
            EpisodeNumber = EpisodeNumber,
            CubeCount = CubeCount,
            Cash = Cash,
            Cash2 = Cash2,
        };
    }
}
