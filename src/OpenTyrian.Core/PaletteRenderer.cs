namespace OpenTyrian.Core;

public static class PaletteRenderer
{
    public static void Render(IndexedFrameBuffer source, PaletteColor[] palette, ArgbFrameBuffer destination)
    {
        if (source.Width != destination.Width || source.Height != destination.Height)
        {
            throw new ArgumentException("Source and destination framebuffer sizes must match.");
        }

        byte[] src = source.Pixels;
        uint[] dst = destination.Pixels;

        for (int i = 0; i < src.Length; i++)
        {
            dst[i] = palette[src[i]].ToArgb32();
        }
    }
}
