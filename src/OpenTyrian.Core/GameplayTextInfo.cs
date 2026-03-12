namespace OpenTyrian.Core;

public sealed class GameplayTextInfo
{
    public required IReadOnlyList<string> GameplayNames { get; init; }

    public required IReadOnlyList<string> MainMenuHelp { get; init; }
}
