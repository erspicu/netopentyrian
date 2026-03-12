namespace OpenTyrian.Core;

public static class ItemNameResolver
{
    public static string GetShortCategoryLabel(ItemCategoryKind kind)
    {
        return kind switch
        {
            ItemCategoryKind.Ship => "ship",
            ItemCategoryKind.FrontWeapon => "front",
            ItemCategoryKind.RearWeapon => "rear",
            ItemCategoryKind.Shield => "shield",
            ItemCategoryKind.Generator => "gen",
            ItemCategoryKind.SidekickLeft => "left",
            ItemCategoryKind.SidekickRight => "right",
            ItemCategoryKind.Special => "spec",
            ItemCategoryKind.SidekickOptions => "opt",
            _ => kind.ToString().ToLowerInvariant(),
        };
    }

    public static string GetItemName(ItemCategoryKind kind, int itemId, ItemCatalog? catalog = null)
    {
        if (itemId == 0)
        {
            return "None";
        }

        return catalog?.GetName(kind, itemId) ?? $"{GetCategoryDisplayName(kind)} {itemId}";
    }

    public static string GetCompactItemName(ItemCategoryKind kind, int itemId, ItemCatalog? catalog = null)
    {
        if (itemId == 0)
        {
            return "None";
        }

        string? catalogName = catalog?.GetName(kind, itemId);
        if (!string.IsNullOrWhiteSpace(catalogName))
        {
            return catalogName ?? string.Empty;
        }

        return $"{GetCategoryCompactName(kind)} {itemId}";
    }

    private static string GetCategoryDisplayName(ItemCategoryKind kind)
    {
        return kind switch
        {
            ItemCategoryKind.Ship => "Ship",
            ItemCategoryKind.FrontWeapon => "Front Weapon",
            ItemCategoryKind.RearWeapon => "Rear Weapon",
            ItemCategoryKind.Shield => "Shield",
            ItemCategoryKind.Generator => "Generator",
            ItemCategoryKind.SidekickLeft => "Left Sidekick",
            ItemCategoryKind.SidekickRight => "Right Sidekick",
            ItemCategoryKind.Special => "Special",
            ItemCategoryKind.SidekickOptions => "Sidekick Option",
            _ => "Item",
        };
    }

    private static string GetCategoryCompactName(ItemCategoryKind kind)
    {
        return kind switch
        {
            ItemCategoryKind.Ship => "Ship",
            ItemCategoryKind.FrontWeapon => "Front",
            ItemCategoryKind.RearWeapon => "Rear",
            ItemCategoryKind.Shield => "Shield",
            ItemCategoryKind.Generator => "Gen",
            ItemCategoryKind.SidekickLeft => "Left",
            ItemCategoryKind.SidekickRight => "Right",
            ItemCategoryKind.Special => "Spec",
            ItemCategoryKind.SidekickOptions => "Option",
            _ => "Item",
        };
    }
}
