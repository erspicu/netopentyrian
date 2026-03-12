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

        Dictionary<int, string> weaponPorts = ReadWeaponPorts(data);
        Dictionary<int, string> specials = ReadSpecials(data);
        Dictionary<int, string> generators = ReadGenerators(data);
        Dictionary<int, string> ships = ReadShips(data);
        Dictionary<int, string> options = ReadOptions(data);
        Dictionary<int, string> shields = ReadShields(data);

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

    private static Dictionary<int, string> ReadWeaponPorts(TyrianDataStream data)
    {
        Dictionary<int, string> names = new(WeaponPortCount);
        for (int i = 0; i < WeaponPortCount; i++)
        {
            names[i] = ReadFixedName(data);
            data.ReadByte();
            data.Seek(44 + 2 + 2 + 2, SeekOrigin.Current);
        }

        return names;
    }

    private static Dictionary<int, string> ReadSpecials(TyrianDataStream data)
    {
        Dictionary<int, string> names = new(SpecialCount);
        for (int i = 0; i < SpecialCount; i++)
        {
            names[i] = ReadFixedName(data);
            data.Seek(2 + 1 + 1 + 2, SeekOrigin.Current);
        }

        return names;
    }

    private static Dictionary<int, string> ReadGenerators(TyrianDataStream data)
    {
        Dictionary<int, string> names = new(GeneratorCount);
        for (int i = 0; i < GeneratorCount; i++)
        {
            names[i] = ReadFixedName(data);
            data.Seek(2 + 1 + 1 + 2, SeekOrigin.Current);
        }

        return names;
    }

    private static Dictionary<int, string> ReadShips(TyrianDataStream data)
    {
        Dictionary<int, string> names = new(ShipCount);
        for (int i = 0; i < ShipCount; i++)
        {
            names[i] = ReadFixedName(data);
            data.Seek(2 + 2 + 1 + 1 + 1 + 2 + 1, SeekOrigin.Current);
        }

        return names;
    }

    private static Dictionary<int, string> ReadOptions(TyrianDataStream data)
    {
        Dictionary<int, string> names = new(OptionCount);
        for (int i = 0; i < OptionCount; i++)
        {
            names[i] = ReadFixedName(data);
            data.Seek(1 + 2 + 2 + 1 + 1 + 1 + 1 + 40 + 1 + 2 + 1 + 1 + 1, SeekOrigin.Current);
        }

        return names;
    }

    private static Dictionary<int, string> ReadShields(TyrianDataStream data)
    {
        Dictionary<int, string> names = new(ShieldCount);
        for (int i = 0; i < ShieldCount; i++)
        {
            names[i] = ReadFixedName(data);
            data.Seek(1 + 1 + 2 + 2, SeekOrigin.Current);
        }

        return names;
    }

    private static string ReadFixedName(TyrianDataStream data)
    {
        int nameLength = data.ReadByte();
        byte[] raw = data.ReadBytes(30);
        int safeLength = nameLength < 0 ? 0 : (nameLength > raw.Length ? raw.Length : nameLength);
        return System.Text.Encoding.ASCII.GetString(raw, 0, safeLength).TrimEnd('\0', ' ');
    }
}
