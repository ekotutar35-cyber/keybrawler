namespace KeyBrawler;

public sealed record TextLine(double Left, double Top, double Right, double Bottom, string Text);

public static class BubbleGrouping
{
    public static List<TextLine> Group(IEnumerable<TextLine> lines, int gap)
    {
        var groups = new List<TextLine>();
        foreach (var line in lines.OrderBy(l => l.Top).ThenBy(l => l.Left))
        {
            if (string.IsNullOrWhiteSpace(line.Text)) continue;
            var index = -1;
            var distance = double.MaxValue;
            for (var i = 0; i < groups.Count; i++)
            {
                var group = groups[i];
                var dy = line.Top - group.Bottom;
                if (dy < 0 || dy > gap || dy >= distance ||
                    Math.Min(line.Right, group.Right) <= Math.Max(line.Left, group.Left)) continue;
                index = i;
                distance = dy;
            }
            if (index < 0) groups.Add(line);
            else
            {
                var previous = groups[index];
                groups[index] = new TextLine(Math.Min(previous.Left, line.Left), previous.Top,
                    Math.Max(previous.Right, line.Right), line.Bottom, previous.Text + " " + line.Text);
            }
        }
        return groups;
    }
}
