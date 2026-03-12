namespace OpenTyrian.Core;

public sealed class JukeboxScene : IScene, IScenePresentation, ICustomMusicScene, ICustomPaletteScene
{
    private const int StarCount = 84;
    private static readonly PaletteColor[] JukeboxPalette = BuildJukeboxPalette();
    private readonly Random _random = new Random();
    private OpenTyrian.Platform.InputSnapshot _previousInput;
    private int _trackIndex;
    private int _musicRevision;
    private bool _stopped;
    private bool _hideText;

    public JukeboxScene()
    {
        _trackIndex = OriginalMusicCatalog.TitleMusic;
    }

    public int? BackgroundPictureNumber
    {
        get { return 2; }
    }

    public SceneMusicKind? MusicOverride
    {
        get { return SceneMusicKind.Silence; }
    }

    public string MusicCacheKey
    {
        get { return string.Format("jukebox:{0}:{1}:{2}", _trackIndex, _stopped ? 1 : 0, _musicRevision); }
    }

    public int? MusicTrackIndex
    {
        get { return _stopped ? (int?)null : _trackIndex; }
    }

    public bool StopMusic
    {
        get { return _stopped; }
    }

    public AudioCueSample CreateFallbackMusicTrack(int sampleRate, int channelCount)
    {
        return _stopped
            ? new AudioCueSample { Buffer = new byte[0], FrameCount = 0 }
            : BackgroundMusicSynthesizer.CreateJukeboxTrack(_trackIndex, sampleRate, channelCount);
    }

    public PaletteColor[] PaletteOverride
    {
        get { return JukeboxPalette; }
    }

    public IScene? Update(SceneResources resources, OpenTyrian.Platform.InputSnapshot input, double deltaSeconds)
    {
        string typedText = resources.TextEntrySource is null
            ? string.Empty
            : resources.TextEntrySource.ConsumeText();

        bool cancelPressed = input.Cancel && !_previousInput.Cancel;
        bool confirmPressed = input.Confirm && !_previousInput.Confirm;
        bool upPressed = input.Up && !_previousInput.Up;
        bool downPressed = input.Down && !_previousInput.Down;
        bool leftPressed = input.Left && !_previousInput.Left;
        bool rightPressed = input.Right && !_previousInput.Right;
        bool pointerPressed = (input.PointerConfirm && !_previousInput.PointerConfirm) ||
            (input.PointerCancel && !_previousInput.PointerCancel);
        bool spacePressed = typedText.IndexOf(' ') >= 0;

        if (ContainsCommand(typedText, 'Q') || cancelPressed || pointerPressed)
        {
            SceneAudio.PlayCancel(resources);
            _previousInput = input;
            return new TitleSetupScene();
        }

        if (spacePressed)
        {
            SceneAudio.PlayCursor(resources);
            _hideText = !_hideText;
        }

        if (ContainsCommand(typedText, 'S'))
        {
            SceneAudio.PlayCancel(resources);
            StopTrack();
        }

        if (ContainsCommand(typedText, 'R'))
        {
            SceneAudio.PlayConfirm(resources);
            RestartTrack();
        }

        if (ContainsCommand(typedText, 'P'))
        {
            SceneAudio.PlayCursor(resources);
            SelectPreviousTrack();
        }

        if (ContainsCommand(typedText, 'N'))
        {
            SceneAudio.PlayCursor(resources);
            SelectNextTrack();
        }

        if (ContainsCommand(typedText, 'A'))
        {
            SceneAudio.PlayCursor(resources);
            _trackIndex = _random.Next(OriginalMusicCatalog.Titles.Length);
            ResumeTrack();
        }

        if (leftPressed || upPressed)
        {
            SceneAudio.PlayCursor(resources);
            SelectPreviousTrack();
        }
        else if (rightPressed || downPressed || (confirmPressed && !spacePressed))
        {
            SceneAudio.PlayCursor(resources);
            SelectNextTrack();
        }

        _previousInput = input;
        return null;
    }

    public void Render(IndexedFrameBuffer surface, SceneResources resources, double timeSeconds)
    {
        Vga256.Clear(surface, 0);
        RenderStarField(surface, timeSeconds);

        if (resources.FontRenderer is null || _hideText)
        {
            return;
        }

        string trackLabel = string.Format("{0} {1}", _trackIndex + 1, OriginalMusicCatalog.Titles[_trackIndex]);
        resources.FontRenderer.DrawText(surface, 160, 170, "Press ESC to quit the jukebox.", FontKind.Tiny, FontAlignment.Center, 15, 0, shadow: true);
        resources.FontRenderer.DrawText(surface, 160, 180, "Arrow keys change the song being played.", FontKind.Tiny, FontAlignment.Center, 15, 0, shadow: true);
        resources.FontRenderer.DrawText(surface, 160, 190, trackLabel, FontKind.Tiny, FontAlignment.Center, 15, 4, shadow: true);

        if (_stopped)
        {
            resources.FontRenderer.DrawDark(surface, 160, 160, "Stopped - press Enter or R to restart.", FontKind.Tiny, FontAlignment.Center, black: false);
        }
        else
        {
            resources.FontRenderer.DrawDark(surface, 160, 160, "Space hides text. S stops. R restarts.", FontKind.Tiny, FontAlignment.Center, black: false);
        }
    }

    private void SelectPreviousTrack()
    {
        _trackIndex = _trackIndex == 0 ? OriginalMusicCatalog.Titles.Length - 1 : _trackIndex - 1;
        ResumeTrack();
    }

    private void SelectNextTrack()
    {
        _trackIndex = (_trackIndex + 1) % OriginalMusicCatalog.Titles.Length;
        ResumeTrack();
    }

    private void ResumeTrack()
    {
        _stopped = false;
        _musicRevision++;
    }

