namespace OpenTyrian.Core;

public static class TyrianHelpTextLoader
{
    private static readonly byte[] CryptKey = [204, 129, 63, 255, 71, 19, 25, 62, 1, 99];
    private static readonly int[] SectionCounts =
    [
        39, // helpTxt
        21, // pName
        68, // miscText
        5,  // miscTextB
        11, // menuInt[6]
        7,  // menuText
        9,  // outputs
        6,  // topicName
        34, // mainMenuHelp
        7,  // menuInt[1]
        9,  // menuInt[2]
        8,  // menuInt[3]
        6,  // inGameText
        5,  // detailLevel
        4,  // gameSpeedText
    ];

    public static TyrianHelpTextCatalog Load(Stream stream)
    {
        using TyrianDataStream data = new(stream, leaveOpen: true);

        data.ReadInt32(); // episode1DataLoc

        for (int i = 0; i < 8; i++)
        {
            SkipSection(data, SectionCounts[i]);
        }

        List<string> mainMenuHelp = ReadSection(data, SectionCounts[8]);

        for (int i = 9; i < SectionCounts.Length; i++)
        {
            SkipSection(data, SectionCounts[i]);
        }

        List<string> episodeNames = ReadSection(data, 6);
        SkipSection(data, 7); // difficulty_name
        List<string> gameplayNames = ReadSection(data, 5);

        return new TyrianHelpTextCatalog(mainMenuHelp, gameplayNames, episodeNames);
    }

    private static void SkipSection(TyrianDataStream data, int count)
    {
        ReadEncryptedPascalString(data); // leading section label/header
        for (int i = 0; i < count; i++)
        {
            ReadEncryptedPascalString(data);
        }

        ReadEncryptedPascalString(data); // trailing section label/footer
    }

    private static List<string> ReadSection(TyrianDataStream data, int count)
    {
        ReadEncryptedPascalString(data); // leading section label/header

        List<string> values = new(count);
        for (int i = 0; i < count; i++)
        {
            values.Add(ReadEncryptedPascalString(data));
        }

        ReadEncryptedPascalString(data); // trailing section label/footer
        return values;
    }

    public static string ReadEncryptedPascalString(TyrianDataStream data)
    {
        int length = data.ReadByte();
        byte[] buffer = data.ReadBytes(length);

        if (length == 0)
        {
            return string.Empty;
        }

        Decrypt(buffer);
        return System.Text.Encoding.ASCII.GetString(buffer);
    }

    private static void Decrypt(byte[] buffer)
    {
        for (int i = buffer.Length - 1; i >= 0; i--)
        {
            buffer[i] ^= CryptKey[i % CryptKey.Length];
            if (i > 0)
            {
                buffer[i] ^= buffer[i - 1];
            }
        }
    }
}
