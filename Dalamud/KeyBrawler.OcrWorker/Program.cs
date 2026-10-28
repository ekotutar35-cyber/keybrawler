using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using KeyBrawler;

// COM/WinRT is initialized in this process only, never in FFXIV's plugin load context.
Console.InputEncoding = new UTF8Encoding(false);
Console.OutputEncoding = new UTF8Encoding(false);
SetProcessDPIAware();
while (await Console.In.ReadLineAsync() is { } line)
{
    OcrResponse response;
    try
    {
        var request = JsonSerializer.Deserialize<OcrRequest>(line)
            ?? throw new InvalidOperationException(UiText.T("Empty OCR request."));
        UiText.Language = request.Language == UiLanguage.English ? UiLanguage.English : UiLanguage.Turkish;
        request.Validate();
        var lines = await WindowsOcr.ReadAsync(request.X, request.Y, request.Width, request.Height, request.Gap);
        response = new OcrResponse(lines);
    }
    catch (Exception e)
    {
        response = new OcrResponse(null, $"{e.GetType().Name} (0x{e.HResult:X8}): {e.Message}", e.ToString());
    }
    await Console.Out.WriteLineAsync(JsonSerializer.Serialize(response));
    await Console.Out.FlushAsync();
}

[DllImport("user32.dll")]
static extern bool SetProcessDPIAware();
