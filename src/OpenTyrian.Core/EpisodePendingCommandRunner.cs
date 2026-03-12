namespace OpenTyrian.Core;

public static class EpisodePendingCommandRunner
{
    private const int MaxAutoExecutionPasses = 32;

    public static EpisodeCommandExecutionResult ExecutePending(EpisodeSessionState sessionState)
    {
        EpisodeCommandExecutionResult lastResult = new(false, false, 0, false);
        int autoExecutionPasses = 0;

        while (sessionState.ShouldAutoExecuteCurrentMainLevel() && autoExecutionPasses < MaxAutoExecutionPasses)
        {
            autoExecutionPasses++;
            sessionState.MarkCurrentMainLevelAutoExecuted();
            lastResult = EpisodeCommandInterpreter.ExecuteCurrentSection(sessionState);

            if (!lastResult.Jumped)
            {
                break;
            }
        }

        return lastResult;
    }
}
