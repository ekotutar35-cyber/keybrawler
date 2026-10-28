namespace KeyBrawler;

public readonly record struct PhysicalKey(ushort Scan, bool Extended, bool Shift, bool Control, bool Alt)
{
    public static PhysicalKey FromMapping(char character, short virtualMapping, uint scanMapping, bool capsLock)
    {
        if (char.IsControl(character) || virtualMapping == -1 || (scanMapping & 0xFF) == 0)
            throw new InvalidOperationException(UiText.T("Character '{0}' cannot be produced with the current keyboard layout. Check the layout or select direct text mode." , character));
        var modifiers = (virtualMapping >> 8) & 7;
        var shift = (modifiers & 1) != 0;
        if (capsLock && char.IsLetter(character) && char.ToLowerInvariant(character) != char.ToUpperInvariant(character)) shift = !shift;
        return new PhysicalKey((ushort)(scanMapping & 0xFF), (scanMapping & 0xFF00) == 0xE000,
            shift, (modifiers & 2) != 0, (modifiers & 4) != 0);
    }
}

public interface IPhysicalKeySink
{
    void Press(PhysicalKey key);
    void Release();
}

/// <summary>One down/up pair per character, across separate framework frames.</summary>
public sealed class PhysicalTextWriter(IReadOnlyList<PhysicalKey> keys, IPhysicalKeySink sink)
{
    private int index;
    private bool held, cancelled;
    private DateTime releaseAt;

    public bool Tick(DateTime now)
    {
        if (cancelled || index >= keys.Count) return true;
        if (held)
        {
            if (now < releaseAt) return false;
            sink.Release();
            held = false;
            index++;
            return index >= keys.Count;
        }
        held = true;
        releaseAt = now.AddMilliseconds(10);
        sink.Press(keys[index]);
        return false;
    }

    public void Cancel()
    {
        try { if (held) sink.Release(); }
        finally { held = false; cancelled = true; }
    }
}
