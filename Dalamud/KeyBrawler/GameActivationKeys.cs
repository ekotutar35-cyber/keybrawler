namespace KeyBrawler;

internal sealed class GameActivationKeys(GameActivity activity, GameTextEntry target) : IActivationKeys
{
    public bool EnterAlreadyDown => (Native.GetAsyncKeyState(13) & 0x8000) != 0;
    public void Down()
    {
        if (!Native.GameFocused() || !activity.MinigameActive || target.ForeignTextFocused())
            throw new InvalidOperationException("Minioyun girişini açmak için güvenli hedef bulunamadı.");
        Native.EnterDown();
    }
    public void Up() => Native.EnterUp();
}
