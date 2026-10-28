using Windows.Globalization;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage.Streams;

namespace KeyBrawler;

internal static class WindowsOcr
{
    public static async Task<List<TextLine>> ReadAsync(int x, int y, int width, int height, int gap)
    {
        var engine = OcrEngine.TryCreateFromUserProfileLanguages()
            ?? OcrEngine.TryCreateFromLanguage(new Language("en-US"))
            ?? throw new InvalidOperationException("Windows OCR dili bulunamadı. Windows Ayarları > Dil bölümünden İngilizce OCR bileşenini kurun.");
        if (width < 1 || height < 1 || width > OcrEngine.MaxImageDimension || height > OcrEngine.MaxImageDimension)
            throw new InvalidOperationException($"Bölge boyutu 1–{OcrEngine.MaxImageDimension} piksel arasında olmalı.");
        var pixels = Native.Capture(x, y, width, height);
        using var writer = new DataWriter();
        writer.WriteBytes(pixels);
        using var bitmap = SoftwareBitmap.CreateCopyFromBuffer(writer.DetachBuffer(),
            BitmapPixelFormat.Bgra8, width, height, BitmapAlphaMode.Ignore);
        var result = await engine.RecognizeAsync(bitmap);
        return BubbleGrouping.Group(result.Lines.Where(l => l.Words.Count > 0).Select(l =>
            new TextLine(l.Words.Min(w => w.BoundingRect.Left), l.Words.Min(w => w.BoundingRect.Top),
                l.Words.Max(w => w.BoundingRect.Right), l.Words.Max(w => w.BoundingRect.Bottom), l.Text)), gap);
    }
}
