using OpenTyrian.Platform;

namespace OpenTyrian.Core;

public sealed class GameHost
{
    private readonly IAssetLocator _assetLocator;
    private readonly IInputSource _inputSource;
    private IScene _scene;
    private double _timeSeconds;
    private PaletteBank? _paletteBank;
    private PaletteColor[]? _activePalette;
    private PicImage? _titleImage;
    private PcxImage? _testPcxImage;
    private Sprite2Sheet? _testSpriteSheet;
    private MainShapeTables? _mainShapeTables;
    private TyrianFontRenderer? _fontRenderer;
    private IReadOnlyList<EpisodeInfo> _episodes = Array.Empty<EpisodeInfo>();
    private GameplayTextInfo? _gameplayText;

    public GameHost(IAssetLocator assetLocator, IInputSource inputSource)
    {
        _assetLocator = assetLocator;
        _inputSource = inputSource;
        _scene = new TitleScene();
        IndexedFrameBuffer = new IndexedFrameBuffer(320, 200);
        FrameBuffer = new ArgbFrameBuffer(320, 200);
        LoadPalette();
        LoadTitleImage();
        LoadTestPcx();
        LoadTestSpriteSheet();
        LoadFonts();
        LoadEpisodes();
        LoadGameplayText();
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
        IScene? nextScene = _scene.Update(CreateSceneResources(), _inputSource.Capture(), deltaSeconds);
        if (nextScene is not null)
        {
            _scene = nextScene;
        }

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
        _scene.Render(IndexedFrameBuffer, CreateSceneResources(), _timeSeconds);
    }

    private SceneResources CreateSceneResources()
    {
        return new SceneResources
        {
            PaletteCount = PaletteCount,
            TitleImage = _titleImage,
            TestPcxImage = _testPcxImage,
            TestSpriteSheet = _testSpriteSheet,
            MainShapeTables = _mainShapeTables,
            FontRenderer = _fontRenderer,
            Episodes = _episodes,
            GameplayText = _gameplayText,
        };
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

    private void LoadTestPcx()
    {
        if (!_assetLocator.FileExists("tshp2.pcx"))
        {
            return;
        }

        try
        {
            using Stream stream = _assetLocator.OpenRead("tshp2.pcx");
            _testPcxImage = PcxLoader.Load(stream);

            if (_testPcxImage.Width == IndexedFrameBuffer.Width &&
                _testPcxImage.Height == IndexedFrameBuffer.Height &&
                _titleImage is null)
            {
                _activePalette = _testPcxImage.Palette;
                StatusText = $"Data OK: {DataDirectory} | palette.dat: {PaletteCount} palettes | tshp2.pcx loaded";
            }
        }
        catch (Exception ex)
        {
            StatusText = $"PCX load failed: {ex.Message}";
        }
    }

    private void LoadTestSpriteSheet()
    {
        if (!_assetLocator.FileExists("newsh1.shp"))
        {
            return;
        }

        try
        {
            using Stream stream = _assetLocator.OpenRead("newsh1.shp");
            _testSpriteSheet = Sprite2Loader.Load(stream);

            if (_titleImage is null && _testPcxImage is null)
            {
                StatusText = $"Data OK: {DataDirectory} | sprite sheet loaded: newsh1.shp ({_testSpriteSheet.Count} sprites)";
            }
        }
        catch (Exception ex)
        {
            StatusText = $"Sprite sheet load failed: {ex.Message}";
        }
    }

    private void LoadFonts()
    {
        if (!_assetLocator.FileExists("tyrian.shp"))
        {
            return;
        }

        try
        {
            using Stream stream = _assetLocator.OpenRead("tyrian.shp");
            _mainShapeTables = MainShapeTablesLoader.Load(stream);
            _fontRenderer = new TyrianFontRenderer(_mainShapeTables);

            if (_titleImage is null && _testPcxImage is null && _testSpriteSheet is null)
            {
                StatusText = $"Data OK: {DataDirectory} | main fonts loaded";
            }
        }
        catch (Exception ex)
        {
            StatusText = $"Font load failed: {ex.Message}";
        }
    }

    private void LoadEpisodes()
    {
        _episodes = EpisodeCatalogLoader.Load(_assetLocator);
    }

    private void LoadGameplayText()
    {
        _gameplayText = GameplayTextLoader.Load(_assetLocator);
    }
}
