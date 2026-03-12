using System.Runtime.InteropServices;
using System.Text;

namespace OpenTyrian.WinForms;

internal sealed class HybridJoystickDevice : IDisposable
{
    private const int XInputIdBase = 1000;
    private readonly List<DirectInputDeviceEntry> _directInputDevices = new();
    private readonly Dictionary<int, string> _deviceLabels = new();
    private readonly XInputApi _xinput = new();
    private IntPtr _directInput;
    private IntPtr _windowHandle;

    public string BackendName
    {
        get { return "XInput + DirectInput"; }
    }

    public bool HasConnectedDevice { get; private set; }

    public string DeviceSummary { get; private set; } = "Waiting for form handle";

    public void Initialize(IntPtr windowHandle)
    {
        _windowHandle = windowHandle;
        RefreshStatus();
    }

    public void RefreshStatus()
    {
        ReleaseDirectInput();
        _deviceLabels.Clear();

        if (_windowHandle != IntPtr.Zero)
        {
            try
            {
                _directInput = DirectInputNative.CreateDirectInput();
                IList<DiDeviceInfo> devices = DirectInputNative.EnumJoysticks(_directInput);
                for (int i = 0; i < devices.Count; i++)
                {
                    DiDeviceInfo info = devices[i];
                    IntPtr device = DirectInputNative.OpenDevice(_directInput, info.GuidInstance, _windowHandle);
                    if (device == IntPtr.Zero)
                    {
                        continue;
                    }

                    _directInputDevices.Add(new DirectInputDeviceEntry
                    {
                        Id = i,
                        Device = device,
                        Name = string.IsNullOrWhiteSpace(info.Name)
                            ? string.Format("DI {0}", i + 1)
                            : string.Format("DI {0}: {1}", i + 1, info.Name),
                    });
                }
            }
            catch
            {
                ReleaseDirectInput();
            }
        }

        Poll();
    }

    public HybridJoystickSnapshot Poll()
    {
        Dictionary<int, HybridJoystickState> states = new();
        Dictionary<int, string> labels = new();

        for (int i = 0; i < _directInputDevices.Count; i++)
        {
            DirectInputDeviceEntry entry = _directInputDevices[i];
            DIJOYSTATE rawState;
            if (!DirectInputNative.PollDevice(entry.Device, out rawState))
            {
                continue;
            }

            states[entry.Id] = HybridJoystickState.FromDirectInput(rawState);
            labels[entry.Id] = entry.Name;
        }

        if (_xinput.IsAvailable)
        {
            for (int playerIndex = 0; playerIndex < 4; playerIndex++)
            {
                XINPUT_STATE state;
                if (!_xinput.TryGetState((uint)playerIndex, out state))
                {
                    continue;
                }

                int id = XInputIdBase + playerIndex;
                states[id] = HybridJoystickState.FromXInput(state);
                labels[id] = string.Format("XI {0}", playerIndex + 1);
            }
        }

        _deviceLabels.Clear();
        foreach (KeyValuePair<int, string> entry in labels)
        {
            _deviceLabels[entry.Key] = entry.Value;
        }

        HasConnectedDevice = states.Count > 0;
        DeviceSummary = BuildSummary(labels);
        return new HybridJoystickSnapshot(states, labels);
    }

    public string GetDeviceLabel(int deviceId)
    {
        if (deviceId < 0)
        {
            return "Any";
        }

        string label;
        if (_deviceLabels.TryGetValue(deviceId, out label))
        {
            return label;
        }

        if (deviceId >= XInputIdBase)
        {
            return string.Format("XI {0}", (deviceId - XInputIdBase) + 1);
        }

        return string.Format("DI {0}", deviceId + 1);
    }

    public void Dispose()
    {
        ReleaseDirectInput();
    }

