using SlymLynk.Services;

namespace SlymLynk.Tests;

/// <summary>Configurable fake <see cref="IFileDialogService"/> for ViewModel tests.</summary>
internal sealed class FakeFileDialogService : IFileDialogService
{
    public string? SourceResult { get; init; }
    public string? DestinationResult { get; init; }
    public bool DestinationRequested { get; private set; }
    public bool LastIsDirectory { get; private set; }

    public string? PickSource() => SourceResult;

    public string? PickDestination(string? suggestedName, bool isDirectory)
    {
        DestinationRequested = true;
        LastIsDirectory = isDirectory;
        return DestinationResult;
    }
}

/// <summary>Configurable fake <see cref="IDropTargetResolver"/> for drag-out tests.</summary>
internal sealed class FakeDropTargetResolver : IDropTargetResolver
{
    public string? FolderResult { get; init; }
    public bool Resolved { get; private set; }

    public string? ResolveFolderUnderCursor()
    {
        Resolved = true;
        return FolderResult;
    }
}
