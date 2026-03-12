namespace OpenTyrian.Core;

public static class PcxLoader
{
    public static PcxImage Load(Stream stream)
    {
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        byte[] data = memory.ToArray();

        if (data.Length < 128 + 769)
        {
            throw new InvalidDataException("PCX file too small.");
        }

        int width = ReadDimension(data[4], data[5], data[8], data[9]);
        int height = ReadDimension(data[6], data[7], data[10], data[11]);

        if (width <= 0 || height <= 0)
        {
            throw new InvalidDataException("Invalid PCX dimensions.");
        }

        int paletteMarkerOffset = data.Length - 769;
        if (data[paletteMarkerOffset] != 12)
        {
            throw new InvalidDataException("PCX VGA palette marker missing.");
        }

        var palette = new PaletteColor[256];
        int paletteOffset = paletteMarkerOffset + 1;
        for (int i = 0; i < 256; i++)
        {
            int colorOffset = paletteOffset + i * 3;
            palette[i] = new PaletteColor(
                data[colorOffset],
                data[colorOffset + 1],
                data[colorOffset + 2]);
        }

        byte[] pixels = DecodeImageData(data.AsSpan(128, paletteMarkerOffset - 128), width * height);
        return new PcxImage(width, height, pixels, palette);
    }

    private static int ReadDimension(byte low1, byte high1, byte low2, byte high2)
    {
        int start = low1 | (high1 << 8);
        int end = low2 | (high2 << 8);
        return end - start + 1;
    }

    private static byte[] DecodeImageData(ReadOnlySpan<byte> encoded, int pixelCount)
    {
        byte[] output = new byte[pixelCount];
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
                    throw new InvalidDataException("Invalid PCX RLE stream.");
                }

                byte runValue = encoded[src++];
                int count = Math.Min(runLength, output.Length - dst);
                output.AsSpan(dst, count).Fill(runValue);
                dst += count;
            }
            else
            {
                output[dst++] = value;
            }
        }

        if (dst != output.Length)
        {
            throw new InvalidDataException("Decoded PCX size mismatch.");
        }

        return output;
    }
}
