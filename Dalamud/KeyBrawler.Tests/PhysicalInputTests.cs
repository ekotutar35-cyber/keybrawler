using KeyBrawler;

internal static class PhysicalInputTests
{
    private sealed class Sink : IPhysicalKeySink
    {
        public readonly List<string> Events = [];
        public bool ThrowOnPress;
        public void Press(PhysicalKey key)
        {
            Events.Add("down " + key.Scan);
            if (ThrowOnPress) throw new InvalidOperationException("Windows input failed");
        }
        public void Release() => Events.Add("up");
    }

    private sealed class Target : ITextEntryTarget
    {
        public readonly List<string> Events = [];
        public bool RejectRead;
        public void Prepare() => Events.Add("prepare");
        public void MouseDown() => throw new Exception("Unexpected click");
        public void MouseUp() { }
        public nint WriteAndReadBack(string text) => throw new Exception("Physical mode must not call SetText");
        public bool StillContains(nint input, string text) => input == 42;
        public void EnterDown() => Events.Add("enter down");
        public void EnterUp() => Events.Add("enter up");
        public bool BeginPhysicalText(string text) { Events.Add("begin " + text); return true; }
        public bool TickPhysicalText(DateTime now) { Events.Add("keys sent"); return true; }
        public nint ReadPhysicalText(string text)
        {
            if (RejectRead) throw new InvalidOperationException("Readback mismatch");
            Events.Add("readback"); return 42;
        }
        public void ReleasePhysicalText() => Events.Add("release physical");
    }

    public static void Run()
    {
        var count = 0;
        void Check(bool condition, string name)
        {
            if (!condition) throw new Exception(name);
            count++; Console.WriteLine("PASS " + name);
        }
        var a = PhysicalKey.FromMapping('a', 0x41, 0x1e, false);
        Check(a.Scan == 0x1e && !a.Shift, "Physical scan code retained for lowercase letter");
        Check(PhysicalKey.FromMapping('A', 0x141, 0x1e, false).Shift, "Uppercase uses Shift");
        Check(!PhysicalKey.FromMapping('A', 0x141, 0x1e, true).Shift && PhysicalKey.FromMapping('a', 0x41, 0x1e, true).Shift,
            "Caps Lock state is accounted for");
        var altGr = PhysicalKey.FromMapping('@', 0x651, 0x10, false);
        Check(altGr.Control && altGr.Alt, "Keyboard-layout AltGr mapping retained");
        Check(PhysicalKey.FromMapping('x', 0x58, 0xe01d, false).Extended, "Extended key flag retained");
        try { PhysicalKey.FromMapping('☃', -1, 0, false); throw new Exception("Expected unmapped rejection"); }
        catch (InvalidOperationException) { Check(true, "Unmappable OCR characters fail before sending input"); }
        var now = DateTime.UtcNow;
        var sink = new Sink();
        var writer = new PhysicalTextWriter([a, a], sink);
        writer.Tick(now); writer.Tick(now.AddMilliseconds(5));
        Check(sink.Events.SequenceEqual(["down 30"]), "Physical key is held across frames");
        writer.Tick(now.AddMilliseconds(10)); writer.Tick(now.AddMilliseconds(20));
        var done = writer.Tick(now.AddMilliseconds(30));
        Check(done && sink.Events.SequenceEqual(["down 30", "up", "down 30", "up"]), "Repeated letters have separate down/up pairs");
        writer.Tick(now.AddSeconds(1));
        Check(sink.Events.Count == 4, "Completed physical phrase is not repeated");
        sink = new Sink(); writer = new PhysicalTextWriter([a], sink);
        writer.Tick(now); writer.Cancel(); writer.Cancel(); writer.Tick(now.AddSeconds(1));
        Check(sink.Events.SequenceEqual(["down 30", "up"]), "Stop releases held character exactly once");
        sink = new Sink { ThrowOnPress = true }; writer = new PhysicalTextWriter([a], sink);
        try { writer.Tick(now); } catch (InvalidOperationException) { writer.Cancel(); }
        Check(sink.Events.SequenceEqual(["down 30", "up"]), "Partial send failure releases owned keys");
        var target = new Target(); var sequence = new TextEntrySequence("abc", false, false);
        sequence.Tick(now, target); sequence.Tick(now.AddMilliseconds(60), target);
        sequence.Tick(now.AddMilliseconds(70), target); sequence.Tick(now.AddMilliseconds(120), target);
        Check(sequence.TextVerified && !target.Events.Contains("enter down"), "Physical text read back before confirmation");
        sequence.Tick(now.AddMilliseconds(150), target); sequence.Tick(now.AddMilliseconds(230), target);
        Check(sequence.Done && target.Events.SequenceEqual(["prepare", "begin abc", "keys sent", "readback", "enter down", "enter up"]),
            "Physical-mode sequence never writes directly to the text buffer");
        target = new Target { RejectRead = true }; sequence = new TextEntrySequence("abc", false, false);
        sequence.Tick(now, target); sequence.Tick(now.AddMilliseconds(60), target); sequence.Tick(now.AddMilliseconds(70), target);
        try { sequence.Tick(now.AddMilliseconds(120), target); } catch (InvalidOperationException) { sequence.Release(target); }
        Check(!target.Events.Contains("enter down") && target.Events.Last() == "release physical", "Readback mismatch cancels physical keys without Enter");
        Console.WriteLine($"{count} physical-input tests passed.");
    }
}
