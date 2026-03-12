namespace OpenTyrian.Core;

public sealed class MenuItemDefinition
{
    public required string Id { get; init; }

    public required string Label { get; init; }

    public required string Description { get; init; }

    public bool IsEnabled { get; init; } = true;
}
