namespace KeyBrawler;

public sealed class RunState
{
    public bool Running { get; private set; }
    public bool Paused { get; private set; }
    public string Reason { get; private set; } = "";
    private DateTime retryAfter;

    public void Start() { Running = true; Paused = false; Reason = ""; retryAfter = default; }
    public void Stop() { Running = false; Paused = false; Reason = ""; retryAfter = default; }

    public bool Pause(DateTime now, string reason, int retryMs = 1500)
    {
        if (!Running || (Paused && Reason == reason)) return false;
        Paused = true;
        Reason = reason;
        retryAfter = now.AddMilliseconds(retryMs);
        return true;
    }

    public bool TryResume(DateTime now)
    {
        if (!Running || now < retryAfter) return false;
        Paused = false; Reason = "";
        return true;
    }
}
