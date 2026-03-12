using OpenTyrian.Platform;

namespace OpenTyrian.Core;

public static class SaveGameFileManager
{
    private const int SaveFilesNum = 22;
    private const int SaveFilesSize = 2398;
    private const int SizeOfSaveGameTemp = SaveFilesSize + 4 + 100;
    private const int SaveFileSize = SizeOfSaveGameTemp - 4;
    private const int ExtraDataSize = 100;
    private static readonly byte[] CryptKey = { 15, 50, 89, 240, 147, 34, 86, 9, 32, 208 };

    public static SaveGameFile Load(IUserFileStore userFileStore)
    {
        string sourcePath = userFileStore.GetFullPath("tyrian.sav");
        if (!userFileStore.FileExists("tyrian.sav"))
        {
            return BuildEmptyFile(sourcePath, hasSaveFile: false, isValid: false);
        }

        try
        {
            using Stream stream = userFileStore.OpenRead("tyrian.sav");
            byte[] encrypted = ReadAllBytes(stream);
            if (encrypted.Length < SizeOfSaveGameTemp)
            {
                return BuildEmptyFile(sourcePath, hasSaveFile: true, isValid: false);
            }

            byte[] decrypted = DecryptAndValidate(encrypted);
            return ParseFile(sourcePath, decrypted, hasSaveFile: true, isValid: true);
        }
        catch
        {
            return BuildEmptyFile(sourcePath, hasSaveFile: true, isValid: false);
        }
    }

    public static void Save(IUserFileStore userFileStore, SaveGameFile saveFile)
    {
        byte[] decrypted = Serialize(saveFile);
        byte[] encrypted = Encrypt(decrypted);

        using Stream stream = userFileStore.OpenWrite("tyrian.sav");
        stream.Write(encrypted, 0, encrypted.Length);
        stream.Flush();

        saveFile.SourcePath = userFileStore.GetFullPath("tyrian.sav");
        saveFile.HasSaveFile = true;
        saveFile.IsValid = true;
    }

    private static SaveGameFile ParseFile(string sourcePath, byte[] decrypted, bool hasSaveFile, bool isValid)
    {
        List<SaveSlotRecord> slots = new List<SaveSlotRecord>(SaveFilesNum);
        using MemoryStream stream = new MemoryStream(decrypted, writable: false);
        using TyrianDataStream data = new TyrianDataStream(stream, leaveOpen: true);

        for (int slotIndex = 0; slotIndex < SaveFilesNum; slotIndex++)
        {
            slots.Add(new SaveSlotRecord
            {
                SlotIndex = slotIndex + 1,
                PageIndex = slotIndex / 11,
                Encode = data.ReadUInt16(),
                LevelNumber = data.ReadUInt16(),
                Items = data.ReadBytes(12),
                Cash = data.ReadInt32(),
                Cash2 = data.ReadInt32(),
                LevelName = ReadPascalField(data.ReadBytes(10)),
                Name = ReadFixedText(data.ReadBytes(14)),
                CubeCount = data.ReadByte(),
                WeaponPowers = data.ReadBytes(2),
                EpisodeNumber = data.ReadByte(),
                LastItems = data.ReadBytes(12),
                Difficulty = data.ReadByte(),
                SecretHint = data.ReadByte(),
                Input1 = data.ReadByte(),
                Input2 = data.ReadByte(),
                GameHasRepeated = data.ReadByte() != 0,
                InitialDifficulty = data.ReadByte(),
                HighScore1 = data.ReadInt32(),
                HighScore2 = data.ReadInt32(),
                HighScoreName = ReadPascalField(data.ReadBytes(30)),
                HighScoreDiff = data.ReadByte(),
            });
        }

        byte[] extraData = data.ReadBytes(ExtraDataSize);
        return new SaveGameFile
        {
            SourcePath = sourcePath,
            HasSaveFile = hasSaveFile,
            IsValid = isValid,
            Slots = slots,
            ExtraData = extraData,
        };
    }

    private static byte[] Serialize(SaveGameFile saveFile)
    {
        using MemoryStream stream = new MemoryStream(SaveFileSize);
        using BinaryWriter writer = new BinaryWriter(stream, System.Text.Encoding.ASCII);

        for (int i = 0; i < SaveFilesNum; i++)
        {
            SaveSlotRecord slot = i < saveFile.Slots.Count
                ? saveFile.Slots[i]
                : BuildEmptySlot(i + 1);
            WriteSlot(writer, slot);
        }

        byte[] extraData = saveFile.ExtraData ?? new byte[0];
        if (extraData.Length >= ExtraDataSize)
        {
            writer.Write(extraData, 0, ExtraDataSize);
        }
        else
        {
            writer.Write(extraData);
            if (extraData.Length < ExtraDataSize)
            {
                writer.Write(new byte[ExtraDataSize - extraData.Length]);
            }
        }

        writer.Flush();
        byte[] data = stream.ToArray();
        if (data.Length != SaveFileSize)
        {
            throw new InvalidDataException(string.Format("Unexpected save buffer length {0}.", data.Length));
        }

        return data;
    }

