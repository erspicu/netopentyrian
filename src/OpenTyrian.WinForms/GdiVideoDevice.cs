using System.Runtime.InteropServices;
using OpenTyrian.Platform;

namespace OpenTyrian.WinForms;

internal sealed class GdiVideoDevice : IVideoDevice, IDisposable
{
    private readonly Control _target;
    private readonly GCHandle _pixelHandle;
    private readonly uint[] _pixels;
    private readonly BitmapInfo _bitmapInfo;
    private bool _disposed;

    public GdiVideoDevice(Control target, int width, int height)
    {
        _target = target;
        Width = width;
        Height = height;
        _pixels = new uint[width * height];
        _pixelHandle = GCHandle.Alloc(_pixels, GCHandleType.Pinned);

        _bitmapInfo = new BitmapInfo
        {
            bmiHeader = new BitmapInfoHeader
            {
                biSize = (uint)Marshal.SizeOf<BitmapInfoHeader>(),
                biWidth = width,
                biHeight = -height,
                biPlanes = 1,
                biBitCount = 32,
                biCompression = 0,
                biSizeImage = (uint)(width * height * sizeof(uint))
            },
            bmiColors = new[] { new RgbQuad() }
        };
    }

    public int Width { get; }

    public int Height { get; }

    public Span<uint> LockFrame()
    {
        return _pixels;
    }

    public void Present()
    {
        if (_disposed || !_target.IsHandleCreated || _target.Width <= 0 || _target.Height <= 0)
        {
            return;
        }

        int scale = Math.Max(1, Math.Min(_target.ClientSize.Width / Width, _target.ClientSize.Height / Height));
        int drawWidth = Width * scale;
        int drawHeight = Height * scale;
        int offsetX = (_target.ClientSize.Width - drawWidth) / 2;
        int offsetY = (_target.ClientSize.Height - drawHeight) / 2;

        using Graphics graphics = _target.CreateGraphics();
        graphics.Clear(Color.Black);

        IntPtr hdc = graphics.GetHdc();
        BitmapInfo bitmapInfo = _bitmapInfo;

        try
        {
            GdiNative.StretchDIBits(
                hdc,
                offsetX,
                offsetY,
                drawWidth,
                drawHeight,
                0,
                0,
                Width,
                Height,
                _pixelHandle.AddrOfPinnedObject(),
                ref bitmapInfo,
                GdiNative.DibRgbColors,
                GdiNative.Srccopy);
        }
        finally
        {
            graphics.ReleaseHdc(hdc);
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (_pixelHandle.IsAllocated)
        {
            _pixelHandle.Free();
        }
    }
}
