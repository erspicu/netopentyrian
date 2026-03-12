namespace OpenTyrian.Core;

public sealed class ItemCatalogEntry
{
    public ItemCatalogEntry(string name, int cost, int primaryStat = 0, int secondaryStat = 0, int spriteId = 0)
    {
        Name = name;
        Cost = cost;
        PrimaryStat = primaryStat;
        SecondaryStat = secondaryStat;
        SpriteId = spriteId;
    }

    public string Name { get; private set; }

    public int Cost { get; private set; }

    public int PrimaryStat { get; private set; }

    public int SecondaryStat { get; private set; }

    public int SpriteId { get; private set; }
}
