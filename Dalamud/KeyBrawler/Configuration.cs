using Dalamud.Configuration;

namespace KeyBrawler;

[Serializable]
public sealed class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;
    public int Left = 100;
    public int Top = 100;
    public int Width = 800;
    public int Height = 400;
    public int InputX = 600;
    public int InputY = 800;
    public int MergeGap = 12;
    public int DelayMs = 900;
    public bool RegionSet;
    public bool InputSet;
    public bool ClickBeforeWriting;
    public bool PreventIdleKick = true;
    public bool AutoOpenInput = true;
}
