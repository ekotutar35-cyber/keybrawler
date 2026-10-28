using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace KeyBrawler;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface Interface { get; private set; } = null!;
    [PluginService] internal static ICommandManager Commands { get; private set; } = null!;
    [PluginService] internal static IFramework Framework { get; private set; } = null!;
    private readonly Configuration config;
    private readonly HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);
    private readonly Queue<string> log = new();
    private bool visible = true, running, dragging, previousF8, previousF9;
    private int selection, offset;
    private Vector2 anchor;
    private string? pending;
    private Task<List<TextLine>>? scan;
    private int scanGeneration, generation;
    private bool scanPreview;
    private DateTime nextAction;

    public Plugin()
    {
        config = Interface.GetPluginConfig() as Configuration ?? new Configuration();
        Commands.AddHandler("/keybrawler", new CommandInfo(OnCommand) { HelpMessage = "Open KeyBrawler. /keybrawler stop stops input." });
        Interface.UiBuilder.Draw += Draw;
        Interface.UiBuilder.OpenConfigUi += Open;
        Interface.UiBuilder.OpenMainUi += Open;
        Framework.Update += Update;
        Report("Önce OCR önizlemesini deneyin. F8 başlat/durdur, F9 acil durdur.");
    }

    private void Open() => visible = true;
    private void OnCommand(string command, string args)
    {
        if (args.Trim().Equals("stop", StringComparison.OrdinalIgnoreCase)) Stop();
        else Open();
    }
    private void Report(string text)
    {
        log.Enqueue(text);
        while (log.Count > 12) log.Dequeue();
    }
    private void Save() => Interface.SavePluginConfig(config);
    private void Stop()
    {
        running = false;
        generation++;
        if (pending != null && offset > 0) Report("Yarım kalan metni yazı alanından elle temizleyin.");
        pending = null;
    }
    private void Toggle()
    {
        if (running) { Stop(); return; }
        if (!config.RegionSet || !config.InputSet) { Report("Önce bölge ve yazı alanını seçin."); return; }
        Stop();
        seen.Clear();
        running = true;
        visible = false;
        nextAction = DateTime.UtcNow.AddMilliseconds(500);
    }
    private void BeginScan(bool preview)
    {
        if (scan != null) return;
        scanGeneration = generation;
        scanPreview = preview;
        var x = config.Left; var y = config.Top; var w = config.Width; var h = config.Height; var gap = config.MergeGap;
        scan = Task.Run(async () =>
        {
            // Let the settings window disappear from the rendered screen before capture.
            await Task.Delay(350);
            return await WindowsOcr.ReadAsync(x, y, w, h, gap);
        });
    }
    private void Update(IFramework framework)
    {
        var f8 = (Native.GetAsyncKeyState(0x77) & 0x8000) != 0;
        var f9 = (Native.GetAsyncKeyState(0x78) & 0x8000) != 0;
        if (f9 && !previousF9) Stop();
        else if (f8 && !previousF8 && Native.GameFocused() && selection == 0) Toggle();
        previousF8 = f8; previousF9 = f9;

        if (scan is { IsCompleted: true })
        {
            var completed = scan;
            scan = null;
            try
            {
                var bubbles = completed.GetAwaiter().GetResult();
                if (scanGeneration == generation)
                {
                    if (scanPreview)
                    {
                        Report(bubbles.Count == 0 ? "Metin bulunamadı." : $"{bubbles.Count} baloncuk adayı:");
                        foreach (var bubble in bubbles) Report(bubble.Text);
                        visible = true;
                    }
                    else if (running)
                    {
                        pending = bubbles.FirstOrDefault(b => !seen.Contains(b.Text))?.Text;
                        offset = -1;
                    }
                }
            }
            catch (Exception e) { Stop(); Report("OCR hatası: " + e.Message); visible = true; }
        }
        if (!running) return;
        if (!Native.GameFocused())
        {
            Stop(); Report("Oyun odağı kayboldu; durduruldu."); return;
        }
        if (visible || selection != 0 || DateTime.UtcNow < nextAction) return;
        try
        {
            if (pending == null)
            {
                BeginScan(false);
                nextAction = DateTime.UtcNow.AddMilliseconds(300);
            }
            else if (offset == -1)
            {
                Native.Click(config.InputX, config.InputY);
                offset = 0;
                nextAction = DateTime.UtcNow.AddMilliseconds(150);
            }
            else if (offset < pending.Length)
            {
                Native.Character(pending[offset++]);
                nextAction = DateTime.UtcNow.AddMilliseconds(30);
            }
            else
            {
                Native.Enter();
                seen.Add(pending);
                Report("Windows'a gönderildi: " + pending);
                pending = null;
                nextAction = DateTime.UtcNow.AddMilliseconds(Math.Clamp(config.DelayMs, 300, 10000));
            }
        }
        catch (Exception e) { Stop(); Report(e.Message); visible = true; }
    }

    private void Draw()
    {
        if (selection != 0) { DrawSelection(); return; }
        if (!visible) return;
        ImGui.SetNextWindowSize(new Vector2(640, 510), ImGuiCond.FirstUseEver);
        if (ImGui.Begin("KeyBrawler", ref visible))
        {
            ImGui.TextWrapped("Her baloncuk ayrı yazılır ve Enter ile onaylanır. Önce boş bir giriş alanıyla deneyin.");
            if (ImGui.Button("1. Baloncuk bölgesini oyun içinde seç")) Select(1);
            if (ImGui.Button("2. Oyundaki yazı alanını seç")) Select(2);
            ImGui.Text($"Bölge: {config.Left}, {config.Top}, {config.Width} x {config.Height}");
            ImGui.Text($"Yazı alanı: {config.InputX}, {config.InputY}");
            var changed = ImGui.InputInt("Sol (ekran px)", ref config.Left);
            changed |= ImGui.InputInt("Üst (ekran px)", ref config.Top);
            changed |= ImGui.InputInt("Genişlik", ref config.Width);
            changed |= ImGui.InputInt("Yükseklik", ref config.Height);
            changed |= ImGui.InputInt("Yazı X", ref config.InputX);
            changed |= ImGui.InputInt("Yazı Y", ref config.InputY);
            if (changed)
            {
                Stop(); config.Width = Math.Clamp(config.Width, 1, 4000); config.Height = Math.Clamp(config.Height, 1, 4000);
                config.RegionSet = config.InputSet = true; Save();
            }
            if (ImGui.SliderInt("Satır birleştirme (px)", ref config.MergeGap, 0, 40)) { Stop(); Save(); }
            if (ImGui.SliderInt("Onaylar arası (ms)", ref config.DelayMs, 300, 5000)) { Stop(); Save(); }
            if (ImGui.Button("Sadece OCR önizleme (tuş göndermez)"))
            {
                Stop();
                if (config.RegionSet) { visible = false; BeginScan(true); }
                else Report("Önce bir bölge seçin.");
            }
            if (ImGui.Button(running ? "Durdur (F9)" : "Başlat (F8)")) Toggle();
            ImGui.TextWrapped("F8: başlat/durdur · F9: acil durdur · /keybrawler: pencereyi aç. Odağı kaybedince durur. Aynı metin bir oturumda bir kez gönderilir.");
            ImGui.Separator();
            foreach (var entry in log) ImGui.TextWrapped(entry);
        }
        ImGui.End();
    }

    private void Select(int mode)
    {
        Stop(); selection = mode; dragging = false;
    }
    private void DrawSelection()
    {
        var viewport = ImGui.GetMainViewport();
        ImGui.SetNextWindowPos(viewport.Pos);
        ImGui.SetNextWindowSize(viewport.Size);
        var flags = ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoMove |
                    ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoBackground;
        ImGui.Begin("KeyBrawler seçim katmanı", flags);
        ImGui.Text(selection == 1 ? "Baloncuk alanını sürükleyin. Esc: iptal" : "Yazı kutusuna tıklayın. Esc: iptal");
        var mouse = ImGui.GetMousePos();
        if (ImGui.IsKeyPressed(ImGuiKey.Escape)) { selection = 0; dragging = false; }
        else if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
        {
            anchor = mouse; dragging = true;
        }
        if (dragging)
        {
            ImGui.GetForegroundDrawList().AddRect(Vector2.Min(anchor, mouse), Vector2.Max(anchor, mouse), 0xFF00FFFF, 0, ImDrawFlags.None, 2);
            if (ImGui.IsMouseReleased(ImGuiMouseButton.Left))
            {
                var viewportsEnabled = (ImGui.GetIO().ConfigFlags & ImGuiConfigFlags.ViewportsEnable) != 0;
                var screenMouse = Native.ToScreen(mouse, viewportsEnabled);
                var screenAnchor = Native.ToScreen(anchor, viewportsEnabled);
                if (selection == 2)
                {
                    config.InputX = (int)screenMouse.X; config.InputY = (int)screenMouse.Y; config.InputSet = true;
                }
                else if (Math.Abs(mouse.X - anchor.X) > 10 && Math.Abs(mouse.Y - anchor.Y) > 10)
                {
                    config.Left = (int)Math.Min(screenAnchor.X, screenMouse.X); config.Top = (int)Math.Min(screenAnchor.Y, screenMouse.Y);
                    config.Width = (int)Math.Abs(mouse.X - anchor.X); config.Height = (int)Math.Abs(mouse.Y - anchor.Y);
                    config.RegionSet = true;
                }
                Save(); selection = 0; dragging = false;
            }
        }
        ImGui.End();
    }

    public void Dispose()
    {
        Stop();
        // Background OCR only owns captured pixels; it never sends input or accesses Dalamud.
        if (scan != null) _ = scan.ContinueWith(t => _ = t.Exception, TaskContinuationOptions.OnlyOnFaulted);
        Framework.Update -= Update;
        Interface.UiBuilder.Draw -= Draw;
        Interface.UiBuilder.OpenConfigUi -= Open;
        Interface.UiBuilder.OpenMainUi -= Open;
        Commands.RemoveHandler("/keybrawler");
    }
}
