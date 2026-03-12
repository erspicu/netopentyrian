namespace OpenTyrian.Platform;

public interface IInputSource
{
    InputSnapshot Capture();
}
