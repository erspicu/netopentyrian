namespace OpenTyrian.Core;

public static class PaletteRenderer
{
    public static void Render(IndexedFrameBuffer source, ReadOnlySpan<PaletteColor> palette, ArgbFrameBuffer destination)
    {
        if (source.Width != destination.Width || source.Height != destination.Height)
        {
            throw new ArgumentException("Source and destination framebuffer sizes must match.");
        }

        Span<byte> src = source.Pixels;
        Span<uint> dst = destination.Pixels;

        for (int i = 0; i < src.Length; i++)
        {
            dst[i] = palette[src[i]].ToArgb32();
        }
    }
}
