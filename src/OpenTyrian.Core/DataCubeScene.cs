namespace OpenTyrian.Core;

public sealed class DataCubeScene : IScene
{
    private const int ContentX = 18;
    private const int ContentY = 100;
    private const int ContentWidth = 284;
    private const int ContentLineHeight = 9;
    private const int VisibleContentLines = 9;

    private readonly EpisodeSessionState _sessionState;
    private OpenTyrian.Platform.InputSnapshot _previousInput;
    private int _selectedEntryIndex;
    private int _scrollOffset;

    public DataCubeScene(EpisodeSessionState sessionState)
    {
        _sessionState = sessionState;
        _selectedEntryIndex = 0;
        _scrollOffset = 0;
    }

    public IScene? Update(SceneResources resources, OpenTyrian.Platform.InputSnapshot input, double deltaSeconds)
    {
        bool cancelPressed = input.Cancel && !_previousInput.Cancel;
        bool upPressed = input.Up && !_previousInput.Up;
        bool downPressed = input.Down && !_previousInput.Down;
        bool leftPressed = input.Left && !_previousInput.Left;
        bool rightPressed = input.Right && !_previousInput.Right;

        if (cancelPressed)
        {
            _previousInput = input;
            return new EpisodeSessionScene(_sessionState);
        }

        if (_sessionState.CubeEntries.Count > 0)
        {
            if (leftPressed)
            {
                _selectedEntryIndex = _selectedEntryIndex == 0
                    ? _sessionState.CubeEntries.Count - 1
                    : _selectedEntryIndex - 1;
                _scrollOffset = 0;
            }

            if (rightPressed)
            {
                _selectedEntryIndex = (_selectedEntryIndex + 1) % _sessionState.CubeEntries.Count;
                _scrollOffset = 0;
            }

            IList<string> wrappedLines = BuildWrappedLines(GetSelectedEntry(), resources.FontRenderer);
            int maxScrollOffset = Math.Max(0, wrappedLines.Count - VisibleContentLines);

            if (upPressed && _scrollOffset > 0)
            {
                _scrollOffset--;
            }

            if (downPressed && _scrollOffset < maxScrollOffset)
            {
                _scrollOffset++;
            }
        }

        _previousInput = input;
        return null;
    }

    public void Render(IndexedFrameBuffer surface, SceneResources resources, double timeSeconds)
    {
        TitleScreenRenderer.RenderBackground(surface, resources, timeSeconds);
        TitleScreenRenderer.RenderTitleOverlay(surface, resources.FontRenderer, resources.PaletteCount);

        if (resources.FontRenderer is null)
        {
            return;
        }

        resources.FontRenderer.DrawShadowText(surface, 160, 78, "Data Cubes", FontKind.Normal, FontAlignment.Center, 15, 0, black: false, shadowDistance: 1);

        if (_sessionState.CubeEntries.Count == 0)
        {
            resources.FontRenderer.DrawText(surface, 160, 112, "No data cube entries decoded from cubetxt yet", FontKind.Tiny, FontAlignment.Center, 12, 0, shadow: true);
            resources.FontRenderer.DrawDark(surface, 160, 192, "Esc returns to episode session", FontKind.Tiny, FontAlignment.Center, black: false);
            return;
        }

        CubeTextEntry entry = GetSelectedEntry();
        IList<string> wrappedLines = BuildWrappedLines(entry, resources.FontRenderer);
        int clampedScrollOffset = Math.Min(_scrollOffset, Math.Max(0, wrappedLines.Count - VisibleContentLines));
        _scrollOffset = clampedScrollOffset;

        resources.FontRenderer.DrawText(
            surface,
            160,
            90,
            string.Format("entry {0}/{1}", _selectedEntryIndex + 1, _sessionState.CubeEntries.Count),
            FontKind.Tiny,
            FontAlignment.Center,
            14,
            0,
            shadow: true);
        resources.FontRenderer.DrawText(surface, 160, 98, entry.Title, FontKind.Small, FontAlignment.Center, 15, 0, shadow: true);

        for (int i = 0; i < VisibleContentLines; i++)
        {
            int lineIndex = _scrollOffset + i;
            if (lineIndex >= wrappedLines.Count)
            {
                break;
            }

            resources.FontRenderer.DrawText(
                surface,
                ContentX,
                ContentY + (i * ContentLineHeight),
                wrappedLines[lineIndex],
                FontKind.Tiny,
                FontAlignment.Left,
                13,
                0,
                shadow: true);
        }

        if (_scrollOffset > 0)
        {
            resources.FontRenderer.DrawText(surface, 304, ContentY - 10, "^", FontKind.Tiny, FontAlignment.Center, 12, 0, shadow: true);
        }

        if (_scrollOffset + VisibleContentLines < wrappedLines.Count)
        {
            resources.FontRenderer.DrawText(surface, 304, ContentY + (VisibleContentLines * ContentLineHeight), "v", FontKind.Tiny, FontAlignment.Center, 12, 0, shadow: true);
        }

        resources.FontRenderer.DrawText(
            surface,
            160,
            188,
            string.Format("lines {0}-{1}/{2}", Math.Min(wrappedLines.Count, _scrollOffset + 1), Math.Min(wrappedLines.Count, _scrollOffset + VisibleContentLines), wrappedLines.Count),
            FontKind.Tiny,
            FontAlignment.Center,
            12,
            0,
            shadow: true);
        resources.FontRenderer.DrawDark(surface, 160, 198, "Left/Right entry  Up/Down scroll  Esc back", FontKind.Tiny, FontAlignment.Center, black: false);
    }

