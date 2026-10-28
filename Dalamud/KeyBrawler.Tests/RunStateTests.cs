using KeyBrawler;

internal static class RunStateTests
{
    public static void Run()
    {
        var count = 0;
        void Check(bool value, string name)
        {
            if (!value) throw new Exception(name);
            count++; Console.WriteLine("PASS " + name);
        }
        var now = DateTime.UtcNow;
        var state = new RunState();
        state.Start(); state.Pause(now, "input unavailable");
        Check(state.Running && state.Paused, "Transient input failure pauses without disabling the session");
        Check(!state.TryResume(now.AddMilliseconds(1000)) && state.Running, "Retry backoff does not turn the plugin off");
        Check(!state.Pause(now.AddMilliseconds(1000), "input unavailable"), "Repeated focus loss does not continually extend the pause");
        Check(state.TryResume(now.AddMilliseconds(1500)) && state.Running && !state.Paused, "Session resumes automatically when retry is due");
        state.Pause(now.AddSeconds(2), "OCR unavailable");
        state.Stop();
        Check(!state.Running && !state.TryResume(now.AddHours(1)), "Manual stop cannot auto-restart later");
        Check(!state.Pause(now.AddHours(1), "error") && !state.Running, "Late error cannot restart a stopped session");
        state.Start();
        Check(state.Running && state.TryResume(now), "Explicit start clears an earlier error pause");
        Console.WriteLine($"{count} run-state tests passed.");
    }
}
