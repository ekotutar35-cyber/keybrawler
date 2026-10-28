namespace KeyBrawler;

public sealed record OcrRequest(int X, int Y, int Width, int Height, int Gap, UiLanguage Language = UiLanguage.Turkish,
    List<TextLine>? GameBubbles = null)
{
    public void Validate()
    {
        if (Width is < 1 or > 4000 || Height is < 1 or > 4000 || Gap is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(Width), UiText.T("OCR region must be 1-4000 pixels and line gap must be 0-100."));
        if (GameBubbles != null && (GameBubbles.Count > 128 || GameBubbles.Any(n =>
            BubbleSourcePolicy.Choose(BubbleSourcePolicy.AddonName, [n], Width, Height) == null)))
            throw new ArgumentException(UiText.T("Invalid game bubble snapshot."));
    }
}

public sealed record OcrResponse(List<TextLine>? Lines, string? Error = null, string? Details = null);
