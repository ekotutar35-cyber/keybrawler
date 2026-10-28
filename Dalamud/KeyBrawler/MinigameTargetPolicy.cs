namespace KeyBrawler;

public static class MinigameTargetPolicy
{
    public const string AddonName = "KTGInput";

    public static void RequireAddon(string name)
    {
        if (!string.Equals(name, AddonName, StringComparison.Ordinal))
            throw new InvalidOperationException($"Hedef '{name}' Keybound Brawler giriş alanı değil. Sohbete yazılmadı; minioyunu açın.");
    }
}
