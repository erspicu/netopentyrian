namespace OpenTyrian.Platform;

public interface IJoystickConfigurator
{
    bool IsSupported { get; }

    bool IsEnabled { get; }

    bool HasConnectedDevice { get; }

    InputButton? PendingBinding { get; }

    string BackendName { get; }

    string DeviceSummary { get; }

    string GetBindingLabel(InputButton button);

    void RefreshStatus();

    void SetEnabled(bool enabled);

    void BeginRebind(InputButton button);

    void CancelRebind();

    void ResetToDefaults();
}
