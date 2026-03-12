namespace OpenTyrian.Core;

public sealed class ShipSpecsScene : IScene
{
    private const int DescriptionWidth = 184;
    private const int DescriptionLineHeight = 9;
    private const int DescriptionLinesPerBox = 6;

    private readonly EpisodeSessionState _sessionState;
    private OpenTyrian.Platform.InputSnapshot _previousInput;

    public ShipSpecsScene(EpisodeSessionState sessionState)
    {
        _sessionState = sessionState;
    }

    public IScene? Update(SceneResources resources, OpenTyrian.Platform.InputSnapshot input, double deltaSeconds)
    {
        bool cancelPressed = input.Cancel && !_previousInput.Cancel;
        bool confirmPressed = input.Confirm && !_previousInput.Confirm;
        bool pointerConfirmPressed = input.PointerConfirm && !_previousInput.PointerConfirm;
        bool pointerCancelPressed = input.PointerCancel && !_previousInput.PointerCancel;

        if (cancelPressed || pointerCancelPressed)
        {
            SceneAudio.PlayCancel(resources);
        }
        else if (confirmPressed || pointerConfirmPressed)
        {
            SceneAudio.PlayConfirm(resources);
        }

        _previousInput = input;
        return cancelPressed || confirmPressed || pointerConfirmPressed || pointerCancelPressed
            ? new FullGameMenuScene(_sessionState)
            : null;
    }

    public void Render(IndexedFrameBuffer surface, SceneResources resources, double timeSeconds)
    {
        TitleScreenRenderer.RenderBackground(surface, resources, timeSeconds);
        TitleScreenRenderer.RenderTitleOverlay(surface, resources.FontRenderer, resources.PaletteCount);

        if (resources.FontRenderer is null)
        {
            return;
        }

        DrawPanels(surface);

        int shipId = _sessionState.PlayerLoadout.GetEquippedItemId(ItemCategoryKind.Ship);
        ItemCatalogEntry? shipEntry = resources.ItemCatalog?.GetEntry(ItemCategoryKind.Ship, shipId);
        ShipDescriptionEntry description = GetShipDescription(resources.GameplayText, shipId);

        resources.FontRenderer.DrawShadowText(
            surface,
            160,
            78,
            "Ship Specs",
            FontKind.Normal,
            FontAlignment.Center,
            15,
            0,
            black: false,
            shadowDistance: 1);
        resources.FontRenderer.DrawText(
            surface,
            160,
            90,
            string.Format(
                "{0}  value:{1}  cash:{2}",
                ItemNameResolver.GetCompactItemName(ItemCategoryKind.Ship, shipId, resources.ItemCatalog),
                ItemPriceCalculator.GetItemValue(ItemCategoryKind.Ship, shipId, 0, resources.ItemCatalog),
                _sessionState.Cash),
            FontKind.Tiny,
            FontAlignment.Center,
            14,
            1,
            shadow: true);

        RenderShipIllustration(surface, resources.MainShapeTables, shipEntry);
        RenderDescriptionBox(surface, resources.FontRenderer, 106, 48, description.Summary);
        RenderDescriptionBox(surface, resources.FontRenderer, 106, 114, description.Detail);
        RenderStats(surface, resources.FontRenderer, resources.ItemCatalog, shipEntry);

        resources.FontRenderer.DrawDark(surface, 160, 194, "Enter/Esc/click returns to full-game menu", FontKind.Tiny, FontAlignment.Center, black: false);
    }

    private void DrawPanels(IndexedFrameBuffer surface)
    {
        Vga256.FillRectangleXY(surface, 8, 44, 311, 188, 0x20);
        Vga256.ShadeRectangle(surface, 10, 46, 309, 186);
        Vga256.DrawRectangle(surface, 8, 44, 311, 188, 0x25);
        Vga256.DrawRectangle(surface, 9, 45, 310, 187, 0x23);
        Vga256.DrawRectangle(surface, 14, 50, 92, 176, 0x2C);
        Vga256.DrawRectangle(surface, 100, 46, 304, 103, 0x2C);
        Vga256.DrawRectangle(surface, 100, 112, 304, 169, 0x2C);
    }

