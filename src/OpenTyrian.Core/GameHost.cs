using OpenTyrian.Platform;

namespace OpenTyrian.Core;

public sealed class GameHost : IAudioCueSink
{
    private const int AudioChunkFrames = 1024;
    private readonly IAssetLocator _assetLocator;
    private readonly IInputSource _inputSource;
    private readonly IAudioDevice _audioDevice;
    private readonly IUserFileStore _userFileStore;
    private readonly Dictionary<AudioCueKind, AudioCueSample> _audioCues = new();
    private readonly Dictionary<SceneMusicKind, AudioCueSample> _musicTracks = new();
    private readonly List<AudioCuePlayback> _activeCuePlaybacks = new();
    private IScene _scene;
    private double _timeSeconds;
    private double _bufferedAudioSeconds;
    private int _musicFrameCursor;
    private PaletteBank? _paletteBank;
    private PaletteColor[]? _activePalette;
    private PicImage? _titleImage;
    private PcxImage? _testPcxImage;
    private Sprite2Sheet? _testSpriteSheet;
    private MainShapeTables? _mainShapeTables;
    private TyrianFontRenderer? _fontRenderer;
    private IList<EpisodeInfo> _episodes = new EpisodeInfo[0];
    private GameplayTextInfo? _gameplayText;
    private ItemCatalog? _itemCatalog;
    private SaveSlotCatalog? _saveSlots;
    private SceneMusicKind _currentMusicKind;

