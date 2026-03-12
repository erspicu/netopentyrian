namespace OpenTyrian.Core;

public sealed class IndexedFrameBuffer
{
    private readonly byte[] _pixels;

    public IndexedFrameBuffer(int width, int height)
    {
        Width = width;
        Height = height;
        _pixels = new byte[width * height];
    }

    public int Width { get; }

    public int Height { get; }

    public Span<byte> Pixels => _pixels;

    public void Clear(byte colorIndex = 0)
    {
        _pixels.AsSpan().Fill(colorIndex);
    }
}
