using IqcQms.ClientAgent.Application.Storage;
using IqcQms.ClientAgent.Infrastructure.Storage;
using Xunit;

namespace IqcQms.ClientAgent.Tests;

public class AllowedInputPathValidatorTests : IDisposable
{
    private readonly string _tempBaseDir;
    private readonly string _allowedRootDir;
    private readonly string _siblingRootDir;
    private readonly string _outsideDir;

    public AllowedInputPathValidatorTests()
    {
        _tempBaseDir = Path.Combine(Path.GetTempPath(), $"path_validator_test_{Guid.NewGuid():N}");
        _allowedRootDir = Path.Combine(_tempBaseDir, "Allowed");
        _siblingRootDir = Path.Combine(_tempBaseDir, "Allowed2");
        _outsideDir = Path.Combine(_tempBaseDir, "Outside");

        Directory.CreateDirectory(_allowedRootDir);
        Directory.CreateDirectory(_siblingRootDir);
        Directory.CreateDirectory(_outsideDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempBaseDir))
        {
            try { Directory.Delete(_tempBaseDir, true); } catch { }
        }
    }

    [Fact]
    public void ValidFile_BeneathAllowedRoot_IsAllowed()
    {
        var validFile = Path.Combine(_allowedRootDir, "input.csv");
        File.WriteAllText(validFile, "col1,col2\nval1,val2");

        var validator = new AllowedInputPathValidator();
        var result = validator.ValidatePath(validFile, new[] { _allowedRootDir });

        Assert.True(result.IsAllowed);
        Assert.Equal(PathValidationReason.Allowed, result.Reason);
    }

    [Fact]
    public void SiblingPrefixBypass_IsRejected()
    {
        var siblingFile = Path.Combine(_siblingRootDir, "file.xlsx");
        File.WriteAllText(siblingFile, "test");

        var validator = new AllowedInputPathValidator();
        // Candidate is C:\...\Allowed2\file.xlsx, Root is C:\...\Allowed
        var result = validator.ValidatePath(siblingFile, new[] { _allowedRootDir });

        Assert.False(result.IsAllowed);
        Assert.Equal(PathValidationReason.OutsideAllowedRoots, result.Reason);
    }

    [Fact]
    public void Directory_IsNotAcceptedAsRegularFile()
    {
        var validator = new AllowedInputPathValidator();
        var result = validator.ValidatePath(_allowedRootDir, new[] { _allowedRootDir });

        Assert.False(result.IsAllowed);
        Assert.Equal(PathValidationReason.NotRegularFile, result.Reason);
    }

    [Fact]
    public void RelativePath_IsRejected()
    {
        var validator = new AllowedInputPathValidator();
        var result = validator.ValidatePath("relative/file.csv", new[] { _allowedRootDir });

        Assert.False(result.IsAllowed);
        Assert.Equal(PathValidationReason.RelativePathNotAllowed, result.Reason);
    }

    [Fact]
    public void AlternateDataStream_IsRejected()
    {
        var validFile = Path.Combine(_allowedRootDir, "input.csv");
        File.WriteAllText(validFile, "data");

        var adsPath = $"{validFile}:hidden_stream";
        var validator = new AllowedInputPathValidator();
        var result = validator.ValidatePath(adsPath, new[] { _allowedRootDir });

        Assert.False(result.IsAllowed);
        Assert.Equal(PathValidationReason.AlternateDataStreamNotAllowed, result.Reason);
    }

    [Fact]
    public void DeviceNamespace_IsRejected()
    {
        var devicePath = @"\\.\C:\Windows\System32\drivers\etc\hosts";
        var validator = new AllowedInputPathValidator();
        var result = validator.ValidatePath(devicePath, new[] { _allowedRootDir });

        Assert.False(result.IsAllowed);
        Assert.Equal(PathValidationReason.DeviceNamespaceNotAllowed, result.Reason);
    }

    [Fact]
    public void ParentTraversal_IsRejected()
    {
        var traversalPath = Path.Combine(_allowedRootDir, "..", "Outside", "secret.txt");
        var secretFile = Path.Combine(_outsideDir, "secret.txt");
        File.WriteAllText(secretFile, "secret");

        var validator = new AllowedInputPathValidator();
        var result = validator.ValidatePath(traversalPath, new[] { _allowedRootDir });

        Assert.False(result.IsAllowed);
        Assert.Equal(PathValidationReason.OutsideAllowedRoots, result.Reason);
    }

    [Fact(Skip = "Live OS symlink creation requires Windows Developer Mode or Administrator privileges; covered deterministically by DeterministicPathReparseTests.")]
    public void WindowsJunction_Or_Symlink_EscapingRoot_IsRejected()
    {
        var targetFile = Path.Combine(_outsideDir, "outside_data.txt");
        File.WriteAllText(targetFile, "outside content");

        var linkDir = Path.Combine(_allowedRootDir, "JunctionToOutside");
        Directory.CreateSymbolicLink(linkDir, _outsideDir);
        var linkCandidateFile = Path.Combine(linkDir, "outside_data.txt");

        var validator = new AllowedInputPathValidator();
        var result = validator.ValidatePath(linkCandidateFile, new[] { _allowedRootDir });

        Assert.False(result.IsAllowed);
        Assert.Equal(PathValidationReason.OutsideAllowedRoots, result.Reason);
    }

    [Theory]
    [InlineData(@"C:\AllowedRoot\input.csv", false)]
    [InlineData(@"D:\Data\file.xlsx", false)]
    [InlineData(@"C:\AllowedRoot\input.csv:stream", true)]
    [InlineData(@"/var/data/input.csv", false)]
    [InlineData(@"/var/data/input.csv:hidden_stream", true)]
    public void AlternateDataStream_Classification_IsPlatformNeutral(string candidatePath, bool expectedIsAds)
    {
        var isAds = AllowedInputPathValidator.HasAlternateDataStreamForTest(candidatePath);
        Assert.Equal(expectedIsAds, isAds);
    }

    [Fact]
    public void LinuxAbsolutePath_DoesNotTriggerFalseAds()
    {
        var linuxFile = Path.Combine(_allowedRootDir, "linux_test.csv");
        File.WriteAllText(linuxFile, "content");

        var validator = new AllowedInputPathValidator();
        var result = validator.ValidatePath(linuxFile, new[] { _allowedRootDir });

        Assert.True(result.IsAllowed);
        Assert.Equal(PathValidationReason.Allowed, result.Reason);
    }
}
