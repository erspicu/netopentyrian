using System.Runtime.InteropServices;

namespace OpenTyrian.WinForms;

internal static class GdiNative
{
    public const uint DibRgbColors = 0;

    [DllImport("gdi32.dll")]
    public static extern int SetDIBitsToDevice(
        IntPtr hdc,
        int xDest,
        int yDest,
        uint width,
        uint height,
        int xSrc,
        int ySrc,
        uint startScan,
        uint scanLines,
        IntPtr bits,
        ref BitmapInfo bitmapInfo,
        uint colorUse);

    [DllImport("gdi32.dll")]
    public static extern int StretchDIBits(
        IntPtr hdc,
        int xDest,
        int yDest,
        int destWidth,
        int destHeight,
        int xSrc,
        int ySrc,
        int srcWidth,
        int srcHeight,
        IntPtr bits,
        ref BitmapInfo bitmapInfo,
        uint colorUse,
        uint rasterOp);

    public const uint Srccopy = 0x00CC0020;
}

[StructLayout(LayoutKind.Sequential)]
internal struct BitmapInfo
{
    public BitmapInfoHeader bmiHeader;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
    public RgbQuad[] bmiColors;
}

[StructLayout(LayoutKind.Sequential)]
internal struct BitmapInfoHeader
{
    public uint biSize;
    public int biWidth;
    public int biHeight;
    public ushort biPlanes;
    public ushort biBitCount;
    public uint biCompression;
    public uint biSizeImage;
    public int biXPelsPerMeter;
    public int biYPelsPerMeter;
    public uint biClrUsed;
    public uint biClrImportant;
}

[StructLayout(LayoutKind.Sequential)]
internal struct RgbQuad
{
    public byte rgbBlue;
    public byte rgbGreen;
    public byte rgbRed;
    public byte rgbReserved;
}
