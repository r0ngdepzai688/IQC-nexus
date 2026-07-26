namespace IqcQms.ClientAgent.Application.Storage;

public enum PathValidationReason
{
    Allowed,
    EmptyOrNullPath,
    RelativePathNotAllowed,
    InvalidPathSyntax,
    AlternateDataStreamNotAllowed,
    DeviceNamespaceNotAllowed,
    OutsideAllowedRoots,
    PathNotFound,
    NotRegularFile,
    ContainsReparsePointOutsideRoot,
    BrokenLinkOrJunction,
    ReparseDepthExceeded,
    ConfiguredRootInvalid
}

public class AllowedInputPathValidationResult
{
    public bool IsAllowed { get; set; }
    public string PhysicalResolvedPath { get; set; } = string.Empty;
    public PathValidationReason Reason { get; set; } = PathValidationReason.Allowed;
    public bool EncounteredReparsePoint { get; set; }

    public static AllowedInputPathValidationResult Success(string resolvedPath, bool reparseEncountered = false) =>
        new() { IsAllowed = true, PhysicalResolvedPath = resolvedPath, Reason = PathValidationReason.Allowed, EncounteredReparsePoint = reparseEncountered };

    public static AllowedInputPathValidationResult Denied(PathValidationReason reason) =>
        new() { IsAllowed = false, Reason = reason };
}

public interface IAllowedInputPathValidator
{
    AllowedInputPathValidationResult ValidatePath(string candidatePath, IEnumerable<string> allowedRoots);
}
