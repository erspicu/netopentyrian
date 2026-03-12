namespace OpenTyrian.Core;

public sealed class ShopCategory
{
    public required ItemCategoryKind Kind { get; init; }

    public required int AvailabilityRowIndex { get; init; }

    public required string DisplayName { get; init; }

    public required IReadOnlyList<int> ItemIds { get; init; }

    public int ItemCount => ItemIds.Count;
}
