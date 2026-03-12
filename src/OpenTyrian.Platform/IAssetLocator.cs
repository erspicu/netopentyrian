namespace OpenTyrian.Platform;

public interface IAssetLocator
{
    string DataDirectory { get; }

    bool FileExists(string relativePath);

    string GetFullPath(string relativePath);

    Stream OpenRead(string relativePath);
}
