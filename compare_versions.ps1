param(
    [string]$OutputDir = "temp\\compare-versions"
)

Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Windows.Forms

Add-Type @"
using System;
using System.Runtime.InteropServices;

public static class NativeWindowTools
{
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    public static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);

    [DllImport("user32.dll")]
    public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint flags);

    [DllImport("user32.dll")]
    public static extern bool PrintWindow(IntPtr hwnd, IntPtr hDC, uint nFlags);

    [DllImport("user32.dll")]
    public static extern IntPtr PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    public static extern uint MapVirtualKey(uint uCode, uint uMapType);
}
"@

function Wait-ForMainWindow {
    param(
        [System.Diagnostics.Process]$Process,
        [int]$TimeoutSeconds = 20
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        if ($Process.HasExited) {
            throw "Process exited before creating a main window: $($Process.StartInfo.FileName)"
        }

        $Process.Refresh()
        if ($Process.MainWindowHandle -ne 0) {
            return $Process.MainWindowHandle
        }

        Start-Sleep -Milliseconds 250
    }

    throw "Timed out waiting for main window: $($Process.StartInfo.FileName)"
}

function Focus-Window {
    param(
        [System.Diagnostics.Process]$Process,
        [object]$Shell
    )

    $handle = Wait-ForMainWindow -Process $Process
    [NativeWindowTools]::SetWindowPos($handle, [IntPtr]::Zero, 40, 40, 0, 0, 0x0045) | Out-Null
    [NativeWindowTools]::ShowWindowAsync($handle, 5) | Out-Null
    Start-Sleep -Milliseconds 250
    [NativeWindowTools]::SetForegroundWindow($handle) | Out-Null
    Start-Sleep -Milliseconds 250

    if (-not $Shell.AppActivate($Process.Id)) {
        $Process.Refresh()
        if ($Process.MainWindowTitle) {
            $Shell.AppActivate($Process.MainWindowTitle) | Out-Null
        }
    }

    Start-Sleep -Milliseconds 400
}

function Save-WindowScreenshot {
    param(
        [System.Diagnostics.Process]$Process,
        [string]$Path
    )

    $Process.Refresh()
    $handle = Wait-ForMainWindow -Process $Process
    $rect = New-Object NativeWindowTools+RECT
    if (-not [NativeWindowTools]::GetWindowRect($handle, [ref]$rect)) {
        throw "GetWindowRect failed for $($Process.ProcessName)"
    }

    $width = [Math]::Max(1, $rect.Right - $rect.Left)
    $height = [Math]::Max(1, $rect.Bottom - $rect.Top)

    $bitmap = New-Object System.Drawing.Bitmap $width, $height
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $hdc = [IntPtr]::Zero

    try {
        $hdc = $graphics.GetHdc()
        $printed = [NativeWindowTools]::PrintWindow($handle, $hdc, 0)
        $graphics.ReleaseHdc($hdc)
        $hdc = [IntPtr]::Zero

        if (-not $printed) {
            $graphics.CopyFromScreen($rect.Left, $rect.Top, 0, 0, $bitmap.Size)
        }

        $bitmap.Save($Path, [System.Drawing.Imaging.ImageFormat]::Png)
    }
    finally {
        if ($hdc -ne [IntPtr]::Zero) {
            $graphics.ReleaseHdc($hdc)
        }

        $graphics.Dispose()
        $bitmap.Dispose()
    }
}

function Send-KeySequence {
    param(
        [System.Diagnostics.Process]$Process,
        [object]$Shell,
        [string[]]$Keys,
        [int]$DelayMilliseconds = 1200
    )

    Focus-Window -Process $Process -Shell $Shell
    foreach ($key in $Keys) {
        $virtualKey = switch ($key) {
            "Enter" { 0x0D }
            "Down" { 0x28 }
            "Up" { 0x26 }
            "Escape" { 0x1B }
            default { throw "Unsupported key: $key" }
        }

        $handle = Wait-ForMainWindow -Process $Process
        $scanCode = [NativeWindowTools]::MapVirtualKey([uint32]$virtualKey, 0)
        $extendedFlag = if ($key -in @("Down", "Up")) { 0x01000000 } else { 0 }
        $keyDownLParam = [IntPtr](1 -bor ($scanCode -shl 16) -bor $extendedFlag)
        $keyUpLParam = [IntPtr](1 -bor ($scanCode -shl 16) -bor $extendedFlag -bor 0xC0000000)

        [NativeWindowTools]::PostMessage($handle, 0x0100, [IntPtr]$virtualKey, $keyDownLParam) | Out-Null
        Start-Sleep -Milliseconds 80
        [NativeWindowTools]::PostMessage($handle, 0x0101, [IntPtr]$virtualKey, $keyUpLParam) | Out-Null
        Start-Sleep -Milliseconds 400
    }

    Start-Sleep -Milliseconds $DelayMilliseconds
}

function Stop-Application {
    param(
        [System.Diagnostics.Process]$Process
    )

    if ($Process.HasExited) {
        return
    }

    $Process.CloseMainWindow() | Out-Null
    if (-not $Process.WaitForExit(3000)) {
        $Process.Kill()
        $Process.WaitForExit()
    }
}

function Invoke-AppCapture {
    param(
        [string]$Name,
        [string]$ExecutablePath,
        [string]$WorkingDirectory,
        [string]$OutputRoot
    )

    $shell = New-Object -ComObject WScript.Shell
    $appDir = Join-Path $OutputRoot $Name
    New-Item -ItemType Directory -Force -Path $appDir | Out-Null

    $process = Start-Process -FilePath $ExecutablePath -WorkingDirectory $WorkingDirectory -PassThru

    try {
        Focus-Window -Process $process -Shell $shell
        Start-Sleep -Milliseconds 2000
        Save-WindowScreenshot -Process $process -Path (Join-Path $appDir "01-start.png")

        Send-KeySequence -Process $process -Shell $shell -Keys @("Enter") -DelayMilliseconds 1800
        Save-WindowScreenshot -Process $process -Path (Join-Path $appDir "02-after-enter.png")

        Send-KeySequence -Process $process -Shell $shell -Keys @("Down", "Enter") -DelayMilliseconds 1800
        Save-WindowScreenshot -Process $process -Path (Join-Path $appDir "03-after-menu-action.png")
    }
    finally {
        Stop-Application -Process $process
    }
}

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$outputRoot = Join-Path $root $OutputDir
New-Item -ItemType Directory -Force -Path $outputRoot | Out-Null

$originalExe = Join-Path $root "opentyrian-master\visualc\build\x64\Release\opentyrian-x64-Release.exe"
$dotNetExe = Join-Path $root "build\OpenTyrian.WinForms.exe"

if (-not (Test-Path $originalExe)) {
    throw "Original C executable not found: $originalExe"
}

if (-not (Test-Path $dotNetExe)) {
    throw "C# executable not found: $dotNetExe"
}

Invoke-AppCapture -Name "original-c" -ExecutablePath $originalExe -WorkingDirectory (Split-Path -Parent $originalExe) -OutputRoot $outputRoot
Invoke-AppCapture -Name "csharp-port" -ExecutablePath $dotNetExe -WorkingDirectory (Split-Path -Parent $dotNetExe) -OutputRoot $outputRoot

Write-Host "Screenshots written to $outputRoot"
