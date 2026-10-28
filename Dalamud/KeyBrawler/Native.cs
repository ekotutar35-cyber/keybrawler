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
    [DllImport("user32.dll")] private static extern nint GetKeyboardLayout(uint thread);
    [DllImport("user32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)] private static extern short VkKeyScanExW(char character, nint layout);
    [DllImport("user32.dll", ExactSpelling = true)] private static extern uint MapVirtualKeyExW(uint code, uint mode, nint layout);
    [DllImport("user32.dll")] private static extern short GetKeyState(int key);
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

    public static List<PhysicalKey> MapText(string text)
    {
        var thread = GetWindowThreadProcessId(GetForegroundWindow(), out _);
        var layout = GetKeyboardLayout(thread);
        var caps = (GetKeyState(0x14) & 1) != 0;
        // Validate the complete phrase before sending its first key.
        return text.Select(c =>
        {
            var mapping = VkKeyScanExW(c, layout);
            var scan = mapping == -1 ? 0 : MapVirtualKeyExW((uint)(mapping & 0xFF), 4, layout);
            return PhysicalKey.FromMapping(c, mapping, scan, caps);
        }).ToList();
    }

    public static bool ModifierHeld() => new[] { 0x10, 0x11, 0x12 }.Any(k => (GetAsyncKeyState(k) & 0x8000) != 0);

    public static void ScanDown(ushort scan, bool extended) => Send(
        new Input { Type = 1, Payload = new Payload { Keyboard = new KeyboardInput { Scan = scan, Flags = 8u | (extended ? 1u : 0) } } });
    public static void ScanUp(ushort scan, bool extended) => Send(
        new Input { Type = 1, Payload = new Payload { Keyboard = new KeyboardInput { Scan = scan, Flags = 10u | (extended ? 1u : 0) } } });

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
