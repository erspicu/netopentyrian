namespace OpenTyrian.Core;

public static class TitleScreenRenderer
{
    private const int MenuCenterX = 160;
    private const int MenuStartY = 98;
    private const int MenuRowHeight = 16;
    private const int MenuHitHalfWidth = 110;

    public static void RenderBackground(IndexedFrameBuffer surface, SceneResources resources, double timeSeconds)
    {
        if (resources.TitleImage is not null)
        {
            Array.Copy(resources.TitleImage.IndexedPixels, surface.Pixels, Math.Min(resources.TitleImage.IndexedPixels.Length, surface.Pixels.Length));
        }
        else if (resources.TestPcxImage is not null)
        {
            Array.Copy(resources.TestPcxImage.IndexedPixels, surface.Pixels, Math.Min(resources.TestPcxImage.IndexedPixels.Length, surface.Pixels.Length));
        }
        else
        {
            RenderFallback(surface, timeSeconds);
        }

        RenderInterfaceOverlay(surface, resources.MainShapeTables);
        RenderTestSpriteOverlay(surface, resources.TestSpriteSheet);
    }

    public static void RenderTitleOverlay(IndexedFrameBuffer surface, TyrianFontRenderer? fontRenderer, int paletteCount)
    {
        if (fontRenderer is null)
        {
            return;
        }

        const string title = "OpenTyrian .NET Framework 4.0";
        const string subtitle = "~WinForms~ Panel + GDI";
        string detail = $"~Palette~ {paletteCount}  ~Data~ OK";
        string footer = "Font path from tyrian.shp";

        fontRenderer.DrawFullShadowText(surface, 160, 6, title, FontKind.Small, FontAlignment.Center, 15, 0, black: false, shadowDistance: 1);
        fontRenderer.DrawBlendText(surface, 160, 22, subtitle, FontKind.Tiny, FontAlignment.Center, 13, 3);
        fontRenderer.DrawShadowText(surface, 8, 182, detail, FontKind.Tiny, FontAlignment.Left, 14, 2, black: false, shadowDistance: 2);
        fontRenderer.DrawDark(surface, 312, 190, footer, FontKind.Tiny, FontAlignment.Right, black: false);
    }

    public static void RenderMenuOverlay(IndexedFrameBuffer surface, TyrianFontRenderer? fontRenderer, MenuDefinition menu, MenuState menuState)
    {
        if (fontRenderer is null)
        {
            return;
        }

        fontRenderer.DrawShadowText(surface, 160, 78, menu.Title, FontKind.Normal, FontAlignment.Center, 15, 0, black: false, shadowDistance: 1);

        for (int i = 0; i < menu.Items.Count; i++)
        {
            MenuItemDefinition item = menu.Items[i];
            bool selected = i == menuState.SelectedIndex;
            int y = MenuStartY + (i * MenuRowHeight);
            byte hue = item.IsEnabled ? (selected ? (byte)15 : (byte)13) : (byte)8;
            int value = item.IsEnabled ? (selected ? 4 : 1) : -2;

            if (selected)
            {
                fontRenderer.DrawBlendText(surface, 160, y, $"> {item.Label} <", FontKind.Tiny, FontAlignment.Center, hue, value);
            }
            else
            {
                fontRenderer.DrawText(surface, 160, y, item.Label, FontKind.Tiny, FontAlignment.Center, hue, value, shadow: true);
            }
        }

        fontRenderer.DrawText(surface, 160, 168, menuState.SelectedItem.Description, FontKind.Tiny, FontAlignment.Center, 14, 0, shadow: true);
        fontRenderer.DrawDark(surface, 160, 182, menu.Footer, FontKind.Tiny, FontAlignment.Center, black: false);
    }