    private string BuildSummary(Dictionary<int, string> labels)
    {
        if (_windowHandle == IntPtr.Zero)
        {
            return "Waiting for form handle";
        }

        if (labels.Count == 0)
        {
            return "No XInput / DirectInput device detected";
        }

        List<int> orderedIds = new List<int>(labels.Keys);
        orderedIds.Sort();
        string first = labels[orderedIds[0]];
        if (orderedIds.Count == 1)
        {
            return first;
        }

        return string.Format("{0} (+{1})", first, orderedIds.Count - 1);
    }

    private void ReleaseDirectInput()
    {
        for (int i = 0; i < _directInputDevices.Count; i++)
        {
            DirectInputNative.ReleaseDevice(_directInputDevices[i].Device);
        }

        _directInputDevices.Clear();

        if (_directInput != IntPtr.Zero)
        {
            DirectInputNative.ReleaseDI(_directInput);
            _directInput = IntPtr.Zero;
        }
    }

    private struct DirectInputDeviceEntry
    {
        public int Id;
        public IntPtr Device;
        public string Name;
    }
}

internal sealed class HybridJoystickSnapshot
{
    public static readonly HybridJoystickSnapshot Empty = new HybridJoystickSnapshot(new Dictionary<int, HybridJoystickState>(), new Dictionary<int, string>());

    public HybridJoystickSnapshot(Dictionary<int, HybridJoystickState> states, Dictionary<int, string> labels)
    {
        States = states;
        Labels = labels;
    }

    public Dictionary<int, HybridJoystickState> States { get; private set; }

    public Dictionary<int, string> Labels { get; private set; }

    public bool HasAnyPressedControl()
    {
        foreach (KeyValuePair<int, HybridJoystickState> entry in States)
        {
            if (entry.Value.HasAnyPressedControl())
            {
                return true;
            }
        }

        return false;
    }

    public bool TryGetState(int deviceId, out HybridJoystickState state)
    {
        return States.TryGetValue(deviceId, out state);
    }

    public List<int> GetOrderedDeviceIds()
    {
        List<int> ids = new List<int>(States.Keys);
        ids.Sort();
        return ids;
    }
}

internal struct HybridJoystickState
{
    private const int AxisCenter = 32767;
    private const int AxisNoise = 256;
    private const short XInputDeadZone = 7849;
    private static readonly int[] XInputButtonMasks = { 0x1000, 0x2000, 0x4000, 0x8000, 0x0100, 0x0200, 0x0010, 0x0020 };

    public bool Up;
    public bool Down;
    public bool Left;
    public bool Right;
    public uint Buttons;
    public int ButtonCount;

    public bool HasAnyPressedControl()
    {
        return Up || Down || Left || Right || Buttons != 0;
    }

    public bool IsButtonPressed(int buttonNumber)
    {
        if (buttonNumber <= 0 || buttonNumber > 32)
        {
            return false;
        }

        return (Buttons & (1u << (buttonNumber - 1))) != 0;
    }

    public static HybridJoystickState FromDirectInput(DIJOYSTATE state)
    {
        int x = SnapAxisDigital(Math.Abs(state.lX - AxisCenter) < AxisNoise ? AxisCenter : state.lX);
        int y = SnapAxisDigital(Math.Abs(state.lY - AxisCenter) < AxisNoise ? AxisCenter : state.lY);
        int povX;
        int povY;
        PovToXY(state.rgdwPOV[0], out povX, out povY);

        uint buttons = 0;
        for (int i = 0; i < 32; i++)
        {
            if ((state.rgbButtons[i] & 0x80) != 0)
            {
                buttons |= 1u << i;
            }
        }

        return new HybridJoystickState
        {
            Left = x == 0 || (x == AxisCenter && povX == 0),
            Right = x == 65535 || (x == AxisCenter && povX == 65535),
            Up = y == 0 || (y == AxisCenter && povY == 0),
            Down = y == 65535 || (y == AxisCenter && povY == 65535),
            Buttons = buttons,
            ButtonCount = 32,
        };
    }

