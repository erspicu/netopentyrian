namespace OpenTyrian.Core;

public sealed class UpgradeMenuScene : IScene
{
    private const int VisibleSubmenuRows = 6;

    private enum UpgradeMenuMode
    {
        CategorySelect,
        ItemSelect,
    }

    private readonly EpisodeSessionState _sessionState;
    private readonly int[] _selectedSlots;
    private readonly int?[] _confirmedItemIds;
    private OpenTyrian.Platform.InputSnapshot _previousInput;
    private int _selectedCategoryIndex;
    private string _statusText;
    private UpgradeMenuMode _mode;

    public UpgradeMenuScene(EpisodeSessionState sessionState)
    {
        _sessionState = sessionState;
        _selectedSlots = new int[Math.Max(1, sessionState.ShopCategories.Count)];
        _confirmedItemIds = new int?[Math.Max(1, sessionState.ShopCategories.Count)];
        _selectedCategoryIndex = 0;
        _statusText = "Select a shop category";
        _mode = UpgradeMenuMode.CategorySelect;
        InitializeSlotsFromLoadout();
    }

    public IScene? Update(SceneResources resources, OpenTyrian.Platform.InputSnapshot input, double deltaSeconds)
    {
        bool cancelPressed = input.Cancel && !_previousInput.Cancel;
        bool confirmPressed = input.Confirm && !_previousInput.Confirm;
        bool upPressed = input.Up && !_previousInput.Up;
        bool downPressed = input.Down && !_previousInput.Down;
        bool leftPressed = input.Left && !_previousInput.Left;
        bool rightPressed = input.Right && !_previousInput.Right;

        if (cancelPressed)
        {
            if (_mode == UpgradeMenuMode.ItemSelect)
            {
                _mode = UpgradeMenuMode.CategorySelect;
                _statusText = "Returned to category select";
                _previousInput = input;
                return null;
            }

            _previousInput = input;
            return new EpisodeSessionScene(_sessionState);
        }

        if (_sessionState.ShopCategories.Count == 0)
        {
            _previousInput = input;
            return null;
        }

        if (_mode == UpgradeMenuMode.CategorySelect && upPressed)
        {
            _selectedCategoryIndex = _selectedCategoryIndex == 0
                ? _sessionState.ShopCategories.Count - 1
                : _selectedCategoryIndex - 1;
        }

        if (_mode == UpgradeMenuMode.CategorySelect && downPressed)
        {
            _selectedCategoryIndex = (_selectedCategoryIndex + 1) % _sessionState.ShopCategories.Count;
        }

        ShopCategory category = _sessionState.ShopCategories[_selectedCategoryIndex];
        int selectableCount = Math.Max(1, category.ItemCount + 1);

        if (_mode == UpgradeMenuMode.ItemSelect && leftPressed)
        {
            _selectedSlots[_selectedCategoryIndex] = _selectedSlots[_selectedCategoryIndex] == 0
                ? selectableCount - 1
                : _selectedSlots[_selectedCategoryIndex] - 1;
        }

        if (_mode == UpgradeMenuMode.ItemSelect && rightPressed)
        {
            _selectedSlots[_selectedCategoryIndex] = (_selectedSlots[_selectedCategoryIndex] + 1) % selectableCount;
        }

        if (confirmPressed)
        {
            if (_mode == UpgradeMenuMode.CategorySelect)
            {
                _mode = UpgradeMenuMode.ItemSelect;
                _statusText = $"{category.DisplayName} submenu opened";
            }
            else if (_selectedSlots[_selectedCategoryIndex] >= category.ItemCount)
            {
                _mode = UpgradeMenuMode.CategorySelect;
                _statusText = $"{category.DisplayName} submenu closed";
            }
            else
            {
                int itemId = category.ItemCount > 0 ? category.ItemIds[_selectedSlots[_selectedCategoryIndex]] : 0;
                _confirmedItemIds[_selectedCategoryIndex] = itemId;
                _sessionState.EquipShopItem(category.Kind, itemId);
                _statusText = $"{category.DisplayName} equipped item id {itemId}";
            }
        }

        _previousInput = input;
        return null;
    }

