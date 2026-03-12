namespace OpenTyrian.Core;

public sealed class HighScoresScene : IScene, IScenePresentation
{
    private const int EpisodeCount = 3;
    private static readonly string[] DifficultyNames =
    {
        "Unranked",
        "Easy",
        "Normal",
        "Hard",
        "Impossible",
        "Rank 5",
        "Rank 6",
        "Rank 7",
        "Rank 8",
        "Rank 9",
        "Rank 10",
    };

    private SaveGameFile? _saveFile;
    private OpenTyrian.Platform.InputSnapshot _previousInput;
    private int _episodeIndex;

    public int? BackgroundPictureNumber
    {
        get { return 2; }
    }

    public SceneMusicKind? MusicOverride
    {
        get { return SceneMusicKind.Title; }
    }

    public IScene? Update(SceneResources resources, OpenTyrian.Platform.InputSnapshot input, double deltaSeconds)
    {
        bool cancelPressed = input.Cancel && !_previousInput.Cancel;
        bool confirmPressed = input.Confirm && !_previousInput.Confirm;
        bool leftPressed = input.Left && !_previousInput.Left;
        bool rightPressed = input.Right && !_previousInput.Right;
        bool pointerConfirmPressed = input.PointerConfirm && !_previousInput.PointerConfirm;
        bool pointerCancelPressed = input.PointerCancel && !_previousInput.PointerCancel;

        EnsureSaveFile(resources);

        if (cancelPressed || confirmPressed || pointerCancelPressed)
        {
            SceneAudio.PlayCancel(resources);
            _previousInput = input;
            return new TitleMenuScene();
        }

        if (leftPressed || (pointerConfirmPressed && HitTestLeftArrow(input.PointerX, input.PointerY)))
        {
            SceneAudio.PlayCursor(resources);
            _episodeIndex = _episodeIndex == 0 ? EpisodeCount - 1 : _episodeIndex - 1;
        }
        else if (rightPressed || (pointerConfirmPressed && HitTestRightArrow(input.PointerX, input.PointerY)))
        {
            SceneAudio.PlayCursor(resources);
            _episodeIndex = (_episodeIndex + 1) % EpisodeCount;
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

        EnsureSaveFile(resources);
        resources.FontRenderer.DrawShadowText(surface, 160, 3, "High Scores", FontKind.Normal, FontAlignment.Center, 15, -3, black: false, shadowDistance: 2);

        string episodeLabel = ResolveEpisodeLabel(resources.Episodes, _episodeIndex);
        bool episodeAvailable = ResolveEpisodeAvailability(resources.Episodes, _episodeIndex);
        resources.FontRenderer.DrawShadowText(surface, 160, 30, episodeLabel, FontKind.Small, FontAlignment.Center, 15, episodeAvailable ? -3 : -7, black: false, shadowDistance: 2);

        resources.FontRenderer.DrawShadowText(surface, 160, 55, "One Player", FontKind.Small, FontAlignment.Center, 15, -3, black: false, shadowDistance: 2);
        for (int i = 0; i < 3; i++)
        {
            DrawEntry(surface, resources.FontRenderer, GetSlot((_episodeIndex * 6) + i), i, 75);
        }

        resources.FontRenderer.DrawShadowText(surface, 160, 120, "Two Player", FontKind.Small, FontAlignment.Center, 15, -3, black: false, shadowDistance: 2);
        for (int i = 0; i < 3; i++)
        {
            DrawEntry(surface, resources.FontRenderer, GetSlot((_episodeIndex * 6) + 3 + i), i, 135);
        }

        if (_episodeIndex > 0)
        {
            resources.FontRenderer.DrawText(surface, 95, 179, "<", FontKind.Small, FontAlignment.Center, 15, 1, shadow: true);
        }

        if (_episodeIndex + 1 < EpisodeCount)
        {
            resources.FontRenderer.DrawText(surface, 225, 179, ">", FontKind.Small, FontAlignment.Center, 15, 1, shadow: true);
        }

        resources.FontRenderer.DrawText(surface, 160, 190, "Left or right to select episode.", FontKind.Tiny, FontAlignment.Center, 15, 1, shadow: true);
    }

    private void EnsureSaveFile(SceneResources resources)
    {
        if (_saveFile is not null || resources.UserFileStore is null)
        {
            return;
        }

        try
        {
            _saveFile = SaveGameFileManager.Load(resources.UserFileStore);
        }
        catch
        {
            _saveFile = new SaveGameFile
            {
                SourcePath = string.Empty,
                HasSaveFile = false,
                IsValid = false,
                Slots = new List<SaveSlotRecord>(),
                ExtraData = new byte[0],
            };
        }
    }

    private SaveSlotRecord GetSlot(int index)
    {
        if (_saveFile is null || index < 0 || index >= _saveFile.Slots.Count)
        {
            return BuildEmptyScore();
        }

        return _saveFile.Slots[index];
    }

    private static void DrawEntry(IndexedFrameBuffer surface, TyrianFontRenderer fontRenderer, SaveSlotRecord slot, int rankIndex, int y)
    {
        string scoreText = string.Format("#{0}:  {1}", rankIndex + 1, Math.Max(0, slot.HighScore1));
        string nameText = string.IsNullOrWhiteSpace(slot.HighScoreName) ? "---" : slot.HighScoreName;
        string difficultyText = ResolveDifficultyName(slot.HighScoreDiff, slot.HighScore1);
        int difficultyValue = slot.HighScore1 <= 0 ? 0 : Math.Min(slot.HighScoreDiff, DifficultyNames.Length - 1);

        fontRenderer.DrawText(surface, 20, y, scoreText, FontKind.Tiny, FontAlignment.Left, 15, 0, shadow: true);
        fontRenderer.DrawText(surface, 110, y, nameText, FontKind.Tiny, FontAlignment.Left, 15, 2, shadow: true);
        fontRenderer.DrawText(surface, 250, y, difficultyText, FontKind.Tiny, FontAlignment.Left, 15, difficultyValue == 0 ? 0 : difficultyValue - 1, shadow: true);
    }

    private static string ResolveEpisodeLabel(IList<EpisodeInfo> episodes, int episodeIndex)
    {
        if (episodeIndex >= 0 && episodeIndex < episodes.Count)
        {
            return episodes[episodeIndex].Label;
        }

        return string.Format("Episode {0}", episodeIndex + 1);
    }

    private static bool ResolveEpisodeAvailability(IList<EpisodeInfo> episodes, int episodeIndex)
    {
        return episodeIndex >= 0 &&
            episodeIndex < episodes.Count &&
            episodes[episodeIndex].IsAvailable;
    }

    private static string ResolveDifficultyName(byte difficulty, int score)
    {
        if (score <= 0)
        {
            return DifficultyNames[0];
        }

        int index = difficulty;
        if (index < 0)
        {
            index = 0;
        }
        else if (index >= DifficultyNames.Length)
        {
            index = DifficultyNames.Length - 1;
        }

        return DifficultyNames[index];
    }

    private static SaveSlotRecord BuildEmptyScore()
    {
        return new SaveSlotRecord
        {
            SlotIndex = 0,
            PageIndex = 0,
            Encode = 0,
            LevelNumber = 0,
            Items = new byte[12],
            Cash = 0,
            Cash2 = 0,
            LevelName = string.Empty,
            Name = string.Empty,
            CubeCount = 0,
            WeaponPowers = new byte[2],
            EpisodeNumber = 0,
            LastItems = new byte[12],
            Difficulty = 0,
            SecretHint = 0,
            Input1 = 0,
            Input2 = 0,
            GameHasRepeated = false,
            InitialDifficulty = 0,
            HighScore1 = 0,
            HighScore2 = 0,
            HighScoreName = string.Empty,
            HighScoreDiff = 0,
        };
    }

    private static bool HitTestLeftArrow(int x, int y)
    {
        return y >= 176 && y <= 192 && x >= 83 && x <= 107;
    }

    private static bool HitTestRightArrow(int x, int y)
    {
        return y >= 176 && y <= 192 && x >= 213 && x <= 237;
    }
}
