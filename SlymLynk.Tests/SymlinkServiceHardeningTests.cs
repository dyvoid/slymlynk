using System.IO;
using SlymLynk.Models;

namespace SlymLynk.Tests;

/// <summary>M2 security-hardening tests for <see cref="SymlinkService"/> path validation.</summary>
public class SymlinkServiceHardeningTests : IDisposable
{
    private readonly SymlinkService _svc = new();
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), $"SlymLynkTests_{Guid.NewGuid():N}");

    public SymlinkServiceHardeningTests() => Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); } catch { /* best-effort */ }
    }

    // --- reserved names in every path component ---

    [Theory]
    [InlineData("NUL.txt")]
    [InlineData("con.tar.gz")]
    [InlineData("Com1.log")]
    public void ValidateSource_ReservedNameWithExtension_Throws(string name)
    {
        var path = Path.Combine(_tempDir, name);
        Assert.Throws<ArgumentException>(() => _svc.ValidateSource(path));
    }

    [Fact]
    public void ValidateSource_ReservedIntermediateComponent_Throws()
    {
        // The reserved name is a directory in the middle of the path, not the leaf.
        var path = Path.Combine(_tempDir, "LPT3", "file.txt");
        Assert.Throws<ArgumentException>(() => _svc.ValidateSource(path));
    }

    [Fact]
    public void Create_ReservedDestinationComponent_Throws()
    {
        var source = _svc.ValidateSource(CreateTempDir("src_reserved"));
        var dest = Path.Combine(_tempDir, "AUX", "link");
        Assert.Throws<ArgumentException>(() => _svc.Create(source, dest));
    }

    // --- control characters and null bytes ---

    [Theory]
    [InlineData("bad\nname.txt")]
    [InlineData("bad\0name.txt")]
    [InlineData("bad\tname.txt")]
    [InlineData("bad\u001bname.txt")]
    public void ValidateSource_ControlCharacters_Throws(string name)
    {
        var path = _tempDir + Path.DirectorySeparatorChar + name;
        Assert.Throws<ArgumentException>(() => _svc.ValidateSource(path));
    }

    [Fact]
    public void Create_ControlCharacterDestination_Throws()
    {
        var source = _svc.ValidateSource(CreateTempDir("src_ctrl"));
        var dest = _tempDir + Path.DirectorySeparatorChar + "link\0name";
        Assert.Throws<ArgumentException>(() => _svc.Create(source, dest));
    }

    // --- NTFS alternate data streams ---

    [Fact]
    public void ValidateSource_AlternateDataStream_Throws()
    {
        var path = _tempDir + Path.DirectorySeparatorChar + "file.txt:hidden";
        Assert.Throws<ArgumentException>(() => _svc.ValidateSource(path));
    }

    // --- characters Windows forbids in filenames ---

    [Theory]
    [InlineData("bad|name.txt")]
    [InlineData("bad*name.txt")]
    [InlineData("bad?name")]
    [InlineData("bad<name>.txt")]
    [InlineData("bad\"name\".txt")]
    public void ValidateSource_InvalidFilenameCharacters_Throws(string name)
    {
        var path = _tempDir + Path.DirectorySeparatorChar + name;
        Assert.Throws<ArgumentException>(() => _svc.ValidateSource(path));
    }

    // --- path length ---

    [Fact]
    public void ValidateSource_PathBeyondMaxPath_ThrowsWithClearMessage()
    {
        var path = Path.Combine(_tempDir, new string('a', 300));
        var ex = Assert.Throws<PathTooLongException>(() => _svc.ValidateSource(path));
        Assert.Contains("260", ex.Message);
    }

    // --- destination parent directory ---

    [Fact]
    public void Create_ParentDirectoryMissing_Throws()
    {
        var source = _svc.ValidateSource(CreateTempDir("src_parent"));
        var dest = Path.Combine(_tempDir, "no_such_dir", "link");
        Assert.Throws<DirectoryNotFoundException>(() => _svc.Create(source, dest));
    }

    // --- upfront privilege detection ---

    [Fact]
    public void DetectSymlinkPrivilege_ReturnsDefinedValue()
    {
        Assert.True(Enum.IsDefined(_svc.DetectSymlinkPrivilege()));
    }

    [Fact]
    public void Create_FileSourceWithDeniedPrivilege_ThrowsBeforeAttemptingCreation()
    {
        var svc = new DeniedPrivilegeService();
        var source = svc.ValidateSource(CreateTempFile("denied.txt"));
        var dest = Path.Combine(_tempDir, "denied_link.txt");

        var ex = Assert.Throws<UnauthorizedAccessException>(() => svc.Create(source, dest));
        Assert.Contains("Developer Mode", ex.Message);
        Assert.False(File.Exists(dest));
    }

    [Fact]
    public void Create_DirectorySourceWithDeniedPrivilege_StillCreatesJunction()
    {
        // Junctions never require symlink privilege.
        var svc = new DeniedPrivilegeService();
        var source = svc.ValidateSource(CreateTempDir("junction_src"));
        var dest = Path.Combine(_tempDir, "junction_no_priv");

        svc.Create(source, dest);

        Assert.True(Directory.Exists(dest));
    }

    private class DeniedPrivilegeService : SymlinkService
    {
        public override SymlinkPrivilege DetectSymlinkPrivilege() => SymlinkPrivilege.Denied;
    }

    // --- fuzz: validation must fail closed on adversarial input ---

    public static TheoryData<string> AdversarialInputs => new()
    {
        @"..\..\..\Windows\System32\config\SAM",
        @"C:\Windows\..\..\..\secret",
        @"\\?\C:\Windows",
        @"\\.\PhysicalDrive0",
        @"\\server\share\x",
        "CON",
        @"C:\temp\NUL.txt",
        @"C:\temp\file.txt:$DATA",
        "C:\\temp\\x\0y",
        "trailing.dot.",
        "trailing space ",
        "‮exe.gnp",           // RTL override
        "﻿bom.txt",           // zero-width BOM
        "ファイル\u0000.txt",
        new string(' ', 10),
        new string('.', 64),
        "C:" + new string('a', 4096),
    };

    [Theory]
    [MemberData(nameof(AdversarialInputs))]
    public void ValidateSource_AdversarialInput_FailsClosed(string input) => AssertFailsClosed(input);

    [Fact]
    public void ValidateSource_RandomAdversarialInputs_FailClosed()
    {
        // Deterministic seed so failures are reproducible.
        var rng = new Random(20260712);
        const string alphabet = "abcXYZ019 ._-:\\/\"<>|?*\t\n\0\u001b‮﻿日本語éüñ";

        for (int i = 0; i < 300; i++)
        {
            var length = rng.Next(1, 300);
            var chars = new char[length];
            for (int j = 0; j < length; j++)
                chars[j] = alphabet[rng.Next(alphabet.Length)];

            AssertFailsClosed(new string(chars));
        }
    }

    private void AssertFailsClosed(string input)
    {
        try
        {
            // If validation accepts the input, it must have resolved to a real,
            // existing path — anything else is a validation bypass.
            var result = _svc.ValidateSource(input);
            Assert.True(File.Exists(result.Path) || Directory.Exists(result.Path),
                $"Validation accepted a non-existent path for input: {Printable(input)}");
        }
        catch (Exception ex) when (ex is ArgumentException or IOException or NotSupportedException
                                       or UnauthorizedAccessException or System.Security.SecurityException)
        {
            // Controlled rejection — expected. (PathTooLong/FileNotFound/DirectoryNotFound
            // derive from IOException.) Anything else escapes and fails the test.
        }
    }

    private static string Printable(string s) =>
        string.Concat(s.Select(c => char.IsControl(c) ? $"\\u{(int)c:x4}" : c.ToString()));

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
