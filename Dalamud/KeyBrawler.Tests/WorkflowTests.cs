using KeyBrawler;

internal static class WorkflowTests
{
    public static void Run()
    {
        var count = 0;
        void Check(bool condition, string name)
        {
            if (!condition) throw new Exception(name);
            count++;
            Console.WriteLine("PASS " + name);
        }
        Check(WorkflowGate.Decide(true, false, false, false, false) == InputStep.Process,
            "Native KTGInput writes even when optional duty/agent detection is false");
        Check(WorkflowGate.Decide(false, true, false, true, false) == InputStep.Process,
            "Submitted input can finish releasing Enter when field closes");
        Check(WorkflowGate.Decide(false, false, false, true, false) == InputStep.WaitMinigame,
            "Undetected minigame waits instead of stopping the plugin");
        Check(WorkflowGate.Decide(false, false, true, true, false) == InputStep.OpenInput,
            "Recognized minigame may open inactive input");
        Check(WorkflowGate.Decide(false, false, true, false, false) == InputStep.WaitManualEnter,
            "Manual mode waits for player Enter");
        Check(WorkflowGate.Decide(false, false, true, true, true) == InputStep.WaitForeignInput,
            "Chat focus never receives automatic Enter");
        Check(new MinigamePresence(false, false, true, false).IsActive,
            "Visible KTGInput identifies the minigame without agent or director flags");
        Check(new MinigamePresence(false, false, false, true).IsActive,
            "Visible minigame HUD identifies game while input is closed");
        Check(new MinigamePresence(true, false, false, false).IsActive,
            "Dedicated instance identifies game without agent flag");
        Check(!new MinigamePresence(false, true, false, false).IsActive,
            "Agent flag alone does not authorize activity updates");
        Check(!new MinigamePresence(false, false, false, false).IsActive,
            "No matching game evidence leaves activity updates disabled");
        Console.WriteLine($"{count} workflow regression tests passed.");
    }
}
