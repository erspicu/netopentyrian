namespace OpenTyrian.Core;

public static class SimpleSpriteTableLoader
{
    public static SpriteTable Load(TyrianDataStream stream)
    {
        int count = stream.ReadUInt16();
        SpriteFrame?[] frames = new SpriteFrame?[count];

        for (int i = 0; i < count; i++)
        {
            bool populated = stream.ReadBoolean();
            if (!populated)
            {
                continue;
            }

            int width = stream.ReadUInt16();
            int height = stream.ReadUInt16();
            int size = stream.ReadUInt16();
            byte[] data = stream.ReadBytes(size);
            frames[i] = new SpriteFrame(width, height, data);
        }

        return new SpriteTable(frames);
    }
}
