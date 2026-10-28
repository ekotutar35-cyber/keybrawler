using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace KeyBrawler;

/// <summary>Resolve KTGInput afresh on the framework thread; never fall back to ChatLog.</summary>
internal sealed unsafe class GameTextEntry : ITextEntryTarget
{
    private nint preparedInput;

    private static AtkUnitBase* MinigameAddon()
    {
        var manager = RaptureAtkUnitManager.Instance();
        var addon = manager == null ? null : manager->GetAddonByName(MinigameTargetPolicy.AddonName);
        if (addon == null || !addon->IsReady || !addon->IsVisible)
            throw new InvalidOperationException("Keybound Brawler giriş arayüzü (KTGInput) açık değil. Minioyunun yazı aşamasında tekrar deneyin.");
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
            if (uld->NodeListCount > 4096) throw new InvalidOperationException("Minioyun düğüm listesi beklenen boyutu aşıyor; işlem durduruldu.");
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
        if (input == null) throw new InvalidOperationException("Minioyunun metin alanı odaklanamadı. 'Hedef teşhisi' sonucunu paylaşın.");
        MinigameTargetPolicy.RequireAddon(input->OwnerAddon->NameString);
        if (input->OwnerAddon != addon) throw new InvalidOperationException("Minioyun arayüzü değişti; tekrar başlatın.");
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
                throw new InvalidOperationException($"KTGInput içinde {candidates.Count} etkin metin alanı bulundu; hedef tahmin edilmedi. 'Hedef teşhisi' sonucunu paylaşın.");
            input = (AtkComponentTextInput*)candidates[0];
        }
        var focus = input->GetFocusNode();
        if (focus == null) focus = (AtkResNode*)input->OwnerNode;
        if (focus == null) throw new InvalidOperationException("Minioyun giriş odak düğümü bulunamadı.");
        addon->Focus();
        var stage = AtkStage.Instance();
        if (stage == null || stage->AtkInputManager == null || !stage->AtkInputManager->SetFocus(focus, addon, 0))
            throw new InvalidOperationException("KTGInput yazı alanına odak verilemedi.");
        preparedInput = (nint)input;
    }

    private static bool Matches(AtkComponentTextInput* input, string text) =>
        string.Equals(input->RawString.ToString(), text, StringComparison.Ordinal) ||
        string.Equals(input->EvaluatedString.ToString(), text, StringComparison.Ordinal);

    public nint WriteAndReadBack(string text)
    {
        var input = FocusedMinigameInput();
        if ((nint)input != preparedInput) throw new InvalidOperationException("Minioyun yazı alanı değişti; tekrar başlatın.");
        if (!string.IsNullOrEmpty(input->RawString.ToString()))
            throw new InvalidOperationException("Minioyun yazı alanı boş değil. Mevcut metni temizleyip yeniden başlatın.");
        input->SetText(text);
        if (!Matches(input, text)) throw new InvalidOperationException("Minioyun metni değiştirdi veya reddetti; Enter gönderilmedi.");
        return (nint)input;
    }

    public bool StillContains(nint input, string text)
    {
        var current = FocusedMinigameInput();
        return (nint)current == input && Matches(current, text);
    }

    public string Describe()
    {
        var current = CurrentFocusedInput();
        var focusedName = current == null ? "yok" : current->OwnerAddon->NameString;
        try
        {
            var addon = MinigameAddon();
            var inputs = FindInputs(addon);
            return $"Odak: {focusedName}; KTGInput açık; metin alanları: {inputs.Count}; düğümler: " +
                   string.Join(", ", inputs.Select(p => ((AtkComponentTextInput*)p)->OwnerNode == null
                       ? "yok" : ((AtkComponentTextInput*)p)->OwnerNode->NodeId.ToString()));
        }
        catch (Exception e) { return $"Odak: {focusedName}; {e.Message}"; }
    }

    public void MouseDown() => throw new NotSupportedException("Minioyun hedefi koordinatla tıklanmaz.");
    public void MouseUp() { }
    public void EnterDown()
    {
        var input = FocusedMinigameInput();
        if ((nint)input != preparedInput) throw new InvalidOperationException("Enter öncesi minioyun hedefi değişti.");
        Native.EnterDown();
    }
    public void EnterUp() => Native.EnterUp();
}