    public GameHost(IAssetLocator assetLocator, IInputSource inputSource, IAudioDevice audioDevice, IUserFileStore userFileStore)
    {
        _assetLocator = assetLocator;
        _inputSource = inputSource;
        _audioDevice = audioDevice;
        _userFileStore = userFileStore;
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
        LoadItemCatalog();
        LoadSaveSlots();
        InitializeAudio();
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

        PumpAudio(deltaSeconds);

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
            AudioCueSink = this,
            SaveSlots = _saveSlots,
            InputConfigurator = _inputSource as OpenTyrian.Platform.IInputConfigurator,
            JoystickConfigurator = _inputSource as OpenTyrian.Platform.IJoystickConfigurator,
            TextEntrySource = _inputSource as OpenTyrian.Platform.ITextEntrySource,
            UserFileStore = _userFileStore,
            SaveCatalogUpdater = UpdateSaveCatalog,
            TitleImage = _titleImage,
            TestPcxImage = _testPcxImage,
            TestSpriteSheet = _testSpriteSheet,
            MainShapeTables = _mainShapeTables,
            FontRenderer = _fontRenderer,
            Episodes = _episodes,
            GameplayText = _gameplayText,
            ItemCatalog = _itemCatalog,
        };
    }

    private void RenderFallbackArgb()
    {
        byte[] source = IndexedFrameBuffer.Pixels;
        uint[] destination = FrameBuffer.Pixels;

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

    private void LoadItemCatalog()
    {
        try
        {
            _itemCatalog = ItemCatalogLoader.Load(_assetLocator);
        }
        catch
        {
            _itemCatalog = null;
        }
    }

    private void LoadSaveSlots()
    {
        try
        {
            _saveSlots = SaveGameFileManager.Load(_userFileStore).ToCatalog();
        }
        catch
        {
            _saveSlots = null;
        }
    }

    private void UpdateSaveCatalog(SaveSlotCatalog catalog)
    {
        _saveSlots = catalog;
    }

    public void Shutdown()
    {
        _audioDevice.Shutdown();
    }

    public void Enqueue(AudioCueKind cue)
    {
        if (!_audioDevice.IsInitialized)
        {
            return;
        }

        AudioCueSample? sample;
        if (!_audioCues.TryGetValue(cue, out sample) || sample is null)
        {
            return;
        }

        _activeCuePlaybacks.Add(new AudioCuePlayback(sample));
    }

    private void InitializeAudio()
    {
        try
        {
            _audioDevice.Initialize(44100, 2);
            PrepareAudioCues();
            PrepareMusicTracks();
            StatusText = _audioDevice.IsInitialized
                ? string.Format("{0} | audio:{1}", StatusText, _audioDevice.BackendName)
                : string.Format("{0} | audio:unavailable", StatusText);
        }
        catch (Exception ex)
        {
            StatusText = string.Format("{0} | audio init failed: {1}", StatusText, ex.Message);
        }
    }

    private void PrepareAudioCues()
    {
        _audioCues.Clear();
        if (!_audioDevice.IsInitialized)
        {
            return;
        }

        _audioCues[AudioCueKind.Cursor] = AudioCueSynthesizer.Create(AudioCueKind.Cursor, _audioDevice.SampleRate, _audioDevice.ChannelCount);
        _audioCues[AudioCueKind.Confirm] = AudioCueSynthesizer.Create(AudioCueKind.Confirm, _audioDevice.SampleRate, _audioDevice.ChannelCount);
        _audioCues[AudioCueKind.Cancel] = AudioCueSynthesizer.Create(AudioCueKind.Cancel, _audioDevice.SampleRate, _audioDevice.ChannelCount);
    }

    private void PrepareMusicTracks()
    {
        _musicTracks.Clear();
        if (!_audioDevice.IsInitialized)
        {
            return;
        }

        _musicTracks[SceneMusicKind.Title] = BackgroundMusicSynthesizer.Create(SceneMusicKind.Title, _audioDevice.SampleRate, _audioDevice.ChannelCount);
        _musicTracks[SceneMusicKind.Menu] = BackgroundMusicSynthesizer.Create(SceneMusicKind.Menu, _audioDevice.SampleRate, _audioDevice.ChannelCount);
        _musicTracks[SceneMusicKind.Gameplay] = BackgroundMusicSynthesizer.Create(SceneMusicKind.Gameplay, _audioDevice.SampleRate, _audioDevice.ChannelCount);
        _musicTracks[SceneMusicKind.Shop] = BackgroundMusicSynthesizer.Create(SceneMusicKind.Shop, _audioDevice.SampleRate, _audioDevice.ChannelCount);
        _currentMusicKind = SceneMusicKind.Silence;
        _musicFrameCursor = 0;
        _bufferedAudioSeconds = 0.0;
    }

    private void PumpAudio(double deltaSeconds)
    {
        if (!_audioDevice.IsInitialized)
        {
            return;
        }

        _bufferedAudioSeconds -= deltaSeconds;
        if (_bufferedAudioSeconds < 0.0)
        {
            _bufferedAudioSeconds = 0.0;
        }

        SceneMusicKind targetMusic = ResolveSceneMusicKind(_scene);
        if (targetMusic != _currentMusicKind)
        {
            _currentMusicKind = targetMusic;
            _musicFrameCursor = 0;
            _bufferedAudioSeconds = 0.0;
        }

        double chunkSeconds = (double)AudioChunkFrames / _audioDevice.SampleRate;
        while (_bufferedAudioSeconds < 0.12)
        {
            byte[] mixedChunk = MixAudioChunk(AudioChunkFrames);
            _audioDevice.SubmitSamples(mixedChunk, AudioChunkFrames);
            _bufferedAudioSeconds += chunkSeconds;
        }
    }

    private byte[] MixAudioChunk(int frameCount)
    {
        int channelCount = _audioDevice.ChannelCount;
        int[] mixed = new int[frameCount * channelCount];

        MixMusicTrack(mixed, frameCount, channelCount);
        MixActiveCues(mixed, frameCount, channelCount);

        byte[] buffer = new byte[frameCount * channelCount * sizeof(short)];
        for (int i = 0; i < mixed.Length; i++)
        {
            int sample = mixed[i];
            if (sample > short.MaxValue)
            {
                sample = short.MaxValue;
            }
            else if (sample < short.MinValue)
            {
                sample = short.MinValue;
            }

            int byteOffset = i * sizeof(short);
            buffer[byteOffset] = (byte)(sample & 0xFF);
            buffer[byteOffset + 1] = (byte)((sample >> 8) & 0xFF);
        }

        return buffer;
    }

    private void MixMusicTrack(int[] mixed, int frameCount, int channelCount)
    {
        AudioCueSample? musicTrack;
        if (_currentMusicKind == SceneMusicKind.Silence ||
            !_musicTracks.TryGetValue(_currentMusicKind, out musicTrack) ||
            musicTrack is null ||
            musicTrack.FrameCount <= 0)
        {
            return;
        }

        for (int frame = 0; frame < frameCount; frame++)
        {
            int sourceFrame = (_musicFrameCursor + frame) % musicTrack.FrameCount;
            int sourceByteOffset = sourceFrame * channelCount * sizeof(short);
            int destinationOffset = frame * channelCount;
            for (int channel = 0; channel < channelCount; channel++)
            {
                mixed[destinationOffset + channel] += ReadPcmSample(musicTrack.Buffer, sourceByteOffset + (channel * sizeof(short)));
            }
        }

        _musicFrameCursor = (_musicFrameCursor + frameCount) % musicTrack.FrameCount;
    }

    private void MixActiveCues(int[] mixed, int frameCount, int channelCount)
    {
        for (int playbackIndex = _activeCuePlaybacks.Count - 1; playbackIndex >= 0; playbackIndex--)
        {
            AudioCuePlayback playback = _activeCuePlaybacks[playbackIndex];
            AudioCueSample sample = playback.Sample;
            int remainingFrames = sample.FrameCount - playback.PositionFrame;
            if (remainingFrames <= 0)
            {
                _activeCuePlaybacks.RemoveAt(playbackIndex);
                continue;
            }

            int framesToMix = Math.Min(frameCount, remainingFrames);
            for (int frame = 0; frame < framesToMix; frame++)
            {
                int sourceFrame = playback.PositionFrame + frame;
                int sourceByteOffset = sourceFrame * channelCount * sizeof(short);
                int destinationOffset = frame * channelCount;
                for (int channel = 0; channel < channelCount; channel++)
                {
                    mixed[destinationOffset + channel] += ReadPcmSample(sample.Buffer, sourceByteOffset + (channel * sizeof(short)));
                }
            }

            playback.PositionFrame += framesToMix;
            if (playback.PositionFrame >= sample.FrameCount)
            {
                _activeCuePlaybacks.RemoveAt(playbackIndex);
            }
            else
            {
                _activeCuePlaybacks[playbackIndex] = playback;
            }
        }
    }

    private static short ReadPcmSample(byte[] buffer, int byteOffset)
    {
        return (short)(buffer[byteOffset] | (buffer[byteOffset + 1] << 8));
    }

    private static SceneMusicKind ResolveSceneMusicKind(IScene scene)
    {
        return scene switch
        {
            TitleScene => SceneMusicKind.Title,
            GameplayScene => SceneMusicKind.Gameplay,
            UpgradeMenuScene => SceneMusicKind.Shop,
            MainMenuScene => SceneMusicKind.Menu,
            EpisodeSelectScene => SceneMusicKind.Menu,
            FullGameMenuScene => SceneMusicKind.Menu,
            LevelSelectScene => SceneMusicKind.Menu,
            OptionsScene => SceneMusicKind.Menu,
            SaveSlotsScene => SceneMusicKind.Menu,
            KeyboardSetupScene => SceneMusicKind.Menu,
            JoystickSetupScene => SceneMusicKind.Menu,
            ShipSpecsScene => SceneMusicKind.Menu,
            DataCubeScene => SceneMusicKind.Menu,
            EpisodeSessionScene => SceneMusicKind.Menu,
            QuitConfirmationScene => SceneMusicKind.Menu,
            _ => SceneMusicKind.Menu,
        };
    }

    private sealed class AudioCuePlayback
    {
        public AudioCuePlayback(AudioCueSample sample)
        {
            Sample = sample;
            PositionFrame = 0;
        }

        public AudioCueSample Sample { get; private set; }

        public int PositionFrame { get; set; }
    }
}
