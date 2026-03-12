namespace OpenTyrian.Platform;

public sealed class FileSystemAssetLocator : IAssetLocator
{
    public FileSystemAssetLocator(string dataDirectory)
    {
        DataDirectory = Path.GetFullPath(dataDirectory);
    }

    public string DataDirectory { get; }

    public bool FileExists(string relativePath)
    {
        return File.Exists(GetFullPath(relativePath));
    }

    public string GetFullPath(string relativePath)
    {
        return Path.Combine(DataDirectory, relativePath);
    }

    public Stream OpenRead(string relativePath)
    {
        return File.OpenRead(GetFullPath(relativePath));
    }
}
