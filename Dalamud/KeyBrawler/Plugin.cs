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
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;
    private readonly Configuration config;
    private readonly GameTextEntry entryTarget;
    private readonly OcrWorkerClient ocr;
    private readonly HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);
    private readonly Queue<string> log = new();
    private bool visible = true, running, dragging, previousF8, previousF9;
    private int selection;
    private Vector2 anchor;
    private TextEntrySequence? entry;
    private Task<List<TextLine>>? scan;
    private int scanGeneration, generation;
    private bool scanPreview;
    private DateTime nextAction;
    private string status = "Hazır";

    public Plugin()
    {
        config = Interface.GetPluginConfig() as Configuration ?? new Configuration();
        entryTarget = new GameTextEntry();
        ocr = new OcrWorkerClient(Path.Combine(Interface.AssemblyLocation.Directory!.FullName, "OcrWorker", "KeyBrawler.OcrWorker.exe"));
        Commands.AddHandler("/keybrawler", new CommandInfo(OnCommand) { HelpMessage = "Open KeyBrawler. /keybrawler stop stops input." });
        Interface.UiBuilder.Draw += Draw;
        Interface.UiBuilder.OpenConfigUi += Open;
        Interface.UiBuilder.OpenMainUi += Open;
        Framework.Update += Update;
        Report("Önce OCR önizlemesini deneyin. F8 başlat/durdur, F9 acil durdur.");
    }

    private void Open() { Stop(); visible = true; }
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
        if (entry != null)
        {
            try { entry.Release(entryTarget); }
            catch (Exception e) { Report("Tuş bırakma hatası: " + e.Message); }
            if (entry.TextVerified) Report("Yazı alanında kalan metni kontrol edin.");
            entry = null;
        }
        status = "Durduruldu";
    }
    private void Toggle()
    {
        if (running) { Stop(); return; }
        if (!config.RegionSet) { Report("Önce baloncuk bölgesini seçin."); return; }
        Stop();
        seen.Clear();
        running = true;
        status = "OCR bekleniyor";
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
            return await ocr.ReadAsync(new OcrRequest(x, y, w, h, gap));
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
                        var text = bubbles.FirstOrDefault(b => !seen.Contains(b.Text))?.Text;
                        entry = text == null ? null : new TextEntrySequence(text, false, false);
                        status = text == null ? "Yeni metin bekleniyor" : "Oyun yazı alanı doğrulanıyor";
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "KeyBrawler OCR failed");
                if (scanGeneration == generation)
                {
                    Stop(); Report("OCR hatası: " + e.Message); Report("Tam hata ayrıntıları: /xllog → KeyBrawler"); visible = true;
                }
            }
        }
        if (!running) return;
        if (!Native.GameFocused())
        {
            Stop(); Report("Oyun odağı kayboldu; durduruldu."); return;
        }
        if (visible || selection != 0 || DateTime.UtcNow < nextAction) return;
        try
        {
            if (entry == null)
            {
                BeginScan(false);
                nextAction = DateTime.UtcNow.AddMilliseconds(300);
            }
            else
            {
                entry.Tick(DateTime.UtcNow, entryTarget);
                if (entry.Done)
                {
                    var finished = entry;
                    entry = null;
                    if (finished.TestOnly)
                    {
                        Stop();
                        Report("Yazma testi başarılı: minioyun alanında 'test' doğrulandı. Enter gönderilmedi; metni elle temizleyin.");
                        visible = true;
                    }
                    else
                    {
                        seen.Add(finished.Text);
                        Report("Oyun alanında metin doğrulandı; Enter gönderildi: " + finished.Text);
                        status = "Sonraki baloncuk bekleniyor";
                        nextAction = DateTime.UtcNow.AddMilliseconds(Math.Clamp(config.DelayMs, 300, 10000));
                    }
                }
            }
        }
        catch (Exception e) { Stop(); Report(e.Message); visible = true; }
    }

    private void Draw()
    {
        if (selection != 0) { DrawSelection(); return; }
        if (!visible) return;
        ImGui.SetNextWindowSize(new Vector2(640, 510), ImGuiCond.FirstUseEver);
        if (ImGui.Begin("KeyBrawler 0.5", ref visible))
        {
            ImGui.TextWrapped("Her baloncuk ayrı yazılır ve Enter ile onaylanır. Önce boş bir giriş alanıyla deneyin.");
            if (ImGui.Button("1. Baloncuk bölgesini oyun içinde seç")) Select(1);
            ImGui.Text($"Bölge: {config.Left}, {config.Top}, {config.Width} x {config.Height}");
            ImGui.Text("Yazı hedefi: Keybound Brawler / KTGInput (otomatik)");
            ImGui.Text("Durum: " + status);
            ImGui.TextWrapped("Yalnızca minioyunun KTGInput alanına yazılır. Sohbet kutusuna yazılmaz. Minioyunun yazı aşaması açık olmalı.");
            if (ImGui.Button("Hedef teşhisi (yazı göndermez)")) { Stop(); Report(entryTarget.Describe()); }
            var changed = ImGui.InputInt("Sol (ekran px)", ref config.Left);
            changed |= ImGui.InputInt("Üst (ekran px)", ref config.Top);
            changed |= ImGui.InputInt("Genişlik", ref config.Width);
            changed |= ImGui.InputInt("Yükseklik", ref config.Height);
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
            if (ImGui.Button("Yazma testi: test yaz (Enter yok, OCR yok)"))
            {
                Stop();
                {
                    entry = new TextEntrySequence("test", false, true);
                    running = true; visible = false;
                    status = "Yazma testi: minioyun kutusuna 3 saniye sonra yazılacak";
                    nextAction = DateTime.UtcNow.AddSeconds(3);
                }
            }
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
        ocr.Dispose();
        // Background OCR only owns captured pixels; it never sends input or accesses Dalamud.
        if (scan != null) _ = scan.ContinueWith(t => _ = t.Exception, TaskContinuationOptions.OnlyOnFaulted);
        Framework.Update -= Update;
        Interface.UiBuilder.Draw -= Draw;
        Interface.UiBuilder.OpenConfigUi -= Open;
        Interface.UiBuilder.OpenMainUi -= Open;
        Commands.RemoveHandler("/keybrawler");
    }
}
