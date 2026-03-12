namespace OpenTyrian.Core;

public static class PaletteLoader
{
    public static PaletteBank Load(string filePath)
    {
        using Stream stream = File.OpenRead(filePath);
        return Load(stream);
    }

    public static PaletteBank Load(Stream stream)
    {
        using var reader = new TyrianDataStream(stream, leaveOpen: true);

        if (reader.Length == 0 || reader.Length % PaletteBank.BytesPerPalette != 0)
        {
            throw new InvalidDataException($"Invalid palette.dat length: {reader.Length}");
        }

        int paletteCount = (int)(reader.Length / PaletteBank.BytesPerPalette);
        var palettes = new List<PaletteColor[]>(paletteCount);

        for (int paletteIndex = 0; paletteIndex < paletteCount; paletteIndex++)
        {
            var colors = new PaletteColor[PaletteBank.ColorsPerPalette];

            for (int colorIndex = 0; colorIndex < PaletteBank.ColorsPerPalette; colorIndex++)
            {
                byte r6 = reader.ReadByte();
                byte g6 = reader.ReadByte();
                byte b6 = reader.ReadByte();

                colors[colorIndex] = new PaletteColor(
                    ExpandVga6To8(r6),
                    ExpandVga6To8(g6),
                    ExpandVga6To8(b6));
            }

            palettes.Add(colors);
        }

        return new PaletteBank(palettes);
    }

    private static byte ExpandVga6To8(byte value)
    {
        return (byte)((value << 2) | (value >> 4));
    }
}
