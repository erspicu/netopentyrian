namespace OpenTyrian.Core;

public sealed class EpisodeSessionScene : IScene
{
    private const int MaxAutoExecutionPasses = 32;

    private readonly EpisodeSessionState _sessionState;
    private OpenTyrian.Platform.InputSnapshot _previousInput;
    private EpisodeCommandExecutionResult _lastExecutionResult;

    public EpisodeSessionScene(EpisodeSessionState sessionState)
    {
        _sessionState = sessionState;
    }

    public IScene? Update(SceneResources resources, OpenTyrian.Platform.InputSnapshot input, double deltaSeconds)
    {
        bool cancelPressed = input.Cancel && !_previousInput.Cancel;
        bool confirmPressed = input.Confirm && !_previousInput.Confirm;
        bool upPressed = input.Up && !_previousInput.Up;
        bool downPressed = input.Down && !_previousInput.Down;

        IScene? autoScene = TryAutoExecuteCurrentSection(input);
        if (autoScene is not null)
        {
            return autoScene;
        }

        if (upPressed && _sessionState.CubeEntries.Count > 0)
        {
            _previousInput = input;
            return new DataCubeScene(_sessionState);
        }

        if (downPressed && _sessionState.ShopCategories.Count > 0)
        {
            _previousInput = input;
            return new UpgradeMenuScene(_sessionState);
        }

        if (confirmPressed)
        {
            _lastExecutionResult = EpisodeCommandInterpreter.ExecuteCurrentSection(_sessionState);
            if (_lastExecutionResult.ShopRequested && _sessionState.ShopCategories.Count > 0)
            {
                _previousInput = input;
                return new UpgradeMenuScene(_sessionState);
            }

            IScene? chainedAutoScene = TryAutoExecuteCurrentSection(input);
            if (chainedAutoScene is not null)
            {
                return chainedAutoScene;
            }
        }

        _previousInput = input;

        return cancelPressed ? new EpisodeSelectScene(_sessionState.StartMode) : null;
    }

