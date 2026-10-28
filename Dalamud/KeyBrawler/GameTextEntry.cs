using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace KeyBrawler;

/// <summary>Resolve KTGInput afresh on the framework thread; never fall back to ChatLog.</summary>
internal sealed unsafe class GameTextEntry(Configuration config) : ITextEntryTarget
{
    private nint preparedInput;
    private PhysicalTextWriter? physical;

    public bool IsInputActive()
    {
        var manager = RaptureAtkUnitManager.Instance();
        var addon = manager == null ? null : manager->GetAddonByName(MinigameTargetPolicy.AddonName);
        var focused = CurrentFocusedInput();
        return addon != null && addon->IsReady && addon->IsVisible && focused != null && focused->OwnerAddon == addon;
    }

    public bool ForeignTextFocused()
    {
        var focused = CurrentFocusedInput();
        return focused != null && !string.Equals(focused->OwnerAddon->NameString, MinigameTargetPolicy.AddonName, StringComparison.Ordinal);
    }

    public bool HasExistingInputText()
    {
        var input = CurrentFocusedInput();
        return input != null && string.Equals(input->OwnerAddon->NameString, MinigameTargetPolicy.AddonName, StringComparison.Ordinal)
            && !string.IsNullOrEmpty(input->RawString.ToString());
    }

    private static AtkUnitBase* MinigameAddon()
    {
        var manager = RaptureAtkUnitManager.Instance();
        var addon = manager == null ? null : manager->GetAddonByName(MinigameTargetPolicy.AddonName);
        if (addon == null || !addon->IsReady || !addon->IsVisible)
            throw new InvalidOperationException(UiText.T("Keybound Brawler input (KTGInput) is not open. Try again during the minigame typing phase."));
        MinigameTargetPolicy.RequireAddon(addon->NameString);
        return addon;
    }

    private static AtkComponentTextInput* CurrentFocusedInput()
    {
        var stage = AtkStage.Instance();
        if (stage == null || stage->AtkInputManager == null || !stage->AtkInputManager->IsTextInputActive) return null;
        var text = stage->AtkInputManager->TextInput;
        if (text == null || text->TargetTextInputEventInterface == null) return null;
        var node = text->TargetTextInputEventInterface->GetOwnerNode();
        for (var depth = 0; node != null && depth < 16; depth++, node = node->ParentNode)
        {
            var component = node->GetAsAtkComponentTextInput();
            if (component != null && component->Enabled && component->OwnerAddon != null) return component;
        }
        return null;
    }

    private static List<nint> FindInputs(AtkUnitBase* addon)
    {
        var result = new HashSet<nint>();
        var visited = new HashSet<nint>();
        void Visit(AtkUldManager* uld, int depth)
        {
            if (depth > 8 || uld == null || uld->NodeList == null || !visited.Add((nint)uld)) return;
            if (uld->NodeListCount > 4096) throw new InvalidOperationException(UiText.T("Minigame node list is larger than expected; operation cancelled."));
            for (var i = 0; i < uld->NodeListCount; i++)
            {
                var node = uld->NodeList[i];
                if (node == null) continue;
                var input = node->GetAsAtkComponentTextInput();
                if (input != null && input->Enabled && input->OwnerAddon == addon) result.Add((nint)input);
                var component = node->GetAsAtkComponentNode();
                if (component != null && component->Component != null) Visit(&component->Component->UldManager, depth + 1);
            }
        }
        Visit(&addon->UldManager, 0);
        return result.ToList();
    }

    private static AtkComponentTextInput* FocusedMinigameInput()
    {
        var addon = MinigameAddon();
        var input = CurrentFocusedInput();
        if (input == null) throw new InvalidOperationException(UiText.T("Could not focus the minigame input. Share the Target diagnostics output."));
        MinigameTargetPolicy.RequireAddon(input->OwnerAddon->NameString);
        if (input->OwnerAddon != addon) throw new InvalidOperationException(UiText.T("The minigame interface changed; try starting again."));
        return input;
    }

