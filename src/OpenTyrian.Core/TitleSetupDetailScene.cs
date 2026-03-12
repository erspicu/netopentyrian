namespace OpenTyrian.Core;

public sealed class TitleSetupDetailScene : IScene, IScenePresentation
{
    private readonly string _title;
    private readonly string[] _lines;
    private OpenTyrian.Platform.InputSnapshot _previousInput;

    public TitleSetupDetailScene(string title, string[] lines)
    {
        _title = title;
        _lines = lines;
    }

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
        bool pointerConfirmPressed = input.PointerConfirm && !_previousInput.PointerConfirm;

        if (cancelPressed || confirmPressed || pointerConfirmPressed)
        {
            if (cancelPressed)
            {
                SceneAudio.PlayCancel(resources);
            }
            else
            {
                SceneAudio.PlayConfirm(resources);
            }

            _previousInput = input;
            return new TitleSetupScene();
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

        resources.FontRenderer.DrawShadowText(surface, 160, 10, _title, FontKind.Normal, FontAlignment.Center, 15, -3, black: false, shadowDistance: 2);

        for (int i = 0; i < _lines.Length; i++)
        {
            resources.FontRenderer.DrawText(surface, 26, 58 + (i * 20), _lines[i], FontKind.Tiny, FontAlignment.Left, 13, 0, shadow: true);
        }

        resources.FontRenderer.DrawDark(surface, 160, 190, "Enter or Esc returns to Setup", FontKind.Tiny, FontAlignment.Center, black: false);
    }
}
