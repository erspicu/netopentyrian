using OpenTyrian.Platform;

namespace OpenTyrian.WinForms;

public sealed class WinFormsInputSource : IInputSource
{
    private bool _up;
    private bool _down;
    private bool _left;
    private bool _right;
    private bool _confirm;
    private bool _cancel;
    private bool _pointerPresent;
    private int _pointerX;
    private int _pointerY;
    private bool _pointerConfirm;
    private bool _pointerCancel;

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

    public void SetPointerPosition(int x, int y, bool present)
    {
        _pointerPresent = present;

        if (!present)
        {
            return;
        }

        _pointerX = x;
        _pointerY = y;
    }

    public void SetPointerButtonState(MouseButtons button, bool isDown)
    {
        switch (button)
        {
            case MouseButtons.Left:
                _pointerConfirm = isDown;
                break;

            case MouseButtons.Right:
                _pointerCancel = isDown;
                break;
        }
    }

    public void ClearPointer()
    {
        _pointerPresent = false;
        _pointerConfirm = false;
        _pointerCancel = false;
    }

    public InputSnapshot Capture()
    {
        return new InputSnapshot(_up, _down, _left, _right, _confirm, _cancel)
        {
            PointerPresent = _pointerPresent,
            PointerX = _pointerX,
            PointerY = _pointerY,
            PointerConfirm = _pointerConfirm,
            PointerCancel = _pointerCancel,
        };
    }
}
