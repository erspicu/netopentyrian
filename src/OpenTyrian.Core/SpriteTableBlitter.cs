namespace OpenTyrian.Core;

public static class SpriteTableBlitter
{
    public static void Blit(IndexedFrameBuffer surface, int x, int y, SpriteTable table, int index)
    {
        SpriteFrame frame = table.GetFrame(index);
        BlitInternal(surface, x, y, frame, static (_, src) => src);
    }

    public static void BlitBlend(IndexedFrameBuffer surface, int x, int y, SpriteTable table, int index)
    {
        SpriteFrame frame = table.GetFrame(index);
        BlitInternal(surface, x, y, frame, static (dst, src) =>
            (byte)((src & 0xF0) | ((((dst & 0x0F) + (src & 0x0F)) / 2) & 0x0F)));
    }

    public static void BlitHueValue(IndexedFrameBuffer surface, int x, int y, SpriteTable table, int index, byte hue, int value)
    {
        SpriteFrame frame = table.GetFrame(index);
        byte hueShifted = (byte)(hue << 4);

        BlitInternal(surface, x, y, frame, (_, src) =>
        {
            int adjusted = (src & 0x0F) + value;
            if (adjusted < 0)
            {
                adjusted = 0;
            }
            else if (adjusted > 0x0F)
            {
                adjusted = 0x0F;
            }

            return (byte)(hueShifted | adjusted);
        });
    }

    public static void BlitHueValueBlend(IndexedFrameBuffer surface, int x, int y, SpriteTable table, int index, byte hue, int value)
    {
        SpriteFrame frame = table.GetFrame(index);
        byte hueShifted = (byte)(hue << 4);

        BlitInternal(surface, x, y, frame, (dst, src) =>
        {
            int adjusted = (src & 0x0F) + value;
            if (adjusted < 0)
            {
                adjusted = 0;
            }
            else if (adjusted > 0x0F)
            {
                adjusted = 0x0F;
            }

            return (byte)(hueShifted | (((dst & 0x0F) + adjusted) / 2));
        });
    }

    public static void BlitDark(IndexedFrameBuffer surface, int x, int y, SpriteTable table, int index, bool black)
    {
        SpriteFrame frame = table.GetFrame(index);
        BlitInternal(surface, x, y, frame, (dst, _) =>
            black ? (byte)0x00 : (byte)((dst & 0xF0) | ((dst & 0x0F) / 2)));
    }

    private static void BlitInternal(
        IndexedFrameBuffer surface,
        int originX,
        int originY,
        SpriteFrame frame,
        Func<byte, byte, byte> pixelTransform)
    {
        ReadOnlySpan<byte> data = frame.Data;
        Span<byte> pixels = surface.Pixels;

        int x = 0;
        int y = 0;

        for (int src = 0; src < data.Length; src++)
        {
            byte token = data[src];

            switch (token)
            {
                case 255:
                    src++;
                    if (src >= data.Length)
                    {
                        return;
                    }

                    x += data[src];
                    break;

                case 254:
                    x = 0;
                    y += 1;
                    break;

                case 253:
                    x += 1;
                    break;

                default:
                    int targetX = originX + x;
                    int targetY = originY + y;
                    if ((uint)targetX < (uint)surface.Width && (uint)targetY < (uint)surface.Height)
                    {
                        int offset = targetY * surface.Width + targetX;
                        pixels[offset] = pixelTransform(pixels[offset], token);
                    }

                    x += 1;
                    break;
            }

            if (x >= frame.Width)
            {
                x = 0;
                y += 1;
            }
        }
    }
}
