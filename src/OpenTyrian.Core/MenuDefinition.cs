namespace OpenTyrian.Core;

public sealed class MenuDefinition
{
    public required string Title { get; init; }

    public required string Footer { get; init; }

    public required IReadOnlyList<MenuItemDefinition> Items { get; init; }
}
