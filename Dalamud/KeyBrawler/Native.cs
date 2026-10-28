using System.ComponentModel;
using System.Runtime.InteropServices;

namespace KeyBrawler;

internal static class Native
{
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint hwnd, out uint process);
    [DllImport("user32.dll")] internal static extern short GetAsyncKeyState(int key);
    [DllImport("user32.dll")] private static extern nint GetDC(nint window);
    [DllImport("user32.dll")] private static extern int ReleaseDC(nint window, nint dc);
    [DllImport("gdi32.dll")] private static extern nint CreateCompatibleDC(nint dc);
    [DllImport("gdi32.dll")] private static extern nint CreateCompatibleBitmap(nint dc, int width, int height);
    [DllImport("gdi32.dll")] private static extern nint SelectObject(nint dc, nint obj);
    [DllImport("gdi32.dll")] private static extern bool DeleteObject(nint obj);
    [DllImport("gdi32.dll")] private static extern bool DeleteDC(nint dc);
    [DllImport("gdi32.dll", SetLastError = true)] private static extern bool BitBlt(nint dest, int x, int y, int width, int height, nint source, int sx, int sy, uint operation);
    [DllImport("gdi32.dll", SetLastError = true)] private static extern int GetDIBits(nint dc, nint bitmap, uint start, uint lines, byte[] bytes, ref BitmapInfo info, uint usage);
    [DllImport("user32.dll", SetLastError = true)] private static extern uint SendInput(uint count, Input[] inputs, int size);
    [DllImport("user32.dll")] private static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] private static extern bool ClientToScreen(nint hwnd, ref Point point);
    [StructLayout(LayoutKind.Sequential)]
    private struct Point { public int X, Y; }

    public static System.Numerics.Vector2 ToScreen(System.Numerics.Vector2 position, bool viewportsEnabled)
    {
        if (viewportsEnabled) return position;
        var point = new Point { X = (int)position.X, Y = (int)position.Y };
        if (!ClientToScreen(GetForegroundWindow(), ref point)) throw new Win32Exception("Ekran koordinatı dönüştürülemedi.");
        return new System.Numerics.Vector2(point.X, point.Y);
    }

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
    [StructLayout(LayoutKind.Sequential)]
    private struct MouseInput
    {
        public int X, Y;
        public uint Data, Flags, Time;
        public nuint Extra;
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct KeyboardInput
    {
        public ushort Vk, Scan;
        public uint Flags, Time;
        public nuint Extra;
    }
    [StructLayout(LayoutKind.Explicit)]
    private struct Payload
    {
        [FieldOffset(0)] public MouseInput Mouse;
        [FieldOffset(0)] public KeyboardInput Keyboard;
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct Input { public uint Type; public Payload Payload; }

    public static bool GameFocused()
    {
        GetWindowThreadProcessId(GetForegroundWindow(), out var process);
        return process == (uint)Environment.ProcessId;
    }

    public static byte[] Capture(int x, int y, int width, int height)
    {
        var screen = GetDC(0);
        if (screen == 0) throw new Win32Exception("Ekran yakalanamadı.");
        nint dc = 0, bitmap = 0, previous = 0;
        try
        {
            dc = CreateCompatibleDC(screen);
            bitmap = CreateCompatibleBitmap(screen, width, height);
            if (dc == 0 || bitmap == 0) throw new Win32Exception("Yakalama tamponu oluşturulamadı.");
            previous = SelectObject(dc, bitmap);
            if (!BitBlt(dc, 0, 0, width, height, screen, x, y, 0x00CC0020 | 0x40000000))
                throw new Win32Exception(Marshal.GetLastWin32Error());
            SelectObject(dc, previous);
            previous = 0;
            var bytes = new byte[checked(width * height * 4)];
            var info = new BitmapInfo { Size = 40, Width = width, Height = -height, Planes = 1, BitCount = 32 };
            if (GetDIBits(dc, bitmap, 0, (uint)height, bytes, ref info, 0) != height)
                throw new Win32Exception("Ekran pikselleri okunamadı.");
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

    private static void Send(params Input[] inputs)
    {
        if (SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<Input>()) != inputs.Length)
            throw new Win32Exception("Tuş gönderimi engellendi. Yazı alanını kontrol edin.");
    }

    public static void MouseDown(int x, int y)
    {
        if (!SetCursorPos(x, y)) throw new Win32Exception("İmleç taşınamadı.");
        Send(new Input { Payload = new Payload { Mouse = new MouseInput { Flags = 2 } } });
    }

    public static void MouseUp() => Send(new Input { Payload = new Payload { Mouse = new MouseInput { Flags = 4 } } });

    public static void EnterDown() => Send(
        new Input { Type = 1, Payload = new Payload { Keyboard = new KeyboardInput { Vk = 13, Scan = 0x1C } } });
    public static void EnterUp() => Send(
        new Input { Type = 1, Payload = new Payload { Keyboard = new KeyboardInput { Vk = 13, Scan = 0x1C, Flags = 2 } } });
}
