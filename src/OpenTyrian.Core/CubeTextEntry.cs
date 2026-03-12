namespace OpenTyrian.Core;

public sealed class CubeTextEntry
{
    public required int Index { get; init; }

    public required string Title { get; init; }

    public required IList<string> Lines { get; init; }
}
