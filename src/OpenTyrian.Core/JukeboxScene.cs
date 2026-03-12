namespace OpenTyrian.Core;

public sealed class JukeboxScene : IScene, IScenePresentation, ICustomMusicScene
{
    private static readonly string[] TrackTitles =
    {
        "Asteroid Dance Part 2",
        "Asteroid Dance Part 1",
        "Buy/Sell Music",
        "CAMANIS",
        "CAMANISE",
        "Deli Shop Quartet",
        "Deli Shop Quartet No. 2",
        "Ending Number 1",
        "Ending Number 2",
        "End of Level",
        "Game Over Solo",
        "Gryphons of the West",
        "Somebody pick up the Gryphone",
        "Gyges, Will You Please Help Me?",
        "I speak Gygese",
        "Halloween Ramble",
        "Tunneling Trolls",
        "Tyrian, The Level",
        "The MusicMan",
        "The Navigator",
        "Come Back to Me, Savara",
        "Come Back again to Savara",
        "Space Journey 1",
        "Space Journey 2",
        "The final edge",
        "START5",
        "Parlance",
        "Torm - The Gathering",
        "TRANSON",
        "Tyrian: The Song",
        "ZANAC3",
        "ZANACS",
        "Return me to Savara",
        "High Score Table",
        "One Mustn't Fall",
        "Sarah's Song",
        "A Field for Mag",
        "Rock Garden",
        "Quest for Peace",
        "Composition in Q",
        "BEER",
    };

    private static readonly string[] Actions =
    {
        "Previous Track",
        "Next Track",
        "Random Track",
        "Stop",
        "Done",
    };

    private readonly Random _random = new Random();
    private OpenTyrian.Platform.InputSnapshot _previousInput;
    private int _trackIndex;
    private int _selectedAction;
    private bool _stopped;

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
        get { return _stopped ? "jukebox:stopped" : string.Format("jukebox:{0}", _trackIndex); }
    }

    public AudioCueSample CreateMusicTrack(int sampleRate, int channelCount)
    {
        return _stopped
            ? new AudioCueSample { Buffer = new byte[0], FrameCount = 0 }
            : BackgroundMusicSynthesizer.CreateJukeboxTrack(_trackIndex, sampleRate, channelCount);
    }

    public IScene? Update(SceneResources resources, OpenTyrian.Platform.InputSnapshot input, double deltaSeconds)
    {
        bool cancelPressed = input.Cancel && !_previousInput.Cancel;
        bool confirmPressed = input.Confirm && !_previousInput.Confirm;
        bool upPressed = input.Up && !_previousInput.Up;
        bool downPressed = input.Down && !_previousInput.Down;
        bool leftPressed = input.Left && !_previousInput.Left;
        bool rightPressed = input.Right && !_previousInput.Right;
        bool pointerConfirmPressed = input.PointerConfirm && !_previousInput.PointerConfirm;

        int? hoveredIndex = input.PointerPresent ? HitTestAction(input.PointerX, input.PointerY) : null;
        if (hoveredIndex.HasValue)
        {
            if (_selectedAction != hoveredIndex.Value)
            {
                SceneAudio.PlayCursor(resources);
            }

            _selectedAction = hoveredIndex.Value;
        }

        if (cancelPressed)
        {
            SceneAudio.PlayCancel(resources);
            _previousInput = input;
            return new TitleSetupScene();
        }

        if (leftPressed)
        {
            SceneAudio.PlayCursor(resources);
            SelectPreviousTrack();
        }
        else if (rightPressed)
        {
            SceneAudio.PlayCursor(resources);
            SelectNextTrack();
        }
        else if (upPressed)
        {
            SceneAudio.PlayCursor(resources);
            _selectedAction = _selectedAction == 0 ? Actions.Length - 1 : _selectedAction - 1;
        }
        else if (downPressed)
        {
            SceneAudio.PlayCursor(resources);
            _selectedAction = (_selectedAction + 1) % Actions.Length;
        }

        if (confirmPressed || (pointerConfirmPressed && hoveredIndex.HasValue))
        {
            _previousInput = input;
            return ExecuteSelectedAction(resources);
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

        resources.FontRenderer.DrawShadowText(surface, 160, 20, "Jukebox", FontKind.Normal, FontAlignment.Center, 15, -3, black: false, shadowDistance: 2);
        resources.FontRenderer.DrawText(surface, 160, 52, string.Format("Track {0} / {1}", _trackIndex + 1, TrackTitles.Length), FontKind.Tiny, FontAlignment.Center, 13, 0, shadow: true);
        resources.FontRenderer.DrawShadowText(surface, 160, 68, TrackTitles[_trackIndex], FontKind.Small, FontAlignment.Center, 15, 0, black: false, shadowDistance: 1);
        resources.FontRenderer.DrawText(surface, 160, 84, _stopped ? "Stopped" : "Playing", FontKind.Tiny, FontAlignment.Center, _stopped ? (byte)12 : (byte)13, 0, shadow: true);

        for (int i = 0; i < Actions.Length; i++)
        {
            string label = i == 3 ? (_stopped ? "Resume" : "Stop") : Actions[i];
            int y = 116 + (i * 14);
            if (i == _selectedAction)
            {
                resources.FontRenderer.DrawBlendText(surface, 160, y, label, FontKind.Small, FontAlignment.Center, 15, 1);
            }
            else
            {
                resources.FontRenderer.DrawText(surface, 160, y, label, FontKind.Small, FontAlignment.Center, 15, -3, shadow: true);
            }
        }

        resources.FontRenderer.DrawText(surface, 160, 188, "Left/Right track  Up/Down action  Enter confirm", FontKind.Tiny, FontAlignment.Center, 13, 0, shadow: true);
        resources.FontRenderer.DrawDark(surface, 160, 196, "Esc returns to Setup", FontKind.Tiny, FontAlignment.Center, black: false);
    }

    private IScene? ExecuteSelectedAction(SceneResources resources)
    {
        switch (_selectedAction)
        {
            case 0:
                SceneAudio.PlayCursor(resources);
                SelectPreviousTrack();
                return null;
            case 1:
                SceneAudio.PlayCursor(resources);
                SelectNextTrack();
                return null;
            case 2:
                SceneAudio.PlayCursor(resources);
                _trackIndex = _random.Next(TrackTitles.Length);
                _stopped = false;
                return null;
            case 3:
                SceneAudio.PlayConfirm(resources);
                _stopped = !_stopped;
                return null;
            default:
                SceneAudio.PlayConfirm(resources);
                return new TitleSetupScene();
        }
    }

    private void SelectPreviousTrack()
    {
        _trackIndex = _trackIndex == 0 ? TrackTitles.Length - 1 : _trackIndex - 1;
        _stopped = false;
    }

    private void SelectNextTrack()
    {
        _trackIndex = (_trackIndex + 1) % TrackTitles.Length;
        _stopped = false;
    }

    private static int? HitTestAction(int x, int y)
    {
        if (x < 82 || x > 238)
        {
            return null;
        }

        for (int i = 0; i < Actions.Length; i++)
        {
            int top = 114 + (i * 14);
            int bottom = top + 12;
            if (y >= top && y <= bottom)
            {
                return i;
            }
        }

        return null;
    }
}
