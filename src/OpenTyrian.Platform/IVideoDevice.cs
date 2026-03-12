using System;

namespace OpenTyrian.Platform;

public interface IVideoDevice
{
    int Width { get; }
    int Height { get; }
    uint[] LockFrame();
    void Present();
}
