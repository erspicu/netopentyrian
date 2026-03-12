using System.Runtime.InteropServices;
using OpenTyrian.Platform;

namespace OpenTyrian.WinForms;

internal sealed class WaveOutAudioDevice : IAudioDevice
{
    private const int WaveMapper = -1;
    private const ushort WaveFormatPcm = 1;
    private const int CallbackNull = 0;
    private const uint HeaderInQueue = 0x00000010u;
    private const int BufferCount = 4;
    private const int FramesPerBuffer = 2048;

    [StructLayout(LayoutKind.Sequential)]
    private struct WaveFormatEx
    {
        public ushort FormatTag;
        public ushort Channels;
        public uint SamplesPerSec;
        public uint AvgBytesPerSec;
        public ushort BlockAlign;
        public ushort BitsPerSample;
        public ushort ExtraSize;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct WaveHeader
    {
        public IntPtr Data;
        public uint BufferLength;
        public uint BytesRecorded;
        public IntPtr UserData;
        public uint Flags;
        public uint Loops;
        public IntPtr Next;
        public IntPtr Reserved;
    }

    [DllImport("winmm.dll")]
    private static extern int waveOutOpen(out IntPtr handle, int deviceId, ref WaveFormatEx format, IntPtr callback, IntPtr instance, int flags);

    [DllImport("winmm.dll")]
    private static extern int waveOutPrepareHeader(IntPtr handle, IntPtr header, int size);

    [DllImport("winmm.dll")]
    private static extern int waveOutUnprepareHeader(IntPtr handle, IntPtr header, int size);

    [DllImport("winmm.dll")]
    private static extern int waveOutWrite(IntPtr handle, IntPtr header, int size);

    [DllImport("winmm.dll")]
    private static extern int waveOutReset(IntPtr handle);

    [DllImport("winmm.dll")]
    private static extern int waveOutClose(IntPtr handle);

    private readonly object _syncRoot = new();
    private readonly byte[][] _buffers = new byte[BufferCount][];
    private readonly GCHandle[] _bufferPins = new GCHandle[BufferCount];
    private WaveHeader[] _headers = new WaveHeader[BufferCount];
    private GCHandle _headerPin;
    private IntPtr _handle = IntPtr.Zero;
    private int _nextBufferIndex;

    public string BackendName => "waveOut";

    public bool IsInitialized { get; private set; }

    public int SampleRate { get; private set; }

    public int ChannelCount { get; private set; }

    public void Initialize(int sampleRate, int channelCount)
    {
        lock (_syncRoot)
        {
            Shutdown();

            WaveFormatEx format = new()
            {
                FormatTag = WaveFormatPcm,
                Channels = (ushort)channelCount,
                SamplesPerSec = (uint)sampleRate,
                BitsPerSample = 16,
                BlockAlign = (ushort)(channelCount * sizeof(short)),
                AvgBytesPerSec = (uint)(sampleRate * channelCount * sizeof(short)),
                ExtraSize = 0,
            };

            int result = waveOutOpen(out _handle, WaveMapper, ref format, IntPtr.Zero, IntPtr.Zero, CallbackNull);
            if (result != 0 || _handle == IntPtr.Zero)
            {
                _handle = IntPtr.Zero;
                IsInitialized = false;
                SampleRate = 0;
                ChannelCount = 0;
                return;
            }

            int bufferLength = FramesPerBuffer * format.BlockAlign;
            for (int i = 0; i < BufferCount; i++)
            {
                _buffers[i] = new byte[bufferLength];
                _bufferPins[i] = GCHandle.Alloc(_buffers[i], GCHandleType.Pinned);
                _headers[i] = new WaveHeader
                {
                    Data = _bufferPins[i].AddrOfPinnedObject(),
                    BufferLength = (uint)bufferLength,
                };
            }

            _headerPin = GCHandle.Alloc(_headers, GCHandleType.Pinned);
            int headerSize = Marshal.SizeOf(typeof(WaveHeader));
            for (int i = 0; i < BufferCount; i++)
            {
                IntPtr headerPtr = Marshal.UnsafeAddrOfPinnedArrayElement(_headers, i);
                waveOutPrepareHeader(_handle, headerPtr, headerSize);
            }

            _nextBufferIndex = 0;
            SampleRate = sampleRate;
            ChannelCount = channelCount;
            IsInitialized = true;
        }
    }

    public void SubmitSamples(byte[] pcmBuffer, int sampleCount)
    {
        lock (_syncRoot)
        {
            if (!IsInitialized || _handle == IntPtr.Zero || pcmBuffer.Length == 0 || sampleCount <= 0)
            {
                return;
            }

            int blockAlign = ChannelCount * sizeof(short);
            int byteCount = sampleCount * blockAlign;
            if (byteCount <= 0)
            {
                return;
            }

            int bufferIndex = _nextBufferIndex;
            WaitForBuffer(bufferIndex);
            if (!IsInitialized || _handle == IntPtr.Zero)
            {
                return;
            }

            int headerSize = Marshal.SizeOf(typeof(WaveHeader));
            IntPtr headerPtr = Marshal.UnsafeAddrOfPinnedArrayElement(_headers, bufferIndex);
            waveOutUnprepareHeader(_handle, headerPtr, headerSize);

            byte[] target = _buffers[bufferIndex];
            int copyCount = Math.Min(target.Length, Math.Min(byteCount, pcmBuffer.Length));
            Array.Copy(pcmBuffer, target, copyCount);
            if (copyCount < target.Length)
            {
                Array.Clear(target, copyCount, target.Length - copyCount);
            }

            _headers[bufferIndex].BufferLength = (uint)copyCount;
            _headers[bufferIndex].Flags = 0;
            waveOutPrepareHeader(_handle, headerPtr, headerSize);
            waveOutWrite(_handle, headerPtr, headerSize);

            _nextBufferIndex = (_nextBufferIndex + 1) % BufferCount;
        }
    }

    public void Shutdown()
    {
        lock (_syncRoot)
        {
            if (_handle != IntPtr.Zero)
            {
                waveOutReset(_handle);

                int headerSize = Marshal.SizeOf(typeof(WaveHeader));
                if (_headerPin.IsAllocated)
                {
                    for (int i = 0; i < BufferCount; i++)
                    {
                        IntPtr headerPtr = Marshal.UnsafeAddrOfPinnedArrayElement(_headers, i);
                        waveOutUnprepareHeader(_handle, headerPtr, headerSize);
                    }
                }

                waveOutClose(_handle);
                _handle = IntPtr.Zero;
            }

            for (int i = 0; i < BufferCount; i++)
            {
                if (_bufferPins[i].IsAllocated)
                {
                    _bufferPins[i].Free();
                }
            }

            if (_headerPin.IsAllocated)
            {
                _headerPin.Free();
            }

            _headers = new WaveHeader[BufferCount];
            _nextBufferIndex = 0;
            IsInitialized = false;
            SampleRate = 0;
            ChannelCount = 0;
        }
    }

    private void WaitForBuffer(int bufferIndex)
    {
        int spinCount = 0;
        while ((_headers[bufferIndex].Flags & HeaderInQueue) != 0 && spinCount < 50)
        {
            Thread.Sleep(1);
            spinCount++;
        }
    }
}
