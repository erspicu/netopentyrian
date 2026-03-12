namespace OpenTyrian.Core;

public readonly record struct PaletteColor(byte R, byte G, byte B)
{
    public uint ToArgb32()
    {
        return 0xFF000000u | (uint)(R << 16) | (uint)(G << 8) | B;
    }
}
