namespace OpenTyrian.Core;

public sealed class InstructionsScene : IScene, IScenePresentation
{
    private const int MaxPage = 8;
    private static readonly int[] TopicStart = { 0, 1, 2, 3, 7, 255 };
    private static readonly int[][] PageMessageIds =
    {
        new[] { 2, 5, 21, 1, 28 },
        new[] { 1, 2, 21, 28 },
        new[] { 5, 6, 7 },
        new[] { 8, 9, 10, 11, 13 },
        new[] { 14, 15, 16 },
        new[] { 17, 18, 20 },
        new[] { 21, 22, 23, 24 },
        new[] { 25, 26, 27, 28, 29 },
    };

    private static readonly int[][] PageMessageY =
    {
        new[] { 20, 50, 80, 110, 140 },
        new[] { 20, 60, 100, 140 },
        new[] { 20, 70, 110 },
        new[] { 20, 55, 87, 120, 170 },
        new[] { 20, 80, 120 },
        new[] { 20, 40, 130 },
        new[] { 20, 70, 110, 140 },
        new[] { 20, 60, 100, 140, 170 },
    };

    private OpenTyrian.Platform.InputSnapshot _previousInput;
    private bool _inTopicMenu = true;
    private int _selectedIndex;
    private int _currentPage = 1;

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
        bool upPressed = input.Up && !_previousInput.Up;
        bool downPressed = input.Down && !_previousInput.Down;
        bool leftPressed = input.Left && !_previousInput.Left;
        bool rightPressed = input.Right && !_previousInput.Right;
        bool pointerConfirmPressed = input.PointerConfirm && !_previousInput.PointerConfirm;
        bool pointerCancelPressed = input.PointerCancel && !_previousInput.PointerCancel;

        if (_inTopicMenu)
        {
            int menuItemCount = GetMenuItemCount(resources.GameplayText);
            int? hoveredIndex = input.PointerPresent
                ? HitTestTopicItem(resources.FontRenderer, resources.GameplayText, input.PointerX, input.PointerY)
                : null;

            if (hoveredIndex.HasValue && hoveredIndex.Value < menuItemCount)
            {
                if (_selectedIndex != hoveredIndex.Value)
                {
                    SceneAudio.PlayCursor(resources);
                }

                _selectedIndex = hoveredIndex.Value;
            }

            if (cancelPressed || pointerCancelPressed)
            {
                SceneAudio.PlayCancel(resources);
                _previousInput = input;
                return new TitleMenuScene();
            }

            if (upPressed)
            {
                SceneAudio.PlayCursor(resources);
                _selectedIndex = _selectedIndex == 0 ? menuItemCount - 1 : _selectedIndex - 1;
            }

            if (downPressed)
            {
                SceneAudio.PlayCursor(resources);
                _selectedIndex = (_selectedIndex + 1) % menuItemCount;
            }

            if (confirmPressed || (pointerConfirmPressed && hoveredIndex.HasValue))
            {
                if (_selectedIndex == menuItemCount - 1)
                {
                    SceneAudio.PlayCancel(resources);
                    _previousInput = input;
                    return new TitleMenuScene();
                }

                SceneAudio.PlayConfirm(resources);
                _currentPage = GetStartPageForTopic(_selectedIndex);
                _inTopicMenu = false;
            }
        }
        else
        {
            if (cancelPressed || pointerCancelPressed)
            {
                SceneAudio.PlayCancel(resources);
                _selectedIndex = GetTopicIndexForPage(_currentPage);
                _inTopicMenu = true;
                _previousInput = input;
                return null;
            }

            if (leftPressed)
            {
                SceneAudio.PlayCursor(resources);
                AdvancePage(-1, resources.GameplayText);
            }
            else if (rightPressed || confirmPressed)
            {
                SceneAudio.PlayCursor(resources);
                AdvancePage(1, resources.GameplayText);
            }
            else if (pointerConfirmPressed)
            {
                SceneAudio.PlayCursor(resources);
                AdvancePage(input.PointerX < 160 ? -1 : 1, resources.GameplayText);
            }
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

        if (_inTopicMenu)
        {
            RenderTopicMenu(surface, resources);
        }
        else
        {
            RenderPage(surface, resources);
        }
    }