    public void Render(IndexedFrameBuffer surface, SceneResources resources, double timeSeconds)
    {
        TitleScreenRenderer.RenderBackground(surface, resources, timeSeconds);
        TitleScreenRenderer.RenderTitleOverlay(surface, resources.FontRenderer, resources.PaletteCount);

        if (resources.FontRenderer is null)
        {
            return;
        }

        resources.FontRenderer.DrawShadowText(surface, 160, 78, "Upgrade Shop Prototype", FontKind.Normal, FontAlignment.Center, 15, 0, black: false, shadowDistance: 1);

        if (_sessionState.ShopCategories.Count == 0)
        {
            resources.FontRenderer.DrawText(surface, 160, 112, "No shop categories loaded from ]I yet", FontKind.Tiny, FontAlignment.Center, 12, 0, shadow: true);
            resources.FontRenderer.DrawDark(surface, 160, 182, "Esc returns to episode session", FontKind.Tiny, FontAlignment.Center, black: false);
            return;
        }

        for (int i = 0; i < _sessionState.ShopCategories.Count; i++)
        {
            ShopCategory category = _sessionState.ShopCategories[i];
            bool selected = i == _selectedCategoryIndex;
            int y = 100 + (i * 12);
            string label = $"{category.DisplayName} ({category.ItemCount})";
            if (_mode == UpgradeMenuMode.ItemSelect && selected)
            {
                label += " [open]";
            }

            if (selected)
            {
                resources.FontRenderer.DrawBlendText(surface, 92, y, $"> {label}", FontKind.Tiny, FontAlignment.Left, 15, 4);
            }
            else
            {
                resources.FontRenderer.DrawText(surface, 92, y, label, FontKind.Tiny, FontAlignment.Left, 13, 0, shadow: true);
            }
        }

        ShopCategory selectedCategory = _sessionState.ShopCategories[_selectedCategoryIndex];
        int selectedSlot = _selectedSlots[_selectedCategoryIndex];
        int selectedItemId = selectedCategory.ItemCount > 0 && selectedSlot < selectedCategory.ItemCount ? selectedCategory.ItemIds[selectedSlot] : 0;
        int? confirmedItemId = _confirmedItemIds[_selectedCategoryIndex];
        int equippedItemId = _sessionState.PlayerLoadout.GetEquippedItemId(selectedCategory.Kind);
        string rowPreview = selectedCategory.ItemCount > 0
            ? string.Join(", ", selectedCategory.ItemIds.Take(4)) + (selectedCategory.ItemCount > 4 ? "..." : string.Empty)
            : "<empty>";

        resources.FontRenderer.DrawText(surface, 228, 110, selectedCategory.DisplayName, FontKind.Small, FontAlignment.Center, 14, 1, shadow: true);
        resources.FontRenderer.DrawText(surface, 228, 132, $"row index: {selectedCategory.AvailabilityRowIndex + 1}", FontKind.Tiny, FontAlignment.Center, 13, 0, shadow: true);
        resources.FontRenderer.DrawText(surface, 228, 144, $"slot: {selectedSlot + 1}/{Math.Max(1, selectedCategory.ItemCount + 1)}", FontKind.Tiny, FontAlignment.Center, 13, 0, shadow: true);
        resources.FontRenderer.DrawText(surface, 228, 156, $"item id: {selectedItemId}", FontKind.Tiny, FontAlignment.Center, 15, 0, shadow: true);
        resources.FontRenderer.DrawText(surface, 228, 168, $"prepared: {(confirmedItemId.HasValue ? confirmedItemId.Value : 0)} equipped:{equippedItemId}", FontKind.Tiny, FontAlignment.Center, 13, 0, shadow: true);
        resources.FontRenderer.DrawText(surface, 228, 180, $"preview: {rowPreview}", FontKind.Tiny, FontAlignment.Center, 13, 0, shadow: true);

        RenderItemSubmenu(surface, resources.FontRenderer, selectedCategory, selectedSlot, confirmedItemId, equippedItemId, _mode == UpgradeMenuMode.ItemSelect);

        resources.FontRenderer.DrawText(surface, 160, 192, _statusText, FontKind.Tiny, FontAlignment.Center, 12, 0, shadow: true);
        resources.FontRenderer.DrawDark(surface, 160, 204, BuildFooterText(), FontKind.Tiny, FontAlignment.Center, black: false);
    }

    private void InitializeSlotsFromLoadout()
    {
        for (int i = 0; i < _sessionState.ShopCategories.Count; i++)
        {
            ShopCategory category = _sessionState.ShopCategories[i];
            int equippedItemId = _sessionState.PlayerLoadout.GetEquippedItemId(category.Kind);
            int equippedSlot = FindItemSlot(category, equippedItemId);

            _selectedSlots[i] = equippedSlot >= 0 ? equippedSlot : 0;
            _confirmedItemIds[i] = equippedItemId != 0 ? equippedItemId : null;
        }
    }