    public void Render(IndexedFrameBuffer surface, SceneResources resources, double timeSeconds)
    {
        TitleScreenRenderer.RenderBackground(surface, resources, timeSeconds);
        TitleScreenRenderer.RenderTitleOverlay(surface, resources.FontRenderer, resources.PaletteCount);

        if (resources.FontRenderer is null)
        {
            return;
        }

        resources.FontRenderer.DrawShadowText(surface, 160, 84, "Episode Session State", FontKind.Normal, FontAlignment.Center, 15, 1, black: false, shadowDistance: 1);
        resources.FontRenderer.DrawText(surface, 160, 104, _sessionState.StartInfo.DisplayName, FontKind.Tiny, FontAlignment.Center, 14, 2, shadow: true);
        resources.FontRenderer.DrawText(surface, 160, 116, $"initial episode: {_sessionState.InitialEpisodeNumber}", FontKind.Tiny, FontAlignment.Center, 13, 0, shadow: true);
        resources.FontRenderer.DrawText(surface, 160, 128, $"current episode: {_sessionState.CurrentEpisodeNumber}", FontKind.Tiny, FontAlignment.Center, 13, 0, shadow: true);
        resources.FontRenderer.DrawText(surface, 160, 140, $"current level: {_sessionState.CurrentLevelNumber}", FontKind.Tiny, FontAlignment.Center, 13, 0, shadow: true);
        resources.FontRenderer.DrawText(surface, 160, 152, $"level count: {_sessionState.LevelCount}  first offset: {_sessionState.CurrentLevelOffset}", FontKind.Tiny, FontAlignment.Center, 13, 0, shadow: true);
        resources.FontRenderer.DrawText(surface, 160, 164, $"level file: {_sessionState.LevelFile}", FontKind.Tiny, FontAlignment.Center, 13, 0, shadow: true);
        resources.FontRenderer.DrawText(surface, 160, 176, $"episode file: {_sessionState.EpisodeFile}", FontKind.Tiny, FontAlignment.Center, 13, 0, shadow: true);
        resources.FontRenderer.DrawText(surface, 160, 188, $"cube file: {_sessionState.CubeFile}  end offset: {_sessionState.EndOffset}", FontKind.Tiny, FontAlignment.Center, 13, 0, shadow: true);
        resources.FontRenderer.DrawText(surface, 160, 196, $"script exists:{_sessionState.ScriptExists} len:{_sessionState.ScriptLength} sections:{_sessionState.ScriptSectionMarkerCount}", FontKind.Tiny, FontAlignment.Center, 13, 0, shadow: true);
        string currentSection = _sessionState.CurrentMainLevelEntry?.Section.Label ?? "<none>";
        resources.FontRenderer.DrawText(surface, 160, 204, $"main level {_sessionState.CurrentLevelNumber} section: {currentSection}", FontKind.Tiny, FontAlignment.Center, 13, 0, shadow: true);
        string commandSummary = _sessionState.CurrentMainLevelEntry is { Commands.Count: > 0 } entry
            ? $"{entry.Commands[0].Kind} ({entry.Commands.Count} cmds)"
            : "no recognized commands";
        resources.FontRenderer.DrawText(surface, 160, 212, $"section commands: {commandSummary}", FontKind.Tiny, FontAlignment.Center, 13, 0, shadow: true);
        resources.FontRenderer.DrawText(surface, 160, 220, $"cube exists:{_sessionState.CubeExists} len:{_sessionState.CubeLength} markers:{_sessionState.CubeSectionMarkerCount} entries:{_sessionState.CubeEntries.Count}", FontKind.Tiny, FontAlignment.Center, 13, 0, shadow: true);
        resources.FontRenderer.DrawText(surface, 160, 228, $"mode: {_sessionState.StartMode.GetDisplayName()} players:{_sessionState.PlayerCount} arcadeLike:{_sessionState.IsArcadeLikeMode}", FontKind.Tiny, FontAlignment.Center, 13, 0, shadow: true);
        resources.FontRenderer.DrawText(surface, 160, 236, $"cash:{_sessionState.Cash} assets:{_sessionState.GetTotalAssetValue(resources.ItemCatalog)} total:{_sessionState.GetTotalScore(resources.ItemCatalog)}", FontKind.Tiny, FontAlignment.Center, 13, 0, shadow: true);
        int firstItemRowCount = _sessionState.ItemAvailabilityMaxPerRow.Count > 0 ? _sessionState.ItemAvailabilityMaxPerRow[0] : 0;
        int firstItemValue = _sessionState.ItemAvailabilityRows.Count > 0 && _sessionState.ItemAvailabilityRows[0].Count > 0
            ? _sessionState.ItemAvailabilityRows[0][0]
            : 0;
        resources.FontRenderer.DrawText(surface, 160, 244, $"song:{_sessionState.ItemShopSongIndex} itemRows:{_sessionState.ItemAvailabilityBlockLineCount} firstMax:{firstItemRowCount} firstItem:{firstItemValue}", FontKind.Tiny, FontAlignment.Center, 13, 0, shadow: true);
        string firstShopCategory = _sessionState.ShopCategories.Count > 0
            ? $"{_sessionState.ShopCategories[0].DisplayName}:{_sessionState.ShopCategories[0].ItemCount}"
            : "no shop categories";
        resources.FontRenderer.DrawText(surface, 160, 252, $"shop map: {firstShopCategory}", FontKind.Tiny, FontAlignment.Center, 13, 0, shadow: true);
        resources.FontRenderer.DrawText(surface, 160, 260, $"loadout {_sessionState.PlayerLoadout.BuildSummary()}", FontKind.Tiny, FontAlignment.Center, 13, 0, shadow: true);
        resources.FontRenderer.DrawText(surface, 160, 268, $"fadeBlack:{_sessionState.FadeBlackRequested} autoMain:{_sessionState.AutoExecutedMainLevelNumber}", FontKind.Tiny, FontAlignment.Center, 13, 0, shadow: true);
        resources.FontRenderer.DrawText(surface, 160, 276, $"last exec: cmds={_lastExecutionResult.ExecutedCommands} changed={_lastExecutionResult.StateChanged} jumped={_lastExecutionResult.Jumped} shop={_lastExecutionResult.ShopRequested}", FontKind.Tiny, FontAlignment.Center, 13, 0, shadow: true);
        resources.FontRenderer.DrawText(surface, 160, 284, "Section commands auto-run on entry  Enter reruns  Up cubes  Down shop", FontKind.Tiny, FontAlignment.Center, 13, 0, shadow: true);
        resources.FontRenderer.DrawDark(surface, 160, 292, $"bonus:{_sessionState.BonusLevel} repeat:{_sessionState.GameHasRepeated} jumpBack:{_sessionState.JumpBackToEpisode1}", FontKind.Tiny, FontAlignment.Center, black: false);
    }

    private IScene? TryAutoExecuteCurrentSection(OpenTyrian.Platform.InputSnapshot input)
    {
        int autoExecutionPasses = 0;
        while (_sessionState.ShouldAutoExecuteCurrentMainLevel() && autoExecutionPasses < MaxAutoExecutionPasses)
        {
            autoExecutionPasses++;
            _sessionState.MarkCurrentMainLevelAutoExecuted();
            _lastExecutionResult = EpisodeCommandInterpreter.ExecuteCurrentSection(_sessionState);

            if (_lastExecutionResult.ShopRequested && _sessionState.ShopCategories.Count > 0)
            {
                _previousInput = input;
                return new UpgradeMenuScene(_sessionState);
            }

            if (!_lastExecutionResult.Jumped)
            {
                break;
            }
        }

        return null;
    }
}
