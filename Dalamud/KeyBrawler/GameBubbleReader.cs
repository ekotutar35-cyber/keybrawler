using Vector2 = System.Numerics.Vector2;
using Dalamud.Game.Text.SeStringHandling;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Common.Math;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace KeyBrawler;

/// <summary>Framework-thread-only snapshots. Never pass native UI pointers to the worker.</summary>
internal static unsafe class GameBubbleReader
{
    public static string LastDiagnostics { get; private set; } = "";

    public static List<TextLine> Read(Configuration config)
    {
        var result = new List<TextLine>();
        LastDiagnostics = UiText.T("No game bubble UI is available. Keep the game focused and include the whole bubble in the selected region.");
        if (!Native.GameFocused()) return result;
        var manager = RaptureAtkUnitManager.Instance();
        if (manager == null) return result;
        var origin = Native.ToScreen(Vector2.Zero, false);
        var visibleAddons = 0;
        var textNodes = 0;
        // Multiple enemies have separate instances of the exact bubble addon.
        for (var index = 1; index <= 128; index++)
        {
            var addon = manager->GetAddonByName(BubbleSourcePolicy.AddonName, index);
            if (addon == null || !addon->IsReady || !addon->IsVisible) continue;
            visibleAddons++;
            var nodes = new List<TextLine>();
            var visited = new HashSet<nint>();
            Visit(&addon->UldManager, nodes, visited, origin, config, 0);
            textNodes += nodes.Count(n => !string.IsNullOrWhiteSpace(n.Text));
            var chosen = BubbleSourcePolicy.Choose(addon->NameString, nodes, config.Width, config.Height);
            if (chosen != null) result.Add(chosen);
        }
        LastDiagnostics = UiText.T("Game bubbles: {0} visible addons, {1} text nodes, {2} accepted answers in the region.", visibleAddons, textNodes, result.Count);
        return result;
    }

    private static bool Visible(AtkResNode* node)
    {
        for (var depth = 0; node != null; depth++, node = node->ParentNode)
            if (depth >= 32 || !node->IsVisible()) return false;
        return true;
    }

    private static void Visit(AtkUldManager* uld, List<TextLine> nodes, HashSet<nint> visited,
        Vector2 origin, Configuration config, int depth)
    {
        if (uld == null || uld->NodeList == null || depth > 8 || uld->NodeListCount > 4096 ||
            !visited.Add((nint)uld)) return;
        for (var i = 0; i < uld->NodeListCount; i++)
        {
            var node = uld->NodeList[i];
            if (node == null || !Visible(node)) continue;
            var text = node->GetAsAtkTextNode();
            if (text != null && text->NodeText.Length is > 0 and <= 2048)
            {
                var value = SeString.Parse(text->NodeText.AsSpan()).TextValue;
                Bounds bounds = default;
                node->GetBounds(&bounds);
                nodes.Add(new TextLine(bounds.Pos1.X + origin.X - config.Left, bounds.Pos1.Y + origin.Y - config.Top,
                    bounds.Pos2.X + origin.X - config.Left, bounds.Pos2.Y + origin.Y - config.Top, value));
            }
            var component = node->GetAsAtkComponentNode();
            if (component != null && component->Component != null)
                Visit(&component->Component->UldManager, nodes, visited, origin, config, depth + 1);
        }
    }
}
