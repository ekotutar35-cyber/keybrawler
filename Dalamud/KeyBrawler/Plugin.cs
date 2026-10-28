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
    private readonly CommandInfo commandInfo;
    private readonly GameActivity activity = new();
    private readonly ActivityKeepAlive keepAlive = new();
    private readonly InputActivation activation = new();
    private readonly GameActivationKeys activationKeys;
    private readonly HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);
    private readonly Queue<string> log = new();
    private bool visible = true, dragging, previousF7, previousF8, previousF9;
    private readonly RunState session = new();
    private bool running => session.Running;
    private int selection;
    private Vector2 anchor;
    private TextEntrySequence? entry;
    private Task<List<TextLine>>? scan;
    private int scanGeneration, generation;
    private bool scanPreview, scanAllowEntry;
    private DateTime nextAction;
    private string status = UiText.T("Ready");
    private int ocrCount, submittedCount;
    private string lastError = "";
    private bool activityFaulted;
    private Stopwatch? scanClock;
    private long lastScanMs;
    private string timerStatus = UiText.T("Local timers have not been read yet");
    private DateTime nextTimerSample;
    private string colorSummary = UiText.T("Colors have not been read yet");
    private string targetColor = UiText.T("-");

    public Plugin()
    {
        config = Interface.GetPluginConfig() as Configuration ?? new Configuration();
        UiText.Language = config.Language == UiLanguage.English ? UiLanguage.English : UiLanguage.Turkish;
        ResetDisplayText();
        if (config.Migrate()) Interface.SavePluginConfig(config);
        entryTarget = new GameTextEntry(config);
        activationKeys = new GameActivationKeys(activity, entryTarget);
        ocr = new OcrWorkerClient(Path.Combine(Interface.AssemblyLocation.Directory!.FullName, "OcrWorker", "KeyBrawler.OcrWorker.exe"));
        commandInfo = new CommandInfo(OnCommand) { HelpMessage = UiText.T("Open KeyBrawler. /keybrawler stop stops input.") };
        Commands.AddHandler("/keybrawler", commandInfo);
        Interface.UiBuilder.Draw += Draw;
        Interface.UiBuilder.DisableAutomaticUiHide = true;
        Interface.UiBuilder.OpenConfigUi += Open;
        Interface.UiBuilder.OpenMainUi += Open;
        Framework.Update += Update;
        Report(UiText.T("F7: open panel | F8: start/stop | F9: emergency stop. Try OCR preview first."));
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
    private void ResetDisplayText()
    {
        status = UiText.T("Ready");
        timerStatus = UiText.T("Local timers have not been read yet");
        colorSummary = UiText.T("Colors have not been read yet");
        targetColor = "-";
        lastError = "";
    }

    private void SetLanguage(UiLanguage language)
    {
        config.Language = language;
        UiText.Language = language;
        commandInfo.HelpMessage = UiText.T("Open KeyBrawler. /keybrawler stop stops input.");
        ResetDisplayText();
        log.Clear();
        Report(UiText.T("Interface language: {0}", language == UiLanguage.English ? "English" : "Turkce (ASCII)"));
        Save();
    }
    private void Stop(string? reason = null)
    {
        session.Stop();
        generation++;
        ReleaseInputs();
        status = reason ?? UiText.T("Stopped");
        visible = true;
    }

    private void Pause(string reason)
    {
        if (!session.Pause(DateTime.UtcNow, reason)) return;
        generation++;
        ReleaseInputs();
        status = reason;
        Report(reason);
        visible = true;
    }

    private void ReleaseInputs()
    {
        if (entry != null)
        {
            try { entry.Release(entryTarget); }
            catch (Exception e) { Report(UiText.T("Key release error: ") + e.Message); }
            if (entry.TextStarted) Report(UiText.T("Check for text left in the input field."));
            entry = null;
        }
        try { activation.Release(activationKeys); }
        catch (Exception e) { Report(UiText.T("Could not release the input activation key: ") + e.Message); }
    }
    private void Toggle()
    {
        if (running) { Stop(); return; }
        if (!config.RegionSet) { Report(UiText.T("Select the bubble region first.")); return; }
        Stop();
        seen.Clear();
        ocrCount = submittedCount = 0;
        targetColor = UiText.T("-");
        colorSummary = UiText.T("Colors have not been read yet");
        lastError = "";
        activityFaulted = false;
        session.Start();
        status = UiText.T("Waiting for OCR");
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
        var language = config.Language;
        List<TextLine>? gameBubbles;
        try { gameBubbles = config.ReadGameBubbles ? GameBubbleReader.Read(config) : null; }
        catch (Exception e) { scan = Task.FromException<List<TextLine>>(e); return; }
        if (gameBubbles is { Count: 0 }) { scan = Task.FromResult(gameBubbles); return; }
        scan = Task.Run(async () =>
        {
            // Only preview hides the settings window. Normal OCR needs no extra settling delay.
            if (preview) await Task.Delay(150);
            return await ocr.ReadAsync(new OcrRequest(x, y, w, h, gap, language, gameBubbles));
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
                    colorSummary = UiText.T("Red: {0} | Yellow: {1} | Green: {2} | Blue: {3}" , bubbles.Count(b => b.Color == BubbleColor.Red), bubbles.Count(b => b.Color == BubbleColor.Yellow), bubbles.Count(b => b.Color == BubbleColor.Green), bubbles.Count(b => b.Color == BubbleColor.Blue));
                    if (scanPreview)
                    {
                        if (config.ReadGameBubbles) Report(GameBubbleReader.LastDiagnostics);
                        Report(bubbles.Count == 0 ? UiText.T("No text found.") : UiText.T("{0} bubble candidates:" , bubbles.Count));
                        foreach (var bubble in BubblePriority.Order(bubbles)) Report($"[{BubblePriority.Label(bubble.Color)}] {AnswerText.Normalize(bubble.Text)}");
                        visible = true;
                    }
                    else if (running && scanAllowEntry && entryTarget.IsInputActive())
                    {
                        var ordered = config.PrioritizeColors ? BubblePriority.Order(bubbles) : bubbles;
                        var chosen = ordered.FirstOrDefault(b => !seen.Contains(AnswerText.Normalize(b.Text)));
                        var text = chosen?.Text;
                        targetColor = chosen == null ? UiText.T("-") : BubblePriority.Label(chosen.Color);
                        entry = text == null ? null : new TextEntrySequence(text, false, false);
                        status = text == null
                            ? bubbles.Count == 0 ? UiText.T("Waiting for bubbles; the screen will be scanned again") : UiText.T("OCR: {0} candidates; waiting for new bubbles" , bubbles.Count)
                            : UiText.T("OCR received; verifying the minigame input");
                        nextAction = DateTime.UtcNow.AddMilliseconds(text == null ? 80 : 0);
                    }
                    else if (running)
                    {
                        status = bubbles.Count == 0 ? UiText.T("Waiting for bubbles; scanning continues") : UiText.T("OCR: {0} candidates; waiting for minigame input" , bubbles.Count);
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
                    Pause(UiText.T("Waiting for OCR / will retry: ") + e.Message); Report(UiText.T("Full error details: /xllog -> KeyBrawler")); visible = true;
                }
            }
        }
        if (!running) return;
        if (!Native.GameFocused())
        {
            Pause(UiText.T("Waiting for the game to regain focus; will resume automatically")); return;
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
                    lastError = UiText.T("Idle protection/additional detection disabled: ") + e.Message;
                    Report(lastError);
                    Log.Error(e, UiText.T("Optional activity monitoring failed; native KTGInput writing remains available"));
                }
            }
            if (activation.IsHolding)
            {
                activation.Tick(DateTime.UtcNow, entryTarget.IsInputActive(), entryTarget.ForeignTextFocused(), activationKeys);
                return;
            }
            if (DateTime.UtcNow < nextAction || !session.TryResume(DateTime.UtcNow)) return;
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
                    status = UiText.T("Opening minigame input with Enter");
                }
                else status = step switch
                {
                    InputStep.WaitForeignInput => UiText.T("Another text field is focused; close it and press Enter in the minigame"),
                    InputStep.WaitMinigame => UiText.T("Minigame not detected yet; press Enter in the minigame. Plugin is waiting"),
                    _ => UiText.T("Press Enter to open the minigame input; plugin is waiting"),
                };
                nextAction = DateTime.UtcNow.AddMilliseconds(50);
                return;
            }
            if (entry == null)
            {
                if (entryTarget.HasExistingInputText())
                {
                    status = UiText.T("Waiting for the input field to clear; remove partial text manually if needed");
                    nextAction = DateTime.UtcNow.AddMilliseconds(250);
                    return;
                }
                BeginScan(false);
                status = config.ReadGameBubbles ? UiText.T("Reading game bubbles...") : UiText.T("Scanning OCR...");
                nextAction = DateTime.UtcNow.AddMilliseconds(30);
            }
            else
            {
                if (config.ReadGameBubbles && !entry.TestOnly && !entry.TextStarted &&
                    !GameBubbleReader.Read(config).Any(b => string.Equals(AnswerText.Normalize(b.Text), entry.Text, StringComparison.Ordinal)))
                {
                    entry = null;
                    nextAction = DateTime.UtcNow;
                    return;
                }
                if (!entry.TextStarted && entryTarget.HasExistingInputText())
                {
                    status = UiText.T("Waiting for the input field to clear; plugin is still active");
                    nextAction = DateTime.UtcNow.AddMilliseconds(250);
                    return;
                }
                entry.Tick(DateTime.UtcNow, entryTarget);
                if (entry.Done)
                {
                    var finished = entry;
                    entry = null;
                    if (finished.TestOnly)
                    {
                        Stop();
                        Report(UiText.T("Typing test passed: 'test' verified in the minigame field. Not submitted; clear it manually."));
                        visible = true;
                    }
                    else
                    {
                        seen.Add(finished.Text);
                        submittedCount++;
                        if (!activityFaulted) lastError = "";
                        Report(UiText.T("Text verified in the minigame field; Enter sent: ") + finished.Text);
                        status = UiText.T("Waiting for the next bubble");
                        nextAction = DateTime.UtcNow.AddMilliseconds(Math.Clamp(config.DelayMs, 50, 10000));
                    }
                }
            }
        }
        catch (Exception e) { lastError = e.Message; Pause(UiText.T("Waiting: ") + e.Message); visible = true; }
    }

    private void Draw()
    {
        if (selection != 0) { DrawSelection(); return; }
        if (!visible) return;
        if (running) { DrawRunningPanel(); return; }
        ImGui.SetNextWindowSize(new Vector2(640, 510), ImGuiCond.FirstUseEver);
        if (ImGui.Begin("KeyBrawler 0.12", ref visible))
        {
            ImGui.Text(UiText.T("Interface language"));
            ImGui.SameLine();
            if (ImGui.RadioButton("English", config.Language == UiLanguage.English)) SetLanguage(UiLanguage.English);
            ImGui.SameLine();
            if (ImGui.RadioButton("Turkce (ASCII)", config.Language == UiLanguage.Turkish)) SetLanguage(UiLanguage.Turkish);
            ImGui.TextWrapped(UiText.T("Each bubble is typed and confirmed separately. Test with an empty input field first."));
            if (ImGui.Button(UiText.T("1. Select the bubble region in-game"))) Select(1);
            ImGui.Text(UiText.T("Region: {0}, {1}, {2} x {3}" , config.Left, config.Top, config.Width, config.Height));
            ImGui.Text(UiText.T("Typing target: Keybound Brawler / KTGInput (automatic)"));
            ImGui.Text(UiText.T("Status: ") + status);
            ImGui.Text(UiText.T("Last OCR: {0} candidates | Submitted: {1}" , ocrCount, submittedCount));
            ImGui.Text(UiText.T("Last OCR cycle: {0} ms" , lastScanMs));
            if (ImGui.Checkbox(UiText.T("Color priority: red > yellow > green > other"), ref config.PrioritizeColors)) Save();
            ImGui.TextWrapped(colorSummary);
            if (config.ReadGameBubbles) ImGui.TextWrapped(GameBubbleReader.LastDiagnostics);
            if (!string.IsNullOrEmpty(lastError)) ImGui.TextWrapped(UiText.T("Last error: ") + lastError);
            if (ImGui.Checkbox(UiText.T("Minigame idle protection"), ref config.PreventIdleKick)) Save();
            if (ImGui.Checkbox(UiText.T("Open the input field automatically with Enter"), ref config.AutoOpenInput)) Save();
            if (ImGui.Checkbox(UiText.T("Type using keyboard events (scan codes)"), ref config.PhysicalKeyInput)) Save();
            ImGui.Text(UiText.T("Idle timer refreshes: {0}" , keepAlive.RefreshCount));
            ImGui.TextWrapped(timerStatus);
            ImGui.TextWrapped(UiText.T("Protection runs only while the plugin is active, the game is focused, and Keybound Brawler is detected. If automatic opening is off, press Enter yourself."));
            ImGui.TextWrapped(UiText.T("Only the minigame KTGInput field is used. Nothing is typed into chat. The minigame typing phase must be open."));
            if (ImGui.Button(UiText.T("Target diagnostics (does not type)")))
            {
                Stop(); Report(entryTarget.Describe());
                var presence = activity.Observe();
                Report(UiText.T("Detection: input={0}, HUD={1}, duty={2}, agent={3}" , presence.InputVisible, presence.HudVisible, presence.Instance, presence.Agent));
            }
            var changed = ImGui.InputInt(UiText.T("Left (screen px)"), ref config.Left);
            changed |= ImGui.InputInt(UiText.T("Top (screen px)"), ref config.Top);
            changed |= ImGui.InputInt(UiText.T("Width"), ref config.Width);
            changed |= ImGui.InputInt(UiText.T("Height"), ref config.Height);
            if (changed)
            {
                Stop(); config.Width = Math.Clamp(config.Width, 1, 4000); config.Height = Math.Clamp(config.Height, 1, 4000);
                config.RegionSet = config.InputSet = true; Save();
            }
            if (ImGui.Checkbox(UiText.T("Read game bubble text (recommended)"), ref config.ReadGameBubbles)) { Stop(); Save(); }
            ImGui.TextWrapped(UiText.T("Game mode reads KTGEWord only; cast bars are excluded. No bubbles means keep waiting. Disable for legacy OCR."));
            if (!config.ReadGameBubbles && ImGui.SliderInt(UiText.T("Line merge gap (px)"), ref config.MergeGap, 0, 40)) { Stop(); Save(); }
            if (ImGui.SliderInt(UiText.T("Confirmation interval (ms)"), ref config.DelayMs, 50, 3000)) { Stop(); Save(); }
            if (ImGui.Button(UiText.T("OCR preview only (no keys)")))
            {
                Stop();
                if (config.RegionSet) { visible = false; BeginScan(true); }
                else Report(UiText.T("Select a region first."));
            }
            if (ImGui.Button(running ? UiText.T("Stop (F9)") : UiText.T("Start (F8)"))) Toggle();
            if (ImGui.Button(UiText.T("Typing test: type test (no confirmation, no OCR)")))
            {
                Stop();
                {
                    entry = new TextEntrySequence("test", false, true);
                    session.Start(); visible = true; lastError = "";
                    status = UiText.T("Typing test: will type in the minigame field in 3 seconds");
                    nextAction = DateTime.UtcNow.AddSeconds(3);
                }
            }
            ImGui.TextWrapped(UiText.T("F7: open panel | F8: start/stop | F9: emergency stop. No chat command needed. Waits when unfocused. Each text is submitted once per session."));
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
        if (ImGui.Begin(UiText.T("KeyBrawler 0.12 - running###KeyBrawlerRunning"), flags))
        {
            ImGui.TextWrapped(status);
            ImGui.Text(session.Paused ? UiText.T("Session: active, waiting to retry") : UiText.T("Session: active"));
            ImGui.Text(UiText.T("Confirmation interval: {0} ms" , config.DelayMs));
            ImGui.Text(UiText.T("OCR: {0} candidates | Submitted: {1}" , ocrCount, submittedCount));
            ImGui.Text(config.PhysicalKeyInput ? UiText.T("Typing: Windows keyboard scan codes") : UiText.T("Typing: direct text field"));
            ImGui.Text(UiText.T("Last OCR cycle: {0} ms" , lastScanMs));
            ImGui.TextWrapped(colorSummary);
            ImGui.Text(UiText.T("Selected bubble: ") + targetColor);
            if (scanClock != null) ImGui.Text(UiText.T("OCR in progress: {0} ms" , scanClock.ElapsedMilliseconds));
            if (!string.IsNullOrEmpty(lastError)) ImGui.TextWrapped(lastError);
            ImGui.Text(UiText.T("Idle refreshes: {0}" , keepAlive.RefreshCount));
            ImGui.TextWrapped(config.PreventIdleKick ? activityFaulted ? UiText.T("Protection: ERROR - see details above") : UiText.T("Protection: enabled (local timers)") : UiText.T("Protection: disabled"));
            ImGui.TextWrapped(timerStatus);
            if (ImGui.Button(UiText.T("Stop / settings (F7 or F9)"))) Stop();
            ImGui.TextWrapped(UiText.T("F8: start/stop. Move this panel outside the bubble OCR region."));
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
        ImGui.Begin(UiText.T("KeyBrawler selection overlay###KeyBrawlerSelection"), flags);
        ImGui.Text(selection == 1 ? UiText.T("Drag the bubble region. Esc: cancel") : UiText.T("Click the text field. Esc: cancel"));
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
