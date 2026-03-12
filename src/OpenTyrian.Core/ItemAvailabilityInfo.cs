namespace OpenTyrian.Core;

public sealed class ItemAvailabilityInfo
{
    public const int CategoryCount = 9;
    public const int MaxItemsPerCategory = 10;
    private static readonly ItemCategoryKind[] UpgradeCategoryMap =
    [
        ItemCategoryKind.Ship,
        ItemCategoryKind.FrontWeapon,
        ItemCategoryKind.RearWeapon,
        ItemCategoryKind.Shield,
        ItemCategoryKind.Generator,
        ItemCategoryKind.SidekickLeft,
        ItemCategoryKind.SidekickRight,
    ];

    public required IReadOnlyList<IReadOnlyList<int>> Rows { get; init; }

    public required IReadOnlyList<int> MaxPerRow { get; init; }

    public IReadOnlyList<ShopCategory> ShopCategories => BuildShopCategories();

    public int GetValue(int rowIndex, int slotIndex)
    {
        if (rowIndex < 0 || rowIndex >= Rows.Count)
        {
            return 0;
        }

        IReadOnlyList<int> row = Rows[rowIndex];
        return slotIndex >= 0 && slotIndex < row.Count ? row[slotIndex] : 0;
    }

    public IReadOnlyList<int> GetRow(ItemCategoryKind kind)
    {
        int rowIndex = GetRowIndex(kind);
        return rowIndex >= 0 && rowIndex < Rows.Count ? Rows[rowIndex] : Array.Empty<int>();
    }

    public int GetMax(ItemCategoryKind kind)
    {
        int rowIndex = GetRowIndex(kind);
        return rowIndex >= 0 && rowIndex < MaxPerRow.Count ? MaxPerRow[rowIndex] : 0;
    }

    private IReadOnlyList<ShopCategory> BuildShopCategories()
    {
        List<ShopCategory> categories = new(UpgradeCategoryMap.Length);
        foreach (ItemCategoryKind kind in UpgradeCategoryMap)
        {
            int rowIndex = GetRowIndex(kind);
            categories.Add(new ShopCategory
            {
                Kind = kind,
                AvailabilityRowIndex = rowIndex,
                DisplayName = GetDisplayName(kind),
                ItemIds = GetRow(kind),
            });
        }

        return categories;
    }

    private static int GetRowIndex(ItemCategoryKind kind)
    {
        return kind switch
        {
            ItemCategoryKind.Ship => 0,
            ItemCategoryKind.FrontWeapon => 1,
            ItemCategoryKind.RearWeapon => 2,
            ItemCategoryKind.Generator => 3,
            ItemCategoryKind.SidekickLeft => 5,
            ItemCategoryKind.SidekickRight => 6,
            ItemCategoryKind.Miscellaneous => 7,
            ItemCategoryKind.Shield => 8,
            ItemCategoryKind.Special => 4,
            ItemCategoryKind.SidekickOptions => 8,
            _ => -1,
        };
    }

    private static string GetDisplayName(ItemCategoryKind kind)
    {
        return kind switch
        {
            ItemCategoryKind.Ship => "Ship",
            ItemCategoryKind.FrontWeapon => "Front Weapon",
            ItemCategoryKind.RearWeapon => "Rear Weapon",
            ItemCategoryKind.Shield => "Shield",
            ItemCategoryKind.SidekickLeft => "Left Sidekick",
            ItemCategoryKind.SidekickRight => "Right Sidekick",
            ItemCategoryKind.Generator => "Generator",
            ItemCategoryKind.Special => "Special",
            ItemCategoryKind.SidekickOptions => "Sidekick Option",
            ItemCategoryKind.Miscellaneous => "Misc",
            _ => kind.ToString(),
        };
    }
}
