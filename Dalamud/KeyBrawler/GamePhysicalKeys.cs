namespace KeyBrawler;

internal sealed class GamePhysicalKeys : IPhysicalKeySink
{
    private readonly List<(ushort Scan, bool Extended)> held = [];

    private void Down(ushort scan, bool extended = false)
    {
        held.Add((scan, extended));
        Native.ScanDown(scan, extended);
    }

    public void Press(PhysicalKey key)
    {
        if (Native.ModifierHeld()) throw new InvalidOperationException(UiText.T("Release Shift/Ctrl/Alt while the plugin is typing."));
        if (key.Control) Down(0x1D);
        if (key.Alt) Down(0x38);
        if (key.Shift) Down(0x2A);
        Down(key.Scan, key.Extended);
    }

    public void Release()
    {
        Exception? failure = null;
        for (var i = held.Count - 1; i >= 0; i--)
        {
            try { Native.ScanUp(held[i].Scan, held[i].Extended); }
            catch (Exception e) { failure ??= e; }
        }
        held.Clear();
        if (failure != null) throw failure;
    }
}
