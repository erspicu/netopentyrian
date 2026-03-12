namespace OpenTyrian.Core;

public sealed class EpisodeStartInfo
{
    public required int EpisodeNumber { get; init; }

    public required string DisplayName { get; init; }

    public required string LevelFile { get; init; }

    public required string EpisodeFile { get; init; }

    public required string CubeFile { get; init; }

    public LevelIndexInfo? LevelIndex { get; init; }

    public EpisodeScriptInfo? ScriptInfo { get; init; }

    public CubeTextInfo? CubeInfo { get; init; }
}
