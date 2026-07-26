using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace IqcQms.ClientAgent.Infrastructure.Nasca;

public interface INascaPathSecurityGuard
{
    bool IsReparsePoint(string path);
    bool ContainsReparsePointInAncestors(string rootDirectory, string targetPath);
    void EnsureSafePath(string rootDirectory, string targetPath);
    bool IsValidOpaqueDirectoryName(string name);
}

public class NascaPathSecurityGuard : INascaPathSecurityGuard
{
    private static readonly Regex OpaqueDirectoryRegex = new(@"^work_[a-f0-9]{32}$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public bool IsValidOpaqueDirectoryName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;
        return OpaqueDirectoryRegex.IsMatch(name);
    }

    public bool IsReparsePoint(string path)
    {
        try
        {
            if (!File.Exists(path) && !Directory.Exists(path)) return false;
            var attr = File.GetAttributes(path);
            return attr.HasFlag(FileAttributes.ReparsePoint);
        }
        catch
        {
            // Fail closed on permission denied or file error
            return true;
        }
    }

    public bool ContainsReparsePointInAncestors(string rootDirectory, string targetPath)
    {
        try
        {
            var fullRoot = Path.GetFullPath(rootDirectory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var fullTarget = Path.GetFullPath(targetPath);

            var current = fullTarget;
            while (!string.IsNullOrEmpty(current) && current.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase))
            {
                if (File.Exists(current) || Directory.Exists(current))
                {
                    if (IsReparsePoint(current))
                    {
                        return true;
                    }
                }

                if (current.Equals(fullRoot, StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }

                var parent = Path.GetDirectoryName(current);
                if (parent == current) break;
                current = parent;
            }

            return false;
        }
        catch
        {
            // Fail closed on error
            return true;
        }
    }

    public void EnsureSafePath(string rootDirectory, string targetPath)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(targetPath) || targetPath.Contains("..") || targetPath.Contains('/') && Path.DirectorySeparatorChar == '\\')
            {
                throw new InvalidOperationException("Path security validation failed: Invalid path traversal format.");
            }

            var fullRoot = Path.GetFullPath(rootDirectory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            var fullTarget = Path.GetFullPath(targetPath);

            if (!fullTarget.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Path security validation failed: Target escapes root boundary.");
            }

            if (ContainsReparsePointInAncestors(rootDirectory, targetPath))
            {
                throw new InvalidOperationException("Path security validation failed: Reparse point or junction detected in path ancestors.");
            }
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Path security validation failed due to unexpected filesystem error.", ex);
        }
    }
}
