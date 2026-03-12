namespace OpenTyrian.Core;

public readonly record struct EpisodeCommandExecutionResult(
    bool StateChanged,
    bool Jumped,
    int ExecutedCommands);
