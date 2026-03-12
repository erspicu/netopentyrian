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

    public int GetEquippedItemId(ItemCategoryKind kind)
    {
        return _equippedItems.TryGetValue(kind, out int itemId) ? itemId : 0;
    }

    public void Equip(ItemCategoryKind kind, int itemId)
    {
        _equippedItems[kind] = itemId;
    }

    public IReadOnlyDictionary<ItemCategoryKind, int> Snapshot()
    {
        return _equippedItems;
    }

    public string BuildSummary()
    {
        return string.Join(
            " ",
            SummaryOrder.Select(kind => $"{ItemNameResolver.GetShortCategoryLabel(kind)}:{GetEquippedItemId(kind)}"));
    }
}
