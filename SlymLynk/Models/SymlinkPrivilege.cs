namespace SlymLynk.Models;

/// <summary>
/// The process's capability to create file symbolic links, detected upfront
/// via <see cref="SymlinkService.DetectSymlinkPrivilege"/>.
/// </summary>
public enum SymlinkPrivilege
{
    /// <summary>Windows Developer Mode is enabled; unprivileged symlink creation is allowed.</summary>
    DeveloperMode,

    /// <summary>The process is running elevated as administrator.</summary>
    Administrator,

    /// <summary>Neither Developer Mode nor elevation — file symlink creation will fail.</summary>
    Denied
}
