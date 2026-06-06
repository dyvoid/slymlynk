namespace SlymLynk.Services;

/// <summary>
/// Seam over the platform mechanism that determines which folder a drag-out
/// gesture was released onto. Production code uses
/// <see cref="ExplorerDropTargetResolver"/> (Explorer/COM); tests supply a fake.
/// Keeping resolution behind this interface lets the drag-out orchestration in
/// <c>MainViewModel</c> be unit tested without a live desktop.
/// </summary>
public interface IDropTargetResolver
{
    /// <summary>
    /// Resolves the absolute path of the folder currently under the cursor.
    /// </summary>
    /// <returns>
    /// The destination folder path, or <c>null</c> if the cursor is not over a
    /// recognised Explorer window or the desktop.
    /// </returns>
    string? ResolveFolderUnderCursor();
}
