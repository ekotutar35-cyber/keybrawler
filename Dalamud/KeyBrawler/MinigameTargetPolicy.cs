namespace KeyBrawler;

public static class MinigameTargetPolicy
{
    public const string AddonName = "KTGInput";

    public static void RequireAddon(string name)
    {
        if (!string.Equals(name, AddonName, StringComparison.Ordinal))
            throw new InvalidOperationException(UiText.T("Target '{0}' is not the Keybound Brawler input. Nothing was sent to chat; open the minigame." , name));
    }
}