    private CubeTextEntry GetSelectedEntry()
    {
        if (_selectedEntryIndex < 0 || _selectedEntryIndex >= _sessionState.CubeEntries.Count)
        {
            _selectedEntryIndex = 0;
        }

        return _sessionState.CubeEntries[_selectedEntryIndex];
    }

    private static IList<string> BuildWrappedLines(CubeTextEntry entry, TyrianFontRenderer? fontRenderer)
    {
        List<string> wrappedLines = [];

        foreach (string sourceLine in entry.Lines)
        {
            AppendWrappedLine(wrappedLines, sourceLine ?? string.Empty, fontRenderer);
        }

        return wrappedLines.Count > 0 ? wrappedLines : [string.Empty];
    }

    private static void AppendWrappedLine(ICollection<string> wrappedLines, string sourceLine, TyrianFontRenderer? fontRenderer)
    {
        if (string.IsNullOrWhiteSpace(sourceLine))
        {
            wrappedLines.Add(string.Empty);
            return;
        }

        string remaining = sourceLine.Trim();
        while (remaining.Length > 0)
        {
            string nextLine = TakeWrappedSegment(remaining, fontRenderer);
            wrappedLines.Add(nextLine);
            remaining = remaining.Substring(nextLine.Length).TrimStart();
        }
    }

    private static string TakeWrappedSegment(string sourceLine, TyrianFontRenderer? fontRenderer)
    {
        if (fontRenderer is null)
        {
            return sourceLine.Length <= 40 ? sourceLine : sourceLine.Substring(0, 40);
        }

        if (fontRenderer.MeasureText(sourceLine, FontKind.Tiny) <= ContentWidth)
        {
            return sourceLine;
        }

        int bestBreak = -1;
        for (int i = 0; i < sourceLine.Length; i++)
        {
            string candidate = sourceLine.Substring(0, i + 1);
            if (fontRenderer.MeasureText(candidate, FontKind.Tiny) > ContentWidth)
            {
                break;
            }

            if (char.IsWhiteSpace(sourceLine[i]))
            {
                bestBreak = i;
            }
        }

        if (bestBreak >= 0)
        {
            return sourceLine.Substring(0, bestBreak).TrimEnd();
        }

        int fallbackLength = 1;
        while (fallbackLength < sourceLine.Length &&
               fontRenderer.MeasureText(sourceLine.Substring(0, fallbackLength + 1), FontKind.Tiny) <= ContentWidth)
        {
            fallbackLength++;
        }

        return sourceLine.Substring(0, fallbackLength);
    }
}