    public static HybridJoystickState FromXInput(XINPUT_STATE state)
    {
        int dpad = state.Gamepad.wButtons & 0x000F;
        bool left = (dpad & 0x0004) != 0;
        bool right = (dpad & 0x0008) != 0;
        bool up = (dpad & 0x0001) != 0;
        bool down = (dpad & 0x0002) != 0;

        if (!left && !right)
        {
            int axisX = Math.Abs(state.Gamepad.sThumbLX) < XInputDeadZone ? AxisCenter : SnapAxisDigital((state.Gamepad.sThumbLX + 32768) & 0xFFFF);
            left = axisX == 0;
            right = axisX == 65535;
        }

        if (!up && !down)
        {
            int axisY = Math.Abs(state.Gamepad.sThumbLY) < XInputDeadZone ? AxisCenter : SnapAxisDigital(65535 - ((state.Gamepad.sThumbLY + 32768) & 0xFFFF));
            up = axisY == 0;
            down = axisY == 65535;
        }

        uint buttons = 0;
        for (int i = 0; i < XInputButtonMasks.Length; i++)
        {
            if ((state.Gamepad.wButtons & XInputButtonMasks[i]) != 0)
            {
                buttons |= 1u << i;
            }
        }

        return new HybridJoystickState
        {
            Left = left,
            Right = right,
            Up = up,
            Down = down,
            Buttons = buttons,
            ButtonCount = XInputButtonMasks.Length,
        };
    }

    private static void PovToXY(uint pov, out int x, out int y)
    {
        x = AxisCenter;
        y = AxisCenter;
        if (pov == 0xFFFFFFFFu)
        {
            return;
        }

        int degrees = (int)pov;
        if (degrees >= 4500 && degrees <= 13500)
        {
            x = 65535;
        }
        else if (degrees >= 22500 && degrees <= 31500)
        {
            x = 0;
        }

        if (degrees <= 4500 || degrees >= 31500)
        {
            y = 0;
        }
        else if (degrees >= 13500 && degrees <= 22500)
        {
            y = 65535;
        }
    }

    private static int SnapAxisDigital(int value)
    {
        if (value < 16384)
        {
            return 0;
        }

        if (value > 49151)
        {
            return 65535;
        }

        return AxisCenter;
    }
}

internal sealed class XInputApi
{
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate uint XInputGetStateDelegate(uint dwUserIndex, ref XINPUT_STATE state);

    private readonly XInputGetStateDelegate? _getState;

    public XInputApi()
    {
        string[] dlls = { "xinput1_4.dll", "xinput1_3.dll", "xinput9_1_0.dll" };
        for (int i = 0; i < dlls.Length; i++)
        {
            IntPtr module = LoadLibrary(dlls[i]);
            if (module == IntPtr.Zero)
            {
                continue;
            }

            IntPtr proc = GetProcAddress(module, "XInputGetState");
            if (proc == IntPtr.Zero)
            {
                continue;
            }

            _getState = (XInputGetStateDelegate)Marshal.GetDelegateForFunctionPointer(proc, typeof(XInputGetStateDelegate));
            IsAvailable = true;
            LoadedDll = dlls[i];
            break;
        }
    }

    public bool IsAvailable { get; private set; }

    public string LoadedDll { get; private set; } = "unavailable";

