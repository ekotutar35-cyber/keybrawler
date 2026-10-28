using FFXIVClientStructs.FFXIV.Component.GUI;

namespace KeyBrawler;

/// <summary>Only used on Dalamud's framework thread; never retain a dereferenceable pointer across frames.</summary>
internal sealed unsafe class GameTextEntry(Configuration config) : ITextEntryTarget
{
    private static AtkComponentTextInput* FocusedInput()
    {
        var stage = AtkStage.Instance();
        if (stage == null || stage->AtkInputManager == null || !stage->AtkInputManager->IsTextInputActive)
            throw new InvalidOperationException("Oyun metin alanı odakta değil. Boş yazı kutusuna tıklayıp F8'e basın veya otomatik tıklama koordinatını yeniden seçin.");
        var textInput = stage->AtkInputManager->TextInput;
        if (textInput == null || textInput->TargetTextInputEventInterface == null)
            throw new InvalidOperationException("Oyun metin giriş hedefi bulunamadı.");
        var node = textInput->TargetTextInputEventInterface->GetOwnerNode();
        for (var depth = 0; node != null && depth < 16; depth++, node = node->ParentNode)
        {
            var component = node->GetAsAtkComponentTextInput();
            if (component != null && component->Enabled && component->OwnerAddon != null)
                return component;
        }
        throw new InvalidOperationException("Odaktaki nesne desteklenen bir oyun metin kutusu değil.");
    }

    private static bool Matches(AtkComponentTextInput* input, string text) =>
        string.Equals(input->RawString.ToString(), text, StringComparison.Ordinal) ||
        string.Equals(input->EvaluatedString.ToString(), text, StringComparison.Ordinal);

    public nint WriteAndReadBack(string text)
    {
        var input = FocusedInput();
        if (!string.IsNullOrEmpty(input->RawString.ToString()))
            throw new InvalidOperationException("Yazı alanı boş değil. Mevcut metni temizleyip yeniden başlatın.");
        input->SetText(text);
        if (!Matches(input, text))
            throw new InvalidOperationException("Oyun metni değiştirdi veya reddetti; Enter gönderilmedi. Alanı kontrol edin.");
        return (nint)input;
    }

    public bool StillContains(nint input, string text)
    {
        // Resolve focus again before comparing identity; do not dereference an old address.
        var current = FocusedInput();
        return (nint)current == input && Matches(current, text);
    }

    public void MouseDown() => Native.MouseDown(config.InputX, config.InputY);
    public void MouseUp() => Native.MouseUp();
    public void EnterDown() => Native.EnterDown();
    public void EnterUp() => Native.EnterUp();
}
