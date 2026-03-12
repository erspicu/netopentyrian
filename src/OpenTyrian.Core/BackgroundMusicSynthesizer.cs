namespace OpenTyrian.Core;

public static class BackgroundMusicSynthesizer
{
    public static AudioCueSample Create(SceneMusicKind kind, int sampleRate, int channelCount)
    {
        switch (kind)
        {
            case SceneMusicKind.Title:
                return CreateLoop(sampleRate, channelCount, 92, new[] { 69, 72, 76, 72, 67, 71, 74, 71, 65, 69, 72, 69, 64, 67, 71, 67 }, new[] { 45, 45, 48, 48, 43, 43, 47, 47 }, 0.070, 0.050);

            case SceneMusicKind.Menu:
                return CreateLoop(sampleRate, channelCount, 108, new[] { 72, 76, 79, 76, 71, 74, 79, 74, 69, 72, 76, 72, 67, 71, 74, 71 }, new[] { 48, 48, 43, 43, 45, 45, 40, 40 }, 0.060, 0.048);

            case SceneMusicKind.Gameplay:
                return CreateLoop(sampleRate, channelCount, 132, new[] { 76, 79, 81, 79, 74, 76, 79, 76, 72, 74, 76, 74, 71, 72, 74, 72 }, new[] { 40, 40, 43, 43, 38, 38, 43, 43 }, 0.055, 0.055);

            case SceneMusicKind.Shop:
                return CreateLoop(sampleRate, channelCount, 116, new[] { 79, 83, 86, 83, 78, 81, 84, 81, 76, 79, 83, 79, 74, 78, 81, 78 }, new[] { 52, 52, 48, 48, 50, 50, 45, 45 }, 0.055, 0.045);

            default:
                return new AudioCueSample
                {
                    Buffer = new byte[0],
                    FrameCount = 0,
                };
        }
    }

    private static AudioCueSample CreateLoop(int sampleRate, int channelCount, int bpm, int[] leadNotes, int[] bassNotes, double leadAmplitude, double bassAmplitude)
    {
        int beatFrames = Math.Max(1, (sampleRate * 60) / bpm);
        int totalFrames = beatFrames * leadNotes.Length;
        byte[] buffer = new byte[totalFrames * channelCount * sizeof(short)];

        for (int frame = 0; frame < totalFrames; frame++)
        {
            int beatIndex = frame / beatFrames;
            int localFrame = frame % beatFrames;
            double beatProgress = (double)localFrame / beatFrames;
            double leadEnvelope = 1.0 - (beatProgress * 0.55);
            double bassEnvelope = 1.0 - (beatProgress * 0.35);

            double lead = CreateVoice(localFrame, sampleRate, leadNotes[beatIndex], leadAmplitude * leadEnvelope);
            double harmony = CreateVoice(localFrame, sampleRate, leadNotes[(beatIndex + 2) % leadNotes.Length], leadAmplitude * 0.18 * (1.0 - (beatProgress * 0.70)));
            double bass = CreateVoice(localFrame, sampleRate, bassNotes[beatIndex % bassNotes.Length], bassAmplitude * bassEnvelope);
            double pulse = CreatePulseLayer(localFrame, beatFrames, beatIndex, beatProgress, kindHint: bpm >= 130);

            short sample = ClampToPcm((lead + harmony + bass + pulse) * short.MaxValue);
            WriteFrame(buffer, frame, channelCount, sample);
        }

        return new AudioCueSample
        {
            Buffer = buffer,
            FrameCount = totalFrames,
        };
    }

    private static double CreateVoice(int localFrame, int sampleRate, int midiNote, double amplitude)
    {
        if (midiNote < 0 || amplitude <= 0.0)
        {
            return 0.0;
        }

        double frequency = 440.0 * Math.Pow(2.0, (midiNote - 69) / 12.0);
        double time = (double)localFrame / sampleRate;
        return Math.Sin(time * frequency * 2.0 * Math.PI) * amplitude;
    }

    private static double CreatePulseLayer(int localFrame, int beatFrames, int beatIndex, double beatProgress, bool kindHint)
    {
        if (!kindHint)
        {
            return beatIndex % 4 == 0
                ? Math.Sin(beatProgress * 2.0 * Math.PI * 6.0) * 0.010 * (1.0 - beatProgress)
                : 0.0;
        }

        double pulse = (beatIndex % 2 == 0 ? 1.0 : -1.0) * 0.012 * (1.0 - beatProgress);
        if (localFrame < beatFrames / 8)
        {
            pulse += 0.020 * (1.0 - ((double)localFrame / Math.Max(1, beatFrames / 8)));
        }

        return pulse;
    }

    private static short ClampToPcm(double value)
    {
        if (value > short.MaxValue)
        {
            return short.MaxValue;
        }

        if (value < short.MinValue)
        {
            return short.MinValue;
        }

        return (short)value;
    }

    private static void WriteFrame(byte[] buffer, int frameIndex, int channelCount, short sample)
    {
        int byteOffset = frameIndex * channelCount * sizeof(short);
        for (int channel = 0; channel < channelCount; channel++)
        {
            buffer[byteOffset++] = (byte)(sample & 0xFF);
            buffer[byteOffset++] = (byte)((sample >> 8) & 0xFF);
        }
    }
}