    private void RenderTopicMenu(IndexedFrameBuffer surface, SceneResources resources)
    {
        string header = GetTopicHeader(resources.GameplayText);
        IList<string> items = GetTopicItems(resources.GameplayText);

        resources.FontRenderer!.DrawShadowText(surface, 160, 30, header, FontKind.Normal, FontAlignment.Center, 15, -3, black: false, shadowDistance: 2);

        for (int i = 0; i < items.Count; i++)
        {
            int y = 60 + (i * 20);
            resources.FontRenderer.DrawShadowText(
                surface,
                160,
                y,
                items[i],
                FontKind.Small,
                FontAlignment.Center,
                15,
                -3 + (i == _selectedIndex ? 2 : 0),
                black: false,
                shadowDistance: 2);
        }
    }

    private void RenderPage(IndexedFrameBuffer surface, SceneResources resources)
    {
        string topicName = GetTopicLabel(resources.GameplayText, GetTopicIndexForPage(_currentPage));
        string pageLabel = GetMiscText(resources.GameplayText, 24, "Page");
        string pageOfLabel = GetMiscText(resources.GameplayText, 25, "Page");

        Vga256.FillRectangleWH(surface, 0, 192, 320, 8, 0);
        resources.FontRenderer!.DrawShadowText(surface, 160, 1, topicName, FontKind.Small, FontAlignment.Center, 15, -3, black: false, shadowDistance: 2);
        resources.FontRenderer.DrawText(surface, 10, 192, string.Format("{0} {1}", pageLabel, GetPageWithinTopic(_currentPage)), FontKind.Tiny, FontAlignment.Left, 13, 5, shadow: false);
        resources.FontRenderer.DrawText(surface, 310, 192, string.Format("{0} {1} of {2}", pageOfLabel, _currentPage, MaxPage), FontKind.Tiny, FontAlignment.Right, 13, 5, shadow: false);

        int[] messageIds = PageMessageIds[_currentPage - 1];
        int[] messageY = PageMessageY[_currentPage - 1];

        for (int i = 0; i < messageIds.Length && i < messageY.Length; i++)
        {
            DrawHelpBox(surface, resources.FontRenderer, 10, messageY[i], ResolveHelpText(resources.GameplayText, messageIds[i]));
        }
    }

    private void AdvancePage(int delta, GameplayTextInfo? gameplayText)
    {
        int nextPage = _currentPage + delta;
        if (nextPage < 1)
        {
            _selectedIndex = 0;
            _inTopicMenu = true;
            return;
        }

        if (nextPage > MaxPage)
        {
            _selectedIndex = GetMenuItemCount(gameplayText) - 1;
            _inTopicMenu = true;
            return;
        }

        _currentPage = nextPage;
    }

    private static void DrawHelpBox(IndexedFrameBuffer surface, TyrianFontRenderer fontRenderer, int x, int y, string text)
    {
        IList<string> wrappedLines = WrapText(text, fontRenderer, 284);
        int height = Math.Max(12, 8 + (wrappedLines.Count * 9));
        int bottom = y + height;

        Vga256.FillRectangleXY(surface, x, y, 308, bottom, 0x20);
        Vga256.ShadeRectangle(surface, x + 1, y + 1, 307, bottom - 1);
        Vga256.DrawRectangle(surface, x, y, 308, bottom, 0x2C);

        for (int i = 0; i < wrappedLines.Count; i++)
        {
            fontRenderer.DrawText(surface, x + 6, y + 4 + (i * 9), wrappedLines[i], FontKind.Tiny, FontAlignment.Left, 12, 0, shadow: true);
        }
    }

    private static IList<string> WrapText(string text, TyrianFontRenderer fontRenderer, int maxWidth)
    {
        List<string> wrappedLines = [];
        if (string.IsNullOrWhiteSpace(text))
        {
            wrappedLines.Add(string.Empty);
            return wrappedLines;
        }

        string remaining = text.Trim();
        while (remaining.Length > 0)
        {
            string nextLine = TakeWrappedSegment(remaining, fontRenderer, maxWidth);
            wrappedLines.Add(nextLine);
            remaining = remaining.Substring(nextLine.Length).TrimStart();
        }

        return wrappedLines;
    }

