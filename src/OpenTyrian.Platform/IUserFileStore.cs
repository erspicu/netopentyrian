namespace OpenTyrian.Platform;

public interface IUserFileStore
{
    string RootDirectory { get; }

    bool FileExists(string relativePath);

    string GetFullPath(string relativePath);

    Stream OpenRead(string relativePath);
}
