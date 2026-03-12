namespace OpenTyrian.Core;

public static class DemoPlaybackSceneFactory
{
    public static IScene? TryCreate(SceneResources resources)
    {
        DemoPlaybackInfo? demo = DemoPlaybackLoader.LoadNext(resources.AssetLocator);
        if (demo is null)
        {
            return null;
        }

        EpisodeInfo? episode = ResolveEpisode(resources.Episodes, demo.EpisodeNumber);
        EpisodeSessionState? sessionState = TitleFlowHelper.CreateSession(episode, GameStartMode.ArcadeOnePlayer, 2);
        if (sessionState is null)
        {
            return null;
        }

        ApplyLoadout(sessionState, demo);
        return new GameplayScene(sessionState, true, new DemoPlaybackController(demo), demo.MusicTrackIndex);
    }

    private static EpisodeInfo? ResolveEpisode(IList<EpisodeInfo> episodes, int episodeNumber)
    {
        for (int i = 0; i < episodes.Count; i++)
        {
            if (episodes[i].EpisodeNumber == episodeNumber && episodes[i].IsAvailable)
            {
                return episodes[i];
            }
        }

        return TitleFlowHelper.GetFirstAvailableEpisode(episodes);
    }

    private static void ApplyLoadout(EpisodeSessionState sessionState, DemoPlaybackInfo demo)
    {
        sessionState.PlayerLoadout.Equip(ItemCategoryKind.Ship, demo.ShipId);
        sessionState.PlayerLoadout.Equip(ItemCategoryKind.FrontWeapon, demo.FrontWeaponId);
        sessionState.PlayerLoadout.Equip(ItemCategoryKind.RearWeapon, demo.RearWeaponId);
        sessionState.PlayerLoadout.Equip(ItemCategoryKind.Shield, demo.ShieldId);
        sessionState.PlayerLoadout.Equip(ItemCategoryKind.Generator, demo.GeneratorId);
        sessionState.PlayerLoadout.Equip(ItemCategoryKind.SidekickLeft, demo.LeftSidekickId);
        sessionState.PlayerLoadout.Equip(ItemCategoryKind.SidekickRight, demo.RightSidekickId);
        sessionState.PlayerLoadout.SetWeaponPower(ItemCategoryKind.FrontWeapon, demo.FrontWeaponPower);
        sessionState.PlayerLoadout.SetWeaponPower(ItemCategoryKind.RearWeapon, demo.RearWeaponPower);
    }
}
