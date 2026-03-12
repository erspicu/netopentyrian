namespace OpenTyrian.Core;

public sealed class MainShapeTables
{
    public MainShapeTables(SpriteTable[] tables)
    {
        Tables = tables;
    }

    public SpriteTable[] Tables { get; }

    public int Count => Tables.Length;

    public SpriteTable NormalFont => Tables[(int)FontKind.Normal];

    public SpriteTable SmallFont => Tables[(int)FontKind.Small];

    public SpriteTable TinyFont => Tables[(int)FontKind.Tiny];

    public SpriteTable PlanetShapes => Tables[(int)MainShapeTableKind.Planet];

    public SpriteTable FaceShapes => Tables[(int)MainShapeTableKind.Face];

    public SpriteTable OptionShapes => Tables[(int)MainShapeTableKind.Option];

    public SpriteTable WeaponShapes => Tables[(int)MainShapeTableKind.Weapon];

    public bool HasTable(MainShapeTableKind kind)
    {
        return (int)kind < Tables.Length;
    }

    public SpriteTable GetTable(MainShapeTableKind kind)
    {
        if (!HasTable(kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind), $"Main shape table {kind} is not loaded.");
        }

        return Tables[(int)kind];
    }
}
