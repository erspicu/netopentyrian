namespace OpenTyrian.Platform;

public interface IInputConfigurator
{
    InputButton? PendingBinding { get; }

    string GetBindingLabel(InputButton button);

    void BeginRebind(InputButton button);

    void CancelRebind();

    void ResetToDefaults();
}
