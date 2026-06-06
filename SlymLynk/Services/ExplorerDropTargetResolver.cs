using SlymLynk.Models;

namespace SlymLynk.Services;

/// <summary>
/// Production <see cref="IDropTargetResolver"/> adapter. Delegates to
/// <see cref="DragOutHelper"/>, which reads the cursor position and queries
/// Shell.Application for the Explorer folder under it. No shell execution —
/// COM automation and P/Invoke only.
/// </summary>
public class ExplorerDropTargetResolver : IDropTargetResolver
{
    /// <inheritdoc />
    public string? ResolveFolderUnderCursor() => DragOutHelper.GetDropDestinationFolder();
}
