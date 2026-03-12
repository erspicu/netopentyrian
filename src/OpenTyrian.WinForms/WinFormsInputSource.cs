using OpenTyrian.Platform;

namespace OpenTyrian.WinForms;

public sealed class WinFormsInputSource : IInputSource, IInputConfigurator
{
    private readonly Dictionary<InputButton, Keys[]> _bindings = new();
    private readonly HashSet<Keys> _pressedKeys = new();
    private bool _pointerPresent;
    private int _pointerX;
    private int _pointerY;
    private bool _pointerConfirm;
    private bool _pointerCancel;

    public WinFormsInputSource()
    {
        ResetToDefaults();
    }

    public InputButton? PendingBinding { get; private set; }

    public void SetKeyState(Keys key, bool isDown)
    {
        if (PendingBinding is InputButton pendingBinding && isDown)
        {
            _bindings[pendingBinding] = new[] { key };
            PendingBinding = null;
            _pressedKeys.Remove(key);
            return;
        }

        if (isDown)
        {
            _pressedKeys.Add(key);
        }
        else
        {
            _pressedKeys.Remove(key);
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
        return new InputSnapshot(
            IsPressed(InputButton.Up),
            IsPressed(InputButton.Down),
            IsPressed(InputButton.Left),
            IsPressed(InputButton.Right),
            IsPressed(InputButton.Confirm),
            IsPressed(InputButton.Cancel))
        {
            PointerPresent = _pointerPresent,
            PointerX = _pointerX,
            PointerY = _pointerY,
            PointerConfirm = _pointerConfirm,
            PointerCancel = _pointerCancel,
        };
    }

    public string GetBindingLabel(InputButton button)
    {
        Keys[] bindings;
        if (!_bindings.TryGetValue(button, out bindings) || bindings.Length == 0)
        {
            return "<unbound>";
        }

        return string.Join(" / ", bindings.Select(GetKeyLabel));
    }

    public void BeginRebind(InputButton button)
    {
        PendingBinding = button;
    }

    public void CancelRebind()
    {
        PendingBinding = null;
    }

    public void ResetToDefaults()
    {
        _bindings[InputButton.Up] = new[] { Keys.Up };
        _bindings[InputButton.Down] = new[] { Keys.Down };
        _bindings[InputButton.Left] = new[] { Keys.Left };
        _bindings[InputButton.Right] = new[] { Keys.Right };
        _bindings[InputButton.Confirm] = new[] { Keys.Enter, Keys.Space };
        _bindings[InputButton.Cancel] = new[] { Keys.Escape, Keys.Back };
        PendingBinding = null;
    }

    private bool IsPressed(InputButton button)
    {
        Keys[] bindings;
        if (!_bindings.TryGetValue(button, out bindings))
        {
            return false;
        }

        for (int i = 0; i < bindings.Length; i++)
        {
            if (_pressedKeys.Contains(bindings[i]))
            {
                return true;
            }
        }

        return false;
    }

    private static string GetKeyLabel(Keys key)
    {
        switch (key)
        {
            case Keys.Return:
                return "Enter";
            default:
                return key.ToString();
        }
    }
}
