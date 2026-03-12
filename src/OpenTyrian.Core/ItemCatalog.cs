namespace OpenTyrian.Core;

public sealed class ItemCatalog
{
    public required IReadOnlyDictionary<int, string> Ships { get; init; }

    public required IReadOnlyDictionary<int, string> WeaponPorts { get; init; }

    public required IReadOnlyDictionary<int, string> Shields { get; init; }

    public required IReadOnlyDictionary<int, string> Generators { get; init; }

    public required IReadOnlyDictionary<int, string> Options { get; init; }

    public required IReadOnlyDictionary<int, string> Specials { get; init; }

    public string? GetName(ItemCategoryKind kind, int itemId)
    {
        if (itemId == 0)
        {
            return "None";
        }

        IReadOnlyDictionary<int, string>? source = kind switch
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

        return source is not null && source.TryGetValue(itemId, out string? name) ? name : null;
    }
}
