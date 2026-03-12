namespace OpenTyrian.Core;

public sealed class PlayerLoadoutState
{
    private static readonly ItemCategoryKind[] SummaryOrder =
    [
        ItemCategoryKind.Ship,
        ItemCategoryKind.FrontWeapon,
        ItemCategoryKind.RearWeapon,
        ItemCategoryKind.Shield,
        ItemCategoryKind.Generator,
        ItemCategoryKind.SidekickLeft,
        ItemCategoryKind.SidekickRight,
    ];

    private readonly Dictionary<ItemCategoryKind, int> _equippedItems = new();
    private readonly Dictionary<ItemCategoryKind, int> _weaponPowers = new();

    public int GetEquippedItemId(ItemCategoryKind kind)
    {
        return _equippedItems.TryGetValue(kind, out int itemId) ? itemId : 0;
    }

    public void Equip(ItemCategoryKind kind, int itemId)
    {
        _equippedItems[kind] = itemId;

        if (!ItemPriceCalculator.IsWeaponCategory(kind))
        {
            _weaponPowers.Remove(kind);
            return;
        }

        if (itemId == 0)
        {
            _weaponPowers[kind] = 0;
            return;
        }

        int currentPower = GetWeaponPower(kind);
        _weaponPowers[kind] = ItemPriceCalculator.ClampWeaponPower(itemId, currentPower);
    }

    public int GetWeaponPower(ItemCategoryKind kind)
    {
        if (!ItemPriceCalculator.IsWeaponCategory(kind))
        {
            return 0;
        }

        int itemId = GetEquippedItemId(kind);
        if (itemId == 0)
        {
            return 0;
        }

        int power;
        if (!_weaponPowers.TryGetValue(kind, out power))
        {
            return 1;
        }

        return ItemPriceCalculator.ClampWeaponPower(itemId, power);
    }

    public void SetWeaponPower(ItemCategoryKind kind, int power)
    {
        if (!ItemPriceCalculator.IsWeaponCategory(kind))
        {
            return;
        }

        int itemId = GetEquippedItemId(kind);
        _weaponPowers[kind] = ItemPriceCalculator.ClampWeaponPower(itemId, power);
    }

    public IDictionary<ItemCategoryKind, int> Snapshot()
    {
        return _equippedItems;
    }

    public string BuildSummary()
    {
        return string.Join(
            " ",
            SummaryOrder.Select(BuildSummaryPart));
    }

    private string BuildSummaryPart(ItemCategoryKind kind)
    {
        int itemId = GetEquippedItemId(kind);
        if (ItemPriceCalculator.IsWeaponCategory(kind))
        {
            return string.Format(
                "{0}:{1}x{2}",
                ItemNameResolver.GetShortCategoryLabel(kind),
                itemId,
                GetWeaponPower(kind));
        }

        return string.Format("{0}:{1}", ItemNameResolver.GetShortCategoryLabel(kind), itemId);
    }
}
