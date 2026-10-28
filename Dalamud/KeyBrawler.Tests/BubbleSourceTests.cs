using System.Text.Json;
using KeyBrawler;

internal static class BubbleSourceTests
{
    public static void Run()
    {
        var count = 0;
        void Check(bool value, string name)
        {
            if (!value) throw new Exception(name);
            count++; Console.WriteLine("PASS " + name);
        }
        var word = new TextLine(10, 10, 200, 40, "ceremonial staff");
        TextLine? Choose(params TextLine[] nodes) => BubbleSourcePolicy.Choose("KTGEWord", nodes, 500, 300);
        Check(Choose(word)?.Text == "ceremonial staff", "Game bubble preserves its whole multiword answer");
        foreach (var addon in new[] { "KTGEnemyCastBar", "KTGIGaugeHp", "KTGInput", "ChatLog", "KTGEWordExtra", "ktgeword" })
            Check(BubbleSourcePolicy.Choose(addon, [word with { Text = "Germinate" }], 500, 300) == null,
                "Reject text source " + addon);
        Check(Choose(word, word) != null, "Duplicate render nodes do not duplicate the answer");
        Check(Choose(word, word with { Text = "Germinate" }) == null, "Ambiguous addon never concatenates or guesses text");
        Check(Choose(word with { Left = -1 }) == null, "Partially clipped text waits for the whole bubble");
        Check(Choose(word with { Right = 510 }) == null, "Out-of-region bubble rejected");
        Check(Choose(word with { Top = double.NaN }) == null, "Invalid native bounds rejected");
        Check(Choose(word with { Bottom = 0 }) == null, "Zero or negative text bounds rejected");
        Check(Choose(word with { Text = " \n " }) == null, "Empty wave produces no answer");
        Check(Choose(word with { Text = "bad\0text" }) == null, "Control payload cannot become keyboard input");
        Check(Choose(word with { Text = "ceremonial\nstaff" })?.Text == "ceremonial staff", "Single-node line breaks become phrase spaces");
        var back = word with { Text = "Blizzara" };
        var front = word with { Left = 60, Text = "Kupovalse" };
        var shared = BubbleSourcePolicy.Select("KTGEWord", [back, front], 500, 300);
        Check(shared.Select(n => n.Text).SequenceEqual(["Blizzara", "Kupovalse"]),
            "Multiple creatures in one addon are not discarded or concatenated");
        Check(BubbleSourcePolicy.Select("KTGEWord", [back, front, back], 500, 300).Count == 2,
            "Shared addon render duplicates do not suppress other creatures");
        Check(BubbleSourcePolicy.Select("KTGEWord", [back with { Left = -10 }, front], 500, 300).Single().Text == "Kupovalse",
            "One clipped creature cannot reject the other valid answer");
        Check(BubbleSourcePolicy.Select("KTGEnemyCastBar", [back, front], 500, 300).Count == 0,
            "Multi-node cast bar remains excluded");
        Check(BubbleSourcePolicy.Select("KTGEWord", [front with { Color = BubbleColor.Blue }], 500, 300).Single().Text == "Kupovalse",
            "Blue bubble is a valid game answer");
        Check(new[] { Choose(back)!, Choose(front)! }.Select(n => n.Text).SequenceEqual(["Blizzara", "Kupovalse"]),
            "Overlapping enemies remain independent full answers");
        var request = new OcrRequest(0, 0, 500, 300, 12, UiLanguage.English, [back, front]);
        request.Validate();
        var copy = JsonSerializer.Deserialize<OcrRequest>(JsonSerializer.Serialize(request))!;
        Check(copy.GameBubbles!.SequenceEqual(request.GameBubbles!), "Native text and geometry survive worker transport");
        Check(JsonSerializer.Deserialize<OcrRequest>("{\"X\":0,\"Y\":0,\"Width\":500,\"Height\":300,\"Gap\":12}")!.GameBubbles == null,
            "Legacy OCR requests remain compatible");
        new OcrRequest(0, 0, 500, 300, 12, GameBubbles: []).Validate();
        Check(JsonSerializer.Deserialize<OcrRequest>(JsonSerializer.Serialize(request with { GameBubbles = [] }))!.GameBubbles is { Count: 0 },
            "Empty native wave remains distinct from unrestricted OCR");
        foreach (var invalid in new[] { request with { GameBubbles = [word with { Left = -1 }] },
            request with { GameBubbles = Enumerable.Repeat(word, 129).ToList() } })
        {
            var rejected = false;
            try { invalid.Validate(); } catch (ArgumentException) { rejected = true; }
            Check(rejected, "Malformed or oversized native snapshot rejected");
        }
        Console.WriteLine($"{count} game-bubble source checks passed.");
    }
}
