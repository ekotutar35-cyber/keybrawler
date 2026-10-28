namespace KeyBrawler;

/// <summary>One independently identified game bubble produces at most one answer.</summary>
public static class BubbleSourcePolicy
{
    public const string AddonName = "KTGEWord";

    public static TextLine? Choose(string addonName, IEnumerable<TextLine> nodes, int width, int height)
    {
        if (!string.Equals(addonName, AddonName, StringComparison.Ordinal)) return null;
        var candidates = nodes.Select(n => n with { Text = n.Text.Replace('\r', ' ').Replace('\n', ' ').Trim() })
            .Where(n => !string.IsNullOrWhiteSpace(n.Text)).ToList();
        // Do not guess which node is the answer if the addon layout changes.
        if (candidates.Count == 0 || candidates.Select(n => n.Text).Distinct(StringComparer.Ordinal).Count() != 1)
            return null;
        return candidates.FirstOrDefault(n => n.Text.Length <= 256 && !n.Text.Any(char.IsControl) &&
            double.IsFinite(n.Left) && double.IsFinite(n.Top) && double.IsFinite(n.Right) && double.IsFinite(n.Bottom) &&
            n.Left >= 0 && n.Top >= 0 && n.Right <= width && n.Bottom <= height &&
            n.Right > n.Left && n.Bottom > n.Top);
    }
}
