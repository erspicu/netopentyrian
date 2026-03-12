namespace OpenTyrian.Platform;

public interface IAudioDevice
{
    string BackendName { get; }

    bool IsInitialized { get; }

    int SampleRate { get; }

    int ChannelCount { get; }

    void Initialize(int sampleRate, int channelCount);

    void SubmitSamples(byte[] pcmBuffer, int sampleCount);

    void Shutdown();
}
