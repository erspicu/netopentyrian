namespace OpenTyrian.Core;

public static class Sprite2Loader
{
    public static Sprite2Sheet Load(Stream stream)
    {
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        byte[] data = memory.ToArray();

        if (data.Length < 2)
        {
            throw new InvalidDataException("Sprite2 sheet too small.");
        }

        ushort firstOffset = BitConverter.ToUInt16(data, 0);
        if (firstOffset == 0 || firstOffset % 2 != 0 || firstOffset > data.Length)
        {
            throw new InvalidDataException("Invalid sprite2 offset table.");
        }

        int count = firstOffset / 2;
        var offsets = new ushort[count];
        for (int i = 0; i < count; i++)
        {
            offsets[i] = BitConverter.ToUInt16(data, i * 2);
        }

        return new Sprite2Sheet(data, offsets);
    }
}
