namespace OpenTyrian.Core;

public sealed class ItemCatalogEntry
{
    public ItemCatalogEntry(string name, int cost)
    {
        Name = name;
        Cost = cost;
    }

    public string Name { get; private set; }

    public int Cost { get; private set; }
}