    public bool TryGetState(uint playerIndex, out XINPUT_STATE state)
    {
        state = new XINPUT_STATE();
        if (_getState is null)
        {
            return false;
        }

        return _getState(playerIndex, ref state) == 0;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr LoadLibrary(string lpFileName);

    [DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
    private static extern IntPtr GetProcAddress(IntPtr hModule, string procName);
}

[StructLayout(LayoutKind.Explicit)]
internal struct XINPUT_GAMEPAD
{
    [FieldOffset(0)] public ushort wButtons;
    [FieldOffset(2)] public byte bLeftTrigger;
    [FieldOffset(3)] public byte bRightTrigger;
    [FieldOffset(4)] public short sThumbLX;
    [FieldOffset(6)] public short sThumbLY;
    [FieldOffset(8)] public short sThumbRX;
    [FieldOffset(10)] public short sThumbRY;
}

[StructLayout(LayoutKind.Sequential)]
internal struct XINPUT_STATE
{
    public uint dwPacketNumber;
    public XINPUT_GAMEPAD Gamepad;
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal struct DIDEVICEINSTANCE_MIN
{
    public uint dwSize;
    public Guid guidInstance;
    public Guid guidProduct;
    public uint dwDevType;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
    public string tszInstanceName;
}

[StructLayout(LayoutKind.Sequential)]
internal struct DIJOYSTATE
{
    public int lX;
    public int lY;
    public int lZ;
    public int lRx;
    public int lRy;
    public int lRz;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)] public int[] rglSlider;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)] public uint[] rgdwPOV;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)] public byte[] rgbButtons;
}

[StructLayout(LayoutKind.Sequential)]
internal struct DIPROPHEADER
{
    public uint dwSize;
    public uint dwHeaderSize;
    public uint dwObj;
    public uint dwHow;
}

[StructLayout(LayoutKind.Sequential)]
internal struct DIPROPRANGE
{
    public DIPROPHEADER diph;
    public int lMin;
    public int lMax;
}

[StructLayout(LayoutKind.Explicit, Size = 24)]
internal struct DIOBJECTDATAFORMAT
{
    [FieldOffset(0)] public IntPtr pguid;
    [FieldOffset(8)] public uint dwOfs;
    [FieldOffset(12)] public uint dwType;
    [FieldOffset(16)] public uint dwFlags;
}

[StructLayout(LayoutKind.Explicit, Size = 32)]
internal struct DIDATAFORMAT
{
    [FieldOffset(0)] public uint dwSize;
    [FieldOffset(4)] public uint dwObjSize;
    [FieldOffset(8)] public uint dwFlags;
    [FieldOffset(12)] public uint dwDataSize;
    [FieldOffset(16)] public uint dwNumObjs;
    [FieldOffset(24)] public IntPtr rgodf;
}

[StructLayout(LayoutKind.Sequential)]
internal struct SP_DEVINFO_DATA
{
    public uint cbSize;
    public Guid ClassGuid;
    public uint DevInst;
    public IntPtr Reserved;
}

internal struct DiDeviceInfo
{
    public Guid GuidInstance;
    public Guid GuidProduct;
    public string Name;
}

internal static class DI8Slot
{
    public const int Release = 2;
    public const int CreateDevice = 3;
    public const int EnumDevices = 4;
}

internal static class DI8DevSlot
{
    public const int Release = 2;
    public const int SetProperty = 6;
    public const int Acquire = 7;
    public const int GetDeviceState = 9;
    public const int SetDataFormat = 11;
    public const int SetCooperativeLevel = 13;
    public const int Poll = 25;
}

[UnmanagedFunctionPointer(CallingConvention.StdCall)]
internal delegate uint VtReleaseFn(IntPtr self);

[UnmanagedFunctionPointer(CallingConvention.StdCall)]
internal delegate int VtDI8CreateDeviceFn(IntPtr self, ref Guid rguid, out IntPtr ppDevice, IntPtr punkOuter);

[UnmanagedFunctionPointer(CallingConvention.StdCall)]
internal delegate int VtDI8EnumDevicesFn(IntPtr self, uint devType, IntPtr callback, IntPtr pvRef, uint flags);

[UnmanagedFunctionPointer(CallingConvention.StdCall)]
internal delegate int VtDI8DevSetDataFormatFn(IntPtr self, IntPtr lpdf);

[UnmanagedFunctionPointer(CallingConvention.StdCall)]
internal delegate int VtDI8DevSetCoopLevelFn(IntPtr self, IntPtr hwnd, uint flags);

[UnmanagedFunctionPointer(CallingConvention.StdCall)]
internal delegate int VtDI8DevSetPropertyFn(IntPtr self, IntPtr rguidProp, IntPtr pdiph);

