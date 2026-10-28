namespace KeyBrawler;

public sealed record OcrRequest(int X, int Y, int Width, int Height, int Gap)
{
    public void Validate()
    {
        if (Width is < 1 or > 4000 || Height is < 1 or > 4000 || Gap is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(Width), "OCR bölgesi 1–4000 piksel ve satır aralığı 0–100 olmalı.");
    }
}

public sealed record OcrResponse(List<TextLine>? Lines, string? Error = null, string? Details = null);
