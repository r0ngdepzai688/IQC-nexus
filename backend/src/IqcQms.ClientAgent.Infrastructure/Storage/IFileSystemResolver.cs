namespace IqcQms.ClientAgent.Infrastructure.Storage;

public interface IFileSystemResolver
{
    bool FileExists(string path);
    bool DirectoryExists(string path);
    bool IsReparsePoint(string path);
    string? ResolveLinkTarget(string path);
}

public class DefaultFileSystemResolver : IFileSystemResolver
{
    public bool FileExists(string path) => File.Exists(path);
    public bool DirectoryExists(string path) => Directory.Exists(path);

    public bool IsReparsePoint(string path)
    {
        try
        {
            if (FileExists(path) || DirectoryExists(path))
            {
                var attr = File.GetAttributes(path);
                return attr.HasFlag(FileAttributes.ReparsePoint);
            }
        }
        catch { }
        return false;
    }

    public string? ResolveLinkTarget(string path)
    {
        try
        {
            FileSystemInfo fsi = DirectoryExists(path)
                ? new DirectoryInfo(path)
                : new FileInfo(path);

            var target = fsi.ResolveLinkTarget(returnFinalTarget: true);
            return target?.FullName;
        }
        catch
        {
            return null;
        }
    }
}
