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
                HelpText =
                [
                    "Start a single-player or two-player campaign.",
                    "Load a previously saved full-game slot.",
                    "Review the best stored scores for each episode.",
                    "Read the instructions and system help pages.",
                    "Configure graphics, sound, or the jukebox.",
                    "Finish the current menu and return.",
                    "Adjust ship equipment and buy upgrades.",
                    "Review weapon ports and powered equipment.",
                    "Check shield and generator behavior.",
                    "Tune sidekicks and support hardware.",
                    "Browse options and control settings.",
                    "Leave the current page.",
                    "Done.",
                    "Move through each menu with arrows or a mouse.",
                    "Enter selects the highlighted item.",
                    "Esc goes back to the previous menu.",
                    "Use left and right to change values.",
                    "Watch the title screen to start demo mode.",
                    "Jukebox lets you preview the soundtrack.",
                    "Game options remain local only.",
                    "Options includes load, save and setup items.",
                    "Keyboard setup remaps the main six actions.",
                    "Joystick setup supports XInput and DirectInput.",
                    "Done returns to the previous screen.",
                    "Read page one of the options topic.",
                    "Read page two of the options topic.",
                    "Read page three of the options topic.",
                    "Done returns to the title menu.",
                    "General help footer.",
                ],
                MiscText =
                [
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    "Page",
                    "Topic page",
                ],
                TopicNames =
                [
                    "Instructions",
                    "One-Player Menu",
                    "Two-Player Menu",
                    "Upgrade Ship",
                    "Options",
                    "Done",
                ],
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
                OptionsMenu =
                [
                    "Options",
                    "Load Game",
                    "Save Game",
                    string.Empty,
                    string.Empty,
                    "Joystick Setup",
                    "Keyboard Setup",
                    "Done",
                ],
            };
        }

        using Stream stream = assetLocator.OpenRead("tyrian.hdt");
        TyrianHelpTextCatalog catalog = TyrianHelpTextLoader.Load(stream);

        return new GameplayTextInfo
        {
            HelpText = catalog.HelpText,
            MiscText = catalog.MiscText,
            TopicNames = catalog.TopicNames,
            GameplayNames = catalog.GameplayNames,
            MainMenuHelp = catalog.MainMenuHelp,
            FullGameMenu = catalog.FullGameMenu,
            ShipInfo = catalog.ShipInfo,
            OptionsMenu = catalog.OptionsMenu,
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
