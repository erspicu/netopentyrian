using System.Text;
using OpenTyrian.Platform;

namespace OpenTyrian.WinForms;

public sealed class WinFormsInputSource : IInputSource, IInputConfigurator, IJoystickConfigurator, ITextEntrySource
{
    private const int AnyJoystickDeviceId = -1;
    private readonly Dictionary<InputButton, Keys[]> _keyboardBindings = new();
    private readonly Dictionary<InputButton, JoystickBinding> _joystickBindings = new();
    private readonly HashSet<Keys> _pressedKeys = new();
    private readonly StringBuilder _pendingText = new();
    private readonly HybridJoystickDevice _joystickDevice = new();
    private bool _pointerPresent;
    private int _pointerX;
    private int _pointerY;
    private bool _pointerConfirm;
    private bool _pointerCancel;
    private int _pendingBackspaceCount;
    private bool _joystickEnabled = true;
    private InputButton? _pendingKeyboardBinding;
    private InputButton? _pendingJoystickBinding;
    private bool _waitForJoystickRelease;
    private HybridJoystickSnapshot _previousJoystickSnapshot = HybridJoystickSnapshot.Empty;

    public WinFormsInputSource()
    {
        ResetKeyboardDefaults();
        ResetJoystickDefaults();
    }

    InputButton? IInputConfigurator.PendingBinding
    {
        get { return _pendingKeyboardBinding; }
    }

    InputButton? IJoystickConfigurator.PendingBinding
    {
        get { return _pendingJoystickBinding; }
    }

    bool IJoystickConfigurator.IsSupported
    {
        get { return true; }
    }

    bool IJoystickConfigurator.IsEnabled
    {
        get { return _joystickEnabled; }
    }

    bool IJoystickConfigurator.HasConnectedDevice
    {
        get { return _joystickDevice.HasConnectedDevice; }
    }

    string IJoystickConfigurator.BackendName
    {
        get { return _joystickDevice.BackendName; }
    }

    string IJoystickConfigurator.DeviceSummary
    {
        get { return _joystickDevice.DeviceSummary; }
    }

    string ITextEntrySource.ConsumeText()
    {
        string text = _pendingText.ToString();
        _pendingText.Length = 0;
        return text;
    }

    int ITextEntrySource.ConsumeBackspaceCount()
    {
        int count = _pendingBackspaceCount;
        _pendingBackspaceCount = 0;
        return count;
    }

    void ITextEntrySource.ClearPendingText()
    {
        _pendingText.Length = 0;
        _pendingBackspaceCount = 0;
    }

    public void InitializeJoystick(IntPtr windowHandle)
    {
        _joystickDevice.Initialize(windowHandle);
        _previousJoystickSnapshot = HybridJoystickSnapshot.Empty;
    }

    public void Shutdown()
    {
        _joystickDevice.Dispose();
    }

