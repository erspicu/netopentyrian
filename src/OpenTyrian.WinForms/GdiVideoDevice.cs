using System;
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
                biSize = (uint)Marshal.SizeOf(typeof(BitmapInfoHeader)),
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

    public uint[] LockFrame()
    {
        return _pixels;
    }

    public bool TryMapClientPointToFrame(Point clientPoint, out Point framePoint)
    {
        framePoint = Point.Empty;

        if (!TryGetPresentationBounds(out int scale, out int offsetX, out int offsetY, out int drawWidth, out int drawHeight))
        {
            return false;
        }

        if (clientPoint.X < offsetX ||
            clientPoint.Y < offsetY ||
            clientPoint.X >= offsetX + drawWidth ||
            clientPoint.Y >= offsetY + drawHeight)
        {
            return false;
        }

        int frameX = (clientPoint.X - offsetX) / scale;
        int frameY = (clientPoint.Y - offsetY) / scale;
        framePoint = new Point(Math.Min(Width - 1, Math.Max(0, frameX)), Math.Min(Height - 1, Math.Max(0, frameY)));
        return true;
    }

    public void Present()
    {
        if (_disposed || !_target.IsHandleCreated)
        {
            return;
        }

        if (!TryGetPresentationBounds(out int scale, out int offsetX, out int offsetY, out int drawWidth, out int drawHeight))
        {
            return;
        }

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

    private bool TryGetPresentationBounds(out int scale, out int offsetX, out int offsetY, out int drawWidth, out int drawHeight)
    {
        scale = 0;
        offsetX = 0;
        offsetY = 0;
        drawWidth = 0;
        drawHeight = 0;

        if (_disposed || _target.Width <= 0 || _target.Height <= 0)
        {
            return false;
        }

        scale = Math.Max(1, Math.Min(_target.ClientSize.Width / Width, _target.ClientSize.Height / Height));
        drawWidth = Width * scale;
        drawHeight = Height * scale;
        offsetX = (_target.ClientSize.Width - drawWidth) / 2;
        offsetY = (_target.ClientSize.Height - drawHeight) / 2;
        return true;
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
