namespace KeyBrawler;

public interface IActivationKeys
{
    bool EnterAlreadyDown { get; }
    void Down();
    void Up();
}

public sealed class InputActivation
{
    private bool held;
    private DateTime releaseAt, nextAttempt;
    public bool IsHolding => held;

    public void Tick(DateTime now, bool inputActive, bool foreignTextFocused, IActivationKeys keys)
    {
        if (held)
        {
            if (now < releaseAt) return;
            keys.Up();
            held = false;
            nextAttempt = now.AddMilliseconds(250);
            return;
        }
        if (inputActive || foreignTextFocused || keys.EnterAlreadyDown || now < nextAttempt) return;
        held = true;
        releaseAt = now.AddMilliseconds(80);
        keys.Down();
    }

    public void Release(IActivationKeys keys)
    {
        try { if (held) keys.Up(); }
        finally { held = false; nextAttempt = default; }
    }
}
