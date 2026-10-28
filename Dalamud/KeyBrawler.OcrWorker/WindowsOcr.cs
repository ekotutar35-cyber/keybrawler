using Windows.Globalization;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage.Streams;

namespace KeyBrawler;

internal static class WindowsOcr
{
    private static OcrEngine? engine;
    public static async Task<List<TextLine>> ReadAsync(int x, int y, int width, int height, int gap)
    {
        engine ??= OcrEngine.TryCreateFromLanguage(new Language("en-US"))
            ?? OcrEngine.TryCreateFromUserProfileLanguages()
            ?? throw new InvalidOperationException(UiText.T("No Windows OCR language is installed. Install English OCR in Windows Settings > Language."));
        if (width < 1 || height < 1 || width > OcrEngine.MaxImageDimension || height > OcrEngine.MaxImageDimension)
            throw new InvalidOperationException(UiText.T("Region dimensions must be between 1 and {0} pixels." , OcrEngine.MaxImageDimension));
        var pixels = ScreenCapture.Capture(x, y, width, height);
        using var writer = new DataWriter();
        writer.WriteBytes(pixels);
        using var bitmap = SoftwareBitmap.CreateCopyFromBuffer(writer.DetachBuffer(),
            BitmapPixelFormat.Bgra8, width, height, BitmapAlphaMode.Ignore);
        var result = await engine.RecognizeAsync(bitmap);
        var groups = BubbleGrouping.Group(result.Lines.Where(l => l.Words.Count > 0).Select(l =>
            new TextLine(l.Words.Min(w => w.BoundingRect.Left), l.Words.Min(w => w.BoundingRect.Top),
                l.Words.Max(w => w.BoundingRect.Right), l.Words.Max(w => w.BoundingRect.Bottom), l.Text)), gap);
        return BubbleColorDetector.Annotate(pixels, width, height, groups);
    }
}
