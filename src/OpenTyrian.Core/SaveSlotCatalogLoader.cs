using OpenTyrian.Platform;

namespace OpenTyrian.Core;

public static class SaveSlotCatalogLoader
{
    private const int SaveFilesNum = 22;
    private const int SaveFilesSize = 2398;
    private const int SizeOfSaveGameTemp = SaveFilesSize + 4 + 100;
    private const int SaveFileSize = SizeOfSaveGameTemp - 4;
    private static readonly byte[] CryptKey = { 15, 50, 89, 240, 147, 34, 86, 9, 32, 208 };

    public static SaveSlotCatalog Load(IUserFileStore userFileStore)
    {
        string sourcePath = userFileStore.GetFullPath("tyrian.sav");
        if (!userFileStore.FileExists("tyrian.sav"))
        {
            return BuildEmptyCatalog(sourcePath, hasSaveFile: false, isValid: false);
        }

        try
        {
            using Stream stream = userFileStore.OpenRead("tyrian.sav");
            byte[] encrypted = ReadAllBytes(stream);
            if (encrypted.Length < SizeOfSaveGameTemp)
            {
                return BuildEmptyCatalog(sourcePath, hasSaveFile: true, isValid: false);
            }

            byte[] decrypted = DecryptAndValidate(encrypted);
            List<SaveSlotInfo> slots = ParseSlots(decrypted);
            return new SaveSlotCatalog
            {
                SourcePath = sourcePath,
                HasSaveFile = true,
                IsValid = true,
                Slots = slots,
            };
        }
        catch
        {
            return BuildEmptyCatalog(sourcePath, hasSaveFile: true, isValid: false);
        }
    }

    private static byte[] ReadAllBytes(Stream stream)
    {
        using MemoryStream memory = new();
        stream.CopyTo(memory);
        return memory.ToArray();
    }

    private static byte[] DecryptAndValidate(byte[] encrypted)
    {
        byte[] decrypted = new byte[SaveFileSize];
        for (int i = SaveFileSize - 1; i >= 0; i--)
        {
            byte value = (byte)(encrypted[i] ^ CryptKey[(i + 1) % CryptKey.Length]);
            if (i > 0)
            {
                value ^= encrypted[i - 1];
            }

            decrypted[i] = value;
        }

        ValidateChecksums(encrypted, decrypted);
        return decrypted;
    }

    private static void ValidateChecksums(byte[] encrypted, byte[] decrypted)
    {
        byte additive = 0;
        byte subtractive = 0;
        byte multiplicative = 1;
        byte xor = 0;

        for (int i = 0; i < SaveFileSize; i++)
        {
            byte value = decrypted[i];
            unchecked
            {
                additive += value;
                subtractive -= value;
                multiplicative = (byte)((multiplicative * value) + 1);
                xor ^= value;
            }
        }

        if (encrypted[SaveFileSize] != additive ||
            encrypted[SaveFileSize + 1] != subtractive ||
            encrypted[SaveFileSize + 2] != multiplicative ||
            encrypted[SaveFileSize + 3] != xor)
        {
            throw new InvalidDataException("Save file checksum validation failed.");
        }
    }

    private static List<SaveSlotInfo> ParseSlots(byte[] decrypted)
    {
        List<SaveSlotInfo> slots = new(SaveFilesNum);
        using MemoryStream stream = new(decrypted, writable: false);
        using TyrianDataStream data = new(stream, leaveOpen: true);

        for (int slotIndex = 0; slotIndex < SaveFilesNum; slotIndex++)
        {
            data.ReadUInt16(); // encode
            int levelNumber = data.ReadUInt16();
            data.ReadBytes(12); // items
            int cash = data.ReadInt32();
            int cash2 = data.ReadInt32();
            string levelName = ReadPascalField(data.ReadBytes(10));
            string name = ReadFixedText(data.ReadBytes(14));
            int cubeCount = data.ReadByte();
            data.ReadBytes(2); // power
            int episodeNumber = data.ReadByte();
            data.ReadBytes(12); // lastItems
            data.ReadByte(); // difficulty
            data.ReadByte(); // secretHint
            data.ReadByte(); // input1
            data.ReadByte(); // input2
            data.ReadByte(); // gameHasRepeated
            data.ReadByte(); // initialDifficulty
            data.ReadInt32(); // highScore1
            data.ReadInt32(); // highScore2
            data.ReadBytes(30); // highScoreName
            data.ReadByte(); // highScoreDiff

            bool isEmpty = levelNumber == 0;
            slots.Add(new SaveSlotInfo
            {
                SlotIndex = slotIndex + 1,
                PageIndex = slotIndex / 11,
                IsEmpty = isEmpty,
                Name = isEmpty ? "EMPTY SLOT" : name,
                LevelName = isEmpty ? "-----" : levelName,
                LevelNumber = levelNumber,
                EpisodeNumber = episodeNumber,
                CubeCount = cubeCount,
                Cash = cash,
                Cash2 = cash2,
            });
        }

        return slots;
    }

    private static string ReadPascalField(byte[] buffer)
    {
        if (buffer.Length == 0)
        {
            return string.Empty;
        }

        int length = buffer[0];
        if (length < 0)
        {
            length = 0;
        }
        else if (length > buffer.Length - 1)
        {
            length = buffer.Length - 1;
        }

        return System.Text.Encoding.ASCII.GetString(buffer, 1, length).TrimEnd('\0', ' ');
    }

    private static string ReadFixedText(byte[] buffer)
    {
        return System.Text.Encoding.ASCII.GetString(buffer).TrimEnd('\0', ' ');
    }

    private static SaveSlotCatalog BuildEmptyCatalog(string sourcePath, bool hasSaveFile, bool isValid)
    {
        List<SaveSlotInfo> slots = new(SaveFilesNum);
        for (int slotIndex = 0; slotIndex < SaveFilesNum; slotIndex++)
        {
            slots.Add(new SaveSlotInfo
            {
                SlotIndex = slotIndex + 1,
                PageIndex = slotIndex / 11,
                IsEmpty = true,
                Name = "EMPTY SLOT",
                LevelName = "-----",
                LevelNumber = 0,
                EpisodeNumber = 0,
                CubeCount = 0,
                Cash = 0,
                Cash2 = 0,
            });
        }

        return new SaveSlotCatalog
        {
            SourcePath = sourcePath,
            HasSaveFile = hasSaveFile,
            IsValid = isValid,
            Slots = slots,
        };
    }
}