    public void Prepare()
    {
        var addon = MinigameAddon();
        var input = CurrentFocusedInput();
        if (input != null && input->OwnerAddon == addon)
        {
            preparedInput = (nint)input;
            return;
        }
        if (input == null || input->OwnerAddon != addon)
        {
            var candidates = FindInputs(addon);
            if (candidates.Count != 1)
                throw new InvalidOperationException(UiText.T("Found {0} active text fields in KTGInput; no target was guessed. Share the Target diagnostics output." , candidates.Count));
            input = (AtkComponentTextInput*)candidates[0];
        }
        var focus = input->GetFocusNode();
        if (focus == null) focus = (AtkResNode*)input->OwnerNode;
        if (focus == null) throw new InvalidOperationException(UiText.T("Minigame input focus node not found."));
        addon->Focus();
        var stage = AtkStage.Instance();
        if (stage == null || stage->AtkInputManager == null || !stage->AtkInputManager->SetFocus(focus, addon, 0))
            throw new InvalidOperationException(UiText.T("Could not focus the KTGInput text field."));
        preparedInput = (nint)input;
    }

    private static bool Matches(AtkComponentTextInput* input, string text) =>
        string.Equals(input->RawString.ToString(), text, StringComparison.Ordinal) ||
        string.Equals(input->EvaluatedString.ToString(), text, StringComparison.Ordinal);

    public nint WriteAndReadBack(string text)
    {
        var input = FocusedMinigameInput();
        if ((nint)input != preparedInput) throw new InvalidOperationException(UiText.T("The minigame text field changed; try starting again."));
        if (!string.IsNullOrEmpty(input->RawString.ToString()))
            throw new InvalidOperationException(UiText.T("The minigame text field is not empty. Clear it and try starting again."));
        input->SetText(text);
        if (!Matches(input, text)) throw new InvalidOperationException(UiText.T("The minigame changed or rejected the text; Enter was not sent."));
        return (nint)input;
    }

    public bool BeginPhysicalText(string text)
    {
        if (!config.PhysicalKeyInput) return false;
        var input = FocusedMinigameInput();
        if ((nint)input != preparedInput) throw new InvalidOperationException(UiText.T("Typing target changed; no key was sent."));
        if (!string.IsNullOrEmpty(input->RawString.ToString())) throw new InvalidOperationException(UiText.T("The minigame text field is not empty; clear the existing text."));
        physical = new PhysicalTextWriter(Native.MapText(text), new GamePhysicalKeys());
        return true;
    }

    public bool TickPhysicalText(DateTime now)
    {
        var input = FocusedMinigameInput();
        if ((nint)input != preparedInput) throw new InvalidOperationException(UiText.T("Target changed during keyboard input."));
        return physical?.Tick(now) ?? true;
    }

    public nint ReadPhysicalText(string text)
    {
        var input = FocusedMinigameInput();
        if ((nint)input == preparedInput && !Matches(input, text) &&
            (AnswerText.CanCorrect(input->RawString.ToString(), text) || AnswerText.CanCorrect(input->EvaluatedString.ToString(), text)))
        {
            // Some layouts emit Turkish variants despite an ASCII answer plan. Correct only that exact variant.
            input->SetText(text);
        }
        if ((nint)input != preparedInput || !Matches(input, text))
            throw new InvalidOperationException(UiText.T("Keyboard input did not match the text field. Not submitted; check the keyboard layout and remaining text."));
        return (nint)input;
    }

    public void ReleasePhysicalText()
    {
        try { physical?.Cancel(); }
        finally { physical = null; }
    }

    public bool StillContains(nint input, string text)
    {
        var current = FocusedMinigameInput();
        return (nint)current == input && Matches(current, text);
    }

    public string Describe()
    {
        var current = CurrentFocusedInput();
        var focusedName = current == null ? UiText.T("none") : current->OwnerAddon->NameString;
        try
        {
            var addon = MinigameAddon();
            var inputs = FindInputs(addon);
            return UiText.T("Focus: {0}; KTGInput open; text fields: {1}; nodes: " , focusedName, inputs.Count) +
                   string.Join(", ", inputs.Select(p => ((AtkComponentTextInput*)p)->OwnerNode == null
                       ? UiText.T("none") : ((AtkComponentTextInput*)p)->OwnerNode->NodeId.ToString()));
        }
        catch (Exception e) { return UiText.T("Focus: {0}; {1}" , focusedName, e.Message); }
    }

    public void MouseDown() => throw new NotSupportedException(UiText.T("The minigame target does not use coordinate clicks."));
    public void MouseUp() { }
    public void EnterDown()
    {
        var input = FocusedMinigameInput();
        if ((nint)input != preparedInput) throw new InvalidOperationException(UiText.T("Minigame target changed before Enter."));
        Native.EnterDown();
    }
    public void EnterUp() => Native.EnterUp();
}
