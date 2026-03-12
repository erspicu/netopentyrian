namespace OpenTyrian.Core;

public sealed class JukeboxScene : IScene, IScenePresentation
{
    private static readonly TrackInfo[] Tracks =
    {
        new TrackInfo("Title Theme", SceneMusicKind.Title),
        new TrackInfo("Menu Theme", SceneMusicKind.Menu),
        new TrackInfo("Battle Theme", SceneMusicKind.Gameplay),
        new TrackInfo("Shop Theme", SceneMusicKind.Shop),
    };

    private OpenTyrian.Platform.InputSnapshot _previousInput;
    private int _trackIndex;

    public int? BackgroundPictureNumber
    {
        get { return 2; }
    }

    public SceneMusicKind? MusicOverride
    {
        get { return Tracks[_trackIndex].MusicKind; }
    }

    public IScene? Update(SceneResources resources, OpenTyrian.Platform.InputSnapshot input, double deltaSeconds)
    {
        bool cancelPressed = input.Cancel && !_previousInput.Cancel;
        bool confirmPressed = input.Confirm && !_previousInput.Confirm;
        bool upPressed = input.Up && !_previousInput.Up;
        bool downPressed = input.Down && !_previousInput.Down;
        bool leftPressed = input.Left && !_previousInput.Left;
        bool rightPressed = input.Right && !_previousInput.Right;

        if (cancelPressed)
        {
            SceneAudio.PlayCancel(resources);
            _previousInput = input;
            return new TitleSetupScene();
        }

        if (leftPressed || upPressed)
        {
            SceneAudio.PlayCursor(resources);
            _trackIndex = _trackIndex == 0 ? Tracks.Length - 1 : _trackIndex - 1;
        }

        if (rightPressed || downPressed || confirmPressed)
        {
            SceneAudio.PlayCursor(resources);
            _trackIndex = (_trackIndex + 1) % Tracks.Length;
        }

        _previousInput = input;
        return null;
    }

    public void Render(IndexedFrameBuffer surface, SceneResources resources, double timeSeconds)
    {
        TitleScreenRenderer.RenderPictureBackground(surface, resources, 2, includeOverlays: false);
        if (resources.FontRenderer is null)
        {
            return;
        }

        TrackInfo track = Tracks[_trackIndex];
        resources.FontRenderer.DrawShadowText(surface, 160, 20, "Jukebox", FontKind.Normal, FontAlignment.Center, 15, 0, black: false, shadowDistance: 1);
        resources.FontRenderer.DrawBlendText(surface, 160, 96, track.Label, FontKind.Small, FontAlignment.Center, 15, 2);
        resources.FontRenderer.DrawText(surface, 160, 124, string.Format("Track {0} / {1}", _trackIndex + 1, Tracks.Length), FontKind.Tiny, FontAlignment.Center, 13, 0, shadow: true);
        resources.FontRenderer.DrawText(surface, 160, 176, "Left/Right or Up/Down changes track", FontKind.Tiny, FontAlignment.Center, 13, 0, shadow: true);
        resources.FontRenderer.DrawDark(surface, 160, 188, "Enter also advances  Esc returns to Setup", FontKind.Tiny, FontAlignment.Center, black: false);
    }

    private struct TrackInfo
    {
        public TrackInfo(string label, SceneMusicKind musicKind)
        {
            Label = label;
            MusicKind = musicKind;
        }

        public string Label { get; private set; }

        public SceneMusicKind MusicKind { get; private set; }
    }
}
