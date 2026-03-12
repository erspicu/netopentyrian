namespace OpenTyrian.Core;

public sealed class ItemCatalog
{
    public required IDictionary<int, ItemCatalogEntry> Ships { get; init; }

    public required IDictionary<int, ItemCatalogEntry> WeaponPorts { get; init; }

    public required IDictionary<int, ItemCatalogEntry> Shields { get; init; }

    public required IDictionary<int, ItemCatalogEntry> Generators { get; init; }

    public required IDictionary<int, ItemCatalogEntry> Options { get; init; }

    public required IDictionary<int, ItemCatalogEntry> Specials { get; init; }

    public ItemCatalogEntry? GetEntry(ItemCategoryKind kind, int itemId)
    {
        if (itemId == 0)
        {
            return null;
        }

        if (kind == ItemCategoryKind.Ship && itemId > 90)
        {
            return new ItemCatalogEntry(string.Format("Custom Ship {0}", itemId - 90), 100);
        }

        IDictionary<int, ItemCatalogEntry>? source = kind switch
        {
            ItemCategoryKind.Ship => Ships,
            ItemCategoryKind.FrontWeapon => WeaponPorts,
            ItemCategoryKind.RearWeapon => WeaponPorts,
            ItemCategoryKind.Shield => Shields,
            ItemCategoryKind.Generator => Generators,
            ItemCategoryKind.SidekickLeft => Options,
            ItemCategoryKind.SidekickRight => Options,
            ItemCategoryKind.Special => Specials,
            ItemCategoryKind.SidekickOptions => Options,
            _ => null,
        };

        if (source is null)
        {
            return null;
        }

        source.TryGetValue(itemId, out ItemCatalogEntry? entry);
        return entry;
    }

    public string? GetName(ItemCategoryKind kind, int itemId)
    {
        if (itemId == 0)
        {
            return "None";
        }

        ItemCatalogEntry? entry = GetEntry(kind, itemId);
        return entry is not null ? entry.Name : null;
    }

    public int GetCost(ItemCategoryKind kind, int itemId)
    {
        ItemCatalogEntry? entry = GetEntry(kind, itemId);
        return entry is not null ? entry.Cost : 0;
    }
}
