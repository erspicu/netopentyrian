using OpenTyrian.Platform;

namespace OpenTyrian.Core;

public static class CubeTextLoader
{
    public static CubeTextInfo Load(IAssetLocator assetLocator, string cubeFile)
    {
        if (!assetLocator.FileExists(cubeFile))
        {
            return new CubeTextInfo
            {
                Exists = false,
                Length = 0,
                PreviewStringCount = 0,
                SectionMarkerCount = 0,
                Entries = new CubeTextEntry[0],
            };
        }

        using Stream stream = assetLocator.OpenRead(cubeFile);
        using TyrianDataStream data = new(stream, leaveOpen: true);

        int previewCount = 0;
        int markerCount = 0;
        List<CubeTextEntry> entries = [];
        List<string>? currentLines = null;
        string currentTitle = string.Empty;

        while (data.Position < data.Length)
        {
            string value = TyrianHelpTextLoader.ReadEncryptedPascalString(data);
            previewCount++;

            if (value.Length > 0 && value[0] == '*')
            {
                markerCount++;
                FlushEntry(entries, currentTitle, currentLines);
                currentTitle = GetEntryTitle(value, markerCount);
                currentLines = [];
                continue;
            }

            if (currentLines is null)
            {
                currentTitle = string.Format("Cube {0}", markerCount + 1);
                currentLines = [];
            }

            currentLines.Add(value);
        }

        FlushEntry(entries, currentTitle, currentLines);

        return new CubeTextInfo
        {
            Exists = true,
            Length = data.Length,
            PreviewStringCount = previewCount,
            SectionMarkerCount = markerCount,
            Entries = entries,
        };
    }

    private static string GetEntryTitle(string value, int markerCount)
    {
        string title = value.TrimStart('*').Trim();
        return title.Length > 0 ? title : string.Format("Cube {0}", markerCount);
    }

    private static void FlushEntry(ICollection<CubeTextEntry> entries, string currentTitle, IList<string>? currentLines)
    {
        if (currentLines is null || (currentTitle.Length == 0 && currentLines.Count == 0))
        {
            return;
        }

        entries.Add(new CubeTextEntry
        {
            Index = entries.Count + 1,
            Title = currentTitle.Length > 0 ? currentTitle : string.Format("Cube {0}", entries.Count + 1),
            Lines = currentLines.Count > 0 ? currentLines.ToArray() : [string.Empty],
        });
    }
}
