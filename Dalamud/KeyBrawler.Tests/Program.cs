using KeyBrawler;

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
Console.WriteLine($"{count} tests passed.");
EntryTests.Run();
