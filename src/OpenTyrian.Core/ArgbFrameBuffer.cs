namespace OpenTyrian.Core;

public sealed class ArgbFrameBuffer
{
    private readonly uint[] _pixels;

    public ArgbFrameBuffer(int width, int height)
    {
        Width = width;
        Height = height;
        _pixels = new uint[width * height];
    }

    public int Width { get; }

    public int Height { get; }

    public Span<uint> Pixels => _pixels;
}
