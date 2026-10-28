using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using KeyBrawler;

internal static class LocalizationTests
{
    private sealed class Target(bool physical) : ITextEntryTarget
    {
        public string Captured = "";
        public string ReadBack = "";
        public void Prepare() { }
        public void MouseDown() { }
        public void MouseUp() { }
        public nint WriteAndReadBack(string text) { Captured = ReadBack = text; return 42; }
        public bool StillContains(nint input, string text) => input == 42 && ReadBack == text;
        public void EnterDown() { }
        public void EnterUp() { }
        public bool BeginPhysicalText(string text) { if (physical) Captured = text; return physical; }
        public bool TickPhysicalText(DateTime now) => true;
        public nint ReadPhysicalText(string text) { ReadBack = text; return 42; }
    }

    public static void Run()
    {
        var saved = UiText.Language;
        var count = 0;
        void Check(bool condition, string name)
        {
            if (!condition) throw new Exception(name);
            count++; Console.WriteLine("PASS " + name);
        }
        try
        {
            UiText.Language = UiLanguage.English;
            Check(UiText.T("Ready") == "Ready" && BubblePriority.Label(BubbleColor.Red) == "Red", "English UI and color names selected");
            UiText.Language = UiLanguage.Turkish;
            Check(UiText.T("Ready") == "Hazir" && BubblePriority.Label(BubbleColor.Red) == "Kirmizi", "Turkish UI uses ASCII spellings");
            Check(UiText.Catalog.All(p => p.Key.All(c => c < 128) && p.Value.All(c => c < 128)), "Every owned English/Turkish message template is ASCII");
            Check(UiText.T("Last OCR cycle: {0} ms", 250) == "Son OCR turu: 250 ms", "Localized dynamic values keep their formatting");
            foreach (var item in UiText.Catalog)
            {
                int[] Indices(string value) => Regex.Matches(value, @"\{(\d+)(?:[^}]*)\}").Select(m => int.Parse(m.Groups[1].Value)).ToArray();
                var english = Indices(item.Key); var turkish = Indices(item.Value);
                if (!english.Order().SequenceEqual(turkish.Order())) throw new Exception("Mismatched translation placeholders: " + item.Key);
                var args = Enumerable.Repeat<object>(1.25, english.Length == 0 ? 0 : english.Max() + 1).ToArray();
                _ = string.Format(CultureInfo.InvariantCulture, item.Key, args);
                _ = string.Format(CultureInfo.InvariantCulture, item.Value, args);
            }
            Check(true, "All translation placeholders match and both languages format correctly");
            UiText.Language = (UiLanguage)999;
            Check(UiText.T("Ready") == "Hazir", "Invalid saved language safely falls back to Turkish ASCII");
            const string source = "\u00e7\u00c7\u011f\u011e\u0131\u0130\u00f6\u00d6\u015f\u015e\u00fc\u00dc";
            Check(AnswerText.Normalize(source) == "cCgGiIoOsSuU", "All twelve Turkish letter forms map to ASCII with case preserved");
            Check(AnswerText.Normalize("G\u0306I\u0307s\u0327") == "GIs", "Decomposed OCR accents normalize before typing");
            Check(AnswerText.Normalize("Hello, WORLD! 42") == "Hello, WORLD! 42", "English answer spelling and punctuation remain intact");
            Check(AnswerText.Normalize("caf\u00e9") == "caf\u00e9", "Normalization does not translate or strip unrelated scripts");
            Check(AnswerText.CanCorrect("\u0130\u015f", "Is") && !AnswerText.CanCorrect("wrong", "Is"), "Layout correction only accepts the exact normalized answer");
            foreach (var language in new[] { UiLanguage.English, UiLanguage.Turkish })
            {
                UiText.Language = language;
                foreach (var physical in new[] { false, true })
                {
                    var target = new Target(physical);
                    var sequence = new TextEntrySequence("\u0130\u015e \u00c7\u0131\u011f", false, false);
                    var now = DateTime.UtcNow;
                    for (var elapsed = 0; elapsed <= 500; elapsed += 10) sequence.Tick(now.AddMilliseconds(elapsed), target);
                    Check(sequence.Done && sequence.Text == "IS Cig" && target.Captured == "IS Cig" && target.ReadBack == "IS Cig",
                        $"ASCII answer reaches {(physical ? "keyboard" : "direct")} path in {language} UI");
                }
            }
            var request = new OcrRequest(0, 0, 100, 100, 12, UiLanguage.English);
            Check(JsonSerializer.Deserialize<OcrRequest>(JsonSerializer.Serialize(request))!.Language == UiLanguage.English,
                "UI language preference survives OCR worker protocol");
        }
        finally { UiText.Language = saved; }
        Console.WriteLine($"{count} localization/answer-normalization tests passed.");
    }
}
