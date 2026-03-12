namespace OpenTyrian.Core;

public static class MainShapeTablesLoader
{
    private const int MainTableCount = 7;

    public static MainShapeTables Load(Stream stream)
    {
        using TyrianDataStream data = new(stream, leaveOpen: true);
        int tableCount = data.ReadUInt16();
        int[] offsets = new int[tableCount + 1];

        for (int i = 0; i < tableCount; i++)
        {
            offsets[i] = data.ReadInt32();
        }

        offsets[tableCount] = checked((int)data.Length);

        int loadCount = Math.Min(MainTableCount, tableCount);
        SpriteTable[] tables = new SpriteTable[loadCount];

        for (int i = 0; i < loadCount; i++)
        {
            data.Position = offsets[i];
            tables[i] = SimpleSpriteTableLoader.Load(data);
        }

        return new MainShapeTables(tables);
    }
}