    public static int? HitTestMenuItem(MenuDefinition menu, int x, int y)
    {
        if (menu.Items.Count == 0)
        {
            return null;
        }

        if (x < MenuCenterX - MenuHitHalfWidth || x > MenuCenterX + MenuHitHalfWidth)
        {
            return null;
        }

        for (int i = 0; i < menu.Items.Count; i++)
        {
            int top = (MenuStartY + (i * MenuRowHeight)) - 6;
            int bottom = top + 12;
            if (y >= top && y <= bottom)
            {
                return i;
            }
        }

        return null;
    }

    private static void RenderFallback(IndexedFrameBuffer surface, double timeSeconds)
    {
        int width = surface.Width;
        int height = surface.Height;
        int phase = (int)(timeSeconds * 60.0);
        byte[] pixels = surface.Pixels;

        for (int y = 0; y < height; y++)
        {
            int rowOffset = y * width;

            for (int x = 0; x < width; x++)
            {
                int paletteIndex = (x + y + phase * 2) & 0xFF;
                if (((x / 20) + (y / 20) + (phase / 10)) % 2 == 0)
                {
                    paletteIndex = (paletteIndex + 48) & 0xFF;
                }

                pixels[rowOffset + x] = (byte)paletteIndex;
            }
        }

        Vga256.DrawRectangle(surface, 0, 0, width - 1, height - 1, 255);
        Vga256.FillRectangleWH(surface, 12, 12, 96, 28, 32);
        Vga256.BrightRectangle(surface, 12, 12, 107, 39);
        Vga256.ShadeRectangle(surface, width - 108, 12, width - 13, 39);
        Vga256.DrawRectangle(surface, width - 108, 12, width - 13, 39, 250);
        Vga256.PutCrossPixel(surface, width / 2, height / 2, 254);
    }

    private static void RenderTestSpriteOverlay(IndexedFrameBuffer surface, Sprite2Sheet? spriteSheet)
    {
        if (spriteSheet is null || spriteSheet.Count == 0)
        {
            return;
        }

        Sprite2Blitter.Blit(surface, 8, 8, spriteSheet, 1);
        Sprite2Blitter.Blit(surface, 24, 8, spriteSheet, 2);
        Sprite2Blitter.Blit(surface, 40, 8, spriteSheet, 3);
        Sprite2Blitter.BlitBlend(surface, 8, 28, spriteSheet, 4);
        Sprite2Blitter.BlitDarken(surface, 24, 28, spriteSheet, 5);
        Sprite2Blitter.BlitFilter(surface, 40, 28, spriteSheet, 6, 0x90);
        Sprite2Blitter.BlitFilterClip(surface, -2, 44, spriteSheet, 7, 0xA0);
    }

    private static void RenderInterfaceOverlay(IndexedFrameBuffer surface, MainShapeTables? mainShapeTables)
    {
        if (mainShapeTables is null || !mainShapeTables.HasTable(MainShapeTableKind.Option))
        {
            return;
        }

        SpriteTable optionShapes = mainShapeTables.OptionShapes;

        if (optionShapes.Exists(35))
        {
            SpriteTableBlitter.Blit(surface, 50, 50, optionShapes, 35);
        }

        if (optionShapes.Exists(25))
        {
            SpriteTableBlitter.BlitDark(surface, 228, 60, optionShapes, 25, black: false);
            SpriteTableBlitter.BlitHueValue(surface, 225, 57, optionShapes, 25, 9, 6);
        }

        if (optionShapes.Exists(34))
        {
            SpriteTableBlitter.BlitDark(surface, 198, 62, optionShapes, 34, black: false);
            SpriteTableBlitter.Blit(surface, 196, 60, optionShapes, 34);
        }

        if (mainShapeTables.HasTable(MainShapeTableKind.Face))
        {
            SpriteTable faceShapes = mainShapeTables.FaceShapes;
            if (faceShapes.Exists(0))
            {
                SpriteTableBlitter.Blit(surface, 70, 66, faceShapes, 0);
            }
        }
    }
}
