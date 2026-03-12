using System.Runtime.InteropServices;
using OpenTyrian.Platform;

namespace OpenTyrian.Core;

internal sealed class OriginalMusicPlayer
{
    private short[] _renderBuffer = new short[0];
    private int _currentSongIndex = -1;
    private bool _isPlaying;

    public OriginalMusicPlayer(IAssetLocator assetLocator)
    {
        AssetLocator = assetLocator;
        BackendName = "synth";
    }

    private IAssetLocator AssetLocator { get; }

    public bool IsAvailable { get; private set; }

    public string BackendName { get; private set; }

    public void Initialize(int sampleRate)
    {
        Shutdown();

        if (!AssetLocator.FileExists("music.mus"))
        {
            BackendName = "synth";
            return;
        }

        try
        {
            string musicPath = AssetLocator.GetFullPath("music.mus");
            if (NativeMethods.OpenTyrianMusic_Initialize(musicPath, sampleRate) != 0)
            {
                IsAvailable = true;
                BackendName = "native-opl";
                _currentSongIndex = -1;
                _isPlaying = false;
                return;
            }
        }
        catch (BadImageFormatException)
        {
        }
        catch (DllNotFoundException)
        {
        }
        catch (EntryPointNotFoundException)
        {
        }

        BackendName = "synth";
        IsAvailable = false;
        _currentSongIndex = -1;
        _isPlaying = false;
    }

    public bool PlaySong(int songIndex)
    {
        if (!IsAvailable)
        {
            return false;
        }

        if (_isPlaying && _currentSongIndex == songIndex)
        {
            return true;
        }

        if (NativeMethods.OpenTyrianMusic_PlaySong(songIndex) == 0)
        {
            _isPlaying = false;
            _currentSongIndex = -1;
            return false;
        }

        _currentSongIndex = songIndex;
        _isPlaying = true;
        return true;
    }

    public void Stop()
    {
        if (!IsAvailable)
        {
            return;
        }

        NativeMethods.OpenTyrianMusic_Stop();
        _isPlaying = false;
    }

    public void Shutdown()
    {
        if (IsAvailable)
        {
            try
            {
                NativeMethods.OpenTyrianMusic_Shutdown();
            }
            catch (BadImageFormatException)
            {
            }
            catch (DllNotFoundException)
            {
            }
            catch (EntryPointNotFoundException)
            {
            }
        }

        IsAvailable = false;
        BackendName = "synth";
        _currentSongIndex = -1;
        _isPlaying = false;
        _renderBuffer = new short[0];
    }

    public void MixInto(int[] mixed, int frameCount, int channelCount)
    {
        if (!IsAvailable || !_isPlaying || frameCount <= 0)
        {
            return;
        }

        if (_renderBuffer.Length < frameCount)
        {
            _renderBuffer = new short[frameCount];
        }

        int renderedFrames = NativeMethods.OpenTyrianMusic_Render(_renderBuffer, frameCount);
        if (renderedFrames <= 0)
        {
            return;
        }

        for (int frame = 0; frame < renderedFrames; frame++)
        {
            int destinationOffset = frame * channelCount;
            short sample = _renderBuffer[frame];
            for (int channel = 0; channel < channelCount; channel++)
            {
                mixed[destinationOffset + channel] += sample;
            }
        }
    }

    private static class NativeMethods
    {
        [DllImport("OpenTyrian.NativeMusic", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern int OpenTyrianMusic_Initialize([MarshalAs(UnmanagedType.LPStr)] string musicFilePath, int sampleRate);

        [DllImport("OpenTyrian.NativeMusic", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void OpenTyrianMusic_Shutdown();

        [DllImport("OpenTyrian.NativeMusic", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int OpenTyrianMusic_PlaySong(int songIndex);

        [DllImport("OpenTyrian.NativeMusic", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void OpenTyrianMusic_Stop();

        [DllImport("OpenTyrian.NativeMusic", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int OpenTyrianMusic_Render([Out] short[] output, int frameCount);
    }
}
