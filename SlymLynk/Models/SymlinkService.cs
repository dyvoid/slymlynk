using System.IO;
using System.Runtime.InteropServices;
using System.Security.Principal;
using Microsoft.Win32;

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

    // MAX_PATH. Longer paths are rejected until long-path support is confirmed (see SECURITY.md).
    private const int MaxPathLength = 260;

    /// <summary>Validates and normalises a source path. Throws on invalid input.</summary>
    /// <returns>A <see cref="ValidatedSource"/> carrying the resolved path and link type.</returns>
    public ValidatedSource ValidateSource(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        RejectControlCharacters(path);

        var full = Path.GetFullPath(path);
        ValidatePathSafety(full);

        var isDirectory = Directory.Exists(full);
        if (!File.Exists(full) && !isDirectory)
            throw new FileNotFoundException($"Source path does not exist: {full}");

        return new ValidatedSource(full, isDirectory);
    }

    /// <summary>
    /// Creates a junction (for directories) or symbolic link (for files) at
    /// <paramref name="destinationPath"/> pointing to the validated source.
    /// </summary>
    public void Create(ValidatedSource source, string destinationPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);
        RejectControlCharacters(destinationPath);

        var dest = Path.GetFullPath(destinationPath);
        ValidatePathSafety(dest);

        if (File.Exists(dest) || Directory.Exists(dest))
            throw new IOException($"Destination already exists: {dest}");

        // The parent must already exist: SlymLynk never creates intermediate
        // directories — the only write it performs is the link pointer itself.
        var parent = Path.GetDirectoryName(dest);
        if (string.IsNullOrEmpty(parent) || !Directory.Exists(parent))
            throw new DirectoryNotFoundException($"Destination folder does not exist: {parent ?? dest}");

        if (source.IsDirectory)
        {
            CreateJunction(source.Path, dest);
        }
        else
        {
            if (DetectSymlinkPrivilege() == SymlinkPrivilege.Denied)
                throw new UnauthorizedAccessException(
                    "File symbolic links require Windows Developer Mode or administrator privileges. " +
                    "Enable Developer Mode in Settings → System → For Developers, then try again.");

            CreateSymbolicLink(source.Path, dest);
        }
    }

    /// <summary>
    /// Detects upfront whether this process can create file symbolic links:
    /// Developer Mode, elevation, or neither. Junctions never need privilege.
    /// </summary>
    public virtual SymlinkPrivilege DetectSymlinkPrivilege()
    {
        if (!OperatingSystem.IsWindows())
            return SymlinkPrivilege.Denied;

        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\AppModelUnlock");
            if (key?.GetValue("AllowDevelopmentWithoutDevLicense") is int enabled && enabled == 1)
                return SymlinkPrivilege.DeveloperMode;
        }
        catch (Exception ex) when (ex is System.Security.SecurityException or IOException or UnauthorizedAccessException)
        {
            // Registry unreadable under this account — fall through to the elevation check.
        }

        using var identity = WindowsIdentity.GetCurrent();
        return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator)
            ? SymlinkPrivilege.Administrator
            : SymlinkPrivilege.Denied;
    }

    // --- private helpers ---

    private static void RejectControlCharacters(string path)
    {
        // Null bytes and other control characters have no place in a Windows path;
        // they are classic truncation/injection vectors. The path is deliberately
        // not echoed back here so the message can't carry the characters onward.
        foreach (var c in path)
        {
            if (char.IsControl(c))
                throw new ArgumentException("Path contains control characters, which are not allowed.");
        }
    }

    private static void ValidatePathSafety(string fullPath)
    {
        // Reject UNC paths (network paths are out of scope).
        if (fullPath.StartsWith(@"\\", StringComparison.Ordinal))
            throw new ArgumentException($"Network (UNC) paths are not supported: {fullPath}");

        // Reject paths that contain traversal sequences post-normalisation (belt-and-suspenders).
        if (fullPath.Contains("..", StringComparison.Ordinal))
            throw new ArgumentException($"Path contains traversal sequences: {fullPath}");

        // Reject a colon anywhere past the drive specifier — blocks NTFS alternate
        // data streams ("file.txt:stream") and malformed drive syntax.
        if (fullPath.IndexOf(':', startIndex: 2) >= 0)
            throw new ArgumentException($"Path contains a ':' outside the drive specifier: {fullPath}");

        // Reject paths that exceed MAX_PATH.
        if (fullPath.Length > MaxPathLength)
            throw new PathTooLongException(
                $"Path is {fullPath.Length} characters, which exceeds the Windows limit of {MaxPathLength}. " +
                "Choose a shorter destination.");

        ValidatePathComponents(fullPath);
    }

    // Characters Windows forbids in filenames. Separators and ':' are handled
    // separately; control characters are rejected before normalisation.
    private static readonly char[] InvalidComponentChars = { '"', '<', '>', '|', '*', '?' };

    private static void ValidatePathComponents(string fullPath)
    {
        foreach (var component in fullPath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
        {
            if (component.Length == 0 || IsDriveSpecifier(component))
                continue;

            // .NET Core's GetFullPath no longer rejects these, so check explicitly.
            if (component.IndexOfAny(InvalidComponentChars) >= 0)
                throw new ArgumentException($"Path component '{component}' contains characters that Windows does not allow in filenames.");

            // Windows reserves device names up to the first dot in *every* path
            // component: "CON", "con.txt", and "NUL.tar.gz" are all reserved.
            var baseName = component.Split('.')[0].TrimEnd(' ');
            if (ReservedNames.Contains(baseName))
                throw new ArgumentException(
                    $"'{component}' contains '{baseName}', a reserved Windows device name that cannot be used as a path component.");

            // Belt-and-suspenders: GetFullPath trims these on Windows, but a component
            // ending in a dot or space must never reach the filesystem.
            if (component.EndsWith(' ') || component.EndsWith('.'))
                throw new ArgumentException($"Path component '{component}' ends with a space or dot, which Windows does not allow.");
        }
    }

    private static bool IsDriveSpecifier(string component) =>
        component.Length == 2 && component[1] == ':' && char.IsAsciiLetter(component[0]);

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
                // ERROR_PRIVILEGE_NOT_HELD — backstop in case upfront detection was stale.
                1314 => new UnauthorizedAccessException(
                    "File symbolic links require Windows Developer Mode or administrator privileges. " +
                    "Enable Developer Mode in Settings → System → For Developers, then try again."),
                _ => new IOException($"Failed to create symbolic link (Win32 error {error}).")
            };
        }
    }
}
