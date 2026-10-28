namespace KeyBrawler;

public enum InputStep { Process, OpenInput, WaitMinigame, WaitManualEnter, WaitForeignInput }

public static class WorkflowGate
{
    public static InputStep Decide(bool inputActive, bool textVerified, bool minigameDetected, bool autoOpen, bool foreignTextFocused)
    {
        // A verified KTGInput is sufficient for writing, regardless of optional duty/HUD detection.
        if (inputActive || textVerified) return InputStep.Process;
        if (foreignTextFocused) return InputStep.WaitForeignInput;
        if (!minigameDetected) return InputStep.WaitMinigame;
        return autoOpen ? InputStep.OpenInput : InputStep.WaitManualEnter;
    }
}

public sealed record MinigamePresence(bool Instance, bool Agent, bool InputVisible, bool HudVisible)
{
    public bool IsActive => Instance || InputVisible || HudVisible;
}
