namespace KeyBrawler;

public interface IActivityTimers
{
    bool MinigameActive { get; }
    void ResetInputTimers();
}

public sealed class ActivityKeepAlive
{
    private DateTime next;
    public int RefreshCount { get; private set; }

    public void Tick(DateTime now, bool enabled, bool running, bool focused, bool settingsOpen, IActivityTimers timers)
    {
        if (!enabled || !running || !focused || settingsOpen || now < next || !timers.MinigameActive) return;
        timers.ResetInputTimers();
        RefreshCount++;
        next = now.AddSeconds(10);
    }
}
