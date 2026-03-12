namespace OpenTyrian.Core;

public sealed class IntroLogosScene : IScene, IScenePresentation, ISceneFadeOverlay
{
    private const double FadeSeconds = 0.35;

    private readonly IntroStage[] _stages =
    {
        new IntroStage(10, 3.6),
        new IntroStage(12, 2.4),
    };

    private OpenTyrian.Platform.InputSnapshot _previousInput;
    private int _stageIndex;
    private double _stageTimeSeconds;

    public int? BackgroundPictureNumber
    {
        get { return _stages[_stageIndex].PictureNumber; }
    }

    public SceneMusicKind? MusicOverride
    {
        get { return SceneMusicKind.Silence; }
    }

    public double FadeToBlackAmount
    {
        get
        {
            IntroStage stage = _stages[_stageIndex];
            double fadeAmount = 0.0;

            if (_stageTimeSeconds < FadeSeconds)
            {
                fadeAmount = 1.0 - (_stageTimeSeconds / FadeSeconds);
            }

            double remainingSeconds = stage.DurationSeconds - _stageTimeSeconds;
            if (remainingSeconds < FadeSeconds)
            {
                fadeAmount = Math.Max(fadeAmount, 1.0 - (remainingSeconds / FadeSeconds));
            }

            if (fadeAmount < 0.0)
            {
                return 0.0;
            }

            return fadeAmount > 1.0 ? 1.0 : fadeAmount;
        }
    }

    public IScene? Update(SceneResources resources, OpenTyrian.Platform.InputSnapshot input, double deltaSeconds)
    {
        bool confirmPressed = input.Confirm && !_previousInput.Confirm;
        bool cancelPressed = input.Cancel && !_previousInput.Cancel;
        bool pointerConfirmPressed = input.PointerConfirm && !_previousInput.PointerConfirm;
        bool skipRequested = confirmPressed || cancelPressed || pointerConfirmPressed;

        _stageTimeSeconds += deltaSeconds;
        if (skipRequested || _stageTimeSeconds >= _stages[_stageIndex].DurationSeconds)
        {
            if (_stageIndex + 1 < _stages.Length && !skipRequested)
            {
                _stageIndex++;
                _stageTimeSeconds = 0.0;
            }
            else
            {
                _previousInput = input;
                return new TitleMenuScene();
            }
        }

        _previousInput = input;
        return null;
    }

    public void Render(IndexedFrameBuffer surface, SceneResources resources, double timeSeconds)
    {
        TitleScreenRenderer.RenderPictureBackground(surface, resources, _stages[_stageIndex].PictureNumber, includeOverlays: false);
    }

    private struct IntroStage
    {
        public IntroStage(int pictureNumber, double durationSeconds)
        {
            PictureNumber = pictureNumber;
            DurationSeconds = durationSeconds;
        }

        public int PictureNumber { get; private set; }

        public double DurationSeconds { get; private set; }
    }
}
