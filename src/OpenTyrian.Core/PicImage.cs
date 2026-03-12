namespace OpenTyrian.Core;

public sealed class PicImage
{
    public PicImage(int width, int height, byte[] indexedPixels, int paletteIndex)
    {
        Width = width;
        Height = height;
        IndexedPixels = indexedPixels;
        PaletteIndex = paletteIndex;
    }

    public int Width { get; }

    public int Height { get; }

    public byte[] IndexedPixels { get; }

    public int PaletteIndex { get; }
}
