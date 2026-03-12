namespace OpenTyrian.Core;

public sealed class TyrianFontRenderer
{
    private static readonly int[] AsciiToSprite =
    [
        -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        -1, 26, 33, 60, 61, 62, -1, 32, 64, 65, 63, 84, 29, 83, 28, 80,
        79, 70, 71, 72, 73, 74, 75, 76, 77, 78, 31, 30, -1, 85, -1, 27,
        -1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14,
        15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 68, 82, 69, -1, -1,
        -1, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48,
        49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 66, 81, 67, -1, -1,
        86, 87, 88, 89, 90, 91, 92, 93, 94, 95, 96, 97, 98, 99, 100, 101,
        102, 103, 104, 105, 106, 107, 108, 109, 110, 111, 112, 113, 114, 115, 116, 117,
        118, 119, 120, 121, 122, 123, 124, 125, 126, -1, -1, -1, -1, -1, -1, -1,
        -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
    ];

    private readonly MainShapeTables _tables;

    public TyrianFontRenderer(MainShapeTables tables)
    {
        _tables = tables;
    }

    public int MeasureText(string text, FontKind fontKind)
    {
        SpriteTable font = GetTable(fontKind);
        int width = 0;

        foreach (char c in text)
        {
            if (c == ' ')
            {
                width += 6;
                continue;
            }

            if (c == '~')
            {
                continue;
            }

            int spriteId = GetSpriteId(c);
            if (spriteId >= 0 && font.Exists(spriteId))
            {
                width += font.GetFrame(spriteId).Width + 1;
            }
        }

        return width;
    }

    public int GetAlignedX(string text, FontKind fontKind, FontAlignment alignment, int x)
    {
        return alignment switch
        {
            FontAlignment.Center => x - (MeasureText(text, fontKind) / 2),
            FontAlignment.Right => x - MeasureText(text, fontKind),
            _ => x,
        };
    }

    public void DrawText(
        IndexedFrameBuffer surface,
        int x,
        int y,
        string text,
        FontKind fontKind,
        FontAlignment alignment,
        byte hue,
        int value,
        bool shadow)
    {
        int originX = GetAlignedX(text, fontKind, alignment, x);

        if (shadow)
        {
            DrawDark(surface, originX + 2, y + 2, text, fontKind, alignment: FontAlignment.Left, black: false);
        }

        SpriteTable font = GetTable(fontKind);
        bool highlight = false;
        int cursorX = originX;
        int currentValue = value;

        foreach (char c in text)
        {
            switch (c)
            {
                case ' ':
                    cursorX += 6;
                    break;

                case '~':
                    highlight = !highlight;
                    currentValue += highlight ? 4 : -4;
                    break;

                default:
                    int spriteId = GetSpriteId(c);
                    if (spriteId >= 0 && font.Exists(spriteId))
                    {
                        SpriteTableBlitter.BlitHueValue(surface, cursorX, y, font, spriteId, hue, currentValue);
                        cursorX += font.GetFrame(spriteId).Width + 1;
                    }
                    break;
            }
        }
    }

    public void DrawBlendText(
        IndexedFrameBuffer surface,
        int x,
        int y,
        string text,
        FontKind fontKind,
        FontAlignment alignment,
        byte hue,
        int value)
    {
        SpriteTable font = GetTable(fontKind);
        bool highlight = false;
        int cursorX = GetAlignedX(text, fontKind, alignment, x);
        int currentValue = value;

        foreach (char c in text)
        {
            switch (c)
            {
                case ' ':
                    cursorX += 6;
                    break;

                case '~':
                    highlight = !highlight;
                    currentValue += highlight ? 4 : -4;
                    break;

                default:
                    int spriteId = GetSpriteId(c);
                    if (spriteId >= 0 && font.Exists(spriteId))
                    {
                        SpriteTableBlitter.BlitHueValueBlend(surface, cursorX, y, font, spriteId, hue, currentValue);
                        cursorX += font.GetFrame(spriteId).Width + 1;
                    }
                    break;
            }
        }
    }

    public void DrawShadowText(
        IndexedFrameBuffer surface,
        int x,
        int y,
        string text,
        FontKind fontKind,
        FontAlignment alignment,
        byte hue,
        int value,
        bool black,
        int shadowDistance)
    {
        int originX = GetAlignedX(text, fontKind, alignment, x);
        DrawDark(surface, originX + shadowDistance, y + shadowDistance, text, fontKind, FontAlignment.Left, black);
        DrawText(surface, originX, y, text, fontKind, FontAlignment.Left, hue, value, shadow: false);
    }

    public void DrawFullShadowText(
        IndexedFrameBuffer surface,
        int x,
        int y,
        string text,
        FontKind fontKind,
        FontAlignment alignment,
        byte hue,
        int value,
        bool black,
        int shadowDistance)
    {
        int originX = GetAlignedX(text, fontKind, alignment, x);
        DrawDark(surface, originX, y - shadowDistance, text, fontKind, FontAlignment.Left, black);
        DrawDark(surface, originX + shadowDistance, y, text, fontKind, FontAlignment.Left, black);
        DrawDark(surface, originX, y + shadowDistance, text, fontKind, FontAlignment.Left, black);
        DrawDark(surface, originX - shadowDistance, y, text, fontKind, FontAlignment.Left, black);
        DrawText(surface, originX, y, text, fontKind, FontAlignment.Left, hue, value, shadow: false);
    }

    public void DrawDark(
        IndexedFrameBuffer surface,
        int x,
        int y,
        string text,
        FontKind fontKind,
        FontAlignment alignment,
        bool black)
    {
        SpriteTable font = GetTable(fontKind);
        int cursorX = GetAlignedX(text, fontKind, alignment, x);

        foreach (char c in text)
        {
            if (c == ' ')
            {
                cursorX += 6;
                continue;
            }

            if (c == '~')
            {
                continue;
            }

            int spriteId = GetSpriteId(c);
            if (spriteId >= 0 && font.Exists(spriteId))
            {
                SpriteTableBlitter.BlitDark(surface, cursorX, y, font, spriteId, black);
                cursorX += font.GetFrame(spriteId).Width + 1;
            }
        }
    }

    private SpriteTable GetTable(FontKind fontKind)
    {
        return fontKind switch
        {
            FontKind.Normal => _tables.NormalFont,
            FontKind.Small => _tables.SmallFont,
            _ => _tables.TinyFont,
        };
    }

    private static int GetSpriteId(char c)
    {
        return c <= byte.MaxValue ? AsciiToSprite[(byte)c] : -1;
    }
}
