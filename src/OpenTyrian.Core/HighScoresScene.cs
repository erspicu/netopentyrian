namespace OpenTyrian.Core;

public sealed class HighScoresScene : IScene, IScenePresentation
{
    private IList<HighScoreEntry>? _entries;
    private OpenTyrian.Platform.InputSnapshot _previousInput;

    public int? BackgroundPictureNumber
    {
        get { return 2; }
    }

    public SceneMusicKind? MusicOverride
    {
        get { return SceneMusicKind.Menu; }
    }

    public IScene? Update(SceneResources resources, OpenTyrian.Platform.InputSnapshot input, double deltaSeconds)
    {
        bool cancelPressed = input.Cancel && !_previousInput.Cancel;
        bool confirmPressed = input.Confirm && !_previousInput.Confirm;
        if (cancelPressed || confirmPressed || (input.PointerConfirm && !_previousInput.PointerConfirm))
        {
            SceneAudio.PlayCancel(resources);
            _previousInput = input;
            return new TitleMenuScene();
        }

        EnsureEntries(resources);
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

        EnsureEntries(resources);
        resources.FontRenderer.DrawShadowText(surface, 160, 20, "High Scores", FontKind.Normal, FontAlignment.Center, 15, 0, black: false, shadowDistance: 1);

        IList<HighScoreEntry> entries = _entries ?? new HighScoreEntry[0];
        for (int i = 0; i < entries.Count && i < 10; i++)
        {
            HighScoreEntry entry = entries[i];
            int y = 54 + (i * 13);
            string line = string.Format("{0:00}  {1,-14}  {2,8}", i + 1, entry.Name, entry.Score);
            resources.FontRenderer.DrawText(surface, 36, y, line, FontKind.Tiny, FontAlignment.Left, 15, 0, shadow: true);
        }

        if (entries.Count == 0)
        {
            resources.FontRenderer.DrawText(surface, 160, 100, "No stored high scores were found.", FontKind.Tiny, FontAlignment.Center, 13, 0, shadow: true);
        }

        resources.FontRenderer.DrawDark(surface, 160, 190, "Enter/Click/Esc returns to title menu", FontKind.Tiny, FontAlignment.Center, black: false);
    }

    private void EnsureEntries(SceneResources resources)
    {
        if (_entries is not null)
        {
            return;
        }

        List<HighScoreEntry> entries = new List<HighScoreEntry>();
        if (resources.UserFileStore is not null)
        {
            try
            {
                SaveGameFile saveFile = SaveGameFileManager.Load(resources.UserFileStore);
                for (int i = 0; i < saveFile.Slots.Count; i++)
                {
                    SaveSlotRecord slot = saveFile.Slots[i];
                    int score = Math.Max(slot.HighScore1, slot.HighScore2);
                    if (score <= 0)
                    {
                        continue;
                    }

                    entries.Add(new HighScoreEntry(slot.HighScoreName, score));
                }
            }
            catch
            {
            }
        }

        entries.Sort(delegate (HighScoreEntry left, HighScoreEntry right)
        {
            return right.Score.CompareTo(left.Score);
        });
        _entries = entries;
    }

    private struct HighScoreEntry
    {
        public HighScoreEntry(string name, int score)
        {
            Name = string.IsNullOrWhiteSpace(name) ? "PLAYER" : name;
            Score = score;
        }

        public string Name { get; private set; }

        public int Score { get; private set; }
    }
}
