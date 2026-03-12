namespace OpenTyrian.Core;

public sealed class DemoPlaybackInfo
{
    public required int DemoNumber { get; init; }

    public required int EpisodeNumber { get; init; }

    public required string LevelName { get; init; }

    public required int LevelFileNumber { get; init; }

    public required int FrontWeaponId { get; init; }

    public required int RearWeaponId { get; init; }

    public required int LeftSidekickId { get; init; }

    public required int RightSidekickId { get; init; }

    public required int GeneratorId { get; init; }

    public required int ShieldId { get; init; }

    public required int ShipId { get; init; }

    public required int FrontWeaponPower { get; init; }

    public required int RearWeaponPower { get; init; }

    public required int? MusicTrackIndex { get; init; }

    public required IList<DemoInputSegment> Segments { get; init; }
}

public sealed class DemoInputSegment
{
    public required byte Keys { get; init; }

    public required int Frames { get; init; }
}
