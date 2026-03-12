namespace OpenTyrian.Core;

public sealed class TitleScene : IScene
{
    private OpenTyrian.Platform.InputSnapshot _previousInput;

    public IScene? Update(SceneResources resources, OpenTyrian.Platform.InputSnapshot input, double deltaSeconds)
    {
        bool confirmPressed = input.Confirm && !_previousInput.Confirm;
        bool downPressed = input.Down && !_previousInput.Down;

        if (confirmPressed || downPressed)
        {
            _previousInput = input;
            return new MainMenuScene();
        }

        _previousInput = input;
        return null;
    }

    public void Render(IndexedFrameBuffer surface, SceneResources resources, double timeSeconds)
    {
        TitleScreenRenderer.RenderBackground(surface, resources, timeSeconds);
        TitleScreenRenderer.RenderTitleOverlay(surface, resources.FontRenderer, resources.PaletteCount);
        resources.FontRenderer?.DrawShadowText(
            surface,
            160,
            150,
            "Press ~Enter~ to open menu",
            FontKind.Tiny,
            FontAlignment.Center,
            12,
            2,
            black: false,
            shadowDistance: 1);
    }
}
