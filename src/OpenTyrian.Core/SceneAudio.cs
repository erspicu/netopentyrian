namespace OpenTyrian.Core;

public static class SceneAudio
{
    public static void PlayCursor(SceneResources resources)
    {
        resources.AudioCueSink?.Enqueue(AudioCueKind.Cursor);
    }

    public static void PlayConfirm(SceneResources resources)
    {
        resources.AudioCueSink?.Enqueue(AudioCueKind.Confirm);
    }

    public static void PlayCancel(SceneResources resources)
    {
        resources.AudioCueSink?.Enqueue(AudioCueKind.Cancel);
    }
}
