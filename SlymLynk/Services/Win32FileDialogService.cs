using Microsoft.Win32;

namespace SlymLynk.Services;

/// <summary>
/// Production <see cref="IFileDialogService"/> adapter backed by the WPF
/// <see cref="OpenFileDialog"/> and <see cref="SaveFileDialog"/>.
/// </summary>
public class Win32FileDialogService : IFileDialogService
{
    /// <inheritdoc />
    public string? PickSource()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select source file or folder",
            CheckFileExists = false,
            CheckPathExists = true,
            FileName = "Select folder or file"
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    /// <inheritdoc />
    public string? PickDestination(string? suggestedName, bool isDirectory)
    {
        var dialog = new SaveFileDialog
        {
            Title = "Choose destination for link",
            FileName = suggestedName,
            Filter = isDirectory ? "All files (*)|*" : "All files (*.*)|*.*"
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }
}
