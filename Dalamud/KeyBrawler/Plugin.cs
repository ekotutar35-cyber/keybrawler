using System.Numerics;
using System.Diagnostics;
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
    private readonly GameActivity activity = new();
    private readonly ActivityKeepAlive keepAlive = new();
    private readonly InputActivation activation = new();
    private readonly GameActivationKeys activationKeys;
    private readonly HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);
    private readonly Queue<string> log = new();
    private bool visible = true, running, dragging, previousF7, previousF8, previousF9;
    private int selection;
    private Vector2 anchor;
    private TextEntrySequence? entry;
    private Task<List<TextLine>>? scan;
    private int scanGeneration, generation;
    private bool scanPreview, scanAllowEntry;
    private DateTime nextAction;
    private string status = "Hazır";
    private int ocrCount, submittedCount;
    private string lastError = "";
    private bool activityFaulted;
    private Stopwatch? scanClock;
    private long lastScanMs;
    private string timerStatus = "Yerel sayaçlar henüz okunmadı";
    private DateTime nextTimerSample;

    public Plugin()
    {
        config = Interface.GetPluginConfig() as Configuration ?? new Configuration();
        if (config.Migrate()) Interface.SavePluginConfig(config);
        entryTarget = new GameTextEntry(config);
        activationKeys = new GameActivationKeys(activity, entryTarget);
        ocr = new OcrWorkerClient(Path.Combine(Interface.AssemblyLocation.Directory!.FullName, "OcrWorker", "KeyBrawler.OcrWorker.exe"));
        Commands.AddHandler("/keybrawler", new CommandInfo(OnCommand) { HelpMessage = "Open KeyBrawler. /keybrawler stop stops input." });
        Interface.UiBuilder.Draw += Draw;
        Interface.UiBuilder.DisableAutomaticUiHide = true;
        Interface.UiBuilder.OpenConfigUi += Open;
        Interface.UiBuilder.OpenMainUi += Open;
        Framework.Update += Update;
        Report("F7: paneli aç · F8: başlat/durdur · F9: acil durdur. Önce OCR önizlemesini deneyin.");
    }

    private void Open() { selection = 0; dragging = false; Stop(); visible = true; }
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
    private void Stop(string? reason = null)
    {
        running = false;
        generation++;
        if (entry != null)
        {
            try { entry.Release(entryTarget); }
            catch (Exception e) { Report("Tuş bırakma hatası: " + e.Message); }
            if (entry.TextStarted) Report("Yazı alanında kalan metni kontrol edin.");
            entry = null;
        }
        status = reason ?? "Durduruldu";
        visible = true;
        try { activation.Release(activationKeys); }
        catch (Exception e) { Report("Giriş açma tuşu bırakılamadı: " + e.Message); }
    }
    private void Toggle()
    {
        if (running) { Stop(); return; }
        if (!config.RegionSet) { Report("Önce baloncuk bölgesini seçin."); return; }
        Stop();
        seen.Clear();
        ocrCount = submittedCount = 0;
        lastError = "";
        activityFaulted = false;
        running = true;
        status = "OCR bekleniyor";
        visible = true;
        nextAction = DateTime.UtcNow.AddMilliseconds(100);
    }
    private void BeginScan(bool preview, bool allowEntry = true)
    {
        if (scan != null) return;
        scanGeneration = generation;
        scanPreview = preview;
        scanAllowEntry = allowEntry;
        scanClock = Stopwatch.StartNew();
        var x = config.Left; var y = config.Top; var w = config.Width; var h = config.Height; var gap = config.MergeGap;
        scan = Task.Run(async () =>
        {
            // Only preview hides the settings window. Normal OCR needs no extra settling delay.
            if (preview) await Task.Delay(150);
            return await ocr.ReadAsync(new OcrRequest(x, y, w, h, gap));
        });
    }
    private void Update(IFramework framework)
    {
        var f7 = (Native.GetAsyncKeyState(0x76) & 0x8000) != 0;
        var f8 = (Native.GetAsyncKeyState(0x77) & 0x8000) != 0;
        var f9 = (Native.GetAsyncKeyState(0x78) & 0x8000) != 0;
        if (f9 && !previousF9) Stop();
        else if (f7 && !previousF7 && Native.GameFocused()) Open();
        else if (f8 && !previousF8 && Native.GameFocused() && selection == 0) Toggle();
        previousF7 = f7; previousF8 = f8; previousF9 = f9;

        if (scan is { IsCompleted: true })
        {
            var completed = scan;
            lastScanMs = scanClock?.ElapsedMilliseconds ?? 0;
            scanClock = null;
            scan = null;
            try
            {
                var bubbles = completed.GetAwaiter().GetResult();
                if (scanGeneration == generation)
                {
                    ocrCount = bubbles.Count;
                    if (scanPreview)
                    {
                        Report(bubbles.Count == 0 ? "Metin bulunamadı." : $"{bubbles.Count} baloncuk adayı:");
                        foreach (var bubble in bubbles) Report(bubble.Text);
                        visible = true;
                    }
                    else if (running && scanAllowEntry && entryTarget.IsInputActive())
                    {
                        var text = bubbles.FirstOrDefault(b => !seen.Contains(b.Text))?.Text;
                        entry = text == null ? null : new TextEntrySequence(text, false, false);
                        status = text == null
                            ? bubbles.Count == 0 ? "Baloncuk bekleniyor; ekran yeniden taranacak" : $"OCR: {bubbles.Count} aday; yeni baloncuk bekleniyor"
                            : "OCR okundu; minioyun yazı alanı doğrulanıyor";
                        nextAction = DateTime.UtcNow.AddMilliseconds(text == null ? 80 : 0);
                    }
                    else if (running)
                    {
                        status = bubbles.Count == 0 ? "Baloncuk bekleniyor; tarama devam ediyor" : $"OCR: {bubbles.Count} aday; minioyun girişi bekleniyor";
                        nextAction = DateTime.UtcNow.AddMilliseconds(80);
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "KeyBrawler OCR failed");
                if (scanGeneration == generation)
                {
                    lastError = "OCR: " + e.Message;
                    Stop(lastError); Report("OCR hatası: " + e.Message); Report("Tam hata ayrıntıları: /xllog → KeyBrawler"); visible = true;
                }
            }
        }
        if (!running) return;
        if (!Native.GameFocused())
        {
            Stop("Oyun odağı kayboldu; durduruldu."); Report(status); return;
        }
        if (selection != 0) return;
        try
        {
            var inputActive = entryTarget.IsInputActive();
            var minigameDetected = inputActive;
            if (!activityFaulted)
            {
                try
                {
                    minigameDetected |= activity.MinigameActive;
                    keepAlive.Tick(DateTime.UtcNow, config.PreventIdleKick, running, Native.GameFocused(), false, activity);
                    if (DateTime.UtcNow >= nextTimerSample)
                    {
                        timerStatus = activity.ReadTimerStatus();
                        nextTimerSample = DateTime.UtcNow.AddSeconds(1);
                    }
                }
                catch (Exception e)
                {
                    activityFaulted = true;
                    lastError = "Hareketsizlik koruması/ek algılama kapatıldı: " + e.Message;
                    Report(lastError);
                    Log.Error(e, "Optional activity monitoring failed; native KTGInput writing remains available");
                }
            }
            if (activation.IsHolding)
            {
                activation.Tick(DateTime.UtcNow, entryTarget.IsInputActive(), entryTarget.ForeignTextFocused(), activationKeys);
                return;
            }
            if (DateTime.UtcNow < nextAction) return;
            var step = WorkflowGate.Decide(inputActive, entry?.TextStarted == true,
                minigameDetected, config.AutoOpenInput, entryTarget.ForeignTextFocused());
            if (step != InputStep.Process)
            {
                // Discard stale OCR text and capture again after the player opens the input with Enter.
                if (entry is { TestOnly: false }) entry = null;
                // Observe new waves before the input is active, without queueing stale answers.
                if (entry == null) BeginScan(false, false);
                if (step == InputStep.OpenInput)
                {
                    activation.Tick(DateTime.UtcNow, false, false, activationKeys);
                    status = "Minioyun girişi Enter ile açılıyor";
                }
                else status = step switch
                {
                    InputStep.WaitForeignInput => "Başka bir yazı alanı odakta; onu kapatın ve minioyunda Enter'a basın",
                    InputStep.WaitMinigame => "Minioyun henüz algılanmadı; minioyunda Enter'a basın. Eklenti bekliyor",
                    _ => "Minioyun girişini açmak için Enter'a basın; eklenti bekliyor",
                };
                nextAction = DateTime.UtcNow.AddMilliseconds(50);
                return;
            }
            if (entry == null)
            {
                BeginScan(false);
                status = "OCR taranıyor…";
                nextAction = DateTime.UtcNow.AddMilliseconds(30);
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
                        Report("Yazma testi başarılı: minioyun alanında 'test' doğrulandı. Test onaylanmadı; metni elle temizleyin.");
                        visible = true;
                    }
                    else
                    {
                        seen.Add(finished.Text);
                        submittedCount++;
                        Report("Oyun alanında metin doğrulandı; Enter gönderildi: " + finished.Text);
                        status = "Sonraki baloncuk bekleniyor";
                        nextAction = DateTime.UtcNow.AddMilliseconds(Math.Clamp(config.DelayMs, 50, 10000));
                    }
                }
            }
        }
        catch (Exception e) { lastError = e.Message; Stop(lastError); Report(e.Message); visible = true; }
    }

    private void Draw()
    {
        if (selection != 0) { DrawSelection(); return; }
        if (!visible) return;
        if (running) { DrawRunningPanel(); return; }
        ImGui.SetNextWindowSize(new Vector2(640, 510), ImGuiCond.FirstUseEver);
        if (ImGui.Begin("KeyBrawler 0.8", ref visible))
        {
            ImGui.TextWrapped("Her baloncuk ayrı yazılır ve Enter ile onaylanır. Önce boş bir giriş alanıyla deneyin.");
            if (ImGui.Button("1. Baloncuk bölgesini oyun içinde seç")) Select(1);
            ImGui.Text($"Bölge: {config.Left}, {config.Top}, {config.Width} x {config.Height}");
            ImGui.Text("Yazı hedefi: Keybound Brawler / KTGInput (otomatik)");
            ImGui.Text("Durum: " + status);
            ImGui.Text($"Son OCR: {ocrCount} aday · Gönderilen: {submittedCount}");
            ImGui.Text($"Son OCR turu: {lastScanMs} ms");
            if (!string.IsNullOrEmpty(lastError)) ImGui.TextWrapped("Son hata: " + lastError);
            if (ImGui.Checkbox("Minioyunda hareketsizlik koruması", ref config.PreventIdleKick)) Save();
            if (ImGui.Checkbox("Giriş alanını Enter ile otomatik aç", ref config.AutoOpenInput)) Save();
            if (ImGui.Checkbox("Klavye olaylarıyla yaz (tarama kodları)", ref config.PhysicalKeyInput)) Save();
            ImGui.Text($"Hareketsizlik sayacı yenilemeleri: {keepAlive.RefreshCount}");
            ImGui.TextWrapped(timerStatus);
            ImGui.TextWrapped("Koruma yalnızca eklenti çalışırken, oyun öndeyken ve Keybound Brawler etkin durumdayken uygulanır. Otomatik açma kapalıysa giriş için Enter'a kendiniz basın.");
            ImGui.TextWrapped("Yalnızca minioyunun KTGInput alanına yazılır. Sohbet kutusuna yazılmaz. Minioyunun yazı aşaması açık olmalı.");
            if (ImGui.Button("Hedef teşhisi (yazı göndermez)"))
            {
                Stop(); Report(entryTarget.Describe());
                var presence = activity.Observe();
                Report($"Algılama: giriş={presence.InputVisible}, HUD={presence.HudVisible}, görev={presence.Instance}, ajan={presence.Agent}");
            }
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
            if (ImGui.SliderInt("Onaylar arası (ms)", ref config.DelayMs, 50, 3000)) { Stop(); Save(); }
            if (ImGui.Button("Sadece OCR önizleme (tuş göndermez)"))
            {
                Stop();
                if (config.RegionSet) { visible = false; BeginScan(true); }
                else Report("Önce bir bölge seçin.");
            }
            if (ImGui.Button(running ? "Durdur (F9)" : "Başlat (F8)")) Toggle();
            if (ImGui.Button("Yazma testi: test yaz (onay yok, OCR yok)"))
            {
                Stop();
                {
                    entry = new TextEntrySequence("test", false, true);
                    running = true; visible = true; lastError = "";
                    status = "Yazma testi: minioyun kutusuna 3 saniye sonra yazılacak";
                    nextAction = DateTime.UtcNow.AddSeconds(3);
                }
            }
            ImGui.TextWrapped("F7: paneli aç · F8: başlat/durdur · F9: acil durdur. Sohbet komutu gerekmez. Odağı kaybedince durur. Aynı metin bir oturumda bir kez gönderilir.");
            ImGui.Separator();
            foreach (var entry in log) ImGui.TextWrapped(entry);
        }
        ImGui.End();
    }

    private void DrawRunningPanel()
    {
        var viewport = ImGui.GetMainViewport();
        ImGui.SetNextWindowPos(viewport.Pos + new Vector2(viewport.Size.X - 410, 20), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowSize(new Vector2(390, 210), ImGuiCond.FirstUseEver);
        var flags = ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoNavInputs | ImGuiWindowFlags.NoNavFocus;
        if (ImGui.Begin("KeyBrawler 0.8 — çalışıyor", flags))
        {
            ImGui.TextWrapped(status);
            ImGui.Text($"OCR: {ocrCount} aday · Gönderilen: {submittedCount}");
            ImGui.Text(config.PhysicalKeyInput ? "Yazma: Windows klavye tarama kodları" : "Yazma: doğrudan metin alanı");
            ImGui.Text($"Son OCR turu: {lastScanMs} ms");
            if (scanClock != null) ImGui.Text($"Devam eden OCR: {scanClock.ElapsedMilliseconds} ms");
            if (!string.IsNullOrEmpty(lastError)) ImGui.TextWrapped(lastError);
            ImGui.Text($"Hareketsizlik yenilemeleri: {keepAlive.RefreshCount}");
            ImGui.TextWrapped(config.PreventIdleKick ? activityFaulted ? "Koruma: HATA — ayrıntı yukarıda" : "Koruma: açık (yerel sayaçlar)" : "Koruma: kapalı");
            ImGui.TextWrapped(timerStatus);
            if (ImGui.Button("Durdur / ayarlar (F7 veya F9)")) Stop();
            ImGui.TextWrapped("F8: başlat/durdur. Bu paneli baloncukların bulunduğu OCR bölgesinin dışına taşıyın.");
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
