using System.IO;
using SlymLynk.Models;
using SlymLynk.ViewModels;

namespace SlymLynk.Tests;

public class DragOutTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), $"SlymLynkDragOutTests_{Guid.NewGuid():N}");

    public DragOutTests() => Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); } catch { /* best-effort */ }
    }

    [Fact]
    public void CompleteDragOut_ResolverReturnsFolder_CreatesLinkThere()
    {
        var source = CreateTempDir("source");
        var destFolder = CreateTempDir("dropfolder");
        var resolver = new FakeDropTargetResolver { FolderResult = destFolder };
        var vm = NewVm(resolver: resolver);

        vm.AcceptDropCommand.Execute(source);
        vm.CompleteDragOutCommand.Execute(null);

        var expected = Path.Combine(destFolder, Path.GetFileName(source));
        Assert.True(Directory.Exists(expected));
        Assert.Equal(AppState.SourceLoaded, vm.State);
    }

    [Fact]
    public void CompleteDragOut_ResolverReturnsNull_FallsBackToDialog()
    {
        var source = CreateTempDir("source2");
        var destFolder = Path.Combine(_tempDir, "viadialog");
        var resolver = new FakeDropTargetResolver { FolderResult = null };
        var dialogs = new FakeFileDialogService { DestinationResult = destFolder };
        var vm = NewVm(dialogs: dialogs, resolver: resolver);

        vm.AcceptDropCommand.Execute(source);
        vm.CompleteDragOutCommand.Execute(null);

        Assert.True(resolver.Resolved);
        Assert.True(dialogs.DestinationRequested);
        Assert.True(Directory.Exists(destFolder));
    }

    [Fact]
    public void CompleteDragOut_DestinationConflict_TransitionsToError()
    {
        var source = CreateTempDir("source3");
        var destFolder = CreateTempDir("dropfolder2");
        // Pre-create the link path so creation conflicts.
        Directory.CreateDirectory(Path.Combine(destFolder, Path.GetFileName(source)));
        var resolver = new FakeDropTargetResolver { FolderResult = destFolder };
        var vm = NewVm(resolver: resolver);

        vm.AcceptDropCommand.Execute(source);
        vm.CompleteDragOutCommand.Execute(null);

        Assert.Equal(AppState.Error, vm.State);
    }

    [Fact]
    public void CompleteDragOut_CannotExecuteWhenIdle()
    {
        var vm = NewVm();
        Assert.False(vm.CompleteDragOutCommand.CanExecute(null));
    }

    private MainViewModel NewVm(FakeFileDialogService? dialogs = null, FakeDropTargetResolver? resolver = null) =>
        new(new SymlinkService(), dialogs ?? new FakeFileDialogService(), resolver ?? new FakeDropTargetResolver());

    private string CreateTempDir(string name)
    {
        var path = Path.Combine(_tempDir, name);
        Directory.CreateDirectory(path);
        return path;
    }
}
