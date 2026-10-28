using KeyBrawler;

internal static class ActivityTests
{
    private sealed class Timers : IActivityTimers
    {
        public bool MinigameActive { get; set; } = true;
        public int Resets;
        public void ResetInputTimers() => Resets++;
    }

    public static void Run()
    {
        var now = DateTime.UtcNow;
        void Check(string name, bool enabled, bool running, bool focused, bool settings, bool active)
        {
            var timers = new Timers { MinigameActive = active };
            new ActivityKeepAlive().Tick(now, enabled, running, focused, settings, timers);
            if (timers.Resets != 0) throw new Exception("Unexpected activity update: " + name);
            Console.WriteLine("PASS " + name);
        }
        Check("Disabled idle protection does nothing", false, true, true, false, true);
        Check("Stopped plugin does not refresh activity", true, false, true, false, true);
        Check("Unfocused game does not refresh activity", true, true, false, false, true);
        Check("Settings window pauses activity refresh", true, true, true, true, true);
        Check("Outside Keybound Brawler never refreshes activity", true, true, true, false, false);
        var timers = new Timers();
        var keepAlive = new ActivityKeepAlive();
        keepAlive.Tick(now, true, true, true, false, timers);
        if (timers.Resets != 1 || keepAlive.RefreshCount != 1) throw new Exception("No initial refresh");
        Console.WriteLine("PASS Active minigame refreshes activity");
        keepAlive.Tick(now.AddSeconds(9), true, true, true, false, timers);
        if (timers.Resets != 1) throw new Exception("Activity refresh too frequent");
        Console.WriteLine("PASS Activity refresh is rate limited");
        keepAlive.Tick(now.AddSeconds(10), true, true, true, false, timers);
        if (timers.Resets != 2 || keepAlive.RefreshCount != 2) throw new Exception("Missing periodic refresh");
        Console.WriteLine("PASS Activity refresh repeats after ten seconds");
        timers.MinigameActive = false;
        keepAlive.Tick(now.AddSeconds(20), true, true, true, false, timers);
        if (timers.Resets != 2) throw new Exception("Activity refreshed after minigame ended");
        Console.WriteLine("PASS Leaving minigame stops further activity updates");
        Console.WriteLine("9 activity tests passed.");
    }
}
