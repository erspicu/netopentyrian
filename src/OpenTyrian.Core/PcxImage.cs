namespace OpenTyrian.Core;

public sealed class PcxImage
{
    public PcxImage(int width, int height, byte[] indexedPixels, PaletteColor[] palette)
    {
        Width = width;
        Height = height;
        IndexedPixels = indexedPixels;
        Palette = palette;
    }

    public int Width { get; }

    public int Height { get; }

    public byte[] IndexedPixels { get; }

    public PaletteColor[] Palette { get; }
}
