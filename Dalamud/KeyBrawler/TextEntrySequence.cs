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
}

/// <summary>Keep down/up on separate frames and confirm text before pressing Enter.</summary>
public sealed class TextEntrySequence(string text, bool click, bool testOnly)
{
    private int stage;
    private bool mouseHeld, enterHeld;
    private nint input;
    private DateTime next;
    public bool Done { get; private set; }
    public bool TextVerified { get; private set; }
    public string Text => text;
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
                next = now.AddMilliseconds(180);
                return;
            case 1:
                target.MouseUp();
                mouseHeld = false;
                target.Prepare();
                stage = 2;
                next = now.AddMilliseconds(180);
                return;
            case 2:
                input = target.WriteAndReadBack(text);
                TextVerified = true;
                stage = 3;
                next = now.AddMilliseconds(150);
                return;
            case 3:
                if (!target.StillContains(input, text))
                    throw new InvalidOperationException("Yazı alanı veya metin değişti; Enter gönderilmedi.");
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
            finally { enterHeld = false; Done = true; }
        }
    }
}
