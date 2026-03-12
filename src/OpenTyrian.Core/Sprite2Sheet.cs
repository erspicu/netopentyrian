namespace OpenTyrian.Core;

public sealed class Sprite2Sheet
{
    public Sprite2Sheet(byte[] data, ushort[] offsets)
    {
        Data = data;
        Offsets = offsets;
    }

    public byte[] Data { get; }

    public ushort[] Offsets { get; }

    public int Count => Offsets.Length;

    public ArraySegment<byte> GetSpriteData(int index)
    {
        if (index < 1 || index > Offsets.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        int start = Offsets[index - 1];
        int end = index < Offsets.Length ? Offsets[index] : Data.Length;

        if (start < 0 || start >= Data.Length || end < start || end > Data.Length)
        {
            throw new InvalidDataException("Invalid sprite2 offset table.");
        }

        return new ArraySegment<byte>(Data, start, end - start);
    }
}
