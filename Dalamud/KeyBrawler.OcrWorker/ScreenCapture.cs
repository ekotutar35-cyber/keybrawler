using System.ComponentModel;
using System.Runtime.InteropServices;

namespace KeyBrawler;

internal static class ScreenCapture
{
    [DllImport("user32.dll")] private static extern nint GetDC(nint window);
    [DllImport("user32.dll")] private static extern int ReleaseDC(nint window, nint dc);
    [DllImport("gdi32.dll")] private static extern nint CreateCompatibleDC(nint dc);
    [DllImport("gdi32.dll")] private static extern nint CreateCompatibleBitmap(nint dc, int width, int height);
    [DllImport("gdi32.dll")] private static extern nint SelectObject(nint dc, nint obj);
    [DllImport("gdi32.dll")] private static extern bool DeleteObject(nint obj);
    [DllImport("gdi32.dll")] private static extern bool DeleteDC(nint dc);
    [DllImport("gdi32.dll", SetLastError = true)] private static extern bool BitBlt(nint dest, int x, int y, int width, int height, nint source, int sx, int sy, uint operation);
    [DllImport("gdi32.dll", SetLastError = true)] private static extern int GetDIBits(nint dc, nint bitmap, uint start, uint lines, byte[] bytes, ref BitmapInfo info, uint usage);
    [StructLayout(LayoutKind.Sequential)]
    private struct BitmapInfo
    {
        public uint Size;
        public int Width, Height;
        public ushort Planes, BitCount;
        public uint Compression, SizeImage;
        public int XPels, YPels;
        public uint ColorsUsed, ColorsImportant;
        public uint Color;
    }
    public static byte[] Capture(int x, int y, int width, int height)
    {
        var screen = GetDC(0);
        if (screen == 0) throw new Win32Exception(UiText.T("Could not capture the screen."));
        nint dc = 0, bitmap = 0, previous = 0;
        try
        {
            dc = CreateCompatibleDC(screen);
            bitmap = CreateCompatibleBitmap(screen, width, height);
            if (dc == 0 || bitmap == 0) throw new Win32Exception(UiText.T("Could not create a capture buffer."));
            previous = SelectObject(dc, bitmap);
            if (!BitBlt(dc, 0, 0, width, height, screen, x, y, 0x00CC0020 | 0x40000000))
                throw new Win32Exception(Marshal.GetLastWin32Error());
            SelectObject(dc, previous);
            previous = 0;
            var bytes = new byte[checked(width * height * 4)];
            var info = new BitmapInfo { Size = 40, Width = width, Height = -height, Planes = 1, BitCount = 32 };
            if (GetDIBits(dc, bitmap, 0, (uint)height, bytes, ref info, 0) != height)
                throw new Win32Exception(UiText.T("Could not read screen pixels."));
            return bytes;
        }
        finally
        {
            if (previous != 0) SelectObject(dc, previous);
            if (bitmap != 0) DeleteObject(bitmap);
            if (dc != 0) DeleteDC(dc);
            ReleaseDC(0, screen);
        }
    }

}
