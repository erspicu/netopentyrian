namespace OpenTyrian.Core;

public sealed partial class GameplayScene : IScene, ICustomMusicScene
{
    private const float PlayfieldLeft = 12f;
    private const float PlayfieldTop = 22f;
    private const float PlayfieldRight = 308f;
    private const float PlayfieldBottom = 186f;
    private const int PauseMenuLeft = 92;
    private const int PauseMenuRight = 228;
    private const int PauseMenuTop = 84;
    private const int PauseMenuRowHeight = 10;
    private static readonly string[] PauseMenuItems = { "Resume", "Retry Level", "Return to Menu" };

    private readonly EpisodeSessionState _sessionState;
    private readonly bool _returnToTitleOnExit;
    private readonly DemoPlaybackController? _demoPlayback;
    private readonly Random _random;
    private readonly List<ProjectileState> _playerProjectiles;
    private readonly List<ProjectileState> _enemyProjectiles;
    private readonly List<EnemyState> _enemies;
    private readonly int _missionLevelNumber;
    private readonly int? _musicTrackIndexOverride;
    private OpenTyrian.Platform.InputSnapshot _previousInput;
    private float _playerX;
    private float _playerY;
    private float _spawnTimer;
    private float _fireCooldown;
    private float _rearFireCooldown;
    private float _supportFireCooldown;
    private float _maxArmor;
    private float _armor;
    private float _maxShield;
    private float _shield;
    private float _shieldRechargeRate;
    private int _destroyedEnemies;
    private int _earnedCash;
    private int _pauseSelection;
    private MissionPhase _phase;
    private bool _advancedToNextLevel;
    private bool _combatInitialized;

    public GameplayScene(EpisodeSessionState sessionState, bool returnToTitleOnExit, DemoPlaybackController? demoPlayback = null, int? musicTrackIndexOverride = null)
    {
        _sessionState = sessionState;
        _returnToTitleOnExit = returnToTitleOnExit;
        _demoPlayback = demoPlayback;
        _musicTrackIndexOverride = musicTrackIndexOverride;
        _missionLevelNumber = Math.Max(1, sessionState.CurrentLevelNumber);
        _random = new Random((_missionLevelNumber * 997) + sessionState.Cash);
        _playerProjectiles = new List<ProjectileState>();
        _enemyProjectiles = new List<ProjectileState>();
        _enemies = new List<EnemyState>();
        _playerX = 154f;
        _playerY = 164f;
        _spawnTimer = 0.4f;
        _fireCooldown = 0f;
        _rearFireCooldown = 0f;
        _supportFireCooldown = 0.2f;
        _pauseSelection = 0;
        _phase = MissionPhase.Active;
    }

    public GameplayScene(EpisodeSessionState sessionState)
        : this(sessionState, false)
    {
    }

    public string MusicCacheKey
    {
        get
        {
            return _musicTrackIndexOverride.HasValue
                ? string.Format("gameplay:{0}", _musicTrackIndexOverride.Value)
                : "gameplay:default";
        }
    }

    public int? MusicTrackIndex
    {
        get { return _musicTrackIndexOverride; }
    }

    public bool StopMusic
    {
        get { return false; }
    }

    public AudioCueSample CreateFallbackMusicTrack(int sampleRate, int channelCount)
    {
        return _musicTrackIndexOverride.HasValue
            ? BackgroundMusicSynthesizer.CreateJukeboxTrack(_musicTrackIndexOverride.Value, sampleRate, channelCount)
            : BackgroundMusicSynthesizer.Create(SceneMusicKind.Gameplay, sampleRate, channelCount);
    }

    public IScene? Update(SceneResources resources, OpenTyrian.Platform.InputSnapshot input, double deltaSeconds)
    {
        if (_demoPlayback is not null)
        {
            if (HasMeaningfulUserInput(input))
            {
                SceneAudio.PlayCancel(resources);
                _previousInput = input;
                return new TitleMenuScene();
            }

            OpenTyrian.Platform.InputSnapshot demoInput;
            if (!_demoPlayback.TryAdvance(out demoInput))
            {
                _previousInput = input;
                return new TitleMenuScene();
            }

            input = demoInput;
        }

        bool cancelPressed = input.Cancel && !_previousInput.Cancel;
        bool confirmPressed = input.Confirm && !_previousInput.Confirm;
        bool upPressed = input.Up && !_previousInput.Up;
        bool downPressed = input.Down && !_previousInput.Down;
        bool pointerConfirmPressed = input.PointerConfirm && !_previousInput.PointerConfirm;
        float frameSeconds = (float)Math.Min(0.05, Math.Max(0.0, deltaSeconds));

        EnsureCombatState(resources.ItemCatalog);

        if (_phase == MissionPhase.Paused)
        {
            IScene? nextScene = UpdatePauseState(resources, input, cancelPressed, confirmPressed, upPressed, downPressed, pointerConfirmPressed);
            _previousInput = input;
            return nextScene;
        }

        if (_phase != MissionPhase.Active)
        {
            IScene? nextScene = UpdateResultState(resources, cancelPressed, confirmPressed);
            _previousInput = input;
            return nextScene;
        }

        if (cancelPressed)
        {
            SceneAudio.PlayCancel(resources);
            _phase = MissionPhase.Paused;
            _previousInput = input;
            return null;
        }

        RegenerateShield(frameSeconds);
        UpdatePlayerPosition(resources.ItemCatalog, input, frameSeconds);
        UpdateWeapons(resources.ItemCatalog, input, frameSeconds);
        UpdateEnemies(frameSeconds);
        UpdateProjectiles(frameSeconds, _playerProjectiles);
        UpdateProjectiles(frameSeconds, _enemyProjectiles);
        ResolveCollisions(resources);
        CleanupInactiveEntities();

        if (_armor <= 0f)
        {
            _phase = MissionPhase.Failed;
            SceneAudio.PlayCancel(resources);
        }
        else if (_destroyedEnemies >= GetRequiredKills())
        {
            CompleteMission(resources);
        }

        _previousInput = input;
        return null;
    }

    private static bool HasMeaningfulUserInput(OpenTyrian.Platform.InputSnapshot input)
    {
        return input.Up ||
            input.Down ||
            input.Left ||
            input.Right ||
            input.Confirm ||
            input.Cancel ||
            input.PointerConfirm ||
            input.PointerCancel;
    }

    public void Render(IndexedFrameBuffer surface, SceneResources resources, double timeSeconds)
    {
        RenderBackground(surface, timeSeconds);
        RenderHud(surface, resources);
        RenderPlayer(surface);
        RenderProjectiles(surface, _playerProjectiles);
        RenderProjectiles(surface, _enemyProjectiles);
        RenderEnemies(surface);
        RenderOverlay(surface, resources);
    }

    private enum MissionPhase
    {
        Active,
        Paused,
        Cleared,
        Failed,
    }

    private struct ProjectileState
    {
        public float X;
        public float Y;
        public float VelocityX;
        public float VelocityY;
        public int Damage;
        public byte Color;
        public bool Active;
    }

    private struct ProjectileVector
    {
        public float X;
        public float Y;
    }

    private struct EnemyState
    {
        public float X;
        public float Y;
        public float Speed;
        public float Drift;
        public float ShotCooldown;
        public int HitPoints;
        public bool Active;
    }
}
