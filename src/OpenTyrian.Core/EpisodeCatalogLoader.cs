using OpenTyrian.Platform;

namespace OpenTyrian.Core;

public static class EpisodeCatalogLoader
{
    public static IReadOnlyList<EpisodeInfo> Load(IAssetLocator assetLocator)
    {
        IReadOnlyList<string> episodeTitles = LoadEpisodeTitles(assetLocator);
        List<EpisodeInfo> episodes = new(episodeTitles.Count);

        for (int i = 0; i < episodeTitles.Count; i++)
        {
            int episodeNumber = i + 1;
            string levelFile = $"tyrian{episodeNumber}.lvl";
            bool isAvailable = assetLocator.FileExists(levelFile);
            string title = episodeTitles[i];
            string label = episodeNumber == 1
                ? title
                : $"Episode {episodeNumber}: {title}";
            LevelIndexInfo? levelIndex = LevelIndexLoader.Load(assetLocator, levelFile);
            string episodeFile = $"levels{episodeNumber}.dat";
            string cubeFile = $"cubetxt{episodeNumber}.dat";
            EpisodeScriptInfo scriptInfo = EpisodeScriptLoader.Load(assetLocator, episodeFile);
            CubeTextInfo cubeInfo = CubeTextLoader.Load(assetLocator, cubeFile);

            episodes.Add(new EpisodeInfo
            {
                EpisodeNumber = episodeNumber,
                Label = label,
                Description = isAvailable
                    ? $"Found {levelFile} in tyrian21. This episode can be selected."
                    : $"Missing {levelFile}. This matches the upstream episode availability scan.",
                IsAvailable = isAvailable,
                StartInfo = new EpisodeStartInfo
                {
                    EpisodeNumber = episodeNumber,
                    DisplayName = label,
                    LevelFile = levelFile,
                    EpisodeFile = episodeFile,
                    CubeFile = cubeFile,
                    LevelIndex = levelIndex,
                    ScriptInfo = scriptInfo,
                    CubeInfo = cubeInfo,
                },
            });
        }

        return episodes;
    }

    private static IReadOnlyList<string> LoadEpisodeTitles(IAssetLocator assetLocator)
    {
        if (!assetLocator.FileExists("tyrian.hdt"))
        {
            return
            [
                "Select Episode",
                "Escape",
                "Treachery",
                "Mission Suicide",
                "An End to Fate",
                "Hazudra Fodder",
            ];
        }

        using Stream stream = assetLocator.OpenRead("tyrian.hdt");
        TyrianHelpTextCatalog catalog = TyrianHelpTextLoader.Load(stream);
        return catalog.EpisodeNames;
    }
}
