namespace OpenTyrian.Core;

public sealed class InstructionsScene : IScene, IScenePresentation
{
    private static readonly string[] Lines =
    {
        "Arrows or joystick move the menu cursor and ship.",
        "Enter or Space confirms the current selection.",
        "Esc or right mouse cancels and goes back.",
        "Mouse hover and click works in most menu scenes.",
        "New Game now follows title -> mode -> episode -> difficulty.",
        "Setup contains Jukebox. Network remains disabled.",
    };

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
        bool pointerConfirmPressed = input.PointerConfirm && !_previousInput.PointerConfirm;
        if (cancelPressed || confirmPressed || pointerConfirmPressed)
        {
            SceneAudio.PlayCancel(resources);
            _previousInput = input;
            return new TitleMenuScene();
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

        resources.FontRenderer.DrawShadowText(surface, 160, 20, "Instructions", FontKind.Normal, FontAlignment.Center, 15, 0, black: false, shadowDistance: 1);
        for (int i = 0; i < Lines.Length; i++)
        {
            resources.FontRenderer.DrawText(surface, 20, 50 + (i * 18), Lines[i], FontKind.Tiny, FontAlignment.Left, 13, 0, shadow: true);
        }

        resources.FontRenderer.DrawDark(surface, 160, 190, "Enter/Click/Esc returns to title menu", FontKind.Tiny, FontAlignment.Center, black: false);
    }
}
