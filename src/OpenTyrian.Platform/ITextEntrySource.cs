namespace OpenTyrian.Platform;

public interface ITextEntrySource
{
    string ConsumeText();

    int ConsumeBackspaceCount();

    void ClearPendingText();
}
