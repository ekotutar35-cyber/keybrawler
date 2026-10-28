using KeyBrawler;

internal static class ActivationTests
{
    private sealed class Keys : IActivationKeys
    {
        public bool EnterAlreadyDown { get; set; }
        public readonly List<string> Events = [];
        public void Down() => Events.Add("down");
        public void Up() => Events.Add("up");
    }

    public static void Run()
    {
        void Check(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
            Console.WriteLine("PASS " + message);
        }
        var now = DateTime.UtcNow;
        var activation = new InputActivation();
        var keys = new Keys();
        activation.Tick(now, true, false, keys);
        Check(keys.Events.Count == 0, "Already-active minigame input is not toggled closed");
        activation.Tick(now, false, true, keys);
        Check(keys.Events.Count == 0, "Chat focus blocks automatic Enter");
        keys.EnterAlreadyDown = true;
        activation.Tick(now, false, false, keys);
        Check(keys.Events.Count == 0, "Manual Enter is not pressed or released by plugin");
        keys.EnterAlreadyDown = false;
        activation.Tick(now, false, false, keys);
        activation.Tick(now.AddMilliseconds(20), false, false, keys);
        Check(keys.Events.SequenceEqual(["down"]), "Activation Enter is held across frames");
        activation.Tick(now.AddMilliseconds(80), true, false, keys);
        Check(keys.Events.SequenceEqual(["down", "up"]) && !activation.IsHolding, "Activation key released even when input opens");
        activation.Tick(now.AddMilliseconds(100), false, false, keys);
        Check(keys.Events.Count == 2, "Failed opening attempts are rate limited");
        activation.Tick(now.AddSeconds(2), false, false, keys);
        activation.Release(keys);
        activation.Release(keys);
        Check(keys.Events.SequenceEqual(["down", "up", "down", "up"]), "Stop releases only the owned activation key once");
        Console.WriteLine("7 activation tests passed.");
    }
}
