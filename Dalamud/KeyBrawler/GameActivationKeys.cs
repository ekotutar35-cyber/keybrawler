namespace KeyBrawler;

internal sealed class GameActivationKeys(GameActivity activity, GameTextEntry target) : IActivationKeys
{
    public bool EnterAlreadyDown => (Native.GetAsyncKeyState(13) & 0x8000) != 0;
    public void Down()
    {
        if (!Native.GameFocused() || !activity.MinigameActive || target.ForeignTextFocused())
            throw new InvalidOperationException(UiText.T("No verified minigame target available to open the input."));
        Native.EnterDown();
    }
    public void Up() => Native.EnterUp();
}