    private static ShipDescriptionEntry GetShipDescription(GameplayTextInfo? gameplayText, int shipId)
    {
        if (shipId > 0 && gameplayText is not null && shipId - 1 < gameplayText.ShipInfo.Count)
        {
            return gameplayText.ShipInfo[shipId - 1];
        }

        return new ShipDescriptionEntry
        {
            Summary = "No ship description available for this loadout.",
            Detail = "This prototype currently shows equipment and stats even when the original ship text is not available.",
        };
    }

    private static void RenderShipIllustration(IndexedFrameBuffer surface, MainShapeTables? mainShapeTables, ItemCatalogEntry? shipEntry)
    {
        if (mainShapeTables is null || shipEntry is null || shipEntry.SpriteId < 0 || !mainShapeTables.HasTable(MainShapeTableKind.Option))
        {
            return;
        }

        SpriteTable optionShapes = mainShapeTables.OptionShapes;
        if (!optionShapes.Exists(shipEntry.SpriteId))
        {
            return;
        }

        SpriteTableBlitter.BlitHueValue(surface, 28, 70, optionShapes, shipEntry.SpriteId, 12, 0);
        SpriteTableBlitter.BlitDark(surface, 30, 72, optionShapes, shipEntry.SpriteId, black: false);
    }

    private static void RenderDescriptionBox(IndexedFrameBuffer surface, TyrianFontRenderer fontRenderer, int x, int y, string text)
    {
        IList<string> wrappedLines = WrapText(text, fontRenderer, DescriptionWidth);
        int visibleLines = Math.Min(DescriptionLinesPerBox, wrappedLines.Count);
        for (int i = 0; i < visibleLines; i++)
        {
            fontRenderer.DrawText(surface, x, y + (i * DescriptionLineHeight), wrappedLines[i], FontKind.Tiny, FontAlignment.Left, 13, 0, shadow: true);
        }
    }

    private void RenderStats(IndexedFrameBuffer surface, TyrianFontRenderer fontRenderer, ItemCatalog? itemCatalog, ItemCatalogEntry? shipEntry)
    {
        int shieldId = _sessionState.PlayerLoadout.GetEquippedItemId(ItemCategoryKind.Shield);
        int generatorId = _sessionState.PlayerLoadout.GetEquippedItemId(ItemCategoryKind.Generator);
        int frontWeaponId = _sessionState.PlayerLoadout.GetEquippedItemId(ItemCategoryKind.FrontWeapon);
        int rearWeaponId = _sessionState.PlayerLoadout.GetEquippedItemId(ItemCategoryKind.RearWeapon);
        int leftSidekickId = _sessionState.PlayerLoadout.GetEquippedItemId(ItemCategoryKind.SidekickLeft);
        int rightSidekickId = _sessionState.PlayerLoadout.GetEquippedItemId(ItemCategoryKind.SidekickRight);

        ItemCatalogEntry? shieldEntry = itemCatalog?.GetEntry(ItemCategoryKind.Shield, shieldId);
        ItemCatalogEntry? generatorEntry = itemCatalog?.GetEntry(ItemCategoryKind.Generator, generatorId);

        fontRenderer.DrawText(surface, 18, 150, string.Format("ship speed:{0} armor:{1}", shipEntry?.PrimaryStat ?? 0, shipEntry?.SecondaryStat ?? 0), FontKind.Tiny, FontAlignment.Left, 14, 0, shadow: true);
        fontRenderer.DrawText(surface, 18, 158, string.Format("shield max:{0} regen:{1}", shieldEntry?.PrimaryStat ?? 0, shieldEntry?.SecondaryStat ?? 0), FontKind.Tiny, FontAlignment.Left, 14, 0, shadow: true);
        fontRenderer.DrawText(surface, 18, 166, string.Format("gen power:{0} speed:{1}", generatorEntry?.PrimaryStat ?? 0, generatorEntry?.SecondaryStat ?? 0), FontKind.Tiny, FontAlignment.Left, 14, 0, shadow: true);
        fontRenderer.DrawText(surface, 18, 174, string.Format("assets:{0} total:{1}", _sessionState.GetTotalAssetValue(itemCatalog), _sessionState.GetTotalScore(itemCatalog)), FontKind.Tiny, FontAlignment.Left, 14, 0, shadow: true);

        fontRenderer.DrawText(surface, 106, 178, BuildWeaponLine("front", frontWeaponId, _sessionState.PlayerLoadout.GetWeaponPower(ItemCategoryKind.FrontWeapon), itemCatalog), FontKind.Tiny, FontAlignment.Left, 12, 0, shadow: true);
        fontRenderer.DrawText(surface, 106, 186, BuildWeaponLine("rear", rearWeaponId, _sessionState.PlayerLoadout.GetWeaponPower(ItemCategoryKind.RearWeapon), itemCatalog), FontKind.Tiny, FontAlignment.Left, 12, 0, shadow: true);
        fontRenderer.DrawText(surface, 106, 170, string.Format("shield: {0}  gen: {1}", ItemNameResolver.GetCompactItemName(ItemCategoryKind.Shield, shieldId, itemCatalog), ItemNameResolver.GetCompactItemName(ItemCategoryKind.Generator, generatorId, itemCatalog)), FontKind.Tiny, FontAlignment.Left, 12, 0, shadow: true);
        fontRenderer.DrawText(surface, 106, 162, string.Format("left: {0}", ItemNameResolver.GetCompactItemName(ItemCategoryKind.SidekickLeft, leftSidekickId, itemCatalog)), FontKind.Tiny, FontAlignment.Left, 12, 0, shadow: true);
        fontRenderer.DrawText(surface, 106, 154, string.Format("right: {0}", ItemNameResolver.GetCompactItemName(ItemCategoryKind.SidekickRight, rightSidekickId, itemCatalog)), FontKind.Tiny, FontAlignment.Left, 12, 0, shadow: true);
    }