[UnmanagedFunctionPointer(CallingConvention.StdCall)]
internal delegate int VtDI8DevAcquireFn(IntPtr self);

[UnmanagedFunctionPointer(CallingConvention.StdCall)]
internal delegate int VtDI8DevPollFn(IntPtr self);

[UnmanagedFunctionPointer(CallingConvention.StdCall)]
internal delegate int VtDI8DevGetStateFn(IntPtr self, uint cbData, IntPtr lpvData);

[UnmanagedFunctionPointer(CallingConvention.StdCall)]
internal delegate int DiEnumDevicesNativeCb(IntPtr lpddi, IntPtr pvRef);

internal static class DirectInputNative
{
    private static readonly Guid IID_IDirectInput8W = new Guid("BF798031-483A-4DA2-AA99-5D64ED369700");
    private static readonly IntPtr DipropRange = (IntPtr)4;
    private const uint DI8DEVCLASS_GAMECTRL = 4;
    private const uint DIEDFL_ATTACHEDONLY = 0x00000001;
    private const uint DISCL_NONEXCLUSIVE = 0x00000002;
    private const uint DISCL_BACKGROUND = 0x00000008;
    private const uint DIPH_BYOFFSET = 3;

    [DllImport("dinput8.dll")]
    private static extern int DirectInput8Create(IntPtr hinst, uint dwVersion, ref Guid riidltf, out IntPtr ppvOut, IntPtr punkOuter);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetModuleHandle(string? moduleName);

