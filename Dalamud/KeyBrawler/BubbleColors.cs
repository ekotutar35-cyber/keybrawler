namespace KeyBrawler;

public enum BubbleColor { Unknown, Red, Yellow, Green, Blue }

public static class BubblePriority
{
    public static BubbleColor[] Normalize(IEnumerable<BubbleColor>? order) => (order ?? [])
        .Where(c => Enum.IsDefined(c)).Distinct()
        .Concat(new[] { BubbleColor.Red, BubbleColor.Yellow, BubbleColor.Green, BubbleColor.Blue, BubbleColor.Unknown })
        .Distinct().ToArray();

    public static IEnumerable<TextLine> Order(IEnumerable<TextLine> bubbles, IEnumerable<BubbleColor>? order = null)
    {
        var colors = Normalize(order);
        return bubbles.OrderBy(b => { var rank = Array.IndexOf(colors, b.Color); return rank < 0 ? colors.Length : rank; })
            .ThenBy(b => b.Top).ThenBy(b => b.Left);
    }

    public static string Label(BubbleColor color) => color switch
    {
        BubbleColor.Red => UiText.T("Red"), BubbleColor.Yellow => UiText.T("Yellow"), BubbleColor.Green => UiText.T("Green"), BubbleColor.Blue => UiText.T("Blue"), _ => UiText.T("Other/unknown"),
    };
}

/// <summary>Inspect saturated pixels in and near each grouped OCR text rectangle.</summary>
public static class BubbleColorDetector
{
    public static List<TextLine> Annotate(byte[] bgra, int width, int height, IReadOnlyList<TextLine> bubbles)
    {
        if (width <= 0 || height <= 0 || (long)width * height * 4 > bgra.LongLength)
            throw new ArgumentException(UiText.T("Invalid BGRA image."));
        return bubbles.Select(b => Classify(bgra, width, height, b)).ToList();
    }

    private static TextLine Classify(byte[] bgra, int width, int height, TextLine bubble)
    {
        var margin = Math.Clamp((int)Math.Ceiling((bubble.Bottom - bubble.Top) * 0.75), 8, 32);
        var left = Math.Clamp((int)Math.Floor(bubble.Left) - margin, 0, width);
        var right = Math.Clamp((int)Math.Ceiling(bubble.Right) + margin, 0, width);
        var top = Math.Clamp((int)Math.Floor(bubble.Top) - margin, 0, height);
        var bottom = Math.Clamp((int)Math.Ceiling(bubble.Bottom) + margin, 0, height);
        if (left >= right || top >= bottom) return bubble with { Color = BubbleColor.Unknown, ColorConfidence = 0 };
        var step = Math.Max(2, (int)Math.Ceiling(Math.Sqrt((double)(right - left) * (bottom - top) / 12000)));
        var scores = new double[5];
        double samples = 0;
        for (var y = top; y < bottom; y += step)
        for (var x = left; x < right; x += step)
        {
            var offset = (y * width + x) * 4;
            var b = bgra[offset]; var g = bgra[offset + 1]; var r = bgra[offset + 2];
            var weight = x >= bubble.Left && x <= bubble.Right && y >= bubble.Top && y <= bubble.Bottom ? 2.0 : 1.0;
            samples += weight;
            var max = Math.Max(r, Math.Max(g, b));
            var min = Math.Min(r, Math.Min(g, b));
            var delta = max - min;
            if (max < 80 || delta < 36 || (double)delta / max < 0.35) continue;
            var hue = max == r ? 60.0 * (g - b) / delta
                : max == g ? 120 + 60.0 * (b - r) / delta : 240 + 60.0 * (r - g) / delta;
            if (hue < 0) hue += 360;
            var color = hue < 25 || hue >= 335 ? BubbleColor.Red
                : hue >= 35 && hue < 80 ? BubbleColor.Yellow
                : hue >= 80 && hue <= 165 ? BubbleColor.Green
                : hue >= 200 && hue <= 285 ? BubbleColor.Blue : BubbleColor.Unknown;
            if (color != BubbleColor.Unknown) scores[(int)color] += weight;
        }
        var ranked = new[] { BubbleColor.Red, BubbleColor.Yellow, BubbleColor.Green, BubbleColor.Blue }
            .OrderByDescending(c => scores[(int)c]).ToArray();
        var best = scores[(int)ranked[0]];
        if (best < 5 || best < samples * 0.04 || best < scores[(int)ranked[1]] * 1.35)
            return bubble with { Color = BubbleColor.Unknown, ColorConfidence = 0 };
        return bubble with { Color = ranked[0], ColorConfidence = best / samples };
    }
}
