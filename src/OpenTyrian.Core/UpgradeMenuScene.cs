namespace OpenTyrian.Core;

public sealed class UpgradeMenuScene : IScene
{
    private const int VisibleSubmenuRows = 6;
    private const int CategoryListLeft = 84;
    private const int CategoryListRight = 196;
    private const int CategoryListTop = 96;
    private const int CategoryRowHeight = 12;
    private const int SubmenuLeft = 196;
    private const int SubmenuRight = 316;
    private const int SubmenuTop = 94;
    private const int SubmenuRowHeight = 12;

    private enum UpgradeMenuMode
    {
        CategorySelect,
        ItemSelect,
    }

    private readonly EpisodeSessionState _sessionState;
    private readonly bool _returnToFullGameMenu;
    private readonly int[] _selectedSlots;
    private readonly int[] _preparedItemIds;
    private readonly int[] _preparedWeaponPowers;
    private readonly int[][] _slotWeaponPowers;
    private OpenTyrian.Platform.InputSnapshot _previousInput;
    private int _selectedCategoryIndex;
    private string _statusText;
    private UpgradeMenuMode _mode;

    public UpgradeMenuScene(EpisodeSessionState sessionState, bool returnToFullGameMenu = false)
    {
        _sessionState = sessionState;
        _returnToFullGameMenu = returnToFullGameMenu;
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
        bool pointerConfirmPressed = input.PointerConfirm && !_previousInput.PointerConfirm;
        bool pointerCancelPressed = input.PointerCancel && !_previousInput.PointerCancel;
        ShopCategory? category = GetSelectedCategory();

        if (_mode == UpgradeMenuMode.CategorySelect && input.PointerPresent)
        {
            int? hoveredCategoryRow = HitTestCategoryRow(input.PointerX, input.PointerY, GetCategoryRowCount());
            if (hoveredCategoryRow is int pointerCategoryIndex)
            {
                _selectedCategoryIndex = pointerCategoryIndex;
                category = GetSelectedCategory();
            }
        }
        else if (_mode == UpgradeMenuMode.ItemSelect && category is not null && input.PointerPresent)
        {
            int? hoveredSubmenuRow = HitTestSubmenuRow(category, _selectedSlots[_selectedCategoryIndex], input.PointerX, input.PointerY);
            if (hoveredSubmenuRow is int pointerSlot)
            {
                _selectedSlots[_selectedCategoryIndex] = pointerSlot;
            }
        }

        if (cancelPressed || pointerCancelPressed)
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
            return CreateReturnScene();
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
                if (CommitPreparedSelection(category, resources.ItemCatalog))
                {
                    _mode = UpgradeMenuMode.CategorySelect;
                }
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

                    int preparedPower = _preparedWeaponPowers[_selectedCategoryIndex];
                    int cashAfter = _sessionState.GetCashAfterTransaction(category.Kind, itemId, preparedPower, resources.ItemCatalog);
                    _statusText = cashAfter >= 0
                        ? string.Format(
                            "Prepared {0}  cash after {1}",
                            BuildPreparedSelectionLabel(category.Kind, itemId, preparedPower, resources.ItemCatalog),
                            cashAfter)
                        : string.Format(
                            "Prepared {0}  need {1} more",
                            BuildPreparedSelectionLabel(category.Kind, itemId, preparedPower, resources.ItemCatalog),
                            -cashAfter);
                }
            }
        }
        else if (pointerConfirmPressed)
        {
            if (_mode == UpgradeMenuMode.CategorySelect)
            {
                int? hoveredCategoryRow = HitTestCategoryRow(input.PointerX, input.PointerY, GetCategoryRowCount());
                if (hoveredCategoryRow is int pointerCategoryIndex)
                {
                    _selectedCategoryIndex = pointerCategoryIndex;
                    category = GetSelectedCategory();
                    if (category is null)
                    {
                        _previousInput = input;
                        return CreateReturnScene();
                    }

                    SyncSelectedSlotToPrepared(category);
                    _mode = UpgradeMenuMode.ItemSelect;
                    _statusText = $"{category.DisplayName} submenu opened";
                }
            }
            else if (category is not null)
            {
                int? hoveredSubmenuRow = HitTestSubmenuRow(category, _selectedSlots[_selectedCategoryIndex], input.PointerX, input.PointerY);
                if (hoveredSubmenuRow is int pointerSlot)
                {
                    _selectedSlots[_selectedCategoryIndex] = pointerSlot;
                    if (IsDoneRow(category, pointerSlot))
                    {
                        if (CommitPreparedSelection(category, resources.ItemCatalog))
                        {
                            _mode = UpgradeMenuMode.CategorySelect;
                        }
                    }
                    else
                    {
                        int itemId = category.ItemIds[pointerSlot];
                        _preparedItemIds[_selectedCategoryIndex] = itemId;
                        if (ItemPriceCalculator.IsWeaponCategory(category.Kind))
                        {
                            _preparedWeaponPowers[_selectedCategoryIndex] = GetSelectedWeaponPower(category, pointerSlot);
                        }
                        else
                        {
                            _preparedWeaponPowers[_selectedCategoryIndex] = 0;
                        }

                        int preparedPower = _preparedWeaponPowers[_selectedCategoryIndex];
                        int cashAfter = _sessionState.GetCashAfterTransaction(category.Kind, itemId, preparedPower, resources.ItemCatalog);
                        _statusText = cashAfter >= 0
                            ? string.Format(
                                "Prepared {0}  cash after {1}",
                                BuildPreparedSelectionLabel(category.Kind, itemId, preparedPower, resources.ItemCatalog),
                                cashAfter)
                            : string.Format(
                                "Prepared {0}  need {1} more",
                                BuildPreparedSelectionLabel(category.Kind, itemId, preparedPower, resources.ItemCatalog),
                                -cashAfter);
                    }
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

        resources.FontRenderer.DrawShadowText(surface, 160, 78, "Upgrade Shop", FontKind.Normal, FontAlignment.Center, 15, 0, black: false, shadowDistance: 1);
        resources.FontRenderer.DrawText(
            surface,
            160,
            90,
            string.Format(
                "cash:{0} assets:{1} total:{2}",
                _sessionState.Cash,
                _sessionState.GetTotalAssetValue(resources.ItemCatalog),
                _sessionState.GetTotalScore(resources.ItemCatalog)),
            FontKind.Tiny,
            FontAlignment.Center,
            14,
            0,
            shadow: true);

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
            resources.FontRenderer.DrawText(surface, 228, 156, $"cash: {_sessionState.Cash}", FontKind.Tiny, FontAlignment.Center, 13, 0, shadow: true);
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
        int tradeInValue = _sessionState.GetTradeInValue(selectedCategory.Kind, resources.ItemCatalog);
        int transactionDelta = _sessionState.GetTransactionCostDelta(selectedCategory.Kind, selectedItemId, selectedWeaponPower, resources.ItemCatalog);
        int cashAfter = _sessionState.GetCashAfterTransaction(selectedCategory.Kind, selectedItemId, selectedWeaponPower, resources.ItemCatalog);
        bool affordable = cashAfter >= 0;
        int categoryBudget = _sessionState.Cash + tradeInValue;

        resources.FontRenderer.DrawText(surface, 228, 110, selectedCategory.DisplayName, FontKind.Small, FontAlignment.Center, 14, 1, shadow: true);
        resources.FontRenderer.DrawText(surface, 228, 132, $"row index: {selectedCategory.AvailabilityRowIndex + 1}", FontKind.Tiny, FontAlignment.Center, 13, 0, shadow: true);
        resources.FontRenderer.DrawText(surface, 228, 144, $"slot: {selectedSlot + 1}/{Math.Max(1, selectedCategory.ItemCount + 1)}", FontKind.Tiny, FontAlignment.Center, 13, 0, shadow: true);
        resources.FontRenderer.DrawText(surface, 228, 156, selectedDoneRow ? "Done" : BuildPreparedSelectionLabel(selectedCategory.Kind, selectedItemId, selectedWeaponPower, resources.ItemCatalog), FontKind.Tiny, FontAlignment.Center, affordable ? (byte)15 : (byte)7, 0, shadow: true);
        resources.FontRenderer.DrawText(surface, 228, 168, $"prepared: {BuildPreparedSelectionLabel(selectedCategory.Kind, preparedItemId, preparedWeaponPower, resources.ItemCatalog)} equipped: {BuildPreparedSelectionLabel(selectedCategory.Kind, equippedItemId, equippedWeaponPower, resources.ItemCatalog)}", FontKind.Tiny, FontAlignment.Center, 13, 0, shadow: true);
        resources.FontRenderer.DrawText(surface, 228, 180, BuildCostSummary(selectedCategory.Kind, selectedItemId, selectedWeaponPower, baseCost, totalValue, downgradeValue, upgradeCost), FontKind.Tiny, FontAlignment.Center, 13, 0, shadow: true);
        resources.FontRenderer.DrawText(surface, 228, 192, BuildCashSummary(tradeInValue, transactionDelta, cashAfter, categoryBudget), FontKind.Tiny, FontAlignment.Center, affordable ? (byte)13 : (byte)7, 0, shadow: true);

        RenderItemSubmenu(surface, resources.FontRenderer, resources.ItemCatalog, selectedCategory, selectedSlot, preparedItemId, equippedItemId, _mode == UpgradeMenuMode.ItemSelect, _slotWeaponPowers[_selectedCategoryIndex], categoryBudget);

        resources.FontRenderer.DrawText(surface, 160, 204, _statusText, FontKind.Tiny, FontAlignment.Center, 12, 0, shadow: true);
        resources.FontRenderer.DrawDark(surface, 160, 216, BuildFooterText(), FontKind.Tiny, FontAlignment.Center, black: false);
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

    private bool CommitPreparedSelection(ShopCategory category, ItemCatalog? itemCatalog)
    {
        int preparedItemId = _preparedItemIds[_selectedCategoryIndex];
        int equippedItemId = _sessionState.PlayerLoadout.GetEquippedItemId(category.Kind);
        int preparedWeaponPower = _preparedWeaponPowers[_selectedCategoryIndex];
        int equippedWeaponPower = _sessionState.PlayerLoadout.GetWeaponPower(category.Kind);
        int cashAfter = _sessionState.GetCashAfterTransaction(category.Kind, preparedItemId, preparedWeaponPower, itemCatalog);
        if (!_sessionState.TryCommitShopSelection(category.Kind, preparedItemId, preparedWeaponPower, itemCatalog))
        {
            _statusText = string.Format("{0} needs {1} more cash", category.DisplayName, -cashAfter);
            return false;
        }

        _statusText = preparedItemId == equippedItemId && preparedWeaponPower == equippedWeaponPower
            ? $"{category.DisplayName} unchanged"
            : $"Equipped {BuildPreparedSelectionLabel(category.Kind, preparedItemId, preparedWeaponPower, itemCatalog)}  cash {_sessionState.Cash}";
        InitializeCategoryState(_selectedCategoryIndex, category);
        return true;
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

        if (delta > 0 && !_sessionState.CanAffordTransaction(category.Kind, itemId, nextPower, itemCatalog))
        {
            int cashAfter = _sessionState.GetCashAfterTransaction(category.Kind, itemId, nextPower, itemCatalog);
            _statusText = string.Format("{0} needs {1} more cash", ItemNameResolver.GetCompactItemName(category.Kind, itemId, itemCatalog), -cashAfter);
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
        int[] slotWeaponPowers,
        int categoryBudget)
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
                bool affordable = ItemPriceCalculator.GetItemValue(selectedCategory.Kind, itemId, power, itemCatalog) <= categoryBudget;
                label = BuildItemLabel(itemCatalog, selectedCategory, itemId, power, isPrepared, isEquipped, affordable);
                if (isSelected)
                {
                    hue = affordable ? (byte)15 : (byte)7;
                    value = 4;
                }
                else if (!affordable && !isPrepared && !isEquipped)
                {
                    hue = 8;
                    value = -2;
                }
                else
                {
                    hue = isPrepared ? (byte)14 : isEquipped ? (byte)12 : (byte)13;
                    value = isPrepared ? 2 : isEquipped ? 1 : 0;
                }
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

    private IScene CreateReturnScene()
    {
        return _returnToFullGameMenu
            ? new FullGameMenuScene(_sessionState)
            : new EpisodeSessionScene(_sessionState);
    }

    private string BuildFooterText()
    {
        return _mode == UpgradeMenuMode.CategorySelect
            ? "Up/Down or mouse category  Enter/click open  Esc/right-click back"
            : "Up/Down or mouse item  Left/Right power  Enter/click prepare  Esc/right-click revert";
    }

    private static int? HitTestCategoryRow(int x, int y, int rowCount)
    {
        if (x < CategoryListLeft || x > CategoryListRight || rowCount <= 0)
        {
            return null;
        }

        for (int i = 0; i < rowCount; i++)
        {
            int top = CategoryListTop + (i * CategoryRowHeight);
            int bottom = top + CategoryRowHeight - 2;
            if (y >= top && y <= bottom)
            {
                return i;
            }
        }

        return null;
    }

    private static int? HitTestSubmenuRow(ShopCategory category, int selectedSlot, int x, int y)
    {
        if (x < SubmenuLeft || x > SubmenuRight)
        {
            return null;
        }

        int totalRows = Math.Max(1, category.ItemCount + 1);
        int visibleCount = Math.Min(VisibleSubmenuRows, totalRows);
        int windowStart = GetWindowStart(selectedSlot, totalRows, visibleCount);

        for (int i = 0; i < visibleCount; i++)
        {
            int top = SubmenuTop + (i * SubmenuRowHeight);
            int bottom = top + SubmenuRowHeight - 2;
            if (y >= top && y <= bottom)
            {
                return windowStart + i;
            }
        }

        return null;
    }

    private static string BuildItemLabel(ItemCatalog? itemCatalog, ShopCategory category, int itemId, int weaponPower, bool isPrepared, bool isEquipped, bool affordable)
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

        if (!affordable)
        {
            return $"- {itemName}";
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

    private static string BuildCashSummary(int tradeInValue, int transactionDelta, int cashAfter, int categoryBudget)
    {
        return string.Format(
            "trade:{0} delta:{1} cash:{2} budget:{3}",
            tradeInValue,
            transactionDelta,
            cashAfter,
            categoryBudget);
    }
}
