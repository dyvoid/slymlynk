using System.IO;
using SlymLynk.Models;
using SlymLynk.Services;
using SlymLynk.ViewModels;

namespace SlymLynk.Tests;

public class FileDialogServiceTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), $"SlymLynkDialogTests_{Guid.NewGuid():N}");

    public FileDialogServiceTests() => Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); } catch { /* best-effort */ }
    }

    // --- BrowseSource ---

    [Fact]
    public void BrowseSource_DialogConfirms_TransitionsToSourceLoaded()
    {
        var dir = CreateTempDir("src");
        var dialogs = new FakeFileDialogService { SourceResult = dir };
        var vm = new MainViewModel(new SymlinkService(), dialogs, new FakeDropTargetResolver());

        vm.BrowseSourceCommand.Execute(null);

        Assert.Equal(AppState.SourceLoaded, vm.State);
        Assert.Equal(Path.GetFullPath(dir), vm.SourcePath);
    }

    [Fact]
    public void BrowseSource_DialogCancelled_StaysIdle()
    {
        var dialogs = new FakeFileDialogService { SourceResult = null };
        var vm = new MainViewModel(new SymlinkService(), dialogs, new FakeDropTargetResolver());

        vm.BrowseSourceCommand.Execute(null);

        Assert.Equal(AppState.Idle, vm.State);
        Assert.Null(vm.SourcePath);
    }

    // --- SaveToDestination ---

    [Fact]
    public void SaveToDestination_DialogConfirms_CreatesLink()
    {
        var source = CreateTempDir("source");
        var dest = Path.Combine(_tempDir, "link");
        var dialogs = new FakeFileDialogService { SourceResult = source, DestinationResult = dest };
        var vm = new MainViewModel(new SymlinkService(), dialogs, new FakeDropTargetResolver());

        vm.BrowseSourceCommand.Execute(null);
        vm.SaveToDestinationCommand.Execute(null);

        Assert.True(Directory.Exists(dest));
        Assert.Equal(AppState.SourceLoaded, vm.State);
    }

    [Fact]
    public void SaveToDestination_DialogCancelled_NoLinkCreated()
    {
        var source = CreateTempDir("source2");
        var dialogs = new FakeFileDialogService { SourceResult = source, DestinationResult = null };
        var vm = new MainViewModel(new SymlinkService(), dialogs, new FakeDropTargetResolver());

        vm.BrowseSourceCommand.Execute(null);
        vm.SaveToDestinationCommand.Execute(null);

        Assert.Equal(AppState.SourceLoaded, vm.State);
        Assert.True(dialogs.DestinationRequested);
    }

    [Fact]
    public void SaveToDestination_PassesIsDirectoryFlagToDialog()
    {
        var source = CreateTempDir("dirsrc");
        var dialogs = new FakeFileDialogService { SourceResult = source, DestinationResult = null };
        var vm = new MainViewModel(new SymlinkService(), dialogs, new FakeDropTargetResolver());

        vm.BrowseSourceCommand.Execute(null);
        vm.SaveToDestinationCommand.Execute(null);

        Assert.True(dialogs.LastIsDirectory);
    }

    private string CreateTempDir(string name)
    {
        var path = Path.Combine(_tempDir, name);
        Directory.CreateDirectory(path);
        return path;
    }
}