    [DllImport("setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetupDiGetClassDevs(IntPtr classGuid, string enumerator, IntPtr hwnd, uint flags);

    [DllImport("setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern bool SetupDiEnumDeviceInfo(IntPtr devInfoSet, uint memberIdx, ref SP_DEVINFO_DATA devInfoData);

    [DllImport("setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern bool SetupDiGetDeviceInstanceId(IntPtr devInfoSet, ref SP_DEVINFO_DATA devInfoData, StringBuilder deviceInstanceId, uint deviceInstanceIdSize, out uint requiredSize);

    [DllImport("setupapi.dll", SetLastError = true)]
    private static extern bool SetupDiDestroyDeviceInfoList(IntPtr devInfoSet);

    public static IntPtr CreateDirectInput()
    {
        IntPtr module = GetModuleHandle(null);
        Guid iid = IID_IDirectInput8W;
        IntPtr directInput;
        int hr = DirectInput8Create(module, 0x0800, ref iid, out directInput, IntPtr.Zero);
        if (hr != 0 || directInput == IntPtr.Zero)
        {
            throw new InvalidOperationException("DirectInput8Create failed.");
        }

        return directInput;
    }

    public static IList<DiDeviceInfo> EnumJoysticks(IntPtr directInput)
    {
        HashSet<uint> xinputVidPids = GetXInputVidPids();
        List<DiDeviceInfo> devices = new List<DiDeviceInfo>();

        DiEnumDevicesNativeCb callback = delegate (IntPtr lpddi, IntPtr pvRef)
        {
            try
            {
                DIDEVICEINSTANCE_MIN instance = (DIDEVICEINSTANCE_MIN)Marshal.PtrToStructure(lpddi, typeof(DIDEVICEINSTANCE_MIN));
                uint data1 = BitConverter.ToUInt32(instance.guidProduct.ToByteArray(), 0);
                if (!xinputVidPids.Contains(data1))
                {
                    devices.Add(new DiDeviceInfo
                    {
                        GuidInstance = instance.guidInstance,
                        GuidProduct = instance.guidProduct,
                        Name = instance.tszInstanceName ?? string.Empty,
                    });
                }
            }
            catch
            {
            }

            return 1;
        };

        IntPtr callbackPtr = Marshal.GetFunctionPointerForDelegate(callback);
        GetVtMethod<VtDI8EnumDevicesFn>(directInput, DI8Slot.EnumDevices)(directInput, DI8DEVCLASS_GAMECTRL, callbackPtr, IntPtr.Zero, DIEDFL_ATTACHEDONLY);
        GC.KeepAlive(callback);
        return devices;
    }

    public static IntPtr OpenDevice(IntPtr directInput, Guid guidInstance, IntPtr windowHandle)
    {
        Guid instance = guidInstance;
        IntPtr device;
        if (GetVtMethod<VtDI8CreateDeviceFn>(directInput, DI8Slot.CreateDevice)(directInput, ref instance, out device, IntPtr.Zero) != 0 || device == IntPtr.Zero)
        {
            return IntPtr.Zero;
        }

        if (GetVtMethod<VtDI8DevSetDataFormatFn>(device, DI8DevSlot.SetDataFormat)(device, DfDIJoystick.pFormat) != 0)
        {
            ReleaseDevice(device);
            return IntPtr.Zero;
        }

        if (GetVtMethod<VtDI8DevSetCoopLevelFn>(device, DI8DevSlot.SetCooperativeLevel)(device, windowHandle, DISCL_NONEXCLUSIVE | DISCL_BACKGROUND) != 0)
        {
            ReleaseDevice(device);
            return IntPtr.Zero;
        }

        uint[] axisOffsets = { 0, 4, 8, 12, 16, 20, 24, 28 };
        for (int i = 0; i < axisOffsets.Length; i++)
        {
            SetAxisRange(device, axisOffsets[i], 0, 65535);
        }

        GetVtMethod<VtDI8DevAcquireFn>(device, DI8DevSlot.Acquire)(device);
        return device;
    }

    public static bool PollDevice(IntPtr device, out DIJOYSTATE state)
    {
        state = DefaultState();
        GetVtMethod<VtDI8DevPollFn>(device, DI8DevSlot.Poll)(device);
        IntPtr stateBuffer = Marshal.AllocHGlobal(80);
        try
        {
            int hr = GetVtMethod<VtDI8DevGetStateFn>(device, DI8DevSlot.GetDeviceState)(device, 80, stateBuffer);
            if (hr != 0)
            {
                GetVtMethod<VtDI8DevAcquireFn>(device, DI8DevSlot.Acquire)(device);
                hr = GetVtMethod<VtDI8DevGetStateFn>(device, DI8DevSlot.GetDeviceState)(device, 80, stateBuffer);
            }

            if (hr != 0)
            {
                return false;
            }

            state = (DIJOYSTATE)Marshal.PtrToStructure(stateBuffer, typeof(DIJOYSTATE));
            return true;
        }
        finally
        {
            Marshal.FreeHGlobal(stateBuffer);
        }
    }

    public static void ReleaseDevice(IntPtr device)
    {
        if (device != IntPtr.Zero)
        {
            GetVtMethod<VtReleaseFn>(device, DI8DevSlot.Release)(device);
        }
    }

    public static void ReleaseDI(IntPtr directInput)
    {
        if (directInput != IntPtr.Zero)
        {
            GetVtMethod<VtReleaseFn>(directInput, DI8Slot.Release)(directInput);
        }
    }

    public static DIJOYSTATE DefaultState()
    {
        return new DIJOYSTATE
        {
            rglSlider = new int[2],
            rgdwPOV = new uint[] { 0xFFFFFFFFu, 0xFFFFFFFFu, 0xFFFFFFFFu, 0xFFFFFFFFu },
            rgbButtons = new byte[32],
        };
    }

    private static T GetVtMethod<T>(IntPtr comPtr, int slot) where T : class
    {
        IntPtr vtable = Marshal.ReadIntPtr(comPtr);
        IntPtr fnPtr = Marshal.ReadIntPtr(vtable, slot * IntPtr.Size);
        return (T)(object)Marshal.GetDelegateForFunctionPointer(fnPtr, typeof(T));
    }

    private static void SetAxisRange(IntPtr device, uint offset, int min, int max)
    {
        DIPROPRANGE range = new DIPROPRANGE
        {
            diph = new DIPROPHEADER
            {
                dwSize = (uint)Marshal.SizeOf(typeof(DIPROPRANGE)),
                dwHeaderSize = (uint)Marshal.SizeOf(typeof(DIPROPHEADER)),
                dwObj = offset,
                dwHow = DIPH_BYOFFSET,
            },
            lMin = min,
            lMax = max,
        };

        IntPtr rangeBuffer = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(DIPROPRANGE)));
        try
        {
            Marshal.StructureToPtr(range, rangeBuffer, false);
            GetVtMethod<VtDI8DevSetPropertyFn>(device, DI8DevSlot.SetProperty)(device, DipropRange, rangeBuffer);
        }
        finally
        {
            Marshal.FreeHGlobal(rangeBuffer);
        }
    }

    private static HashSet<uint> GetXInputVidPids()
    {
        HashSet<uint> result = new HashSet<uint>();
        IntPtr deviceInfoSet = SetupDiGetClassDevs(IntPtr.Zero, "HID", IntPtr.Zero, 0x00000002u | 0x00000004u);
        if (deviceInfoSet == new IntPtr(-1))
        {
            return result;
        }

        try
        {
            SP_DEVINFO_DATA deviceInfo = new SP_DEVINFO_DATA();
            deviceInfo.cbSize = (uint)Marshal.SizeOf(typeof(SP_DEVINFO_DATA));
            for (uint i = 0; SetupDiEnumDeviceInfo(deviceInfoSet, i, ref deviceInfo); i++)
            {
                StringBuilder instanceId = new StringBuilder(512);
                uint requiredSize;
                if (!SetupDiGetDeviceInstanceId(deviceInfoSet, ref deviceInfo, instanceId, (uint)instanceId.Capacity, out requiredSize))
                {
                    continue;
                }

                string path = instanceId.ToString().ToUpperInvariant();
                if (!path.Contains("IG_"))
                {
                    continue;
                }

                int vidIndex = path.IndexOf("VID_");
                int pidIndex = path.IndexOf("PID_");
                if (vidIndex < 0 || pidIndex < 0)
                {
                    continue;
                }

                uint vid;
                uint pid;
                if (!uint.TryParse(path.Substring(vidIndex + 4, 4), System.Globalization.NumberStyles.HexNumber, null, out vid))
                {
                    continue;
                }

                if (!uint.TryParse(path.Substring(pidIndex + 4, 4), System.Globalization.NumberStyles.HexNumber, null, out pid))
                {
                    continue;
                }

                result.Add((pid << 16) | vid);
            }
        }
        finally
        {
            SetupDiDestroyDeviceInfoList(deviceInfoSet);
        }

        return result;
    }
}

internal static class DfDIJoystick
{
    private const uint DIDFT_AXIS = 0x00000003;
    private const uint DIDFT_BUTTON = 0x0000000C;
    private const uint DIDFT_POV = 0x00000010;
    private const uint DIDFT_ANYINSTANCE = 0x00FFFF00;
    private const uint DIDFT_OPTIONAL = 0x80000000;
    private const uint DIDF_ABSAXIS = 0x00000001;

    public static readonly IntPtr pFormat;

    static DfDIJoystick()
    {
        IntPtr guidX = AllocGuid(new Guid("A36D02E0-C9F3-11CF-BFC7-444553540000"));
        IntPtr guidY = AllocGuid(new Guid("A36D02E1-C9F3-11CF-BFC7-444553540000"));
        IntPtr guidZ = AllocGuid(new Guid("A36D02E2-C9F3-11CF-BFC7-444553540000"));
        IntPtr guidRx = AllocGuid(new Guid("A36D02F4-C9F3-11CF-BFC7-444553540000"));
        IntPtr guidRy = AllocGuid(new Guid("A36D02F5-C9F3-11CF-BFC7-444553540000"));
        IntPtr guidRz = AllocGuid(new Guid("A36D02E3-C9F3-11CF-BFC7-444553540000"));
        IntPtr guidSlider = AllocGuid(new Guid("A36D02E4-C9F3-11CF-BFC7-444553540000"));
        IntPtr guidPov = AllocGuid(new Guid("A36D02F2-C9F3-11CF-BFC7-444553540000"));

        uint requiredAxis = DIDFT_AXIS | DIDFT_ANYINSTANCE;
        uint optionalAxis = DIDFT_OPTIONAL | DIDFT_AXIS | DIDFT_ANYINSTANCE;
        uint optionalButton = DIDFT_OPTIONAL | DIDFT_BUTTON | DIDFT_ANYINSTANCE;
        uint optionalPov = DIDFT_OPTIONAL | DIDFT_POV | DIDFT_ANYINSTANCE;

        List<DIOBJECTDATAFORMAT> objects = new List<DIOBJECTDATAFORMAT>
        {
            new DIOBJECTDATAFORMAT { pguid = guidX, dwOfs = 0, dwType = requiredAxis, dwFlags = 0 },
            new DIOBJECTDATAFORMAT { pguid = guidY, dwOfs = 4, dwType = requiredAxis, dwFlags = 0 },
            new DIOBJECTDATAFORMAT { pguid = guidZ, dwOfs = 8, dwType = optionalAxis, dwFlags = 0 },
            new DIOBJECTDATAFORMAT { pguid = guidRx, dwOfs = 12, dwType = optionalAxis, dwFlags = 0 },
            new DIOBJECTDATAFORMAT { pguid = guidRy, dwOfs = 16, dwType = optionalAxis, dwFlags = 0 },
            new DIOBJECTDATAFORMAT { pguid = guidRz, dwOfs = 20, dwType = optionalAxis, dwFlags = 0 },
            new DIOBJECTDATAFORMAT { pguid = guidSlider, dwOfs = 24, dwType = optionalAxis, dwFlags = 0 },
            new DIOBJECTDATAFORMAT { pguid = guidSlider, dwOfs = 28, dwType = optionalAxis, dwFlags = 0 },
            new DIOBJECTDATAFORMAT { pguid = guidPov, dwOfs = 32, dwType = optionalPov, dwFlags = 0 },
            new DIOBJECTDATAFORMAT { pguid = guidPov, dwOfs = 36, dwType = optionalPov, dwFlags = 0 },
            new DIOBJECTDATAFORMAT { pguid = guidPov, dwOfs = 40, dwType = optionalPov, dwFlags = 0 },
            new DIOBJECTDATAFORMAT { pguid = guidPov, dwOfs = 44, dwType = optionalPov, dwFlags = 0 },
        };

        for (int i = 0; i < 32; i++)
        {
            objects.Add(new DIOBJECTDATAFORMAT
            {
                pguid = IntPtr.Zero,
                dwOfs = (uint)(48 + i),
                dwType = optionalButton,
                dwFlags = 0,
            });
        }

        int objectSize = Marshal.SizeOf(typeof(DIOBJECTDATAFORMAT));
        IntPtr objectArray = Marshal.AllocHGlobal(objects.Count * objectSize);
        for (int i = 0; i < objects.Count; i++)
        {
            Marshal.StructureToPtr(objects[i], IntPtr.Add(objectArray, i * objectSize), false);
        }

        DIDATAFORMAT format = new DIDATAFORMAT
        {
            dwSize = (uint)Marshal.SizeOf(typeof(DIDATAFORMAT)),
            dwObjSize = (uint)objectSize,
            dwFlags = DIDF_ABSAXIS,
            dwDataSize = 80,
            dwNumObjs = (uint)objects.Count,
            rgodf = objectArray,
        };

        pFormat = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(DIDATAFORMAT)));
        Marshal.StructureToPtr(format, pFormat, false);
    }

    private static IntPtr AllocGuid(Guid guid)
    {
        IntPtr buffer = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(Guid)));
        Marshal.StructureToPtr(guid, buffer, false);
        return buffer;
    }
}
