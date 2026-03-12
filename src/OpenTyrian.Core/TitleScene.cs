namespace OpenTyrian.Core;

public sealed class TitleScene : IScene, IScenePresentation
{
    private OpenTyrian.Platform.InputSnapshot _previousInput;

    public int? BackgroundPictureNumber
    {
        get { return 4; }
    }

    public SceneMusicKind? MusicOverride
    {
        get { return SceneMusicKind.Title; }
    }

    public IScene? Update(SceneResources resources, OpenTyrian.Platform.InputSnapshot input, double deltaSeconds)
    {
        bool confirmPressed = input.Confirm && !_previousInput.Confirm;
        bool downPressed = input.Down && !_previousInput.Down;
        bool pointerConfirmPressed = input.PointerConfirm && !_previousInput.PointerConfirm;

        if (confirmPressed || downPressed || pointerConfirmPressed)
        {
            SceneAudio.PlayConfirm(resources);
            _previousInput = input;
            return new TitleMenuScene();
        }

        _previousInput = input;
        return null;
    }

    public void Render(IndexedFrameBuffer surface, SceneResources resources, double timeSeconds)
    {
        TitleScreenRenderer.RenderPictureBackground(surface, resources, 4, includeOverlays: false);
        resources.FontRenderer?.DrawShadowText(
            surface,
            160,
            150,
            "Press ~Enter~ or click to open title menu",
            FontKind.Tiny,
            FontAlignment.Center,
            12,
            2,
            black: false,
            shadowDistance: 1);
    }
}
