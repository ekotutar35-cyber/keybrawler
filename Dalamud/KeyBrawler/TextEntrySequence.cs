namespace KeyBrawler;

public interface ITextEntryTarget
{
    void Prepare();
    void MouseDown();
    void MouseUp();
    nint WriteAndReadBack(string text);
    bool StillContains(nint input, string text);
    void EnterDown();
    void EnterUp();
    bool BeginPhysicalText(string text) => false;
    bool TickPhysicalText(DateTime now) => true;
    nint ReadPhysicalText(string text) => throw new NotSupportedException();
    void ReleasePhysicalText() { }
}

/// <summary>Keep down/up on separate frames and confirm text before pressing Enter.</summary>
public sealed class TextEntrySequence(string text, bool click, bool testOnly)
{
    private readonly string answer = AnswerText.Normalize(text);
    private int stage;
    private bool mouseHeld, enterHeld;
    private nint input;
    private DateTime next;
    public bool Done { get; private set; }
    public bool TextVerified { get; private set; }
    public bool TextStarted { get; private set; }
    public string Text => answer;
    public bool TestOnly => testOnly;

    public void Tick(DateTime now, ITextEntryTarget target)
    {
        if (Done || now < next) return;
        switch (stage)
        {
            case 0:
                if (click)
                {
                    mouseHeld = true;
                    target.MouseDown();
                    stage = 1;
                    next = now.AddMilliseconds(80);
                    return;
                }
                target.Prepare();
                stage = 2;
                next = now.AddMilliseconds(60);
                return;
            case 1:
                target.MouseUp();
                mouseHeld = false;
                target.Prepare();
                stage = 2;
                next = now.AddMilliseconds(60);
                return;
            case 2:
                if (target.BeginPhysicalText(answer)) { TextStarted = true; stage = 6; return; }
                TextStarted = true;
                input = target.WriteAndReadBack(answer);
                TextVerified = true;
                stage = 3;
                next = now.AddMilliseconds(50);
                return;
            case 3:
                if (!target.StillContains(input, answer))
                    throw new InvalidOperationException(UiText.T("The input field or text changed; Enter was not sent."));
                if (testOnly) { Done = true; return; }
                enterHeld = true;
                target.EnterDown();
                stage = 4;
                next = now.AddMilliseconds(80);
                return;
            case 4:
                target.EnterUp();
                enterHeld = false;
                Done = true;
                return;
            case 6:
                if (!target.TickPhysicalText(now)) return;
                stage = 7;
                next = now.AddMilliseconds(50);
                return;
            case 7:
                input = target.ReadPhysicalText(answer);
                TextVerified = true;
                stage = 3;
                next = now.AddMilliseconds(30);
                return;
        }
    }

    public void Release(ITextEntryTarget target)
    {
        // Attempt both releases even if one Windows input operation fails.
        try { if (mouseHeld) target.MouseUp(); }
        finally
        {
            mouseHeld = false;
            try { if (enterHeld) target.EnterUp(); }
            finally
            {
                enterHeld = false;
                try { target.ReleasePhysicalText(); }
                finally { Done = true; }
            }
        }
    }
}
