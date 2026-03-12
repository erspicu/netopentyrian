namespace OpenTyrian.Core;

public sealed class LevelSelectScene : IScene
{
    private const int VisibleRows = 8;
    private const int ListLeft = 48;
    private const int ListRight = 272;
    private const int ListTop = 96;
    private const int RowHeight = 12;

    private readonly EpisodeSessionState _sessionState;
    private OpenTyrian.Platform.InputSnapshot _previousInput;
    private int _selectedIndex;

    public LevelSelectScene(EpisodeSessionState sessionState)
    {
        _sessionState = sessionState;
        _selectedIndex = Math.Max(0, sessionState.CurrentLevelNumber - 1);
    }

    public IScene? Update(SceneResources resources, OpenTyrian.Platform.InputSnapshot input, double deltaSeconds)
    {
        bool cancelPressed = input.Cancel && !_previousInput.Cancel;
        bool confirmPressed = input.Confirm && !_previousInput.Confirm;
        bool upPressed = input.Up && !_previousInput.Up;
        bool downPressed = input.Down && !_previousInput.Down;
        bool pointerConfirmPressed = input.PointerConfirm && !_previousInput.PointerConfirm;

        if (_sessionState.MainLevelEntries.Count == 0)
        {
            _previousInput = input;
            return cancelPressed ? new FullGameMenuScene(_sessionState) : null;
        }

        int? hoveredIndex = input.PointerPresent
            ? HitTestRow(input.PointerX, input.PointerY, _selectedIndex, _sessionState.MainLevelEntries.Count)
            : null;

        if (hoveredIndex is int pointerIndex)
        {
            if (_selectedIndex != pointerIndex)
            {
                SceneAudio.PlayCursor(resources);
            }

            _selectedIndex = pointerIndex;
        }

        if (cancelPressed)
        {
            SceneAudio.PlayCancel(resources);
            _previousInput = input;
            return new FullGameMenuScene(_sessionState);
        }

        if (upPressed)
        {
            SceneAudio.PlayCursor(resources);
            _selectedIndex = _selectedIndex == 0 ? _sessionState.MainLevelEntries.Count - 1 : _selectedIndex - 1;
        }

        if (downPressed)
        {
            SceneAudio.PlayCursor(resources);
            _selectedIndex = (_selectedIndex + 1) % _sessionState.MainLevelEntries.Count;
        }

        if (confirmPressed || (pointerConfirmPressed && hoveredIndex is not null))
        {
            SceneAudio.PlayConfirm(resources);
            MainLevelEntry selectedEntry = _sessionState.MainLevelEntries[_selectedIndex];
            _sessionState.SetCurrentMainLevel(selectedEntry.MainLevelNumber);
            _previousInput = input;
            return new GameplayScene(_sessionState);
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

        resources.FontRenderer.DrawText(surface, 160, 24, _sessionState.StartInfo.DisplayName, FontKind.Tiny, FontAlignment.Center, 14, 1, shadow: true);
        resources.FontRenderer.DrawShadowText(surface, 160, 38, "Next Level", FontKind.Normal, FontAlignment.Center, 15, -3, black: false, shadowDistance: 2);

        if (_sessionState.MainLevelEntries.Count == 0)
        {
            resources.FontRenderer.DrawText(surface, 160, 110, "No main-level sections were parsed from levelsX.dat", FontKind.Tiny, FontAlignment.Center, 12, 0, shadow: true);
            resources.FontRenderer.DrawDark(surface, 160, 188, "Esc returns to full-game menu", FontKind.Tiny, FontAlignment.Center, black: false);
            return;
        }

        int windowStart = GetWindowStart(_selectedIndex, _sessionState.MainLevelEntries.Count, VisibleRows);
        int visibleCount = Math.Min(VisibleRows, _sessionState.MainLevelEntries.Count);
        for (int i = 0; i < visibleCount; i++)
        {
            int entryIndex = windowStart + i;
            MainLevelEntry entry = _sessionState.MainLevelEntries[entryIndex];
            bool isSelected = entryIndex == _selectedIndex;
            string label = string.Format("{0:00}  {1}", entry.MainLevelNumber, FormatSectionLabel(entry.Section.Label));
            int y = ListTop + (i * RowHeight);

            if (isSelected)
            {
                resources.FontRenderer.DrawBlendText(surface, 42, y, $"> {label}", FontKind.Tiny, FontAlignment.Left, 15, 4);
            }
            else
            {
                resources.FontRenderer.DrawText(surface, 42, y, label, FontKind.Tiny, FontAlignment.Left, 13, 0, shadow: true);
            }
        }

        if (windowStart > 0)
        {
            resources.FontRenderer.DrawText(surface, 286, ListTop - 8, "^", FontKind.Tiny, FontAlignment.Center, 12, 0, shadow: true);
        }

        if (windowStart + visibleCount < _sessionState.MainLevelEntries.Count)
        {
            resources.FontRenderer.DrawText(surface, 286, ListTop + (visibleCount * RowHeight), "v", FontKind.Tiny, FontAlignment.Center, 12, 0, shadow: true);
        }

        MainLevelEntry selectedEntry = _sessionState.MainLevelEntries[_selectedIndex];
        resources.FontRenderer.DrawText(
            surface,
            160,
            194,
            string.Format("selected: level {0}  cmds:{1}  offset:{2}", selectedEntry.MainLevelNumber, selectedEntry.Commands.Count, selectedEntry.Section.FileOffset),
            FontKind.Tiny,
            FontAlignment.Center,
            12,
            0,
            shadow: true);
        resources.FontRenderer.DrawDark(surface, 160, 204, "Up/Down or mouse choose  Enter/click launch  Esc back", FontKind.Tiny, FontAlignment.Center, black: false);
    }

    private static int? HitTestRow(int x, int y, int selectedIndex, int totalRows)
    {
        if (x < ListLeft || x > ListRight || totalRows <= 0)
        {
            return null;
        }

        int visibleCount = Math.Min(VisibleRows, totalRows);
        int windowStart = GetWindowStart(selectedIndex, totalRows, visibleCount);
        for (int i = 0; i < visibleCount; i++)
        {
            int top = ListTop + (i * RowHeight);
            int bottom = top + RowHeight - 2;
            if (y >= top && y <= bottom)
            {
                return windowStart + i;
            }
        }

        return null;
    }

    private static int GetWindowStart(int selectedIndex, int totalRows, int visibleRows)
    {
        if (totalRows <= visibleRows)
        {
            return 0;
        }

        int preferredStart = selectedIndex - (visibleRows / 2);
        if (preferredStart < 0)
        {
            return 0;
        }

        int maxStart = totalRows - visibleRows;
        return preferredStart > maxStart ? maxStart : preferredStart;
    }

    private static string FormatSectionLabel(string rawLabel)
    {
        if (string.IsNullOrWhiteSpace(rawLabel))
        {
            return "<unnamed section>";
        }

        string label = rawLabel[0] == '*' ? rawLabel.Substring(1) : rawLabel;
        return label.Trim();
    }
}
