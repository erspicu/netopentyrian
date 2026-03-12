using OpenTyrian.Platform;

namespace OpenTyrian.Core;

public sealed class GameHost
{
    private readonly IAssetLocator _assetLocator;
    private double _timeSeconds;
    private PaletteBank? _paletteBank;
    private PaletteColor[]? _activePalette;
    private PicImage? _titleImage;

    public GameHost(IAssetLocator assetLocator)
    {
        _assetLocator = assetLocator;
        IndexedFrameBuffer = new IndexedFrameBuffer(320, 200);
        FrameBuffer = new ArgbFrameBuffer(320, 200);
        LoadPalette();
        LoadTitleImage();
    }

    public IndexedFrameBuffer IndexedFrameBuffer { get; }

    public ArgbFrameBuffer FrameBuffer { get; }

    public string DataDirectory => _assetLocator.DataDirectory;

    public int PaletteCount { get; private set; }

    public string StatusText { get; private set; } = "Initializing";

    public bool HasTyrianData()
    {
        return _assetLocator.FileExists("palette.dat");
    }

    public void Tick(double deltaSeconds)
    {
        _timeSeconds += deltaSeconds;
        RenderIndexedFrame();

        if (_activePalette is not null)
        {
            PaletteRenderer.Render(IndexedFrameBuffer, _activePalette, FrameBuffer);
        }
        else
        {
            RenderFallbackArgb();
        }
    }

    private void RenderIndexedFrame()
    {
        if (_titleImage is not null && _activePalette is not null)
        {
            RenderPicImage(_titleImage);
            return;
        }

        Span<byte> pixels = IndexedFrameBuffer.Pixels;
        int width = IndexedFrameBuffer.Width;
        int height = IndexedFrameBuffer.Height;
        int phase = (int)(_timeSeconds * 60.0);

        for (int y = 0; y < height; y++)
        {
            int rowOffset = y * width;

            for (int x = 0; x < width; x++)
            {
                int paletteIndex = (x + y + phase * 2) & 0xFF;

                if (((x / 20) + (y / 20) + (phase / 10)) % 2 == 0)
                {
                    paletteIndex = (paletteIndex + 48) & 0xFF;
                }

                pixels[rowOffset + x] = (byte)paletteIndex;
            }
        }

        DrawBorder(width, height, 255);
    }

    private void RenderPicImage(PicImage image)
    {
        Span<byte> pixels = IndexedFrameBuffer.Pixels;
        ReadOnlySpan<byte> indexed = image.IndexedPixels;
        indexed.CopyTo(pixels);
    }

    private void RenderFallbackArgb()
    {
        Span<byte> source = IndexedFrameBuffer.Pixels;
        Span<uint> destination = FrameBuffer.Pixels;

        for (int i = 0; i < source.Length; i++)
        {
            int value = source[i];
            destination[i] = 0xFF000000u | (uint)(value << 16) | (uint)(value << 8) | (uint)value;
        }
    }

    private void LoadPalette()
    {
        if (!_assetLocator.FileExists("palette.dat"))
        {
            StatusText = $"Missing data: {_assetLocator.GetFullPath("palette.dat")}";
            return;
        }

        try
        {
            using Stream stream = _assetLocator.OpenRead("palette.dat");
            _paletteBank = PaletteLoader.Load(stream);
            PaletteCount = _paletteBank.Count;
            _activePalette = _paletteBank.Palettes[0];
            StatusText = $"Data OK: {DataDirectory} | palette.dat: {PaletteCount} palettes";
        }
        catch (Exception ex)
        {
            StatusText = $"Palette load failed: {ex.Message}";
        }
    }

    private void LoadTitleImage()
    {
        if (!_assetLocator.FileExists("tyrian.pic") || _paletteBank is null || PaletteCount == 0)
        {
            return;
        }

        try
        {
            using Stream stream = _assetLocator.OpenRead("tyrian.pic");
            PicArchive archive = PicArchive.Load(stream);
            PicImage image = archive.Decode(1);

            if (image.PaletteIndex >= 0 && image.PaletteIndex < PaletteCount)
            {
                _titleImage = image;
                _activePalette = _paletteBank.Palettes[image.PaletteIndex];
                StatusText = $"Data OK: {DataDirectory} | palette.dat: {PaletteCount} palettes | title pic loaded";
            }
        }
        catch (Exception ex)
        {
            StatusText = $"PIC load failed: {ex.Message}";
        }
    }

    private void DrawBorder(int width, int height, byte colorIndex)
    {
        Span<byte> pixels = IndexedFrameBuffer.Pixels;

        for (int x = 0; x < width; x++)
        {
            pixels[x] = colorIndex;
            pixels[(height - 1) * width + x] = colorIndex;
        }

        for (int y = 0; y < height; y++)
        {
            pixels[y * width] = colorIndex;
            pixels[y * width + (width - 1)] = colorIndex;
        }
    }
}
