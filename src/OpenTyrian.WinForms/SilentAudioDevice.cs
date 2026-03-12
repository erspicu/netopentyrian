using OpenTyrian.Platform;

namespace OpenTyrian.WinForms;

internal sealed class SilentAudioDevice : IAudioDevice
{
    public string BackendName => "silent";

    public bool IsInitialized { get; private set; }

    public int SampleRate { get; private set; }

    public int ChannelCount { get; private set; }

    public void Initialize(int sampleRate, int channelCount)
    {
        SampleRate = sampleRate;
        ChannelCount = channelCount;
        IsInitialized = sampleRate > 0 && channelCount > 0;
    }

    public void SubmitSamples(byte[] pcmBuffer, int sampleCount)
    {
    }

    public void Shutdown()
    {
        IsInitialized = false;
        SampleRate = 0;
        ChannelCount = 0;
    }
}
