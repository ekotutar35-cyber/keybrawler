# KeyBrawler - Dalamud plugin

[Turkce (ASCII)](README.md)

Reads Keybound Brawler bubbles with Windows OCR and types one answer at a time, followed by Enter. Requires Windows 10/11 and Dalamud API 15. Experimental; not in the official Dalamud plugin list.

## Install or update

In `/xlsettings`, open **Experimental -> Custom Plugin Repositories** and add:

```text
https://raw.githubusercontent.com/ekotutar35-cyber/keybrawler/main/repo.json
```

Enable the repository, save, refresh `/xlplugins`, and install **KeyBrawler 0.10.0.0**. Remove the old `pluginmaster.json` repository entry if present, rather than keeping both. If the list is stale, restart the game.

[Download the full ZIP](https://raw.githubusercontent.com/ekotutar35-cyber/keybrawler/main/packages/KeyBrawler-0.10.0.0.zip) for a developer installation. Extract the entire package, including `OcrWorker/KeyBrawler.OcrWorker.exe`. Add the full path to `KeyBrawler.dll` under **Dev Plugin Locations**, then load the development plugin. No separate Python or .NET installation is needed.

## Language and answer text

Press **F7** and select **English** or **Turkce (ASCII)** at the top of settings. The preference is saved and applies immediately to the interface and subsequent messages. Application-owned Turkish messages use ASCII characters. External Windows error details may still use the operating system's language.

UI language does not translate game answers. Turkish OCR letter variants are converted to their ASCII equivalents before both keyboard and direct text input, while case and English spelling are preserved. OCR prefers the Windows English model, with the user-profile model as a fallback. Install the English OCR language component in Windows Settings for best results. A keyboard-layout result is corrected only if it exactly matches the expected answer after this normalization; unrelated mismatches are not overwritten or submitted.

## Use

1. Use Borderless Windowed mode. Select a screen region containing the bubbles; keep the input field and plugin panel outside it.
2. Try **OCR preview only**. It shows the ASCII answer and detected color. Region selection may be completed before enemies or bubbles appear.
3. Press **F8** or **Start**. Empty frames are normal: the plugin waits and keeps scanning. Only the dedicated `KTGInput` field is used; game chat is rejected.
4. Default priority is **red -> yellow -> green -> other/unknown**. Equal colors use screen order. Color classification uses nearby pixels and may require adjustment for the actual game palette.
5. The default confirmation interval is **1500 ms**. This does not guarantee protection against account penalties.
6. **F7** opens settings and stops; **F8** starts/stops; **F9** is emergency stop. No chat command is required. Temporary errors or focus loss pause input rather than disabling the session. If partial text remains, clear it manually before the plugin continues.

Keyboard-event mode sends Windows scan-code down/up pairs. Text is read back before confirmation; a mismatch does not press Enter. The fallback direct text mode remains available in settings. Do not hold Shift/Ctrl/Alt while automatic typing runs.

The panel shows OCR duration, color counts, selected color, session state, and local activity timers. Idle protection only updates local game timers while the verified minigame is active and focused. Its effectiveness against server-side removal has not been confirmed. The plugin still submits each normalized text once per session, so identical text in separate bubbles can be skipped.

## Build and validation

Requires .NET 10 SDK and Dalamud development assemblies. `DALAMUD_HOME` can override their location.

```powershell
dotnet build .\Dalamud\KeyBrawler\KeyBrawler.csproj -c Release
dotnet run --project .\Dalamud\KeyBrawler.Tests -c Release
& .\Dalamud\Package.ps1
```

Packaging includes the isolated, self-contained Windows OCR worker and creates a version-specific ZIP. Language selection, ASCII normalization, both input paths, and worker protocol are tested in Linux. Windows/FFXIV runtime behavior still requires in-game validation.
