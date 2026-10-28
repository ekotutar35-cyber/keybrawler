namespace KeyBrawler;

public sealed record TextLine(double Left, double Top, double Right, double Bottom, string Text,
    BubbleColor Color = BubbleColor.Unknown, double ColorConfidence = 0);

public static class BubbleGrouping
{
    public static List<TextLine> Group(IEnumerable<TextLine> lines, int gap)
    {
        var groups = new List<TextLine>();
        var lastLines = new List<TextLine>();
        foreach (var line in lines.OrderBy(l => l.Top).ThenBy(l => l.Left))
        {
            if (string.IsNullOrWhiteSpace(line.Text)) continue;
            var index = -1;
            var distance = double.MaxValue;
            for (var i = 0; i < groups.Count; i++)
            {
                var group = lastLines[i];
                var lineHeight = line.Bottom - line.Top;
                var groupHeight = group.Bottom - group.Top;
                var smallerHeight = Math.Min(lineHeight, groupHeight);
                var overlap = Math.Min(line.Right, group.Right) - Math.Max(line.Left, group.Left);
                var smallerWidth = Math.Min(line.Right - line.Left, group.Right - group.Left);
                // Nearby enemies can overlap slightly. Only join similarly sized, centered lines.
                if (smallerHeight <= 0 || smallerWidth <= 0 ||
                    Math.Max(lineHeight, groupHeight) > smallerHeight * 1.4 ||
                    overlap < smallerWidth * 0.8 ||
                    Math.Abs((line.Left + line.Right - group.Left - group.Right) / 2) > smallerHeight) continue;
                var dy = line.Top - group.Bottom;
                if (dy < 0 || dy > gap || dy >= distance ||
                    Math.Min(line.Right, group.Right) <= Math.Max(line.Left, group.Left)) continue;
                index = i;
                distance = dy;
            }
            if (index < 0)
            {
                groups.Add(line);
                lastLines.Add(line);
            }
            else
            {
                var previous = groups[index];
                lastLines[index] = line;
                groups[index] = new TextLine(Math.Min(previous.Left, line.Left), previous.Top,
                    Math.Max(previous.Right, line.Right), line.Bottom, previous.Text + " " + line.Text);
            }
        }
        return groups;
    }
}
