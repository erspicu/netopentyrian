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
    private readonly int[] _preparedItemIds;
    private readonly int[] _preparedWeaponPowers;
    private readonly int[][] _slotWeaponPowers;
    private OpenTyrian.Platform.InputSnapshot _previousInput;
    private int _selectedCategoryIndex;
    private string _statusText;
    private UpgradeMenuMode _mode;

    public UpgradeMenuScene(EpisodeSessionState sessionState)
    {
        _sessionState = sessionState;
        _selectedSlots = new int[Math.Max(1, sessionState.ShopCategories.Count)];
        _preparedItemIds = new int[Math.Max(1, sessionState.ShopCategories.Count)];
        _preparedWeaponPowers = new int[Math.Max(1, sessionState.ShopCategories.Count)];
        _slotWeaponPowers = new int[Math.Max(1, sessionState.ShopCategories.Count)][];
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
        ShopCategory? category = GetSelectedCategory();

        if (cancelPressed)
        {
            if (_mode == UpgradeMenuMode.ItemSelect)
            {
                if (category is not null)
                {
                    RevertPreparedSelection(category);
                    _statusText = $"{category.DisplayName} changes discarded";
                }

                _mode = UpgradeMenuMode.CategorySelect;
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

        if (_mode == UpgradeMenuMode.CategorySelect)
        {
            if (upPressed)
            {
                _selectedCategoryIndex = _selectedCategoryIndex == 0
                    ? GetCategoryRowCount() - 1
                    : _selectedCategoryIndex - 1;
                category = GetSelectedCategory();
            }

            if (downPressed)
            {
                _selectedCategoryIndex = (_selectedCategoryIndex + 1) % GetCategoryRowCount();
                category = GetSelectedCategory();
            }
        }
        else if (category is not null)
        {
            int selectableCount = Math.Max(1, category.ItemCount + 1);

            if (upPressed)
            {
                _selectedSlots[_selectedCategoryIndex] = _selectedSlots[_selectedCategoryIndex] == 0
                    ? selectableCount - 1
                    : _selectedSlots[_selectedCategoryIndex] - 1;
            }

            if (downPressed)
            {
                _selectedSlots[_selectedCategoryIndex] = (_selectedSlots[_selectedCategoryIndex] + 1) % selectableCount;
            }

            if (leftPressed)
            {
                TryAdjustSelectedWeaponPower(category, -1, resources.ItemCatalog);
            }

            if (rightPressed)
            {
                TryAdjustSelectedWeaponPower(category, 1, resources.ItemCatalog);
            }
        }

        if (confirmPressed)
        {
            if (_mode == UpgradeMenuMode.CategorySelect)
            {
                if (category is null)
                {
                    _previousInput = input;
                    return new EpisodeSessionScene(_sessionState);
                }

                SyncSelectedSlotToPrepared(category);
                _mode = UpgradeMenuMode.ItemSelect;
                _statusText = $"{category.DisplayName} submenu opened";
            }
            else if (category is not null && IsDoneRow(category, _selectedSlots[_selectedCategoryIndex]))
            {
                CommitPreparedSelection(category, resources.ItemCatalog);
                _mode = UpgradeMenuMode.CategorySelect;
            }
            else
            {
                if (category is not null)
                {
                    int itemId = category.ItemCount > 0 ? category.ItemIds[_selectedSlots[_selectedCategoryIndex]] : 0;
                    _preparedItemIds[_selectedCategoryIndex] = itemId;
                    if (ItemPriceCalculator.IsWeaponCategory(category.Kind))
                    {
                        _preparedWeaponPowers[_selectedCategoryIndex] = GetSelectedWeaponPower(category, _selectedSlots[_selectedCategoryIndex]);
                    }
                    else
                    {
                        _preparedWeaponPowers[_selectedCategoryIndex] = 0;
                    }

                    _statusText = string.Format(
                        "Prepared {0}",
                        BuildPreparedSelectionLabel(category.Kind, itemId, _preparedWeaponPowers[_selectedCategoryIndex], resources.ItemCatalog));
                }
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

        for (int i = 0; i < GetCategoryRowCount(); i++)
        {
            bool selected = i == _selectedCategoryIndex;
            int y = 100 + (i * 12);
            string label;

            if (i >= _sessionState.ShopCategories.Count)
            {
                label = "Done";
            }
            else
            {
                ShopCategory category = _sessionState.ShopCategories[i];
                int equippedSummaryId = _sessionState.PlayerLoadout.GetEquippedItemId(category.Kind);
                int preparedSummaryId = _preparedItemIds[i];
                int equippedSummaryPower = _sessionState.PlayerLoadout.GetWeaponPower(category.Kind);
                int preparedSummaryPower = _preparedWeaponPowers[i];
                label = $"{category.DisplayName} ({category.ItemCount}) [{BuildCategorySummary(category.Kind, equippedSummaryId, preparedSummaryId, equippedSummaryPower, preparedSummaryPower, resources.ItemCatalog)}]";
                if (_mode == UpgradeMenuMode.ItemSelect && selected)
                {
                    label += " [open]";
                }
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

        ShopCategory? selectedCategory = GetSelectedCategory();
        if (selectedCategory is null)
        {
            resources.FontRenderer.DrawText(surface, 228, 120, "Return to episode session", FontKind.Small, FontAlignment.Center, 14, 1, shadow: true);
            resources.FontRenderer.DrawText(surface, 228, 144, $"loadout: {_sessionState.PlayerLoadout.BuildSummary()}", FontKind.Tiny, FontAlignment.Center, 13, 0, shadow: true);
            resources.FontRenderer.DrawText(surface, 160, 192, _statusText, FontKind.Tiny, FontAlignment.Center, 12, 0, shadow: true);
            resources.FontRenderer.DrawDark(surface, 160, 204, BuildFooterText(), FontKind.Tiny, FontAlignment.Center, black: false);
            return;
        }

        int selectedSlot = _selectedSlots[_selectedCategoryIndex];
        bool selectedDoneRow = IsDoneRow(selectedCategory, selectedSlot);
        int selectedItemId = selectedCategory.ItemCount > 0 && !selectedDoneRow ? selectedCategory.ItemIds[selectedSlot] : 0;
        int preparedItemId = _preparedItemIds[_selectedCategoryIndex];
        int preparedWeaponPower = _preparedWeaponPowers[_selectedCategoryIndex];
        int equippedItemId = _sessionState.PlayerLoadout.GetEquippedItemId(selectedCategory.Kind);
        int equippedWeaponPower = _sessionState.PlayerLoadout.GetWeaponPower(selectedCategory.Kind);
        int selectedWeaponPower = GetSelectedWeaponPower(selectedCategory, selectedSlot);
        int baseCost = ItemPriceCalculator.GetBaseCost(selectedCategory.Kind, selectedItemId, resources.ItemCatalog);
        int totalValue = ItemPriceCalculator.GetItemValue(selectedCategory.Kind, selectedItemId, selectedWeaponPower, resources.ItemCatalog);
        int upgradeCost = ItemPriceCalculator.GetWeaponUpgradeCost(selectedCategory.Kind, selectedItemId, selectedWeaponPower, resources.ItemCatalog);
        int downgradeValue = ItemPriceCalculator.GetWeaponDowngradeValue(selectedCategory.Kind, selectedItemId, selectedWeaponPower, resources.ItemCatalog);

        resources.FontRenderer.DrawText(surface, 228, 110, selectedCategory.DisplayName, FontKind.Small, FontAlignment.Center, 14, 1, shadow: true);
        resources.FontRenderer.DrawText(surface, 228, 132, $"row index: {selectedCategory.AvailabilityRowIndex + 1}", FontKind.Tiny, FontAlignment.Center, 13, 0, shadow: true);
        resources.FontRenderer.DrawText(surface, 228, 144, $"slot: {selectedSlot + 1}/{Math.Max(1, selectedCategory.ItemCount + 1)}", FontKind.Tiny, FontAlignment.Center, 13, 0, shadow: true);
        resources.FontRenderer.DrawText(surface, 228, 156, selectedDoneRow ? "Done" : BuildPreparedSelectionLabel(selectedCategory.Kind, selectedItemId, selectedWeaponPower, resources.ItemCatalog), FontKind.Tiny, FontAlignment.Center, 15, 0, shadow: true);
        resources.FontRenderer.DrawText(surface, 228, 168, $"prepared: {BuildPreparedSelectionLabel(selectedCategory.Kind, preparedItemId, preparedWeaponPower, resources.ItemCatalog)} equipped: {BuildPreparedSelectionLabel(selectedCategory.Kind, equippedItemId, equippedWeaponPower, resources.ItemCatalog)}", FontKind.Tiny, FontAlignment.Center, 13, 0, shadow: true);
        resources.FontRenderer.DrawText(surface, 228, 180, BuildCostSummary(selectedCategory.Kind, selectedItemId, selectedWeaponPower, baseCost, totalValue, downgradeValue, upgradeCost), FontKind.Tiny, FontAlignment.Center, 13, 0, shadow: true);

        RenderItemSubmenu(surface, resources.FontRenderer, resources.ItemCatalog, selectedCategory, selectedSlot, preparedItemId, equippedItemId, _mode == UpgradeMenuMode.ItemSelect, _slotWeaponPowers[_selectedCategoryIndex]);

        resources.FontRenderer.DrawText(surface, 160, 192, _statusText, FontKind.Tiny, FontAlignment.Center, 12, 0, shadow: true);
        resources.FontRenderer.DrawDark(surface, 160, 204, BuildFooterText(), FontKind.Tiny, FontAlignment.Center, black: false);
    }

    private void InitializeSlotsFromLoadout()
    {
        for (int i = 0; i < _sessionState.ShopCategories.Count; i++)
        {
            InitializeCategoryState(i, _sessionState.ShopCategories[i]);
        }
    }

    private int GetCategoryRowCount()
    {
        return _sessionState.ShopCategories.Count + 1;
    }

    private ShopCategory? GetSelectedCategory()
    {
        return _selectedCategoryIndex >= 0 && _selectedCategoryIndex < _sessionState.ShopCategories.Count
            ? _sessionState.ShopCategories[_selectedCategoryIndex]
            : null;
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

    private void InitializeCategoryState(int categoryIndex, ShopCategory category)
    {
        int equippedItemId = _sessionState.PlayerLoadout.GetEquippedItemId(category.Kind);
        int equippedSlot = FindItemSlot(category, equippedItemId);
        int equippedPower = _sessionState.PlayerLoadout.GetWeaponPower(category.Kind);

        _selectedSlots[categoryIndex] = equippedSlot >= 0 ? equippedSlot : 0;
        _preparedItemIds[categoryIndex] = equippedItemId;
        _preparedWeaponPowers[categoryIndex] = equippedPower;
        _slotWeaponPowers[categoryIndex] = BuildSlotWeaponPowers(category, equippedSlot, equippedPower);
    }

    private static int[] BuildSlotWeaponPowers(ShopCategory category, int equippedSlot, int equippedPower)
    {
        int[] slotPowers = new int[Math.Max(1, category.ItemCount + 1)];
        if (!ItemPriceCalculator.IsWeaponCategory(category.Kind))
        {
            return slotPowers;
        }

        for (int i = 0; i < category.ItemCount; i++)
        {
            slotPowers[i] = category.ItemIds[i] == 0 ? 0 : 1;
        }

        if (equippedSlot >= 0 && equippedSlot < category.ItemCount && category.ItemIds[equippedSlot] != 0)
        {
            slotPowers[equippedSlot] = equippedPower > 0 ? equippedPower : 1;
        }

        return slotPowers;
    }

    private void SyncSelectedSlotToPrepared(ShopCategory category)
    {
        int preparedSlot = FindItemSlot(category, _preparedItemIds[_selectedCategoryIndex]);
        _selectedSlots[_selectedCategoryIndex] = preparedSlot >= 0 ? preparedSlot : 0;
    }

    private void RevertPreparedSelection(ShopCategory category)
    {
        InitializeCategoryState(_selectedCategoryIndex, category);
    }

    private void CommitPreparedSelection(ShopCategory category, ItemCatalog? itemCatalog)
    {
        int preparedItemId = _preparedItemIds[_selectedCategoryIndex];
        int equippedItemId = _sessionState.PlayerLoadout.GetEquippedItemId(category.Kind);
        int preparedWeaponPower = _preparedWeaponPowers[_selectedCategoryIndex];
        int equippedWeaponPower = _sessionState.PlayerLoadout.GetWeaponPower(category.Kind);
        _sessionState.EquipShopItem(category.Kind, preparedItemId);
        if (ItemPriceCalculator.IsWeaponCategory(category.Kind))
        {
            _sessionState.SetWeaponPower(category.Kind, preparedWeaponPower);
        }

        _statusText = preparedItemId == equippedItemId && preparedWeaponPower == equippedWeaponPower
            ? $"{category.DisplayName} unchanged"
            : $"Equipped {BuildPreparedSelectionLabel(category.Kind, preparedItemId, preparedWeaponPower, itemCatalog)}";
        InitializeCategoryState(_selectedCategoryIndex, category);
    }

    private int GetSelectedWeaponPower(ShopCategory category, int slotIndex)
    {
        if (!ItemPriceCalculator.IsWeaponCategory(category.Kind) || IsDoneRow(category, slotIndex))
        {
            return 0;
        }

        if (slotIndex < 0 || slotIndex >= category.ItemCount)
        {
            return 0;
        }

        int itemId = category.ItemIds[slotIndex];
        if (itemId == 0)
        {
            return 0;
        }

        int[] slotPowers = _slotWeaponPowers[_selectedCategoryIndex];
        if (slotPowers is null || slotIndex >= slotPowers.Length)
        {
            return 1;
        }

        return ItemPriceCalculator.ClampWeaponPower(itemId, slotPowers[slotIndex]);
    }

    private void TryAdjustSelectedWeaponPower(ShopCategory category, int delta, ItemCatalog? itemCatalog)
    {
        if (!ItemPriceCalculator.IsWeaponCategory(category.Kind))
        {
            return;
        }

        int selectedSlot = _selectedSlots[_selectedCategoryIndex];
        if (IsDoneRow(category, selectedSlot) || selectedSlot < 0 || selectedSlot >= category.ItemCount)
        {
            return;
        }

        int itemId = category.ItemIds[selectedSlot];
        if (itemId == 0)
        {
            return;
        }

        int[] slotPowers = _slotWeaponPowers[_selectedCategoryIndex];
        int currentPower = GetSelectedWeaponPower(category, selectedSlot);
        int nextPower = ItemPriceCalculator.ClampWeaponPower(itemId, currentPower + delta);
        if (currentPower == nextPower)
        {
            return;
        }

        slotPowers[selectedSlot] = nextPower;
        if (_preparedItemIds[_selectedCategoryIndex] == itemId)
        {
            _preparedWeaponPowers[_selectedCategoryIndex] = nextPower;
        }

        _statusText = string.Format(
            "{0} power {1}",
            ItemNameResolver.GetCompactItemName(category.Kind, itemId, itemCatalog),
            nextPower);
    }

    private static bool IsDoneRow(ShopCategory category, int slotIndex)
    {
        return slotIndex >= category.ItemCount;
    }

    private static void RenderItemSubmenu(
        IndexedFrameBuffer surface,
        TyrianFontRenderer fontRenderer,
        ItemCatalog? itemCatalog,
        ShopCategory selectedCategory,
        int selectedSlot,
        int preparedItemId,
        int equippedItemId,
        bool submenuOpen,
        int[] slotWeaponPowers)
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
                bool isPrepared = preparedItemId == itemId;
                bool isEquipped = equippedItemId == itemId;
                int power = slotWeaponPowers is not null && rowIndex < slotWeaponPowers.Length ? slotWeaponPowers[rowIndex] : 0;
                label = BuildItemLabel(itemCatalog, selectedCategory, itemId, power, isPrepared, isEquipped);
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
            ? "Up/Down category  Enter open/done  Esc back"
            : "Up/Down item  Left/Right power  Enter prepare/done  Esc revert";
    }

    private static string BuildItemLabel(ItemCatalog? itemCatalog, ShopCategory category, int itemId, int weaponPower, bool isPrepared, bool isEquipped)
    {
        string itemName = BuildPreparedSelectionLabel(category.Kind, itemId, weaponPower, itemCatalog);

        if (isPrepared)
        {
            return $"* {itemName}";
        }

        if (isEquipped)
        {
            return $"+ {itemName}";
        }

        return itemName;
    }

    private static string BuildCategorySummary(ItemCategoryKind kind, int equippedItemId, int preparedItemId, int equippedWeaponPower, int preparedWeaponPower, ItemCatalog? itemCatalog)
    {
        string equippedName = BuildPreparedSelectionLabel(kind, equippedItemId, equippedWeaponPower, itemCatalog);
        if (preparedItemId == equippedItemId && preparedWeaponPower == equippedWeaponPower)
        {
            return equippedName;
        }

        string preparedName = BuildPreparedSelectionLabel(kind, preparedItemId, preparedWeaponPower, itemCatalog);
        return $"{equippedName} -> {preparedName}";
    }

    private static string BuildPreparedSelectionLabel(ItemCategoryKind kind, int itemId, int weaponPower, ItemCatalog? itemCatalog)
    {
        string itemName = ItemNameResolver.GetCompactItemName(kind, itemId, itemCatalog);
        if (!ItemPriceCalculator.IsWeaponCategory(kind) || itemId == 0)
        {
            return itemName;
        }

        return string.Format("{0} x{1}", itemName, ItemPriceCalculator.ClampWeaponPower(itemId, weaponPower));
    }

    private static string BuildCostSummary(ItemCategoryKind kind, int itemId, int weaponPower, int baseCost, int totalValue, int downgradeValue, int upgradeCost)
    {
        if (itemId == 0)
        {
            return "cost: 0";
        }

        if (!ItemPriceCalculator.IsWeaponCategory(kind))
        {
            return string.Format("cost: {0}", baseCost);
        }

        return string.Format(
            "base:{0} value:{1} pwr:{2} -{3} +{4}",
            baseCost,
            totalValue,
            weaponPower,
            downgradeValue,
            upgradeCost);
    }
}
