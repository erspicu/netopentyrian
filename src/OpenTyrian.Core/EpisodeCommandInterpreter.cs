namespace OpenTyrian.Core;

public static class EpisodeCommandInterpreter
{
    public static EpisodeCommandExecutionResult ExecuteCurrentSection(EpisodeSessionState sessionState)
    {
        sessionState.ClearTransientCommandFlags();

        if (sessionState.CurrentMainLevelEntry is null)
        {
            return new EpisodeCommandExecutionResult(false, false, 0);
        }

        bool stateChanged = false;
        bool jumped = false;
        int executedCommands = 0;

        foreach (EpisodeCommandInfo command in sessionState.CurrentMainLevelEntry.Commands)
        {
            executedCommands++;

            switch (command.Kind)
            {
                case EpisodeCommandKind.SavePoint:
                    sessionState.SetSaveLevel(sessionState.CurrentLevelNumber);
                    stateChanged = true;
                    break;

                case EpisodeCommandKind.LastLevelSave:
                    sessionState.RequestLastLevelSave();
                    stateChanged = true;
                    break;

                case EpisodeCommandKind.ItemShopSong:
                    if (TryParseCommandInt(command.RawText, out int songIndex))
                    {
                        sessionState.SetItemShopSongIndex(songIndex - 1);
                        stateChanged = true;
                    }
                    break;

                case EpisodeCommandKind.ItemAvailabilityBlock:
                    sessionState.SetItemAvailability(command.ItemAvailability);
                    stateChanged = true;
                    break;

                case EpisodeCommandKind.FadeBlack:
                    sessionState.RequestFadeBlack();
                    stateChanged = true;
                    break;

                case EpisodeCommandKind.NetworkTextSync:
                    sessionState.RequestNetworkTextSync();
                    stateChanged = true;
                    break;

                case EpisodeCommandKind.SectionJump when command.TargetMainLevel is int jumpTarget:
                    if (sessionState.SetCurrentMainLevel(jumpTarget))
                    {
                        stateChanged = true;
                        jumped = true;
                        return new EpisodeCommandExecutionResult(stateChanged, jumped, executedCommands);
                    }
                    break;

                case EpisodeCommandKind.TwoPlayerSectionJump when command.TargetMainLevel is int arcadeTarget:
                    if (sessionState.IsArcadeLikeMode && sessionState.SetCurrentMainLevel(arcadeTarget))
                    {
                        stateChanged = true;
                        jumped = true;
                        return new EpisodeCommandExecutionResult(stateChanged, jumped, executedCommands);
                    }
                    break;
            }
        }

        return new EpisodeCommandExecutionResult(stateChanged, jumped, executedCommands);
    }

    private static bool TryParseCommandInt(string rawText, out int value)
    {
        string numericPart = rawText.Length > 3 ? rawText[3..] : string.Empty;
        return int.TryParse(numericPart, out value);
    }
}
