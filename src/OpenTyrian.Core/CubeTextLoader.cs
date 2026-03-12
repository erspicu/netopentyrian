using OpenTyrian.Platform;

namespace OpenTyrian.Core;

public static class CubeTextLoader
{
    private const int PreviewLimit = 128;

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
            };
        }

        using Stream stream = assetLocator.OpenRead(cubeFile);
        using TyrianDataStream data = new(stream, leaveOpen: true);

        int previewCount = 0;
        int markerCount = 0;

        while (previewCount < PreviewLimit && data.Position < data.Length)
        {
            string value = TyrianHelpTextLoader.ReadEncryptedPascalString(data);
            previewCount++;

            if (value.Length > 0 && value[0] == '*')
            {
                markerCount++;
            }
        }

        return new CubeTextInfo
        {
            Exists = true,
            Length = data.Length,
            PreviewStringCount = previewCount,
            SectionMarkerCount = markerCount,
        };
    }
}
