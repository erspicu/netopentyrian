namespace OpenTyrian.Core;

public interface IAudioCueSink
{
    void Enqueue(AudioCueKind cue);
}
