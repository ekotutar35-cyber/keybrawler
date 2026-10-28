using KeyBrawler;

internal static class ColorTests
{
    public static void Run()
    {
        var count = 0;
        void Check(bool condition, string name)
        {
            if (!condition) throw new Exception(name);
            count++; Console.WriteLine("PASS " + name);
        }
        const int width = 240, height = 100;
        var line = new TextLine(70, 35, 170, 65, "bubble");
        byte[] Image(byte r, byte g, byte b)
        {
            var pixels = Enumerable.Repeat((byte)240, width * height * 4).ToArray();
            for (var y = 15; y < 85; y++)
            for (var x = 40; x < 200; x++)
            {
                var i = (y * width + x) * 4;
                pixels[i] = b; pixels[i + 1] = g; pixels[i + 2] = r;
            }
            return pixels;
        }
        foreach (var sample in new[] {
            (R:(byte)230,G:(byte)45,B:(byte)45, Expected:BubbleColor.Red),
            (R:(byte)230,G:(byte)210,B:(byte)40, Expected:BubbleColor.Yellow),
            (R:(byte)45,G:(byte)200,B:(byte)65, Expected:BubbleColor.Green),
            (R:(byte)140,G:(byte)140,B:(byte)140, Expected:BubbleColor.Unknown),
            (R:(byte)40,G:(byte)70,B:(byte)220, Expected:BubbleColor.Unknown) })
        {
            var result = BubbleColorDetector.Annotate(Image(sample.R, sample.G, sample.B), width, height, [line])[0];
            Check(result.Color == sample.Expected, "BGRA classification: " + sample.Expected);
        }
        var edge = BubbleColorDetector.Annotate(Image(230, 45, 45), width, height, [new TextLine(-20, -10, 80, 50, "edge")]);
        Check(edge.Count == 1, "Color sampling clips rectangles to screen bounds");
        var offscreen = BubbleColorDetector.Annotate(Image(230, 45, 45), width, height, [new TextLine(-200, -200, -150, -150, "offscreen")]);
        Check(offscreen[0].Color == BubbleColor.Unknown, "Offscreen text cannot invent a priority color");
        var ordered = BubblePriority.Order([
            line with { Text = "other", Top = 0 },
            line with { Text = "green", Top = 1, Color = BubbleColor.Green },
            line with { Text = "yellow", Top = 2, Color = BubbleColor.Yellow },
            line with { Text = "red", Top = 3, Color = BubbleColor.Red },
        ]).Select(b => b.Text);
        Check(ordered.SequenceEqual(["red", "yellow", "green", "other"]), "Color priority takes precedence over screen order");
        ordered = BubblePriority.Order([
            line with { Text = "later", Top = 50, Color = BubbleColor.Red },
            line with { Text = "first", Top = 10, Color = BubbleColor.Red },
        ]).Select(b => b.Text);
        Check(ordered.SequenceEqual(["first", "later"]), "Equal colors preserve top-to-bottom ordering");
        var noise = Image(240, 240, 240);
        noise[(40 * width + 80) * 4 + 1] = 0; noise[(40 * width + 80) * 4] = 0;
        Check(BubbleColorDetector.Annotate(noise, width, height, [line])[0].Color == BubbleColor.Unknown,
            "A few colored pixels do not elevate a neutral bubble");
        Console.WriteLine($"{count} color-priority tests passed.");
    }
}
