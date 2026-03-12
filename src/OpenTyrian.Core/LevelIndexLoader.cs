using OpenTyrian.Platform;

namespace OpenTyrian.Core;

public static class LevelIndexLoader
{
    public static LevelIndexInfo? Load(IAssetLocator assetLocator, string levelFile)
    {
        if (!assetLocator.FileExists(levelFile))
        {
            return null;
        }

        using Stream stream = assetLocator.OpenRead(levelFile);
        using TyrianDataStream data = new(stream, leaveOpen: true);

        int levelCount = data.ReadUInt16();
        List<int> offsets = new(levelCount);

        for (int i = 0; i < levelCount; i++)
        {
            offsets.Add(data.ReadInt32());
        }

        return new LevelIndexInfo
        {
            LevelCount = levelCount,
            LevelOffsets = offsets,
            EndOffset = checked((int)data.Length),
        };
    }
}
