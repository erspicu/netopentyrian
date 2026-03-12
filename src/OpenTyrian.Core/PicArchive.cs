namespace OpenTyrian.Core;

public sealed class PicArchive
{
    private const int PicCount = 13;
    private static readonly byte[] PicPaletteMap = [0, 7, 5, 8, 10, 5, 18, 19, 19, 20, 21, 22, 5];

    private readonly int[] _offsets;
    private readonly byte[] _data;

    private PicArchive(byte[] data, int[] offsets)
    {
        _data = data;
        _offsets = offsets;
    }

    public static PicArchive Load(Stream stream)
    {
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        byte[] data = memory.ToArray();

        using var reader = new TyrianDataStream(new MemoryStream(data, writable: false), leaveOpen: false);
        ushort pictureCount = reader.ReadUInt16();
        if (pictureCount != PicCount)
        {
            throw new InvalidDataException($"Unexpected tyrian.pic count: {pictureCount}");
        }

        var offsets = new int[PicCount + 1];
        for (int i = 0; i < PicCount; i++)
        {
            offsets[i] = reader.ReadInt32();
        }

        offsets[PicCount] = data.Length;
        return new PicArchive(data, offsets);
    }

    public PicImage Decode(int pictureNumber)
    {
        int index = pictureNumber - 1;
        if (index < 0 || index >= PicCount)
        {
            throw new ArgumentOutOfRangeException(nameof(pictureNumber));
        }

        int start = _offsets[index];
        int end = _offsets[index + 1];
        int size = end - start;
        if (size <= 0)
        {
            throw new InvalidDataException($"Invalid picture span for picture {pictureNumber}.");
        }

        byte[] output = DecodeRle(_data.AsSpan(start, size), 320 * 200);
        return new PicImage(320, 200, output, PicPaletteMap[index]);
    }

    private static byte[] DecodeRle(ReadOnlySpan<byte> encoded, int outputSize)
    {
        byte[] output = new byte[outputSize];
        int src = 0;
        int dst = 0;

        while (src < encoded.Length && dst < output.Length)
        {
            byte value = encoded[src++];
            if ((value & 0xC0) == 0xC0)
            {
                int runLength = value & 0x3F;
                if (src >= encoded.Length)
                {
                    throw new InvalidDataException("Invalid RLE stream in tyrian.pic.");
                }

                byte runValue = encoded[src++];
                output.AsSpan(dst, runLength).Fill(runValue);
                dst += runLength;
            }
            else
            {
                output[dst++] = value;
            }
        }

        if (dst != output.Length)
        {
            throw new InvalidDataException("Decoded tyrian.pic size mismatch.");
        }

        return output;
    }
}
