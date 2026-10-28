using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace KeyBrawler;

public sealed class OcrWorkerException(string message, string? details) : Exception(message)
{
    public override string ToString() => base.ToString() + "\nOCR worker: " + details;
}

/// <summary>Only JSON and managed data cross the process boundary; WinRT stays outside FFXIV.</summary>
public sealed class OcrWorkerClient(string executable, string[]? arguments = null, TimeSpan? timeout = null) : IDisposable
{
    private readonly SemaphoreSlim gate = new(1, 1);
    private readonly CancellationTokenSource lifetime = new();
    private readonly object sync = new();
    private Process? process;
    private bool disposed;

    private Process GetProcess()
    {
        lock (sync)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (process != null && !process.HasExited) return process;
            StopProcess();
            if (!File.Exists(executable))
                throw new FileNotFoundException(UiText.T("OCR helper not found. Update the complete plugin package; do not copy only the DLL."), executable);
            var start = new ProcessStartInfo(executable)
            {
                UseShellExecute = false, CreateNoWindow = true,
                RedirectStandardInput = true, RedirectStandardOutput = true,
                StandardInputEncoding = new UTF8Encoding(false),
                StandardOutputEncoding = new UTF8Encoding(false),
                WorkingDirectory = Path.GetDirectoryName(executable)!,
            };
            foreach (var arg in arguments ?? []) start.ArgumentList.Add(arg);
            process = Process.Start(start) ?? throw new InvalidOperationException(UiText.T("Could not start the OCR helper."));
            return process;
        }
    }

    public async Task<List<TextLine>> ReadAsync(OcrRequest request)
    {
        request.Validate();
        await gate.WaitAsync(lifetime.Token).ConfigureAwait(false);
        try
        {
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
            deadline.CancelAfter(timeout ?? TimeSpan.FromSeconds(30));
            var token = deadline.Token;
            var worker = GetProcess();
            await worker.StandardInput.WriteLineAsync(JsonSerializer.Serialize(request).AsMemory(), token).ConfigureAwait(false);
            await worker.StandardInput.FlushAsync(token).ConfigureAwait(false);
            var line = await worker.StandardOutput.ReadLineAsync(token).ConfigureAwait(false)
                ?? throw new IOException(UiText.T("The OCR helper exited without a response. Check antivirus quarantine or /xllog."));
            var response = JsonSerializer.Deserialize<OcrResponse>(line)
                ?? throw new IOException(UiText.T("The OCR helper returned an empty response."));
            if (response.Error != null) throw new OcrWorkerException(response.Error, response.Details);
            return response.Lines ?? throw new IOException(UiText.T("The OCR response has no text list."));
        }
        catch (OperationCanceledException) when (!lifetime.IsCancellationRequested)
        {
            lock (sync) StopProcess();
            throw new TimeoutException(UiText.T("The OCR helper timed out; it will be restarted."));
        }
        catch
        {
            lock (sync) StopProcess();
            throw;
        }
        finally { gate.Release(); }
    }

    private void StopProcess()
    {
        if (process == null) return;
        try { if (!process.HasExited) process.Kill(entireProcessTree: true); }
        catch (InvalidOperationException) { }
        catch (System.ComponentModel.Win32Exception) { }
        finally { process.Dispose(); process = null; }
    }

    public void Dispose()
    {
        lock (sync)
        {
            if (disposed) return;
            disposed = true;
        }
        lifetime.Cancel();
        lock (sync) StopProcess();
    }
}