    private static string TakeWrappedSegment(string sourceLine, TyrianFontRenderer fontRenderer, int maxWidth)
    {
        if (fontRenderer.MeasureText(sourceLine, FontKind.Tiny) <= maxWidth)
        {
            return sourceLine;
        }

        int bestBreak = -1;
        for (int i = 0; i < sourceLine.Length; i++)
        {
            string candidate = sourceLine.Substring(0, i + 1);
            if (fontRenderer.MeasureText(candidate, FontKind.Tiny) > maxWidth)
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
               fontRenderer.MeasureText(sourceLine.Substring(0, fallbackLength + 1), FontKind.Tiny) <= maxWidth)
        {
            fallbackLength++;
        }

        return sourceLine.Substring(0, fallbackLength);
    }

    private static int GetMenuItemCount(GameplayTextInfo? gameplayText)
    {
        return GetTopicItems(gameplayText).Count;
    }

    private static IList<string> GetTopicItems(GameplayTextInfo? gameplayText)
    {
        IList<string>? topicNames = gameplayText?.TopicNames;
        if (topicNames is not null && topicNames.Count >= 2)
        {
            return topicNames.Skip(1).ToArray();
        }

        return new[] { "One-Player Menu", "Two-Player Menu", "Upgrade Ship", "Options", "Done" };
    }

    private static string GetTopicHeader(GameplayTextInfo? gameplayText)
    {
        IList<string>? topicNames = gameplayText?.TopicNames;
        return topicNames is not null && topicNames.Count > 0
            ? topicNames[0]
            : "Instructions";
    }

    private static string GetTopicLabel(GameplayTextInfo? gameplayText, int topicIndex)
    {
        IList<string> items = GetTopicItems(gameplayText);
        if (topicIndex >= 0 && topicIndex < items.Count)
        {
            return items[topicIndex];
        }

        return "Instructions";
    }

    private static int GetStartPageForTopic(int topicIndex)
    {
        if (topicIndex < 0)
        {
            return 1;
        }

        int arrayIndex = Math.Min(topicIndex + 1, TopicStart.Length - 1);
        int page = TopicStart[arrayIndex];
        return page <= 0 || page > MaxPage ? 1 : page;
    }

    private static int GetTopicIndexForPage(int page)
    {
        if (page <= 1)
        {
            return 0;
        }

        if (page == 2)
        {
            return 1;
        }

        if (page >= 3 && page <= 6)
        {
            return 2;
        }

        return 3;
    }

    private static int GetPageWithinTopic(int page)
    {
        switch (GetTopicIndexForPage(page))
        {
            case 0:
            case 1:
                return 1;
            case 2:
                return page - 2;
            default:
                return page - 6;
        }
    }

    private static string ResolveHelpText(GameplayTextInfo? gameplayText, int messageId)
    {
        IList<string>? helpText = gameplayText?.HelpText;
        int index = messageId - 1;
        if (helpText is not null && index >= 0 && index < helpText.Count && !string.IsNullOrWhiteSpace(helpText[index]))
        {
            return helpText[index];
        }

        return string.Format("Help entry {0}", messageId);
    }

    private static string GetMiscText(GameplayTextInfo? gameplayText, int index, string fallback)
    {
        IList<string>? miscText = gameplayText?.MiscText;
        if (miscText is not null && index >= 0 && index < miscText.Count && !string.IsNullOrWhiteSpace(miscText[index]))
        {
            return miscText[index];
        }

        return fallback;
    }

    private static int? HitTestTopicItem(TyrianFontRenderer? fontRenderer, GameplayTextInfo? gameplayText, int x, int y)
    {
        IList<string> items = GetTopicItems(gameplayText);
        for (int i = 0; i < items.Count; i++)
        {
            int textWidth = fontRenderer is not null ? fontRenderer.MeasureText(items[i], FontKind.Small) : 180;
            int left = 160 - (textWidth / 2);
            int right = left + textWidth;
            int top = 60 + (i * 20);
            int bottom = top + 13;
            if (x >= left && x < right && y >= top && y < bottom)
            {
                return i;
            }
        }

        return null;
    }
}
