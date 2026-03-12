namespace OpenTyrian.Core;

public static class TitleFlowHelper
{
    public static EpisodeInfo? GetFirstAvailableEpisode(IList<EpisodeInfo> episodes)
    {
        for (int i = 0; i < episodes.Count; i++)
        {
            EpisodeInfo episode = episodes[i];
            if (episode.EpisodeNumber > 1 && episode.IsAvailable)
            {
                return episode;
            }
        }

        for (int i = 0; i < episodes.Count; i++)
        {
            if (episodes[i].IsAvailable)
            {
                return episodes[i];
            }
        }

        return episodes.Count > 0 ? episodes[0] : null;
    }

    public static EpisodeSessionState? CreateSession(EpisodeInfo? episode, GameStartMode startMode, int difficultyLevel)
    {
        if (episode is null)
        {
            return null;
        }

        EpisodeSessionState sessionState = new EpisodeSessionState(episode.StartInfo, startMode);
        sessionState.SetDifficulty(difficultyLevel);
        return sessionState;
    }

    public static EpisodeSessionState? CreateFirstAvailableSession(IList<EpisodeInfo> episodes, GameStartMode startMode, int difficultyLevel)
    {
        return CreateSession(GetFirstAvailableEpisode(episodes), startMode, difficultyLevel);
    }
}