    public void SetKeyState(Keys key, bool isDown)
    {
        if (_pendingKeyboardBinding is InputButton pendingBinding && isDown)
        {
            _keyboardBindings[pendingBinding] = new[] { key };
            _pendingKeyboardBinding = null;
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

    public void QueueTextInput(char character)
    {
        if (character == '\b')
        {
            _pendingBackspaceCount++;
            return;
        }

        if (character >= 32 && character <= 126)
        {
            _pendingText.Append(character);
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
        HybridJoystickSnapshot joystickSnapshot = _joystickDevice.Poll();
        bool suppressJoystickInput = ProcessPendingJoystickBinding(joystickSnapshot);
        bool allowJoystickInput = _joystickEnabled && _joystickDevice.HasConnectedDevice && !suppressJoystickInput;

        return new InputSnapshot(
            IsKeyboardPressed(InputButton.Up) || (allowJoystickInput && IsJoystickPressed(InputButton.Up, joystickSnapshot)),
            IsKeyboardPressed(InputButton.Down) || (allowJoystickInput && IsJoystickPressed(InputButton.Down, joystickSnapshot)),
            IsKeyboardPressed(InputButton.Left) || (allowJoystickInput && IsJoystickPressed(InputButton.Left, joystickSnapshot)),
            IsKeyboardPressed(InputButton.Right) || (allowJoystickInput && IsJoystickPressed(InputButton.Right, joystickSnapshot)),
            IsKeyboardPressed(InputButton.Confirm) || (allowJoystickInput && IsJoystickPressed(InputButton.Confirm, joystickSnapshot)),
            IsKeyboardPressed(InputButton.Cancel) || (allowJoystickInput && IsJoystickPressed(InputButton.Cancel, joystickSnapshot)))
        {
            PointerPresent = _pointerPresent,
            PointerX = _pointerX,
            PointerY = _pointerY,
            PointerConfirm = _pointerConfirm,
            PointerCancel = _pointerCancel,
        };
    }

    string IInputConfigurator.GetBindingLabel(InputButton button)
    {
        Keys[] bindings;
        if (!_keyboardBindings.TryGetValue(button, out bindings) || bindings.Length == 0)
        {
            return "<unbound>";
        }

        return string.Join(" / ", bindings.Select(GetKeyLabel));
    }

    string IJoystickConfigurator.GetBindingLabel(InputButton button)
    {
        JoystickBinding binding;
        if (!_joystickBindings.TryGetValue(button, out binding))
        {
            return "<unbound>";
        }

        return binding.GetLabel(_joystickDevice);
    }

    void IInputConfigurator.BeginRebind(InputButton button)
    {
        _pendingKeyboardBinding = button;
    }

    void IJoystickConfigurator.BeginRebind(InputButton button)
    {
        _pendingJoystickBinding = button;
        _waitForJoystickRelease = true;
    }

    void IInputConfigurator.CancelRebind()
    {
        _pendingKeyboardBinding = null;
    }

    void IJoystickConfigurator.CancelRebind()
    {
        _pendingJoystickBinding = null;
        _waitForJoystickRelease = false;
    }

    void IInputConfigurator.ResetToDefaults()
    {
        ResetKeyboardDefaults();
    }

    void IJoystickConfigurator.ResetToDefaults()
    {
        ResetJoystickDefaults();
    }

    void IJoystickConfigurator.RefreshStatus()
    {
        _joystickDevice.RefreshStatus();
        _previousJoystickSnapshot = HybridJoystickSnapshot.Empty;
    }

    void IJoystickConfigurator.SetEnabled(bool enabled)
    {
        _joystickEnabled = enabled;
    }

    private bool ProcessPendingJoystickBinding(HybridJoystickSnapshot joystickSnapshot)
    {
        if (_pendingJoystickBinding is not InputButton pendingBinding)
        {
            _previousJoystickSnapshot = joystickSnapshot;
            return false;
        }

        if (_waitForJoystickRelease)
        {
            if (!joystickSnapshot.HasAnyPressedControl())
            {
                _waitForJoystickRelease = false;
            }

            _previousJoystickSnapshot = joystickSnapshot;
            return true;
        }

        JoystickBinding binding;
        if (TryCaptureJoystickBinding(_previousJoystickSnapshot, joystickSnapshot, out binding))
        {
            _joystickBindings[pendingBinding] = binding;
            _pendingJoystickBinding = null;
        }

        _previousJoystickSnapshot = joystickSnapshot;
        return true;
    }

    private void ResetKeyboardDefaults()
    {
        _keyboardBindings[InputButton.Up] = new[] { Keys.Up };
        _keyboardBindings[InputButton.Down] = new[] { Keys.Down };
        _keyboardBindings[InputButton.Left] = new[] { Keys.Left };
        _keyboardBindings[InputButton.Right] = new[] { Keys.Right };
        _keyboardBindings[InputButton.Confirm] = new[] { Keys.Enter, Keys.Space };
        _keyboardBindings[InputButton.Cancel] = new[] { Keys.Escape, Keys.Back };
        _pendingKeyboardBinding = null;
    }

    private void ResetJoystickDefaults()
    {
        _joystickBindings[InputButton.Up] = JoystickBinding.Direction(AnyJoystickDeviceId, JoystickBindingType.Up);
        _joystickBindings[InputButton.Down] = JoystickBinding.Direction(AnyJoystickDeviceId, JoystickBindingType.Down);
        _joystickBindings[InputButton.Left] = JoystickBinding.Direction(AnyJoystickDeviceId, JoystickBindingType.Left);
        _joystickBindings[InputButton.Right] = JoystickBinding.Direction(AnyJoystickDeviceId, JoystickBindingType.Right);
        _joystickBindings[InputButton.Confirm] = JoystickBinding.Button(AnyJoystickDeviceId, 1);
        _joystickBindings[InputButton.Cancel] = JoystickBinding.Button(AnyJoystickDeviceId, 2);
        _pendingJoystickBinding = null;
        _waitForJoystickRelease = false;
    }

    private bool IsKeyboardPressed(InputButton button)
    {
        Keys[] bindings;
        if (!_keyboardBindings.TryGetValue(button, out bindings))
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

    private bool IsJoystickPressed(InputButton button, HybridJoystickSnapshot joystickSnapshot)
    {
        JoystickBinding binding;
        if (!_joystickBindings.TryGetValue(button, out binding))
        {
            return false;
        }

        return binding.IsPressed(joystickSnapshot);
    }

    private static bool TryCaptureJoystickBinding(HybridJoystickSnapshot previous, HybridJoystickSnapshot current, out JoystickBinding binding)
    {
        List<int> deviceIds = current.GetOrderedDeviceIds();
        for (int i = 0; i < deviceIds.Count; i++)
        {
            int deviceId = deviceIds[i];
            HybridJoystickState currentState;
            HybridJoystickState previousState;
            if (!current.TryGetState(deviceId, out currentState))
            {
                continue;
            }

            previous.TryGetState(deviceId, out previousState);

            int buttonCount = currentState.ButtonCount > 0 ? currentState.ButtonCount : 8;
            if (buttonCount > 32)
            {
                buttonCount = 32;
            }

            for (int buttonNumber = 1; buttonNumber <= buttonCount; buttonNumber++)
            {
                if (currentState.IsButtonPressed(buttonNumber) && !previousState.IsButtonPressed(buttonNumber))
                {
                    binding = JoystickBinding.Button(deviceId, buttonNumber);
                    return true;
                }
            }

            if (currentState.Up && !previousState.Up)
            {
                binding = JoystickBinding.Direction(deviceId, JoystickBindingType.Up);
                return true;
            }

            if (currentState.Down && !previousState.Down)
            {
                binding = JoystickBinding.Direction(deviceId, JoystickBindingType.Down);
                return true;
            }

            if (currentState.Left && !previousState.Left)
            {
                binding = JoystickBinding.Direction(deviceId, JoystickBindingType.Left);
                return true;
            }

            if (currentState.Right && !previousState.Right)
            {
                binding = JoystickBinding.Direction(deviceId, JoystickBindingType.Right);
                return true;
            }
        }

        binding = default;
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

    private enum JoystickBindingType
    {
        Up,
        Down,
        Left,
        Right,
        Button,
    }

    private struct JoystickBinding
    {
        public int DeviceId;
        public JoystickBindingType Type;
        public int ButtonNumber;

        public static JoystickBinding Direction(int deviceId, JoystickBindingType direction)
        {
            return new JoystickBinding
            {
                DeviceId = deviceId,
                Type = direction,
                ButtonNumber = 0,
            };
        }

        public static JoystickBinding Button(int deviceId, int buttonNumber)
        {
            return new JoystickBinding
            {
                DeviceId = deviceId,
                Type = JoystickBindingType.Button,
                ButtonNumber = buttonNumber,
            };
        }

        public bool IsPressed(HybridJoystickSnapshot snapshot)
        {
            if (DeviceId == AnyJoystickDeviceId)
            {
                foreach (KeyValuePair<int, HybridJoystickState> entry in snapshot.States)
                {
                    if (IsPressed(entry.Value))
                    {
                        return true;
                    }
                }

                return false;
            }

            HybridJoystickState state;
            return snapshot.TryGetState(DeviceId, out state) && IsPressed(state);
        }

        public string GetLabel(HybridJoystickDevice device)
        {
            string prefix = DeviceId == AnyJoystickDeviceId
                ? "Any"
                : device.GetDeviceLabel(DeviceId);

            string control;
            switch (Type)
            {
                case JoystickBindingType.Up:
                    control = "Up";
                    break;
                case JoystickBindingType.Down:
                    control = "Down";
                    break;
                case JoystickBindingType.Left:
                    control = "Left";
                    break;
                case JoystickBindingType.Right:
                    control = "Right";
                    break;
                case JoystickBindingType.Button:
                    control = string.Format("Button {0}", ButtonNumber);
                    break;
                default:
                    control = "<unbound>";
                    break;
            }

            return string.Format("{0} {1}", prefix, control);
        }

        private bool IsPressed(HybridJoystickState state)
        {
            switch (Type)
            {
                case JoystickBindingType.Up:
                    return state.Up;
                case JoystickBindingType.Down:
                    return state.Down;
                case JoystickBindingType.Left:
                    return state.Left;
                case JoystickBindingType.Right:
                    return state.Right;
                case JoystickBindingType.Button:
                    return state.IsButtonPressed(ButtonNumber);
                default:
                    return false;
            }
        }
    }
}
