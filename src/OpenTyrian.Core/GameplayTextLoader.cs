using OpenTyrian.Platform;

namespace OpenTyrian.Core;

public static class GameplayTextLoader
{
    public static GameplayTextInfo Load(IAssetLocator assetLocator)
    {
        if (!assetLocator.FileExists("tyrian.hdt"))
        {
            return new GameplayTextInfo
            {
                GameplayNames =
                [
                    "Select Game Mode",
                    "1 Player Full Game",
                    "1 Player Arcade",
                    "2 Player Arcade",
                    "Network Game",
                ],
                MainMenuHelp =
                [
                    "Main campaign route.",
                    "Single-player arcade mode.",
                    "Local two-player arcade mode.",
                    "Network mode is not wired yet.",
                ],
            };
        }

        using Stream stream = assetLocator.OpenRead("tyrian.hdt");
        TyrianHelpTextCatalog catalog = TyrianHelpTextLoader.Load(stream);

        return new GameplayTextInfo
        {
            GameplayNames = catalog.GameplayNames,
            MainMenuHelp = catalog.MainMenuHelp,
        };
    }
}
