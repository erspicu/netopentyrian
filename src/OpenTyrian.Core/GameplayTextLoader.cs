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
                FullGameMenu =
                [
                    "Full Game",
                    "Data Cubes",
                    "Ship Specs",
                    "Upgrade Ship",
                    "Options",
                    "Next Level",
                    "Quit",
                ],
                ShipInfo = BuildDefaultShipInfo(),
            };
        }

        using Stream stream = assetLocator.OpenRead("tyrian.hdt");
        TyrianHelpTextCatalog catalog = TyrianHelpTextLoader.Load(stream);

        return new GameplayTextInfo
        {
            GameplayNames = catalog.GameplayNames,
            MainMenuHelp = catalog.MainMenuHelp,
            FullGameMenu = catalog.FullGameMenu,
            ShipInfo = catalog.ShipInfo,
        };
    }

    private static IList<ShipDescriptionEntry> BuildDefaultShipInfo()
    {
        List<ShipDescriptionEntry> entries = new(13);
        for (int i = 0; i < 13; i++)
        {
            entries.Add(new ShipDescriptionEntry
            {
                Summary = string.Format("Ship profile {0}", i + 1),
                Detail = "Detailed ship background text has not been decoded from tyrian.hdt yet.",
            });
        }

        return entries;
    }
}
