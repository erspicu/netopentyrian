namespace OpenTyrian.Core;

public interface ICustomMusicScene
{
    string MusicCacheKey { get; }

    int? MusicTrackIndex { get; }

    bool StopMusic { get; }

    AudioCueSample CreateFallbackMusicTrack(int sampleRate, int channelCount);
}
