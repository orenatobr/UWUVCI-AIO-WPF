using UWUVCI.Core.Injection;

namespace UWUVCI.Mac.Services;

/// <summary>The long running Core operations, behind an interface so view models can be tested.</summary>
public interface IInjectService
{
    /// <summary>Runs a full inject and returns the output title folder.</summary>
    /// <exception cref="OperationCanceledException">Cancelled; takes effect at the next step boundary.</exception>
    Task<string> InjectAsync(InjectRequest request, string outputRoot, string commonKey,
        IProgress<InjectProgress> progress, CancellationToken ct);

    /// <summary>
    /// Runs a native engine inject: fetches the not64 template, packs the title and writes the SD files.
    /// </summary>
    /// <exception cref="OperationCanceledException">Cancelled; takes effect at the next step boundary.</exception>
    Task<NativeLayout> InjectNativeAsync(InjectRequest request, string outputRoot, string commonKey,
        IProgress<InjectProgress> progress, CancellationToken ct);

    /// <summary>Downloads and decrypts a base into <paramref name="destDir"/>.</summary>
    Task DownloadBaseAsync(string titleId, string titleKey, string commonKey, string destDir,
        IProgress<InjectProgress> progress, CancellationToken ct);
}

/// <summary><see cref="IInjectService"/> backed by <see cref="WupInjector"/> and <see cref="BaseProvider"/>.</summary>
public sealed class InjectService : IInjectService
{
    /// <inheritdoc />
    public Task<string> InjectAsync(InjectRequest request, string outputRoot, string commonKey,
        IProgress<InjectProgress> progress, CancellationToken ct) =>
        Task.Run(() =>
        {
            ct.ThrowIfCancellationRequested();
            Directory.CreateDirectory(outputRoot);
            return WupInjector.Run(request, outputRoot, commonKey, new CancelCheck(progress, ct));
        }, ct);

    /// <inheritdoc />
    public async Task<NativeLayout> InjectNativeAsync(InjectRequest request, string outputRoot, string commonKey,
        IProgress<InjectProgress> progress, CancellationToken ct)
    {
        var check = new CancelCheck(progress, ct);
        ct.ThrowIfCancellationRequested();
        Directory.CreateDirectory(outputRoot);
        var temp = Directory.CreateTempSubdirectory("uwuvci-template-");
        try
        {
            progress.Report(new InjectProgress(2, "Downloading native emulator..."));
            var template = await Not64Template.FetchLatestAsync(temp.FullName, null, ct).ConfigureAwait(false);
            return await Task.Run(() => WupInjector.RunNative(request, template, outputRoot, commonKey, check), ct)
                .ConfigureAwait(false);
        }
        finally
        {
            temp.Delete(true);
        }
    }

    /// <inheritdoc />
    public Task DownloadBaseAsync(string titleId, string titleKey, string commonKey, string destDir,
        IProgress<InjectProgress> progress, CancellationToken ct)
    {
        Directory.CreateDirectory(destDir);
        return BaseProvider.DownloadAsync(titleId, titleKey, commonKey, destDir, progress, ct);
    }

    /// <summary>Core reports progress synchronously between steps, so throwing here aborts at the next boundary.</summary>
    private sealed class CancelCheck(IProgress<InjectProgress> inner, CancellationToken ct) : IProgress<InjectProgress>
    {
        public void Report(InjectProgress value)
        {
            ct.ThrowIfCancellationRequested();
            inner.Report(value);
        }
    }
}