    private static int FindItemSlot(ShopCategory category, int itemId)
    {
        if (itemId == 0)
        {
            return -1;
        }

        for (int i = 0; i < category.ItemIds.Count; i++)
        {
            if (category.ItemIds[i] == itemId)
            {
                return i;
            }
        }

        return -1;
    }

    private static void RenderItemSubmenu(
        IndexedFrameBuffer surface,
        TyrianFontRenderer fontRenderer,
        ShopCategory selectedCategory,
        int selectedSlot,
        int? confirmedItemId,
        int equippedItemId,
        bool submenuOpen)
    {
        int totalRows = Math.Max(1, selectedCategory.ItemCount + 1);
        int visibleCount = Math.Min(VisibleSubmenuRows, totalRows);
        int windowStart = GetWindowStart(selectedSlot, totalRows, visibleCount);

        for (int i = 0; i < visibleCount; i++)
        {
            int rowIndex = windowStart + i;
            int y = 98 + (i * 12);
            bool isDoneRow = rowIndex >= selectedCategory.ItemCount;
            bool isSelected = submenuOpen && (isDoneRow
                ? selectedSlot >= selectedCategory.ItemCount
                : rowIndex == selectedSlot);

            string label;
            byte hue;
            int value;

            if (isDoneRow)
            {
                label = "Done";
                hue = isSelected ? (byte)15 : (byte)12;
                value = isSelected ? 4 : 0;
            }
            else if (rowIndex < selectedCategory.ItemCount)
            {
                int itemId = selectedCategory.ItemIds[rowIndex];
                bool isPrepared = confirmedItemId.HasValue && confirmedItemId.Value == itemId;
                bool isEquipped = equippedItemId == itemId;
                label = BuildItemLabel(selectedCategory, itemId, isPrepared, isEquipped);
                hue = isSelected ? (byte)15 : (isPrepared ? (byte)14 : isEquipped ? (byte)12 : (byte)13);
                value = isSelected ? 4 : (isPrepared ? 2 : isEquipped ? 1 : 0);
            }
            else
            {
                label = string.Empty;
                hue = 13;
                value = 0;
            }

            if (string.IsNullOrEmpty(label))
            {
                continue;
            }

            if (isSelected)
            {
                fontRenderer.DrawBlendText(surface, 204, y, $"> {label}", FontKind.Tiny, FontAlignment.Left, hue, value);
            }
            else
            {
                fontRenderer.DrawText(surface, 204, y, label, FontKind.Tiny, FontAlignment.Left, hue, value, shadow: true);
            }
        }

        if (windowStart > 0)
        {
            fontRenderer.DrawText(surface, 284, 90, "^", FontKind.Tiny, FontAlignment.Center, 12, 0, shadow: true);
        }

        if (windowStart + visibleCount < totalRows)
        {
            fontRenderer.DrawText(surface, 284, 98 + (visibleCount * 12), "v", FontKind.Tiny, FontAlignment.Center, 12, 0, shadow: true);
        }
    }

    private static int GetWindowStart(int selectedSlot, int totalRows, int visibleCount)
    {
        if (totalRows <= visibleCount)
        {
            return 0;
        }

        int preferredStart = selectedSlot - (visibleCount / 2);
        if (preferredStart < 0)
        {
            return 0;
        }

        int maxStart = totalRows - visibleCount;
        return Math.Min(preferredStart, maxStart);
    }

    private string BuildFooterText()
    {
        return _mode == UpgradeMenuMode.CategorySelect
            ? "Up/Down category  Enter open submenu  Esc back"
            : "Left/Right item  Enter equip/done  Esc category list";
    }

    private static string BuildItemLabel(ShopCategory category, int itemId, bool isPrepared, bool isEquipped)
    {
        string prefix = category.Kind switch
        {
            ItemCategoryKind.Ship => "Ship",
            ItemCategoryKind.FrontWeapon => "Front",
            ItemCategoryKind.RearWeapon => "Rear",
            ItemCategoryKind.Shield => "Shield",
            ItemCategoryKind.SidekickLeft => "Left",
            ItemCategoryKind.SidekickRight => "Right",
            ItemCategoryKind.Generator => "Gen",
            ItemCategoryKind.Special => "Spec",
            ItemCategoryKind.SidekickOptions => "Option",
            _ => "Item",
        };

        if (isPrepared)
        {
            return $"* {prefix} {itemId}";
        }

        if (isEquipped)
        {
            return $"+ {prefix} {itemId}";
        }

        return $"{prefix} {itemId}";
    }
}
