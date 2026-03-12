namespace OpenTyrian.Core;

public static class Vga256
{
    public static void Clear(IndexedFrameBuffer surface, byte colorIndex = 0)
    {
        surface.Clear(colorIndex);
    }

    public static void PutPixel(IndexedFrameBuffer surface, int x, int y, byte colorIndex)
    {
        if ((uint)x >= (uint)surface.Width || (uint)y >= (uint)surface.Height)
        {
            return;
        }

        surface.Pixels[y * surface.Width + x] = colorIndex;
    }

    public static void PutCrossPixel(IndexedFrameBuffer surface, int x, int y, byte colorIndex)
    {
        PutPixel(surface, x, y, colorIndex);
        PutPixel(surface, x - 1, y, colorIndex);
        PutPixel(surface, x + 1, y, colorIndex);
        PutPixel(surface, x, y - 1, colorIndex);
        PutPixel(surface, x, y + 1, colorIndex);
    }

    public static void DrawRectangle(IndexedFrameBuffer surface, int x1, int y1, int x2, int y2, byte colorIndex)
    {
        NormalizeRectangle(ref x1, ref y1, ref x2, ref y2);

        if (!TryClipRectangle(surface, ref x1, ref y1, ref x2, ref y2))
        {
            return;
        }

        FillHorizontal(surface, x1, x2, y1, colorIndex);
        FillHorizontal(surface, x1, x2, y2, colorIndex);

        for (int y = y1 + 1; y < y2; y++)
        {
            PutPixel(surface, x1, y, colorIndex);
            PutPixel(surface, x2, y, colorIndex);
        }
    }

    public static void FillRectangleXY(IndexedFrameBuffer surface, int x1, int y1, int x2, int y2, byte colorIndex)
    {
        NormalizeRectangle(ref x1, ref y1, ref x2, ref y2);

        if (!TryClipRectangle(surface, ref x1, ref y1, ref x2, ref y2))
        {
            return;
        }

        for (int y = y1; y <= y2; y++)
        {
            FillHorizontal(surface, x1, x2, y, colorIndex);
        }
    }

    public static void FillRectangleWH(IndexedFrameBuffer surface, int x, int y, int width, int height, byte colorIndex)
    {
        if (width <= 0 || height <= 0)
        {
            return;
        }

        FillRectangleXY(surface, x, y, x + width - 1, y + height - 1, colorIndex);
    }

    public static void ShadeRectangle(IndexedFrameBuffer surface, int x1, int y1, int x2, int y2)
    {
        NormalizeRectangle(ref x1, ref y1, ref x2, ref y2);

        if (!TryClipRectangle(surface, ref x1, ref y1, ref x2, ref y2))
        {
            return;
        }

        Span<byte> pixels = surface.Pixels;
        for (int y = y1; y <= y2; y++)
        {
            int rowOffset = y * surface.Width;
            for (int x = x1; x <= x2; x++)
            {
                byte value = pixels[rowOffset + x];
                pixels[rowOffset + x] = (byte)(((value & 0x0F) >> 1) | (value & 0xF0));
            }
        }
    }

    public static void BrightRectangle(IndexedFrameBuffer surface, int x1, int y1, int x2, int y2)
    {
        NormalizeRectangle(ref x1, ref y1, ref x2, ref y2);

        if (!TryClipRectangle(surface, ref x1, ref y1, ref x2, ref y2))
        {
            return;
        }

        Span<byte> pixels = surface.Pixels;
        for (int y = y1; y <= y2; y++)
        {
            int rowOffset = y * surface.Width;
            for (int x = x1; x <= x2; x++)
            {
                byte value = pixels[rowOffset + x];
                int high = value & 0xF0;
                int low = (value & 0x0F) + 2;
                if (low > 0x0F)
                {
                    low = 0x0F;
                }

                pixels[rowOffset + x] = (byte)(high | low);
            }
        }
    }

    private static void FillHorizontal(IndexedFrameBuffer surface, int x1, int x2, int y, byte colorIndex)
    {
        int rowOffset = y * surface.Width;
        surface.Pixels.Slice(rowOffset + x1, x2 - x1 + 1).Fill(colorIndex);
    }

    private static void NormalizeRectangle(ref int x1, ref int y1, ref int x2, ref int y2)
    {
        if (x1 > x2)
        {
            (x1, x2) = (x2, x1);
        }

        if (y1 > y2)
        {
            (y1, y2) = (y2, y1);
        }
    }

    private static bool TryClipRectangle(IndexedFrameBuffer surface, ref int x1, ref int y1, ref int x2, ref int y2)
    {
        x1 = Math.Max(0, x1);
        y1 = Math.Max(0, y1);
        x2 = Math.Min(surface.Width - 1, x2);
        y2 = Math.Min(surface.Height - 1, y2);

        return x1 <= x2 && y1 <= y2;
    }
}
