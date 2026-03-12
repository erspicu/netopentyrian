namespace OpenTyrian.Core;

public sealed class CubeTextInfo
{
    public required bool Exists { get; init; }

    public required long Length { get; init; }

    public required int PreviewStringCount { get; init; }

    public required int SectionMarkerCount { get; init; }
}
