using IqcQms.ClientAgent.Application.Storage;
using IqcQms.ClientAgent.Infrastructure.Storage;
using Xunit;

namespace IqcQms.ClientAgent.Tests;

public class TestFileSystemResolver : IFileSystemResolver
{
    public Dictionary<string, bool> ExistingFiles { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, bool> ExistingDirectories { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, bool> ReparsePoints { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, string> LinkTargets { get; } = new(StringComparer.OrdinalIgnoreCase);

    public bool FileExists(string path) => ExistingFiles.TryGetValue(Normalize(path), out var exists) && exists;
    public bool DirectoryExists(string path) => ExistingDirectories.TryGetValue(Normalize(path), out var exists) && exists;
    public bool IsReparsePoint(string path) => ReparsePoints.TryGetValue(Normalize(path), out var isReparse) && isReparse;
    public string? ResolveLinkTarget(string path) => LinkTargets.TryGetValue(Normalize(path), out var target) ? target : null;

    private static string Normalize(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return string.Empty;
        var p = path.Trim();
        if (p.Length >= 3 && char.IsAsciiLetter(p[0]) && p[1] == ':' && (p[2] == '\\' || p[2] == '/'))
        {
            return p.Replace('/', '\\').TrimEnd('\\');
        }
        return Path.GetFullPath(p).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }
}

public class DeterministicPathReparseTests
{
    private readonly string _allowedRoot = @"C:\IqcQms\AllowedRoot";
    private readonly string _outsideRoot = @"C:\SecretData";

    [Fact]
    public void FileSymbolicLink_EscapeOutsideRoot_IsRejected()
    {
        var mockFs = new TestFileSystemResolver();

        var symlinkPath = @"C:\IqcQms\AllowedRoot\shortcut.xlsx";
        var targetOutsidePath = @"C:\SecretData\passwords.xlsx";

        mockFs.ExistingDirectories[_allowedRoot] = true;
        mockFs.ExistingDirectories[_outsideRoot] = true;
        mockFs.ExistingFiles[symlinkPath] = true;
        mockFs.ExistingFiles[targetOutsidePath] = true;
        mockFs.ReparsePoints[symlinkPath] = true;
        mockFs.LinkTargets[symlinkPath] = targetOutsidePath;

        var validator = new AllowedInputPathValidator(mockFs);
        var result = validator.ValidatePath(symlinkPath, new[] { _allowedRoot });

        Assert.False(result.IsAllowed);
        Assert.Equal(PathValidationReason.OutsideAllowedRoots, result.Reason);
    }

    [Fact]
    public void DirectorySymbolicLink_EscapeOutsideRoot_IsRejected()
    {
        var mockFs = new TestFileSystemResolver();

        var linkSubDir = @"C:\IqcQms\AllowedRoot\SymlinkFolder";
        var candidateFile = @"C:\IqcQms\AllowedRoot\SymlinkFolder\data.csv";
        var targetOutsideDir = @"C:\SecretData";
        var targetOutsideFile = @"C:\SecretData\data.csv";

        mockFs.ExistingDirectories[_allowedRoot] = true;
        mockFs.ExistingDirectories[_outsideRoot] = true;
        mockFs.ExistingDirectories[linkSubDir] = true;
        mockFs.ReparsePoints[linkSubDir] = true;
        mockFs.LinkTargets[linkSubDir] = targetOutsideDir;

        mockFs.ExistingFiles[candidateFile] = true;
        mockFs.ExistingFiles[targetOutsideFile] = true;

        var validator = new AllowedInputPathValidator(mockFs);
        var result = validator.ValidatePath(candidateFile, new[] { _allowedRoot });

        Assert.False(result.IsAllowed);
        Assert.Equal(PathValidationReason.OutsideAllowedRoots, result.Reason);
    }

    [Fact]
    public void Junction_EscapeOutsideRoot_IsRejected()
    {
        var mockFs = new TestFileSystemResolver();

        var junctionPath = @"C:\IqcQms\AllowedRoot\JunctionToSecret";
        var candidateFile = @"C:\IqcQms\AllowedRoot\JunctionToSecret\confidential.xlsx";
        var targetOutsideDir = @"C:\SecretData";
        var targetOutsideFile = @"C:\SecretData\confidential.xlsx";

        mockFs.ExistingDirectories[_allowedRoot] = true;
        mockFs.ExistingDirectories[_outsideRoot] = true;
        mockFs.ExistingDirectories[targetOutsideDir] = true;
        mockFs.ExistingDirectories[junctionPath] = true;
        mockFs.ReparsePoints[junctionPath] = true;
        mockFs.LinkTargets[junctionPath] = targetOutsideDir;

        mockFs.ExistingFiles[candidateFile] = true;
        mockFs.ExistingFiles[targetOutsideFile] = true;

        var validator = new AllowedInputPathValidator(mockFs);
        var result = validator.ValidatePath(candidateFile, new[] { _allowedRoot });

        Assert.False(result.IsAllowed);
        Assert.Equal(PathValidationReason.OutsideAllowedRoots, result.Reason);
    }

    [Fact]
    public void BrokenLink_IsRejected()
    {
        var mockFs = new TestFileSystemResolver();

        var brokenLink = @"C:\IqcQms\AllowedRoot\broken.xlsx";
        var missingTarget = @"C:\IqcQms\AllowedRoot\deleted.xlsx";

        mockFs.ExistingDirectories[_allowedRoot] = true;
        mockFs.ExistingFiles[brokenLink] = true;
        mockFs.ReparsePoints[brokenLink] = true;
        mockFs.LinkTargets[brokenLink] = missingTarget;
        // missingTarget is NOT in ExistingFiles or ExistingDirectories!

        var validator = new AllowedInputPathValidator(mockFs);
        var result = validator.ValidatePath(brokenLink, new[] { _allowedRoot });

        Assert.False(result.IsAllowed);
        Assert.Equal(PathValidationReason.BrokenLinkOrJunction, result.Reason);
    }

    [Fact]
    public void LinkChangedAfterEnqueue_ProcessingTimeValidationRejects()
    {
        var mockFs = new TestFileSystemResolver();

        var linkFile = @"C:\IqcQms\AllowedRoot\input.xlsx";
        var validTarget = @"C:\IqcQms\AllowedRoot\SubFolder\valid.xlsx";
        var escapedTarget = @"C:\SecretData\escaped.xlsx";

        mockFs.ExistingDirectories[_allowedRoot] = true;
        mockFs.ExistingDirectories[@"C:\IqcQms\AllowedRoot\SubFolder"] = true;
        mockFs.ExistingFiles[linkFile] = true;
        mockFs.ExistingFiles[validTarget] = true;
        mockFs.ExistingFiles[escapedTarget] = true;

        mockFs.ReparsePoints[linkFile] = true;
        mockFs.LinkTargets[linkFile] = validTarget;

        var validator = new AllowedInputPathValidator(mockFs);

        // 1. Enqueue-time validation: points to valid target
        var resultBefore = validator.ValidatePath(linkFile, new[] { _allowedRoot });
        Assert.True(resultBefore.IsAllowed);

        // 2. Link modified before processing time -> points to outside target
        mockFs.LinkTargets[linkFile] = escapedTarget;

        // 3. Processing-time re-validation: detects escape and rejects!
        var resultAfter = validator.ValidatePath(linkFile, new[] { _allowedRoot });
        Assert.False(resultAfter.IsAllowed);
        Assert.Equal(PathValidationReason.OutsideAllowedRoots, resultAfter.Reason);
    }

    [Fact]
    public void ResolvedComponentChain_TraversalIsVerified()
    {
        var mockFs = new TestFileSystemResolver();

        var link1 = @"C:\IqcQms\AllowedRoot\link1";

        mockFs.ExistingDirectories[_allowedRoot] = true;
        mockFs.ExistingDirectories[@"C:\IqcQms\AllowedRoot\SubFolder"] = true;
        mockFs.ExistingDirectories[link1] = true;

        mockFs.ReparsePoints[link1] = true;
        mockFs.LinkTargets[link1] = @"C:\IqcQms\AllowedRoot\SubFolder";

        mockFs.ExistingFiles[@"C:\IqcQms\AllowedRoot\link1\final.xlsx"] = true;
        mockFs.ExistingFiles[@"C:\IqcQms\AllowedRoot\SubFolder\final.xlsx"] = true;

        var validator = new AllowedInputPathValidator(mockFs);
        var result = validator.ValidatePath(@"C:\IqcQms\AllowedRoot\link1\final.xlsx", new[] { _allowedRoot });

        Assert.True(result.IsAllowed);
        Assert.True(result.EncounteredReparsePoint);
    }

    [Fact]
    public void ReparseDepthLimit_ExceedingMaxIsRejected()
    {
        var mockFs = new TestFileSystemResolver();

        mockFs.ExistingDirectories[_allowedRoot] = true;
        mockFs.ExistingFiles[@"C:\IqcQms\AllowedRoot\loop.xlsx"] = true;

        // Create recursive reparse loop
        mockFs.ReparsePoints[@"C:\IqcQms\AllowedRoot\loop.xlsx"] = true;
        mockFs.LinkTargets[@"C:\IqcQms\AllowedRoot\loop.xlsx"] = @"C:\IqcQms\AllowedRoot\loop.xlsx";

        var validator = new AllowedInputPathValidator(mockFs);
        var result = validator.ValidatePath(@"C:\IqcQms\AllowedRoot\loop.xlsx", new[] { _allowedRoot });

        Assert.False(result.IsAllowed);
        Assert.Equal(PathValidationReason.ReparseDepthExceeded, result.Reason);
    }
}
