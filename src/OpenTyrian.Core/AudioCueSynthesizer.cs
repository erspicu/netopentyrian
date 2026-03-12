namespace OpenTyrian.Core;

public static class AudioCueSynthesizer
{
    public static AudioCueSample Create(AudioCueKind cue, int sampleRate, int channelCount)
    {
        switch (cue)
        {
            case AudioCueKind.Cursor:
                return CreateToneSequence(sampleRate, channelCount, new[] { 880.0 }, new[] { 0.028 }, 0.14);

            case AudioCueKind.Confirm:
                return CreateToneSequence(sampleRate, channelCount, new[] { 660.0, 990.0 }, new[] { 0.026, 0.034 }, 0.18);

            case AudioCueKind.Cancel:
                return CreateToneSequence(sampleRate, channelCount, new[] { 392.0, 294.0 }, new[] { 0.034, 0.050 }, 0.16);

            default:
                return CreateToneSequence(sampleRate, channelCount, new[] { 440.0 }, new[] { 0.030 }, 0.10);
        }
    }

    private static AudioCueSample CreateToneSequence(int sampleRate, int channelCount, double[] frequencies, double[] durations, double amplitude)
    {
        int totalFrames = 0;
        for (int i = 0; i < durations.Length; i++)
        {
            totalFrames += Math.Max(1, (int)(sampleRate * durations[i]));
        }

        byte[] buffer = new byte[totalFrames * channelCount * sizeof(short)];
        int frameOffset = 0;
        for (int i = 0; i < frequencies.Length && i < durations.Length; i++)
        {
            int segmentFrames = Math.Max(1, (int)(sampleRate * durations[i]));
            WriteTone(buffer, frameOffset, segmentFrames, sampleRate, channelCount, frequencies[i], amplitude);
            frameOffset += segmentFrames;
        }

        return new AudioCueSample
        {
            Buffer = buffer,
            FrameCount = totalFrames,
        };
    }

    private static void WriteTone(byte[] buffer, int startFrame, int frameCount, int sampleRate, int channelCount, double frequency, double amplitude)
    {
        double phaseStep = 2.0 * Math.PI * frequency / sampleRate;
        for (int i = 0; i < frameCount; i++)
        {
            double progress = frameCount <= 1 ? 1.0 : (double)i / (frameCount - 1);
            double envelope = 1.0 - progress;
            short sample = (short)(Math.Sin(i * phaseStep) * amplitude * envelope * short.MaxValue);

            int frameIndex = startFrame + i;
            int byteOffset = frameIndex * channelCount * sizeof(short);
            for (int channel = 0; channel < channelCount; channel++)
            {
                buffer[byteOffset++] = (byte)(sample & 0xFF);
                buffer[byteOffset++] = (byte)((sample >> 8) & 0xFF);
            }
        }
    }
}
