using System.IO;
using SlymLynk.Models;

namespace SlymLynk.Tests;

public class SymlinkServiceTests : IDisposable
{
    private readonly SymlinkService _svc = new();
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), $"SlymLynkTests_{Guid.NewGuid():N}");

    public SymlinkServiceTests() => Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); } catch { /* best-effort */ }
    }

    // --- ValidateSource ---

    [Fact]
    public void ValidateSource_ExistingFile_ReturnsFullPathAsFile()
    {
        var file = CreateTempFile("test.txt");
        var result = _svc.ValidateSource(file);
        Assert.Equal(Path.GetFullPath(file), result.Path);
        Assert.False(result.IsDirectory);
    }

    [Fact]
    public void ValidateSource_ExistingDirectory_ReturnsFullPathAsDirectory()
    {
        var dir = CreateTempDir("mydir");
        var result = _svc.ValidateSource(dir);
        Assert.Equal(Path.GetFullPath(dir), result.Path);
        Assert.True(result.IsDirectory);
    }

    [Fact]
    public void ValidateSource_NonExistent_Throws()
    {
        var missing = Path.Combine(_tempDir, "ghost.txt");
        Assert.Throws<FileNotFoundException>(() => _svc.ValidateSource(missing));
    }

    [Fact]
    public void ValidateSource_NullOrWhitespace_Throws()
    {
        Assert.Throws<ArgumentException>(() => _svc.ValidateSource(""));
        Assert.Throws<ArgumentException>(() => _svc.ValidateSource("   "));
    }

    [Fact]
    public void ValidateSource_UncPath_Throws()
    {
        Assert.Throws<ArgumentException>(() => _svc.ValidateSource(@"\\server\share\file.txt"));
    }

    [Fact]
    public void ValidateSource_ReservedName_Throws()
    {
        // Build a path that resolves to a reserved filename component.
        // We can't create a file called CON, so we test the validation directly.
        var reservedPath = Path.Combine(_tempDir, "CON");
        Assert.Throws<ArgumentException>(() => _svc.ValidateSource(reservedPath));
    }

    // --- Create (junction) ---

    [Fact]
    public void Create_Junction_CreatesJunctionAtDestination()
    {
        var source = _svc.ValidateSource(CreateTempDir("source"));
        var dest = Path.Combine(_tempDir, "junction_link");

        _svc.Create(source, dest);

        Assert.True(Directory.Exists(dest));
    }

    [Fact]
    public void Create_DestinationAlreadyExists_Throws()
    {
        var source = _svc.ValidateSource(CreateTempDir("src2"));
        var dest = CreateTempDir("existing_dest");

        Assert.Throws<IOException>(() => _svc.Create(source, dest));
    }

    [Fact]
    public void Create_DestinationIsUncPath_Throws()
    {
        var source = _svc.ValidateSource(CreateTempDir("src3"));
        Assert.Throws<ArgumentException>(() => _svc.Create(source, @"\\server\share\link"));
    }

    // --- helpers ---

    private string CreateTempFile(string name)
    {
        var path = Path.Combine(_tempDir, name);
        File.WriteAllText(path, "test");
        return path;
    }

    private string CreateTempDir(string name)
    {
        var path = Path.Combine(_tempDir, name);
        Directory.CreateDirectory(path);
        return path;
    }
}
