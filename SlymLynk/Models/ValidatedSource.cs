namespace SlymLynk.Models;

/// <summary>
/// A source path that has passed <see cref="SymlinkService.ValidateSource"/>.
/// Holding an instance is a compile-time guarantee that validation ran, so
/// <see cref="SymlinkService.Create"/> cannot be called with an unvalidated path.
/// </summary>
/// <param name="Path">The resolved, validated absolute path.</param>
/// <param name="IsDirectory">True if the source is a directory; false if it is a file.</param>
public readonly record struct ValidatedSource(string Path, bool IsDirectory);
