using UWUVCI.Mac.Services;
using UWUVCI.Mac.ViewModels;

namespace UWUVCI.Mac.Tests;

/// <summary>
/// Drives the real view model and the real inject service, with no fakes.
/// It downloads the native template from GitHub and needs a ROM you own, so it only does anything when
/// <c>UWUVCI_E2E_ROM</c> points at an N64 ROM. Without it the test returns at once (it does not prove anything).
/// </summary>
public class EndToEndTests
{
    [Fact]
    public async Task NativeInject_ThroughTheViewModel_WritesTitleAndSdFiles_OnlyWithE2eRom()
    {
        var rom = Environment.GetEnvironmentVariable("UWUVCI_E2E_ROM");
        if (string.IsNullOrEmpty(rom)) return;
        Assert.True(File.Exists(rom), "UWUVCI_E2E_ROM is set but the file does not exist.");

        using var dir = new TempDir();
        var prev = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(new InlineContext());
        try
        {
            var store = new MemoryStore { Current = new AppSettings { CommonKey = new string('0', 32) } };
            var settings = new SettingsViewModel(store, Path.Combine(dir.Path, "default-out"));
            var vm = new InjectViewModel(new InjectService(), settings, Path.Combine(dir.Path, "bases"))
            {
                Engine = InjectEngine.Native,
                GameName = "E2E Game",
            };
            vm.SetRom(rom);
            vm.SetOutput(Path.Combine(dir.Path, "out"));
            Assert.True(vm.CanInject);

            await vm.InjectCommand.ExecuteAsync(null);

            Assert.Equal(InjectPhase.Done, vm.Phase);
            Assert.Null(vm.ErrorMessage);
            foreach (var f in new[] { "title.tmd", "title.tik", "title.cert", "00000000.app" })
                Assert.True(File.Exists(Path.Combine(vm.ResultPath!, f)), f);
            Assert.Empty(Directory.GetFiles(vm.ResultPath!, "*.z64", SearchOption.AllDirectories));
            var autoboot = File.ReadAllText(Path.Combine(vm.ResultSdPath!, "not64", "autoboot.txt"));
            Assert.StartsWith("roms/", autoboot);
            Assert.True(File.Exists(Path.Combine(vm.ResultSdPath!, "not64", autoboot.Trim())));
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(prev);
        }
    }
}
