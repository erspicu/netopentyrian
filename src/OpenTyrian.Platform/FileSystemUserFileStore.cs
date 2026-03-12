namespace OpenTyrian.Platform;

public sealed class FileSystemUserFileStore : IUserFileStore
{
    public FileSystemUserFileStore(string rootDirectory)
    {
        RootDirectory = Path.GetFullPath(rootDirectory);
    }

    public string RootDirectory { get; }

    public bool FileExists(string relativePath)
    {
        return File.Exists(GetFullPath(relativePath));
    }

    public string GetFullPath(string relativePath)
    {
        return Path.Combine(RootDirectory, relativePath);
    }

    public Stream OpenRead(string relativePath)
    {
        return File.OpenRead(GetFullPath(relativePath));
    }

    public Stream OpenWrite(string relativePath)
    {
        string path = GetFullPath(relativePath);
        string? directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        return File.Open(path, FileMode.Create, FileAccess.Write, FileShare.None);
    }
}
