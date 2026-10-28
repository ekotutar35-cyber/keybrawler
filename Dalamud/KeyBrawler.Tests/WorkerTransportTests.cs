using System.Text;
using System.Text.Json;
using KeyBrawler;

internal static class WorkerTransportTests
{
    public static async Task RunFakeWorker()
    {
        Console.InputEncoding = new UTF8Encoding(false);
        Console.OutputEncoding = new UTF8Encoding(false);
        while (await Console.In.ReadLineAsync() is { } line)
        {
            var request = JsonSerializer.Deserialize<OcrRequest>(line)!;
            if (request.X == -2) { await Task.Delay(Timeout.Infinite); return; }
            if (request.X == -3) return;
            if (request.X == -4) { Console.WriteLine("invalid JSON"); continue; }
            if (request.X == -5) { Console.WriteLine(JsonSerializer.Serialize(new OcrResponse([]))); continue; }
            var response = request.X == -1
                ? new OcrResponse(null, "OCR language unavailable", "full worker stack")
                : new OcrResponse([new TextLine(0, 0, 50, 20, "baloncuk çığ"),
                    new TextLine(0, 30, 50, 50, Environment.ProcessId.ToString())]);
            Console.WriteLine(JsonSerializer.Serialize(response));
        }
    }

    public static async Task Run()
    {
        var executable = Environment.ProcessPath!;
        var arguments = Path.GetFileNameWithoutExtension(executable) == "dotnet"
            ? new[] { typeof(WorkerTransportTests).Assembly.Location, "--fake-ocr-worker" }
            : new[] { "--fake-ocr-worker" };
        using var client = new OcrWorkerClient(executable, arguments, TimeSpan.FromSeconds(2));
        var first = await client.ReadAsync(new OcrRequest(0, 0, 50, 50, 12));
        var second = await client.ReadAsync(new OcrRequest(0, 0, 50, 50, 12));
        Expect(first[0].Text == "baloncuk çığ", "UTF-8 OCR text survives process boundary");
        Expect(first[1].Text == second[1].Text, "Repeated scans reuse the same isolated worker");
        var empty = await client.ReadAsync(new OcrRequest(-5, 0, 50, 50, 12));
        Expect(empty.Count == 0, "No bubbles is a successful empty frame, not an OCR error");
        var later = await client.ReadAsync(new OcrRequest(0, 0, 50, 50, 12));
        Expect(later[0].Text == "baloncuk çığ" && later[1].Text == first[1].Text,
            "New bubbles are readable after empty frames without restarting the worker");
        try
        {
            await client.ReadAsync(new OcrRequest(-1, 0, 50, 50, 12));
            throw new Exception("Expected worker error");
        }
        catch (OcrWorkerException e)
        {
            Expect(e.Message == "OCR language unavailable" && e.ToString().Contains("full worker stack"), "Worker exception details preserved");
        }
        var restarted = await client.ReadAsync(new OcrRequest(0, 0, 50, 50, 12));
        Expect(restarted[1].Text != first[1].Text, "Worker restarts after failure");
        await Failure<TimeoutException>(client, -2, "Hung worker times out");
        await Failure<IOException>(client, -3, "Unexpected worker exit is reported");
        await Failure<JsonException>(client, -4, "Malformed worker response is reported");
        var recovered = await client.ReadAsync(new OcrRequest(0, 0, 50, 50, 12));
        Expect(recovered[0].Text == "baloncuk çığ", "Client recovers after transport failures");
        client.Dispose();
        try
        {
            await client.ReadAsync(new OcrRequest(0, 0, 50, 50, 12));
            throw new Exception("Expected cancellation after disposal");
        }
        catch (OperationCanceledException) { Console.WriteLine("PASS Unload prevents new worker requests"); }
        using var missing = new OcrWorkerClient(Path.Combine(Path.GetTempPath(), Guid.NewGuid()+".exe"));
        try
        {
            await missing.ReadAsync(new OcrRequest(0, 0, 50, 50, 12));
            throw new Exception("Expected missing worker");
        }
        catch (FileNotFoundException e) { Expect(e.Message.Contains("yalnızca DLL"), "Missing helper gives actionable error"); }
        Console.WriteLine("12 worker-transport tests passed.");
    }

    private static async Task Failure<T>(OcrWorkerClient client, int x, string name) where T : Exception
    {
        try
        {
            await client.ReadAsync(new OcrRequest(x, 0, 50, 50, 12));
            throw new Exception("Expected " + typeof(T).Name);
        }
        catch (T) { Console.WriteLine("PASS " + name); }
    }
    private static void Expect(bool condition, string name)
    {
        if (!condition) throw new Exception("FAIL " + name);
        Console.WriteLine("PASS " + name);
    }
}
