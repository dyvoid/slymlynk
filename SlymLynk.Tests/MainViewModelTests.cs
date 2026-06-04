using System.IO;
using SlymLynk.Models;
using SlymLynk.ViewModels;

namespace SlymLynk.Tests;

public class MainViewModelTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), $"SlymLynkVMTests_{Guid.NewGuid():N}");

    public MainViewModelTests() => Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); } catch { /* best-effort */ }
    }

    [Fact]
    public void InitialState_IsIdle()
    {
        var vm = new MainViewModel();
        Assert.Equal(AppState.Idle, vm.State);
        Assert.True(vm.IsIdle);
        Assert.False(vm.IsSourceLoaded);
        Assert.False(vm.IsError);
    }

    [Fact]
    public void AcceptDrop_ValidDirectory_TransitionsToSourceLoaded()
    {
        var vm = new MainViewModel();
        var dir = CreateTempDir("src");

        vm.AcceptDropCommand.Execute(dir);

        Assert.Equal(AppState.SourceLoaded, vm.State);
        Assert.Equal(Path.GetFullPath(dir), vm.SourcePath);
        Assert.NotNull(vm.SourceDisplayName);
    }

    [Fact]
    public void AcceptDrop_NonExistentPath_TransitionsToError()
    {
        var vm = new MainViewModel();
        vm.AcceptDropCommand.Execute(Path.Combine(_tempDir, "does_not_exist"));
        Assert.Equal(AppState.Error, vm.State);
        Assert.NotNull(vm.ErrorMessage);
    }

    [Fact]
    public void Clear_FromSourceLoaded_ResetsToIdle()
    {
        var vm = new MainViewModel();
        var dir = CreateTempDir("src2");
        vm.AcceptDropCommand.Execute(dir);
        Assert.Equal(AppState.SourceLoaded, vm.State);

        vm.ClearCommand.Execute(null);

        Assert.Equal(AppState.Idle, vm.State);
        Assert.Null(vm.SourcePath);
        Assert.Null(vm.ErrorMessage);
    }

    [Fact]
    public void Clear_FromError_ResetsToIdle()
    {
        var vm = new MainViewModel();
        vm.AcceptDropCommand.Execute(Path.Combine(_tempDir, "ghost"));
        Assert.Equal(AppState.Error, vm.State);

        vm.ClearCommand.Execute(null);

        Assert.Equal(AppState.Idle, vm.State);
    }

    [Fact]
    public void CreateLink_ValidJunction_StaysInSourceLoaded()
    {
        var vm = new MainViewModel();
        var source = CreateTempDir("source");
        var dest = Path.Combine(_tempDir, "link");

        vm.AcceptDropCommand.Execute(source);
        vm.CreateLink(dest);

        Assert.Equal(AppState.SourceLoaded, vm.State);
        Assert.True(Directory.Exists(dest));
    }

    [Fact]
    public void CreateLink_ConflictAtDestination_TransitionsToError()
    {
        var vm = new MainViewModel();
        var source = CreateTempDir("src3");
        var dest = CreateTempDir("existing");

        vm.AcceptDropCommand.Execute(source);
        vm.CreateLink(dest);

        Assert.Equal(AppState.Error, vm.State);
    }

    [Fact]
    public void AcceptDrop_ReplacesExistingSource()
    {
        var vm = new MainViewModel();
        var dir1 = CreateTempDir("first");
        var dir2 = CreateTempDir("second");

        vm.AcceptDropCommand.Execute(dir1);
        vm.AcceptDropCommand.Execute(dir2);

        Assert.Equal(Path.GetFullPath(dir2), vm.SourcePath);
        Assert.Equal(AppState.SourceLoaded, vm.State);
    }

    private string CreateTempDir(string name)
    {
        var path = Path.Combine(_tempDir, name);
        Directory.CreateDirectory(path);
        return path;
    }
}
