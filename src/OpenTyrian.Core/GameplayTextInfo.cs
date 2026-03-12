namespace OpenTyrian.Core;

public sealed class GameplayTextInfo
{
    public required IList<string> GameplayNames { get; init; }

    public required IList<string> MainMenuHelp { get; init; }

    public required IList<string> FullGameMenu { get; init; }

    public required IList<ShipDescriptionEntry> ShipInfo { get; init; }
}
