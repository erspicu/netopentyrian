namespace OpenTyrian.Core;

public static class Sprite2Blitter
{
    public static void Blit(IndexedFrameBuffer surface, int x, int y, Sprite2Sheet sheet, int index)
    {
        BlitInternal(surface, x, y, sheet, index, clip: false, pixelTransform: static (_, src) => src);
    }

    public static void BlitClip(IndexedFrameBuffer surface, int x, int y, Sprite2Sheet sheet, int index)
    {
        BlitInternal(surface, x, y, sheet, index, clip: true, pixelTransform: static (_, src) => src);
    }

    public static void BlitBlend(IndexedFrameBuffer surface, int x, int y, Sprite2Sheet sheet, int index)
    {
        BlitInternal(surface, x, y, sheet, index, clip: false, pixelTransform: static (dst, src) =>
            (byte)((src & 0xF0) | ((((dst & 0x0F) + (src & 0x0F)) / 2) & 0x0F)));
    }

    public static void BlitDarken(IndexedFrameBuffer surface, int x, int y, Sprite2Sheet sheet, int index)
    {
        BlitInternal(surface, x, y, sheet, index, clip: false, pixelTransform: static (dst, _) =>
            (byte)(((dst & 0x0F) / 2) | (dst & 0xF0)));
    }

    public static void BlitFilter(IndexedFrameBuffer surface, int x, int y, Sprite2Sheet sheet, int index, byte filter)
    {
        BlitInternal(surface, x, y, sheet, index, clip: false, pixelTransform: (_, src) =>
            (byte)(filter | (src & 0x0F)));
    }

    public static void BlitFilterClip(IndexedFrameBuffer surface, int x, int y, Sprite2Sheet sheet, int index, byte filter)
    {
        BlitInternal(surface, x, y, sheet, index, clip: true, pixelTransform: (_, src) =>
            (byte)(filter | (src & 0x0F)));
    }

    private static void BlitInternal(
        IndexedFrameBuffer surface,
        int originX,
        int originY,
        Sprite2Sheet sheet,
        int index,
        bool clip,
        Func<byte, byte, byte> pixelTransform)
    {
        ReadOnlySpan<byte> data = sheet.GetSpriteData(index);
        Span<byte> pixels = surface.Pixels;

        int x = originX;
        int y = originY;
        int src = 0;

        while (src < data.Length)
        {
            byte token = data[src++];
            if (token == 0x0F)
            {
                break;
            }

            int skipCount = token & 0x0F;
            int fillCount = (token >> 4) & 0x0F;
            x += skipCount;

            if (fillCount == 0)
            {
                y += 1;
                x = originX;
                continue;
            }

            for (int i = 0; i < fillCount && src < data.Length; i++)
            {
                byte sourceColor = data[src++];

                if ((uint)x < (uint)surface.Width && (uint)y < (uint)surface.Height)
                {
                    int offset = y * surface.Width + x;
                    pixels[offset] = pixelTransform(pixels[offset], sourceColor);
                }
                else if (!clip && y >= surface.Height)
                {
                    return;
                }

                x += 1;
            }
        }
    }
}
