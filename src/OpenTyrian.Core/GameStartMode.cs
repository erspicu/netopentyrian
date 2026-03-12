namespace OpenTyrian.Core;

public enum GameStartMode
{
    FullGame,
    ArcadeOnePlayer,
    ArcadeTwoPlayer,
}

public static class GameStartModeExtensions
{
    public static bool IsArcadeLike(this GameStartMode mode)
    {
        return mode != GameStartMode.FullGame;
    }

    public static int GetPlayerCount(this GameStartMode mode)
    {
        return mode == GameStartMode.ArcadeTwoPlayer ? 2 : 1;
    }

    public static string GetDisplayName(this GameStartMode mode)
    {
        return mode switch
        {
            GameStartMode.FullGame => "1P Full Game",
            GameStartMode.ArcadeOnePlayer => "1P Arcade",
            GameStartMode.ArcadeTwoPlayer => "2P Arcade",
            _ => "Game Mode",
        };
    }
}
