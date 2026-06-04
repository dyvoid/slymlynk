using System.IO;
using System.Runtime.InteropServices;

namespace SlymLynk.Models;

/// <summary>All filesystem operations for symlink and junction creation.</summary>
public class SymlinkService
{
    // Reserved Windows device names that cannot be used as filenames.
    private static readonly HashSet<string> ReservedNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "CON", "PRN", "AUX", "NUL",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
    };

    /// <summary>Validates and normalises a source path. Throws on invalid input.</summary>
    /// <returns>The resolved, validated absolute path.</returns>
    public string ValidateSource(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var full = Path.GetFullPath(path);
        ValidatePathSafety(full);

        if (!File.Exists(full) && !Directory.Exists(full))
            throw new FileNotFoundException($"Source path does not exist: {full}");

        return full;
    }

    /// <summary>Returns true if the source path is a directory; false if it is a file.</summary>
    public bool IsDirectory(string validatedSourcePath) =>
        Directory.Exists(validatedSourcePath);

    /// <summary>
    /// Creates a junction (for directories) or symbolic link (for files) at
    /// <paramref name="destinationPath"/> pointing to <paramref name="sourcePath"/>.
    /// Both paths must already be validated via <see cref="ValidateSource"/>.
    /// </summary>
    public void Create(string sourcePath, string destinationPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);

        var dest = Path.GetFullPath(destinationPath);
        ValidatePathSafety(dest);

        if (File.Exists(dest) || Directory.Exists(dest))
            throw new IOException($"Destination already exists: {dest}");

        if (IsDirectory(sourcePath))
            CreateJunction(sourcePath, dest);
        else
            CreateSymbolicLink(sourcePath, dest);
    }

    // --- private helpers ---

    private static void ValidatePathSafety(string fullPath)
    {
        // Reject UNC paths (network paths are out of scope).
        if (fullPath.StartsWith(@"\\", StringComparison.Ordinal))
            throw new ArgumentException($"Network (UNC) paths are not supported: {fullPath}");

        // Reject paths that contain traversal sequences post-normalisation (belt-and-suspenders).
        if (fullPath.Contains("..", StringComparison.Ordinal))
            throw new ArgumentException($"Path contains traversal sequences: {fullPath}");

        // Reject reserved Windows filenames.
        var name = Path.GetFileNameWithoutExtension(fullPath);
        if (ReservedNames.Contains(name))
            throw new ArgumentException($"'{name}' is a reserved Windows device name and cannot be used as a path component.");

        // Reject paths that exceed MAX_PATH.
        if (fullPath.Length > 260)
            throw new PathTooLongException($"Path exceeds maximum length (260 characters): {fullPath}");
    }

    private static void CreateJunction(string sourcePath, string destinationPath)
    {
        // Create the directory stub that will become the junction point.
        Directory.CreateDirectory(destinationPath);
        JunctionPoint.Create(destinationPath, sourcePath, overwrite: false);
    }

    private static void CreateSymbolicLink(string sourcePath, string destinationPath)
    {
        // SYMBOLIC_LINK_FLAG_FILE = 0x0
        if (!NativeMethods.CreateSymbolicLink(destinationPath, sourcePath, 0))
        {
            var error = Marshal.GetLastWin32Error();
            throw error switch
            {
                // ERROR_PRIVILEGE_NOT_HELD
                1314 => new UnauthorizedAccessException(
                    "File symbolic links require Windows Developer Mode or administrator privileges. " +
                    "Enable Developer Mode in Settings → System → For Developers, then try again."),
                _ => new IOException($"Failed to create symbolic link (Win32 error {error}).")
            };
        }
    }
}
