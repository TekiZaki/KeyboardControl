# KeyboardControl

A lightweight Windows utility for global volume and screen brightness control via keyboard shortcuts with a non-intrusive on-screen display (OSD) flyout.

Targeted at Windows 10/11 running .NET Framework 4.8 with zero external SDK dependencies.

## Shortcuts

### Screen Brightness
- **Increase Brightness:** `Alt + Right Arrow`
- **Decrease Brightness:** `Alt + Left Arrow`

### Master Volume
- **Increase Volume:** `Alt + Up Arrow`
- **Decrease Volume:** `Alt + Down Arrow`

## Features

- **Global Low-Level Hook:** Captures key combinations asynchronously across any active application via Win32 WH_KEYBOARD_LL.
- **Core Audio COM Interop:** Direct master volume and mute adjustments via IAudioEndpointVolume.
- **Dual-Mode Brightness Engine:** Adjusts internal display panels via WMI (WmiMonitorBrightnessMethods) and external DDC/CI monitors via DXVA2 (SetMonitorBrightness).
- **Minimal Floating OSD:** Non-activating topmost flyout overlay displaying level indicators and auto-dismissing after 1.2 seconds.
- **Zero-Dependency Build:** Can be compiled directly using the native Windows C# compiler (csc.exe) without installing Visual Studio or .NET SDKs.

## Building from Source

Run the included build script in Command Prompt or PowerShell:

`cmd
build.bat
`

The compiled binary will be placed at in\Release\KeyboardControl.exe.

## Requirements

- Windows 10 or Windows 11
- .NET Framework 4.8 (pre-installed on modern Windows builds)
