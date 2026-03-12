using OpenTyrian.Platform;

namespace OpenTyrian.WinForms;

public sealed class WinFormsKeyboardInputSource : IInputSource
{
    private bool _up;
    private bool _down;
    private bool _left;
    private bool _right;
    private bool _confirm;
    private bool _cancel;

    public void SetKeyState(Keys key, bool isDown)
    {
        switch (key)
        {
            case Keys.Up:
                _up = isDown;
                break;

            case Keys.Down:
                _down = isDown;
                break;

            case Keys.Left:
                _left = isDown;
                break;

            case Keys.Right:
                _right = isDown;
                break;

            case Keys.Enter:
            case Keys.Space:
                _confirm = isDown;
                break;

            case Keys.Escape:
            case Keys.Back:
                _cancel = isDown;
                break;
        }
    }

    public InputSnapshot Capture()
    {
        return new InputSnapshot(_up, _down, _left, _right, _confirm, _cancel);
    }
}
