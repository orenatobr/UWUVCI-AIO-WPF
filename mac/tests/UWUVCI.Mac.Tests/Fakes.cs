using UWUVCI.Core.Injection;
using UWUVCI.Core.N64;
using UWUVCI.Mac.Services;
using UWUVCI.Mac.ViewModels;

namespace UWUVCI.Mac.Tests;

internal sealed class MemoryStore : ISettingsStore
{
    public AppSettings Current { get; set; } = new();
    public int Saves { get; private set; }
    public AppSettings Load() => Current;
    public void Save(AppSettings settings) { Current = settings; Saves++; }
}

internal sealed class FakeInjectService : IInjectService
{
    public InjectRequest? Request { get; private set; }
    public string? OutputRoot { get; private set; }
    public string? CommonKeyUsed { get; private set; }
    public (string Id, string Key, string Dest)? Download { get; private set; }
    public Exception? Fail { get; set; }
    public TaskCompletionSource? Gate { get; set; }
    public Action<string>? OnDownload { get; set; }
    public int DownloadCount { get; private set; }
    public TaskCompletionSource? DownloadGate { get; set; }

    public async Task<string> InjectAsync(InjectRequest request, string outputRoot, string commonKey,
        IProgress<InjectProgress> progress, CancellationToken ct)
    {
        Request = request;
        OutputRoot = outputRoot;
        CommonKeyUsed = commonKey;
        progress.Report(new InjectProgress(40, "Halfway"));
        if (Gate is not null) await Gate.Task.WaitAsync(ct);
        if (Fail is not null) throw Fail;
        return Path.Combine(outputRoot, "[WUP][N64] Result");
    }

    public InjectRequest? NativeRequest { get; private set; }

    public async Task<NativeLayout> InjectNativeAsync(InjectRequest request, string outputRoot, string commonKey,
        IProgress<InjectProgress> progress, CancellationToken ct)
    {
        NativeRequest = request;
        OutputRoot = outputRoot;
        CommonKeyUsed = commonKey;
        progress.Report(new InjectProgress(40, "Downloading native emulator..."));
        if (Gate is not null) await Gate.Task.WaitAsync(ct);
        if (Fail is not null) throw Fail;
        return new NativeLayout(Path.Combine(outputRoot, "[WUP][N64] Result Native"),
            Path.Combine(outputRoot, "[WUP][N64] Result Native SD"));
    }

    public async Task DownloadBaseAsync(string titleId, string titleKey, string commonKey, string destDir,
        IProgress<InjectProgress> progress, CancellationToken ct)
    {
        Download = (titleId, titleKey, destDir);
        DownloadCount++;
        CommonKeyUsed = commonKey;
        progress.Report(new InjectProgress(30, "Downloading"));
        if (DownloadGate is not null) await DownloadGate.Task.WaitAsync(ct);
        if (Fail is not null) throw Fail;
        OnDownload?.Invoke(destDir);
    }
}

/// <summary>Runs posted callbacks inline so Progress&lt;T&gt; is deterministic in tests.</summary>
internal sealed class InlineContext : SynchronizationContext
{
    public override void Post(SendOrPostCallback d, object? state) => d(state);
}

/// <summary>A fully wired view model with a valid ROM, base, name, output and key available.</summary>
internal sealed class Fixture : IDisposable
{
    private readonly SynchronizationContext? _previous = SynchronizationContext.Current;
    public TempDir Dir { get; } = new();
    public MemoryStore Store { get; } = new();
    public FakeInjectService Service { get; } = new();
    public SettingsViewModel Settings { get; }
    public InjectViewModel Vm { get; }

    public Fixture(string key = TestData.ValidKey, IReadOnlyList<N64Base>? bases = null)
    {
        SynchronizationContext.SetSynchronizationContext(new InlineContext());
        Store.Current = new AppSettings { CommonKey = key };
        Settings = new SettingsViewModel(Store, Path.Combine(Dir.Path, "default-out"));
        Vm = new InjectViewModel(Service, Settings, Path.Combine(Dir.Path, "bases"), bases: bases);
    }

    public void MakeValid()
    {
        Vm.SetRom(Dir.File("game.z64", TestData.BanjoTooie()));
        Vm.SetBase(Dir.Base());
        Vm.GameName = "Banjo|Tooie";
        Vm.SetOutput(Path.Combine(Dir.Path, "out"));
    }

    /// <summary>Native engine with everything valid except the base.</summary>
    public void MakeValidNative()
    {
        Vm.Engine = InjectEngine.Native;
        Vm.SetRom(Dir.File("game.z64", TestData.BanjoTooie()));
        Vm.GameName = "Banjo|Tooie";
        Vm.SetOutput(Path.Combine(Dir.Path, "out"));
    }

    public void Dispose()
    {
        SynchronizationContext.SetSynchronizationContext(_previous);
        Dir.Dispose();
    }
}
