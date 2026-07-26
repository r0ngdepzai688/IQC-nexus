using System.Text.RegularExpressions;
using IqcQms.ClientAgent.Application.Storage;

namespace IqcQms.ClientAgent.Infrastructure.Storage;

public class AllowedInputPathValidator : IAllowedInputPathValidator
{
    private const int MaxLinkDepth = 10;
    private static readonly Regex DeviceNamespaceRegex = new(@"^\\\\(?:\.|\?)\\", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly IFileSystemResolver _fsResolver;

    public AllowedInputPathValidator(IFileSystemResolver fsResolver)
    {
        _fsResolver = fsResolver;
    }

    public AllowedInputPathValidator() : this(new DefaultFileSystemResolver())
    {
    }

    public AllowedInputPathValidationResult ValidatePath(string candidatePath, IEnumerable<string> allowedRoots)
    {
        if (string.IsNullOrWhiteSpace(candidatePath))
        {
            return AllowedInputPathValidationResult.Denied(PathValidationReason.EmptyOrNullPath);
        }

        var trimmedPath = candidatePath.Trim();

        // 1. Device Namespace Rejection (\\.\, \\?\GLOBALROOT, etc.)
        if (DeviceNamespaceRegex.IsMatch(trimmedPath))
        {
            return AllowedInputPathValidationResult.Denied(PathValidationReason.DeviceNamespaceNotAllowed);
        }

        // 2. Alternate Data Stream Rejection (e.g. C:\file.xlsx:stream)
        if (HasAlternateDataStream(trimmedPath))
        {
            return AllowedInputPathValidationResult.Denied(PathValidationReason.AlternateDataStreamNotAllowed);
        }

        // 3. Absolute Path Check
        if (!IsFullyQualifiedPath(trimmedPath))
        {
            return AllowedInputPathValidationResult.Denied(PathValidationReason.RelativePathNotAllowed);
        }

        string fullCandidatePath;
        try
        {
            fullCandidatePath = CanonicalizePath(trimmedPath);
        }
        catch
        {
            return AllowedInputPathValidationResult.Denied(PathValidationReason.InvalidPathSyntax);
        }

        // 4. Configured Roots Normalization & Physical Target Resolution
        var resolvedRoots = NormalizeAndResolveRoots(allowedRoots);
        if (resolvedRoots.Count == 0)
        {
            return AllowedInputPathValidationResult.Denied(PathValidationReason.ConfiguredRootInvalid);
        }

        // 5. Component-by-Component Reparse Point Resolution & Boundary Verification
        var (resolvedCandidatePhysical, reparseEncountered, errorReason) = ResolvePhysicalPath(fullCandidatePath);
        if (errorReason != PathValidationReason.Allowed)
        {
            return AllowedInputPathValidationResult.Denied(errorReason);
        }

        // 6. Separator-Aware Boundary Comparison against Normalized Allowed Roots
        var isInside = false;
        foreach (var root in resolvedRoots)
        {
            if (IsPathInsideRoot(resolvedCandidatePhysical, root))
            {
                isInside = true;
                break;
            }
        }

        if (!isInside)
        {
            return AllowedInputPathValidationResult.Denied(PathValidationReason.OutsideAllowedRoots);
        }

        // 7. File Existence and Regular File Verification
        if (!_fsResolver.FileExists(fullCandidatePath) && !_fsResolver.FileExists(resolvedCandidatePhysical))
        {
            if (_fsResolver.DirectoryExists(fullCandidatePath) || _fsResolver.DirectoryExists(resolvedCandidatePhysical))
            {
                return AllowedInputPathValidationResult.Denied(PathValidationReason.NotRegularFile);
            }
            return AllowedInputPathValidationResult.Denied(PathValidationReason.PathNotFound);
        }

        if (_fsResolver.DirectoryExists(resolvedCandidatePhysical))
        {
            return AllowedInputPathValidationResult.Denied(PathValidationReason.NotRegularFile);
        }

        return AllowedInputPathValidationResult.Success(resolvedCandidatePhysical, reparseEncountered);
    }

    private List<string> NormalizeAndResolveRoots(IEnumerable<string> rawRoots)
    {
        var result = new List<string>();
        foreach (var raw in rawRoots)
        {
            if (string.IsNullOrWhiteSpace(raw)) continue;
            try
            {
                var full = CanonicalizePath(raw.Trim());
                var (physicalRoot, _, err) = ResolvePhysicalPath(full);
                if (err == PathValidationReason.Allowed)
                {
                    var normalized = NormalizeTrailingSeparator(physicalRoot);
                    if (!result.Contains(normalized, StringComparer.OrdinalIgnoreCase))
                    {
                        result.Add(normalized);
                    }
                }
            }
            catch { }
        }
        return result;
    }

    private (string PhysicalPath, bool ReparseEncountered, PathValidationReason Error) ResolvePhysicalPath(string path)
    {
        var root = GetPathRoot(path);
        if (string.IsNullOrEmpty(root))
        {
            return (path, false, PathValidationReason.RelativePathNotAllowed);
        }

        var segments = path[root.Length..].Split(new[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);

        var currentPath = root;
        var reparseEncountered = false;
        var depth = 0;

        foreach (var segment in segments)
        {
            var separator = currentPath.Contains('/') ? "/" : "\\";
            if (!currentPath.EndsWith('/') && !currentPath.EndsWith('\\'))
            {
                currentPath += separator;
            }
            currentPath += segment;

            try
            {
                while (_fsResolver.IsReparsePoint(currentPath))
                {
                    reparseEncountered = true;
                    depth++;
                    if (depth > MaxLinkDepth)
                    {
                        return (currentPath, true, PathValidationReason.ReparseDepthExceeded);
                    }

                    var targetPath = _fsResolver.ResolveLinkTarget(currentPath);
                    if (string.IsNullOrEmpty(targetPath) || (!_fsResolver.DirectoryExists(targetPath) && !_fsResolver.FileExists(targetPath)))
                    {
                        return (currentPath, true, PathValidationReason.BrokenLinkOrJunction);
                    }

                    currentPath = targetPath;
                }
            }
            catch
            {
                // Unresolvable path segment
            }
        }

        return (CanonicalizePath(currentPath), reparseEncountered, PathValidationReason.Allowed);
    }

    public static bool IsPathInsideRoot(string candidatePath, string rootPath)
    {
        var normalizedCandidate = NormalizeTrailingSeparator(candidatePath).Replace('/', '\\');
        var normalizedRoot = NormalizeTrailingSeparator(rootPath).Replace('/', '\\');

        if (normalizedCandidate.Equals(normalizedRoot, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var prefixWithSeparator = normalizedRoot + "\\";
        return normalizedCandidate.StartsWith(prefixWithSeparator, StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeTrailingSeparator(string path)
    {
        return path.TrimEnd('\\', '/');
    }

    public static bool HasAlternateDataStreamForTest(string path) => HasAlternateDataStream(path);

    private static bool HasAlternateDataStream(string path)
    {
        var root = GetPathRoot(path);
        if (!string.IsNullOrEmpty(root) && root.Length <= path.Length)
        {
            var rest = path[root.Length..];
            if (rest.Contains(':')) return true;
        }

        var p = path;
        if (p.Length >= 2 && char.IsAsciiLetter(p[0]) && p[1] == ':')
        {
            p = p[2..];
        }
        return p.Contains(':');
    }

    private static bool IsFullyQualifiedPath(string path)
    {
        if (Path.IsPathFullyQualified(path)) return true;

        return path.Length >= 3 && char.IsAsciiLetter(path[0]) && path[1] == ':' &&
               (path[2] == '\\' || path[2] == '/');
    }

    private static string GetPathRoot(string path)
    {
        if (string.IsNullOrEmpty(path)) return string.Empty;

        var root = Path.GetPathRoot(path);
        if (!string.IsNullOrEmpty(root)) return root;

        if (path.Length >= 3 && char.IsAsciiLetter(path[0]) && path[1] == ':' && (path[2] == '\\' || path[2] == '/'))
        {
            return path[..3];
        }

        return string.Empty;
    }

    private static string CanonicalizePath(string path)
    {
        if (Path.IsPathFullyQualified(path) && OperatingSystem.IsWindows())
        {
            return Path.GetFullPath(path);
        }

        if (Path.IsPathFullyQualified(path) && !char.IsAsciiLetter(path[0]))
        {
            return Path.GetFullPath(path);
        }

        var root = GetPathRoot(path);
        if (string.IsNullOrEmpty(root))
        {
            return Path.GetFullPath(path);
        }

        var rest = path[root.Length..];
        var segments = rest.Split(new[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);
        var stack = new List<string>();

        foreach (var seg in segments)
        {
            if (seg == ".") continue;
            if (seg == "..")
            {
                if (stack.Count > 0) stack.RemoveAt(stack.Count - 1);
            }
            else
            {
                stack.Add(seg);
            }
        }

        var separator = root.Contains('/') ? "/" : "\\";
        return root.TrimEnd('\\', '/') + separator + string.Join(separator, stack);
    }
}