    private void RestartTrack()
    {
        _stopped = false;
        _musicRevision++;
    }

    private void StopTrack()
    {
        if (_stopped)
        {
            return;
        }

        _stopped = true;
        _musicRevision++;
    }

    private static bool ContainsCommand(string typedText, char command)
    {
        return typedText.IndexOf(command.ToString(), StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static void RenderStarField(IndexedFrameBuffer surface, double timeSeconds)
    {
        const int centerX = 160;
        const int centerY = 100;
        int[] ringRadii = { 24, 32, 40, 50, 62, 74, 86 };
        byte[] ringColors = { 11, 10, 9, 13, 12, 14, 15 };

        for (int ringIndex = 0; ringIndex < ringRadii.Length; ringIndex++)
        {
            int radius = ringRadii[ringIndex];
            int pointCount = 28 + (ringIndex * 10);
            double rotation = timeSeconds * (0.4 + (ringIndex * 0.11)) * (ringIndex % 2 == 0 ? 1.0 : -1.0);

            for (int pointIndex = 0; pointIndex < pointCount; pointIndex++)
            {
                double angle = ((Math.PI * 2.0) * pointIndex / pointCount) + rotation;
                int x = centerX + (int)Math.Round(Math.Cos(angle) * radius);
                int y = centerY + (int)Math.Round(Math.Sin(angle) * (radius * 0.72));
                byte color = ringColors[(ringIndex + pointIndex) % ringColors.Length];

                if ((pointIndex + ringIndex) % 4 == 0)
                {
                    Vga256.PutCrossPixel(surface, x, y, color);
                }
                else
                {
                    Vga256.PutPixel(surface, x, y, color);
                }
            }
        }

        for (int i = 0; i < StarCount; i++)
        {
            int baseX = (i * 37) % surface.Width;
            int baseY = (i * 61) % surface.Height;
            int x = (baseX + (int)(Math.Sin(timeSeconds + (i * 0.21)) * 5.0) + surface.Width) % surface.Width;
            int y = (baseY + (int)(timeSeconds * (3.0 + (i % 5)) * 5.0)) % surface.Height;
            byte color = (byte)(i % 6 == 0 ? 15 : (i % 2 == 0 ? 6 : 2));

            Vga256.PutPixel(surface, x, y, color);
        }
    }

    private static PaletteColor[] BuildJukeboxPalette()
    {
        PaletteColor[] palette = new PaletteColor[PaletteBank.ColorsPerPalette];

        PaletteColor[] ega =
        {
            new PaletteColor(0, 0, 0),
            new PaletteColor(0, 0, 168),
            new PaletteColor(0, 168, 0),
            new PaletteColor(0, 168, 168),
            new PaletteColor(168, 0, 0),
            new PaletteColor(168, 0, 168),
            new PaletteColor(168, 84, 0),
            new PaletteColor(168, 168, 168),
            new PaletteColor(84, 84, 84),
            new PaletteColor(84, 84, 252),
            new PaletteColor(84, 252, 84),
            new PaletteColor(84, 252, 252),
            new PaletteColor(252, 84, 84),
            new PaletteColor(252, 84, 252),
            new PaletteColor(252, 252, 84),
            new PaletteColor(252, 252, 252),
        };

        Array.Copy(ega, 0, palette, 0, ega.Length);

        int[] grayscale = { 0, 20, 32, 44, 56, 68, 80, 96, 112, 128, 144, 160, 180, 200, 224, 252 };
        for (int i = 0; i < grayscale.Length; i++)
        {
            int shade = grayscale[i];
            palette[16 + i] = new PaletteColor((byte)shade, (byte)shade, (byte)shade);
        }

        FillColorCycle(palette, 32, new[] { 0, 64, 124, 188, 252 });
        FillColorCycle(palette, 56, new[] { 124, 156, 188, 220, 252 });
        FillColorCycle(palette, 80, new[] { 180, 196, 216, 232, 252 });
        FillColorCycle(palette, 104, new[] { 0, 28, 56, 84, 112 });
        FillColorCycle(palette, 128, new[] { 56, 68, 84, 96, 112 });
        FillColorCycle(palette, 152, new[] { 80, 88, 96, 104, 112 });
        FillColorCycle(palette, 176, new[] { 0, 16, 32, 48, 64 });
        FillColorCycle(palette, 200, new[] { 32, 40, 48, 56, 64 });
        FillColorCycle(palette, 224, new[] { 44, 48, 52, 60, 64 });

        for (int i = 248; i < palette.Length; i++)
        {
            palette[i] = new PaletteColor(0, 0, 0);
        }

        return palette;
    }

    private static void FillColorCycle(PaletteColor[] palette, int startIndex, int[] ramp)
    {
        byte low = (byte)ramp[0];
        byte high = (byte)ramp[ramp.Length - 1];
        int index = startIndex;

        for (int i = 0; i < ramp.Length; i++)
        {
            palette[index++] = new PaletteColor((byte)ramp[i], low, high);
        }

        for (int i = ramp.Length - 2; i > 0; i--)
        {
            palette[index++] = new PaletteColor(high, low, (byte)ramp[i]);
        }

        for (int i = 0; i < ramp.Length; i++)
        {
            palette[index++] = new PaletteColor(high, (byte)ramp[i], low);
        }

        for (int i = ramp.Length - 2; i > 0; i--)
        {
            palette[index++] = new PaletteColor((byte)ramp[i], high, low);
        }

        for (int i = 0; i < ramp.Length; i++)
        {
            palette[index++] = new PaletteColor(low, high, (byte)ramp[i]);
        }

        for (int i = ramp.Length - 2; i > 0; i--)
        {
            palette[index++] = new PaletteColor(low, (byte)ramp[i], high);
        }
    }
}
