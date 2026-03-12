namespace OpenTyrian.Core;

public interface ICustomMusicScene
{
    string MusicCacheKey { get; }

    AudioCueSample CreateMusicTrack(int sampleRate, int channelCount);
}
