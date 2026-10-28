using System.ComponentModel;
using System.Runtime.InteropServices;

namespace KeyBrawler;

internal static class Native
{
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint hwnd, out uint process);
    [DllImport("user32.dll")] internal static extern short GetAsyncKeyState(int key);
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
