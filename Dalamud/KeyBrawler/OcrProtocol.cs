namespace KeyBrawler;

public sealed record OcrRequest(int X, int Y, int Width, int Height, int Gap, UiLanguage Language = UiLanguage.Turkish)
{
    public void Validate()
    {
        if (Width is < 1 or > 4000 || Height is < 1 or > 4000 || Gap is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(Width), UiText.T("OCR region must be 1-4000 pixels and line gap must be 0-100."));
    }
}

public sealed record OcrResponse(List<TextLine>? Lines, string? Error = null, string? Details = null);
