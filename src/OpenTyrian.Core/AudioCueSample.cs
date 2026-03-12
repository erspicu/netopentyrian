namespace OpenTyrian.Core;

public sealed class AudioCueSample
{
    public required byte[] Buffer { get; init; }

    public required int FrameCount { get; init; }
}
