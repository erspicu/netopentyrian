using OpenTyrian.Platform;

namespace OpenTyrian.Core;

public static class ItemCatalogLoader
{
    private const int WeaponRecordSize = 80;
    private const int WeaponCount = 781;
    private const int WeaponPortCount = 43;
    private const int SpecialCount = 47;
    private const int GeneratorCount = 7;
    private const int ShipCount = 14;
    private const int OptionCount = 31;
    private const int ShieldCount = 11;

    public static ItemCatalog? Load(IAssetLocator assetLocator)
    {
        if (!assetLocator.FileExists("tyrian.hdt"))
        {
            return null;
        }

        using Stream stream = assetLocator.OpenRead("tyrian.hdt");
        using TyrianDataStream data = new(stream, leaveOpen: true);

        int itemDataOffset = data.ReadInt32();
        data.Position = itemDataOffset;

        for (int i = 0; i < 7; i++)
        {
            data.ReadUInt16();
        }

        data.Seek(WeaponRecordSize * WeaponCount, SeekOrigin.Current);

        Dictionary<int, ItemCatalogEntry> weaponPorts = ReadWeaponPorts(data);
        Dictionary<int, ItemCatalogEntry> specials = ReadSpecials(data);
        Dictionary<int, ItemCatalogEntry> generators = ReadGenerators(data);
        Dictionary<int, ItemCatalogEntry> ships = ReadShips(data);
        Dictionary<int, ItemCatalogEntry> options = ReadOptions(data);
        Dictionary<int, ItemCatalogEntry> shields = ReadShields(data);

        return new ItemCatalog
        {
            Ships = ships,
            WeaponPorts = weaponPorts,
            Shields = shields,
            Generators = generators,
            Options = options,
            Specials = specials,
        };
    }

    private static Dictionary<int, ItemCatalogEntry> ReadWeaponPorts(TyrianDataStream data)
    {
        Dictionary<int, ItemCatalogEntry> items = new(WeaponPortCount);
        for (int i = 0; i < WeaponPortCount; i++)
        {
            string name = ReadFixedName(data);
            data.ReadByte();
            data.Seek(44, SeekOrigin.Current);
            int cost = data.ReadUInt16();
            data.Seek(2 + 2, SeekOrigin.Current);
            items[i] = new ItemCatalogEntry(name, cost);
        }

        return items;
    }

    private static Dictionary<int, ItemCatalogEntry> ReadSpecials(TyrianDataStream data)
    {
        Dictionary<int, ItemCatalogEntry> items = new(SpecialCount);
        for (int i = 0; i < SpecialCount; i++)
        {
            string name = ReadFixedName(data);
            data.Seek(2 + 1 + 1 + 2, SeekOrigin.Current);
            items[i] = new ItemCatalogEntry(name, 0);
        }

        return items;
    }

    private static Dictionary<int, ItemCatalogEntry> ReadGenerators(TyrianDataStream data)
    {
        Dictionary<int, ItemCatalogEntry> items = new(GeneratorCount);
        for (int i = 0; i < GeneratorCount; i++)
        {
            string name = ReadFixedName(data);
            data.Seek(2 + 1 + 1, SeekOrigin.Current);
            int cost = data.ReadUInt16();
            items[i] = new ItemCatalogEntry(name, cost);
        }

        return items;
    }

    private static Dictionary<int, ItemCatalogEntry> ReadShips(TyrianDataStream data)
    {
        Dictionary<int, ItemCatalogEntry> items = new(ShipCount);
        for (int i = 0; i < ShipCount; i++)
        {
            string name = ReadFixedName(data);
            data.Seek(2 + 2 + 1 + 1 + 1, SeekOrigin.Current);
            int cost = data.ReadUInt16();
            data.Seek(1, SeekOrigin.Current);
            items[i] = new ItemCatalogEntry(name, cost);
        }

        return items;
    }

    private static Dictionary<int, ItemCatalogEntry> ReadOptions(TyrianDataStream data)
    {
        Dictionary<int, ItemCatalogEntry> items = new(OptionCount);
        for (int i = 0; i < OptionCount; i++)
        {
            string name = ReadFixedName(data);
            data.Seek(1 + 2, SeekOrigin.Current);
            int cost = data.ReadUInt16();
            data.Seek(1 + 1 + 1 + 1 + 40 + 1 + 2 + 1 + 1 + 1, SeekOrigin.Current);
            items[i] = new ItemCatalogEntry(name, cost);
        }

        return items;
    }

    private static Dictionary<int, ItemCatalogEntry> ReadShields(TyrianDataStream data)
    {
        Dictionary<int, ItemCatalogEntry> items = new(ShieldCount);
        for (int i = 0; i < ShieldCount; i++)
        {
            string name = ReadFixedName(data);
            data.Seek(1 + 1 + 2, SeekOrigin.Current);
            int cost = data.ReadUInt16();
            items[i] = new ItemCatalogEntry(name, cost);
        }

        return items;
    }

    private static string ReadFixedName(TyrianDataStream data)
    {
        int nameLength = data.ReadByte();
        byte[] raw = data.ReadBytes(30);
        int safeLength = nameLength < 0 ? 0 : (nameLength > raw.Length ? raw.Length : nameLength);
        return System.Text.Encoding.ASCII.GetString(raw, 0, safeLength).TrimEnd('\0', ' ');
    }
}