    private static string BuildWeaponLine(string label, int itemId, int power, ItemCatalog? itemCatalog)
    {
        return string.Format(
            "{0}: {1} x{2}",
            label,
            ItemNameResolver.GetCompactItemName(label == "front" ? ItemCategoryKind.FrontWeapon : ItemCategoryKind.RearWeapon, itemId, itemCatalog),
            itemId == 0 ? 0 : power);
    }

    private static IList<string> WrapText(string text, TyrianFontRenderer fontRenderer, int maxWidth)
    {
        List<string> wrappedLines = [];
        if (string.IsNullOrWhiteSpace(text))
        {
            wrappedLines.Add(string.Empty);
            return wrappedLines;
        }

        string remaining = text.Trim();
        while (remaining.Length > 0)
        {
            string nextLine = TakeWrappedSegment(remaining, fontRenderer, maxWidth);
            wrappedLines.Add(nextLine);
            remaining = remaining.Substring(nextLine.Length).TrimStart();
        }

        return wrappedLines;
    }

    private static string TakeWrappedSegment(string sourceLine, TyrianFontRenderer fontRenderer, int maxWidth)
    {
        if (fontRenderer.MeasureText(sourceLine, FontKind.Tiny) <= maxWidth)
        {
            return sourceLine;
        }

        int bestBreak = -1;
        for (int i = 0; i < sourceLine.Length; i++)
        {
            string candidate = sourceLine.Substring(0, i + 1);
            if (fontRenderer.MeasureText(candidate, FontKind.Tiny) > maxWidth)
            {
                break;
            }

            if (char.IsWhiteSpace(sourceLine[i]))
            {
                bestBreak = i;
            }
        }

        if (bestBreak >= 0)
        {
            return sourceLine.Substring(0, bestBreak).TrimEnd();
        }

        int fallbackLength = 1;
        while (fallbackLength < sourceLine.Length &&
               fontRenderer.MeasureText(sourceLine.Substring(0, fallbackLength + 1), FontKind.Tiny) <= maxWidth)
        {
            fallbackLength++;
        }

        return sourceLine.Substring(0, fallbackLength);
    }
}