    private static void WriteSlot(BinaryWriter writer, SaveSlotRecord slot)
    {
        writer.Write(slot.Encode);
        writer.Write(slot.LevelNumber);
        writer.Write(FixBuffer(slot.Items, 12));
        writer.Write(slot.Cash);
        writer.Write(slot.Cash2);
        writer.Write(BuildPascalField(slot.LevelName, 10));
        writer.Write(BuildFixedField(slot.Name, 14));
        writer.Write(slot.CubeCount);
        writer.Write(FixBuffer(slot.WeaponPowers, 2));
        writer.Write(slot.EpisodeNumber);
        writer.Write(FixBuffer(slot.LastItems, 12));
        writer.Write(slot.Difficulty);
        writer.Write(slot.SecretHint);
        writer.Write(slot.Input1);
        writer.Write(slot.Input2);
        writer.Write((byte)(slot.GameHasRepeated ? 1 : 0));
        writer.Write(slot.InitialDifficulty);
        writer.Write(slot.HighScore1);
        writer.Write(slot.HighScore2);
        writer.Write(BuildPascalField(slot.HighScoreName, 30));
        writer.Write(slot.HighScoreDiff);
    }

    private static byte[] Encrypt(byte[] decrypted)
    {
        byte[] encrypted = new byte[SizeOfSaveGameTemp];
        Buffer.BlockCopy(decrypted, 0, encrypted, 0, decrypted.Length);

        byte additive = 0;
        byte subtractive = 0;
        byte multiplicative = 1;
        byte xor = 0;

        for (int i = 0; i < decrypted.Length; i++)
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

        encrypted[SaveFileSize] = additive;
        encrypted[SaveFileSize + 1] = subtractive;
        encrypted[SaveFileSize + 2] = multiplicative;
        encrypted[SaveFileSize + 3] = xor;

        for (int i = SaveFileSize - 1; i >= 0; i--)
        {
            encrypted[i] = (byte)(encrypted[i] ^ CryptKey[(i + 1) % CryptKey.Length]);
            if (i > 0)
            {
                encrypted[i] = (byte)(encrypted[i] ^ encrypted[i - 1]);
            }
        }

        return encrypted;
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

    private static byte[] ReadAllBytes(Stream stream)
    {
        using MemoryStream memory = new MemoryStream();
        stream.CopyTo(memory);
        return memory.ToArray();
    }

    private static SaveGameFile BuildEmptyFile(string sourcePath, bool hasSaveFile, bool isValid)
    {
        List<SaveSlotRecord> slots = new List<SaveSlotRecord>(SaveFilesNum);
        for (int i = 0; i < SaveFilesNum; i++)
        {
            slots.Add(BuildEmptySlot(i + 1));
        }

        return new SaveGameFile
        {
            SourcePath = sourcePath,
            HasSaveFile = hasSaveFile,
            IsValid = isValid,
            Slots = slots,
            ExtraData = new byte[ExtraDataSize],
        };
    }

    private static SaveSlotRecord BuildEmptySlot(int slotIndex)
    {
        return new SaveSlotRecord
        {
            SlotIndex = slotIndex,
            PageIndex = (slotIndex - 1) / 11,
            Encode = 0,
            LevelNumber = 0,
            Items = new byte[12],
            Cash = 0,
            Cash2 = 0,
            LevelName = string.Empty,
            Name = "EMPTY SLOT",
            CubeCount = 0,
            WeaponPowers = new byte[2],
            EpisodeNumber = 0,
            LastItems = new byte[12],
            Difficulty = 0,
            SecretHint = 0,
            Input1 = 0,
            Input2 = 0,
            GameHasRepeated = false,
            InitialDifficulty = 0,
            HighScore1 = 0,
            HighScore2 = 0,
            HighScoreName = string.Empty,
            HighScoreDiff = 0,
        };
    }

    private static byte[] FixBuffer(byte[]? source, int length)
    {
        byte[] buffer = new byte[length];
        if (source is null)
        {
            return buffer;
        }

        Buffer.BlockCopy(source, 0, buffer, 0, Math.Min(length, source.Length));
        return buffer;
    }

    private static byte[] BuildFixedField(string? text, int length)
    {
        byte[] buffer = new byte[length];
        if (string.IsNullOrEmpty(text))
        {
            return buffer;
        }

        byte[] source = System.Text.Encoding.ASCII.GetBytes(text);
        Buffer.BlockCopy(source, 0, buffer, 0, Math.Min(length, source.Length));
        return buffer;
    }

    private static byte[] BuildPascalField(string? text, int length)
    {
        byte[] buffer = new byte[length];
        if (string.IsNullOrEmpty(text))
        {
            return buffer;
        }

        int maxTextLength = length - 1;
        byte[] source = System.Text.Encoding.ASCII.GetBytes(text);
        int textLength = Math.Min(maxTextLength, source.Length);
        buffer[0] = (byte)textLength;
        Buffer.BlockCopy(source, 0, buffer, 1, textLength);
        return buffer;
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
}
