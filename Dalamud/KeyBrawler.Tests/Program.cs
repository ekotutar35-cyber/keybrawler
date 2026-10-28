using KeyBrawler;

if (args.Contains("--fake-ocr-worker"))
{
    await WorkerTransportTests.RunFakeWorker();
    return;
}

static TextLine Line(double x, double y, string text) => new(x, y, x + 80, y + 20, text);
var count = 0;
void Check(string name, IEnumerable<TextLine> input, int gap, params string[] expected)
{
    var actual = BubbleGrouping.Group(input, gap).Select(g => g.Text).ToArray();
    if (!actual.SequenceEqual(expected))
        throw new Exception($"{name}: expected [{string.Join(",", expected)}], got [{string.Join(",", actual)}]");
    Console.WriteLine($"PASS {name}");
    count++;
}
Check("Separate bubbles", [Line(0, 0, "first"), Line(200, 0, "second")], 12, "first", "second");
Check("Multiline bubble", [Line(0, 0, "two"), Line(0, 25, "words"), Line(0, 100, "next")], 12, "two words", "next");
Check("Unsorted OCR", [Line(0, 25, "words"), Line(0, 0, "two")], 12, "two words");
Check("Disabled merge", [Line(0, 0, "two"), Line(0, 25, "words")], 0, "two", "words");
Check("No recognized text", [], 12);
Check("Blank lines", [Line(0, 0, " ")], 12);
Check("Independent multiline columns", [Line(0, 0, "a"), Line(200, 0, "c"), Line(0, 25, "b"), Line(200, 25, "d")], 12, "a b", "c d");
Check("Offset enemies stay separate", [Line(0, 0, "enemy"), Line(60, 25, "other")], 12, "enemy", "other");
Check("Different font sizes stay separate", [Line(0, 0, "answer"), new TextLine(0, 25, 80, 35, "skill")], 12, "answer", "skill");
Check("Multiline uses last line height", [Line(0, 0, "one"), Line(0, 25, "two"), Line(0, 50, "three")], 12, "one two three");
Check("Diagonal enemy chain stays separate", [Line(0, 0, "one"), Line(0, 25, "two"), Line(50, 50, "other")], 12, "one two", "other");
Console.WriteLine($"{count} tests passed.");
EntryTests.Run();
await WorkerTransportTests.Run();
MinigameTargetPolicy.RequireAddon("KTGInput");
foreach (var name in new[] { "ChatLog", "ChatLogPanel_0", "_ChatLog", "KTGHudLog", "KTGResult", "KTGInputExtra", "ktginput", "" })
{
    try { MinigameTargetPolicy.RequireAddon(name); throw new Exception("Non-minigame target accepted: " + name); }
    catch (InvalidOperationException) { Console.WriteLine($"PASS Reject non-input addon '{name}'"); }
}
Console.WriteLine("9 minigame-target checks passed.");
ActivityTests.Run();
ActivationTests.Run();
WorkflowTests.Run();
PhysicalInputTests.Run();
ColorTests.Run();
RunStateTests.Run();
LocalizationTests.Run();
BubbleSourceTests.Run();
