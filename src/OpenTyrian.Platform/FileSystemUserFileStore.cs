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
}
