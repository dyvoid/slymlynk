namespace SlymLynk.Services;

/// <summary>
/// Seam over the platform file/folder pickers. Production code uses
/// <see cref="Win32FileDialogService"/>; tests supply a fake. Keeping the
/// dialogs behind this interface lets <c>MainViewModel</c> commands be unit
/// tested without popping real Win32 windows.
/// </summary>
public interface IFileDialogService
{
    /// <summary>
    /// Prompts the user to pick a source file or folder.
    /// </summary>
    /// <returns>The selected path, or <c>null</c> if the user cancelled.</returns>
    string? PickSource();

    /// <summary>
    /// Prompts the user to choose a destination path for the link.
    /// </summary>
    /// <param name="suggestedName">Default file name to pre-fill, if any.</param>
    /// <param name="isDirectory">Whether the source is a directory (affects the filter).</param>
    /// <returns>The chosen destination path, or <c>null</c> if the user cancelled.</returns>
    string? PickDestination(string? suggestedName, bool isDirectory);
}
