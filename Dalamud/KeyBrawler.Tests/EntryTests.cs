using KeyBrawler;

internal static class EntryTests
{
    private sealed class Target : ITextEntryTarget
    {
        public readonly List<string> Events = [];
        public bool Valid = true;
        public bool Reject;
        public void Prepare() => Events.Add("prepare");
        public void MouseDown() => Events.Add("mouse down");
        public void MouseUp() => Events.Add("mouse up");
        public nint WriteAndReadBack(string text)
        {
            if (Reject) throw new InvalidOperationException("No focused input");
            Events.Add("write " + text);
            return 123;
        }
        public bool StillContains(nint input, string text) => Valid && input == 123;
        public void EnterDown() => Events.Add("enter down");
        public void EnterUp() => Events.Add("enter up");
    }

    private static void Expect(bool value, string name)
    {
        if (!value) throw new Exception("FAIL " + name);
        Console.WriteLine("PASS " + name);
    }

    public static void Run()
    {
        var now = DateTime.UtcNow;
        var target = new Target();
        var sequence = new TextEntrySequence("one bubble", true, false);
        sequence.Tick(now, target);
        sequence.Tick(now.AddMilliseconds(20), target);
        Expect(target.Events.SequenceEqual(["mouse down"]), "Click is held across frames");
        sequence.Tick(now.AddMilliseconds(80), target);
        sequence.Tick(now.AddMilliseconds(260), target);
        Expect(!target.Events.Contains("enter down") && sequence.TextVerified, "Text verified before Enter");
        sequence.Tick(now.AddMilliseconds(410), target);
        sequence.Tick(now.AddMilliseconds(430), target);
        Expect(target.Events.Last() == "enter down" && !sequence.Done, "Enter held across frames");
        sequence.Tick(now.AddMilliseconds(490), target);
        sequence.Tick(now.AddSeconds(2), target);
        Expect(sequence.Done && target.Events.SequenceEqual(["mouse down", "mouse up", "prepare", "write one bubble", "enter down", "enter up"]), "One text and one Enter only");

        target = new Target();
        sequence = new TextEntrySequence("test", false, true);
        sequence.Tick(now, target);
        Expect(target.Events.SequenceEqual(["prepare"]) && !sequence.TextVerified, "Native minigame focus settles before writing");
        sequence.Tick(now.AddMilliseconds(180), target);
        sequence.Tick(now.AddSeconds(1), target);
        Expect(sequence.Done && target.Events.SequenceEqual(["prepare", "write test"]), "Minigame test neither clicks nor submits");

        target = new Target { Valid = false };
        sequence = new TextEntrySequence("hello", false, false);
        sequence.Tick(now, target);
        sequence.Tick(now.AddMilliseconds(180), target);
        try { sequence.Tick(now.AddSeconds(1), target); throw new Exception("Expected validation failure"); }
        catch (InvalidOperationException) { }
        Expect(!target.Events.Contains("enter down"), "Changed focus or text prevents Enter");

        target = new Target { Reject = true };
        sequence = new TextEntrySequence("hello", false, false);
        sequence.Tick(now, target);
        try { sequence.Tick(now.AddMilliseconds(180), target); throw new Exception("Expected write failure"); }
        catch (InvalidOperationException) { }
        Expect(!sequence.TextVerified && target.Events.SequenceEqual(["prepare"]), "Rejected write never submits");

        target = new Target();
        sequence = new TextEntrySequence("hello", true, false);
        sequence.Tick(now, target);
        sequence.Release(target);
        sequence.Tick(now.AddSeconds(10), target);
        Expect(target.Events.SequenceEqual(["mouse down", "mouse up"]), "Stopping releases owned mouse button and cancels write");

        target = new Target();
        sequence = new TextEntrySequence("hello", false, false);
        sequence.Tick(now, target);
        sequence.Tick(now.AddMilliseconds(180), target);
        sequence.Tick(now.AddMilliseconds(330), target);
        sequence.Release(target);
        Expect(target.Events.SequenceEqual(["prepare", "write hello", "enter down", "enter up"]), "Stopping releases owned Enter key");
        Console.WriteLine("10 input-sequence tests passed.");
    }
}
