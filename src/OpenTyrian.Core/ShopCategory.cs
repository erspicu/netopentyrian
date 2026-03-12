namespace OpenTyrian.Core;

public sealed class ShopCategory
{
    public required ItemCategoryKind Kind { get; init; }

    public required int AvailabilityRowIndex { get; init; }

    public required string DisplayName { get; init; }

    public required IList<int> ItemIds { get; init; }

    public int ItemCount => ItemIds.Count;
}
