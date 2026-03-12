using OpenTyrian.Platform;

namespace OpenTyrian.Core;

public static class DemoPlaybackLoader
{
    private const int DemoCount = 5;
    private static int _nextDemoNumber;

    public static DemoPlaybackInfo? LoadNext(IAssetLocator assetLocator)
    {
        for (int attempt = 0; attempt < DemoCount; attempt++)
        {
            _nextDemoNumber++;
            if (_nextDemoNumber > DemoCount)
            {
                _nextDemoNumber = 1;
            }

            string relativePath = string.Format("demo.{0}", _nextDemoNumber);
            if (!assetLocator.FileExists(relativePath))
            {
                continue;
            }

            using Stream stream = assetLocator.OpenRead(relativePath);
            return Load(stream, _nextDemoNumber);
        }

        return null;
    }

    private static DemoPlaybackInfo Load(Stream stream, int demoNumber)
    {
        using BinaryReader reader = new(stream, System.Text.Encoding.ASCII);

        int episodeNumber = reader.ReadByte();
        string levelName = System.Text.Encoding.ASCII.GetString(reader.ReadBytes(10)).TrimEnd('\0', ' ');
        int levelFileNumber = reader.ReadByte();
        int frontWeaponId = reader.ReadByte();
        int rearWeaponId = reader.ReadByte();
        reader.ReadByte(); // super arcade mode
        int leftSidekickId = reader.ReadByte();
        int rightSidekickId = reader.ReadByte();
        int generatorId = reader.ReadByte();
        reader.ReadByte(); // sidekick level
        reader.ReadByte(); // sidekick series
        reader.ReadByte(); // initial episode number
        int shieldId = reader.ReadByte();
        reader.ReadByte(); // special
        int shipId = reader.ReadByte();
        int frontWeaponPower = reader.ReadByte();
        int rearWeaponPower = reader.ReadByte();
        reader.ReadBytes(3); // unused
        int rawSongIndex = reader.ReadByte();
        int initialWaitFrames = ReadUInt16BigEndian(reader);

        List<DemoInputSegment> segments = new();
        AppendSegment(segments, 0, initialWaitFrames);

        while (reader.BaseStream.Position + 3 <= reader.BaseStream.Length)
        {
            byte keys = reader.ReadByte();
            int frames = ReadUInt16BigEndian(reader);
            AppendSegment(segments, keys, frames);
        }

        return new DemoPlaybackInfo
        {
            DemoNumber = demoNumber,
            EpisodeNumber = episodeNumber,
            LevelName = string.IsNullOrWhiteSpace(levelName) ? string.Format("Demo {0}", demoNumber) : levelName,
            LevelFileNumber = levelFileNumber,
            FrontWeaponId = frontWeaponId,
            RearWeaponId = rearWeaponId,
            LeftSidekickId = leftSidekickId,
            RightSidekickId = rightSidekickId,
            GeneratorId = generatorId,
            ShieldId = shieldId,
            ShipId = shipId,
            FrontWeaponPower = frontWeaponPower,
            RearWeaponPower = rearWeaponPower,
            MusicTrackIndex = rawSongIndex > 0 ? rawSongIndex - 1 : (int?)null,
            Segments = segments,
        };
    }

    private static void AppendSegment(ICollection<DemoInputSegment> segments, byte keys, int frames)
    {
        if (frames <= 0)
        {
            return;
        }

        segments.Add(new DemoInputSegment
        {
            Keys = keys,
            Frames = frames,
        });
    }

    private static int ReadUInt16BigEndian(BinaryReader reader)
    {
        int high = reader.ReadByte();
        int low = reader.ReadByte();
        return (high << 8) | low;
    }
}
