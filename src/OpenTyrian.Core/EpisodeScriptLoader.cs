using OpenTyrian.Platform;

namespace OpenTyrian.Core;

public static class EpisodeScriptLoader
{
    private const int PreviewLimit = 128;

    public static EpisodeScriptInfo Load(IAssetLocator assetLocator, string episodeFile)
    {
        if (!assetLocator.FileExists(episodeFile))
        {
            return new EpisodeScriptInfo
            {
                Exists = false,
                Length = 0,
                PreviewStringCount = 0,
                SectionMarkerCount = 0,
                Sections = Array.Empty<EpisodeSectionInfo>(),
            };
        }

        using Stream stream = assetLocator.OpenRead(episodeFile);
        using TyrianDataStream data = new(stream, leaveOpen: true);

        int previewCount = 0;
        int markerCount = 0;
        int stringIndex = 0;
        List<EpisodeSectionInfo> sections = [];
        List<EpisodeCommandInfo>? currentCommands = null;

        while (data.Position < data.Length)
        {
            long offset = data.Position;
            string value = TyrianHelpTextLoader.ReadEncryptedPascalString(data);
            stringIndex++;
            if (previewCount < PreviewLimit)
            {
                previewCount++;
            }

            if (value.Length > 0 && value[0] == '*')
            {
                markerCount++;
                currentCommands = [];
                sections.Add(new EpisodeSectionInfo
                {
                    Label = value,
                    StringIndex = stringIndex,
                    FileOffset = offset,
                    Commands = currentCommands,
                });
            }
            else if (currentCommands is not null &&
                     value.Length > 1 &&
                     value[0] == ']')
            {
                EpisodeCommandInfo command = ParseCommand(value, stringIndex, offset, data, ref stringIndex, ref previewCount);
                currentCommands.Add(command);
            }
        }

        return new EpisodeScriptInfo
        {
            Exists = true,
            Length = data.Length,
            PreviewStringCount = previewCount,
            SectionMarkerCount = markerCount,
            Sections = sections,
        };
    }

    private static EpisodeCommandInfo ParseCommand(
        string value,
        int stringIndex,
        long offset,
        TyrianDataStream data,
        ref int runningStringIndex,
        ref int previewCount)
    {
        EpisodeCommandKind kind = value.Length > 1
            ? value[1] switch
            {
                'J' => EpisodeCommandKind.SectionJump,
                '2' => EpisodeCommandKind.TwoPlayerSectionJump,
                's' => EpisodeCommandKind.SavePoint,
                'b' => EpisodeCommandKind.LastLevelSave,
                'i' => EpisodeCommandKind.ItemShopSong,
                'I' => EpisodeCommandKind.ItemAvailabilityBlock,
                'B' => EpisodeCommandKind.FadeBlack,
                'S' => EpisodeCommandKind.NetworkTextSync,
                _ => EpisodeCommandKind.Unknown,
            }
            : EpisodeCommandKind.Unknown;

        int? targetMainLevel = null;
        if (kind is EpisodeCommandKind.SectionJump or EpisodeCommandKind.TwoPlayerSectionJump)
        {
            string numericPart = value.Length > 3 ? value[3..] : string.Empty;
            if (int.TryParse(numericPart, out int parsed))
            {
                targetMainLevel = parsed;
            }
        }

        List<string>? blockLines = null;
        ItemAvailabilityInfo? itemAvailability = null;
        if (kind == EpisodeCommandKind.ItemAvailabilityBlock)
        {
            blockLines = [];
            List<IReadOnlyList<int>> rows = [];
            List<int> maxPerRow = [];
            for (int i = 0; i < 9 && data.Position < data.Length; i++)
            {
                string line = TyrianHelpTextLoader.ReadEncryptedPascalString(data);
                runningStringIndex++;
                if (previewCount < PreviewLimit)
                {
                    previewCount++;
                }

                blockLines.Add(line);
                string numericSource = line.Length > 8 ? line[8..] : string.Empty;
                IReadOnlyList<int> row = ParseIntRow(numericSource);
                rows.Add(row);
                maxPerRow.Add(row.Count);
            }

            itemAvailability = new ItemAvailabilityInfo
            {
                Rows = rows,
                MaxPerRow = maxPerRow,
            };
        }

        return new EpisodeCommandInfo
        {
            Kind = kind,
            RawText = value,
            StringIndex = stringIndex,
            FileOffset = offset,
            TargetMainLevel = targetMainLevel,
            BlockLines = blockLines,
            ItemAvailability = itemAvailability,
        };
    }

    private static IReadOnlyList<int> ParseIntRow(string text)
    {
        List<int> values = [];
        string remaining = text;

        while (remaining.Length > 0 && values.Count < ItemAvailabilityInfo.MaxItemsPerCategory)
        {
            if (!TryPopLeadingInt(ref remaining, out int value))
            {
                break;
            }

            values.Add(value);
        }

        return values;
    }

    private static bool TryPopLeadingInt(ref string text, out int value)
    {
        value = 0;
        if (string.IsNullOrEmpty(text))
        {
            return false;
        }

        int index = 0;
        while (index < text.Length && char.IsWhiteSpace(text[index]))
        {
            index++;
        }

        if (index >= text.Length)
        {
            text = string.Empty;
            return false;
        }

        int start = index;
        if (text[index] == '-')
        {
            index++;
        }

        while (index < text.Length && char.IsDigit(text[index]))
        {
            index++;
        }

        if (index == start || (index == start + 1 && text[start] == '-'))
        {
            return false;
        }

        if (!int.TryParse(text[start..index], out value))
        {
            return false;
        }

        text = text[index..];
        return true;
    }
}
