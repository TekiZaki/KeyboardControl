// ---
// Summary:
// - Purpose: Global keyboard hook manager for volume and brightness shortcuts.
// - Role: Input handling component.
// - Used by: MainForm.
// - Depends on: user32.dll, VolumeControl, BrightnessControl.
// - Key Responsibilities: Non-blocking capture of Alt and Ctrl shortcuts for volume/brightness increments.
// - Notes: Uses WH_KEYBOARD_LL low-level Windows hook with asynchronous dispatch.
// ---

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace KeyboardControl.Controls
{
    public class HotkeyManager : IDisposable
    {
        public delegate void HotkeyChangedHandler(string controlType, int value);
        public event HotkeyChangedHandler OnChange;

        private readonly VolumeControl _volumeControl;
        private readonly BrightnessControl _brightnessControl;
        private readonly int _step = 2;

        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_SYSKEYDOWN = 0x0104;

        private const int VK_CONTROL = 0x11;
        private const int VK_LCONTROL = 0xA2;
        private const int VK_RCONTROL = 0xA3;
        private const int VK_MENU = 0x12;
        private const int VK_LMENU = 0xA4;
        private const int VK_RMENU = 0xA5;
        private const int VK_LWIN = 0x5B;
        private const int VK_RWIN = 0x5C;

        private const int VK_LEFT = 0x25;
        private const int VK_UP = 0x26;
        private const int VK_RIGHT = 0x27;
        private const int VK_DOWN = 0x28;
        private const int VK_OEM_PLUS = 0xBB;
        private const int VK_ADD = 0x6B;
        private const int VK_OEM_MINUS = 0xBD;
        private const int VK_SUBTRACT = 0x6D;
        private const int VK_OEM_4 = 0xDB; // [ and {
        private const int VK_OEM_6 = 0xDD; // ] and }

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
        private LowLevelKeyboardProc _proc;
        private IntPtr _hookId = IntPtr.Zero;

        public HotkeyManager(VolumeControl volumeControl, BrightnessControl brightnessControl)
        {
            _volumeControl = volumeControl;
            _brightnessControl = brightnessControl;
        }

        public void Setup()
        {
            _proc = HookCallback;
            using (var curProcess = Process.GetCurrentProcess())
            using (var curModule = curProcess.MainModule)
            {
                _hookId = SetWindowsHookEx(WH_KEYBOARD_LL, _proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN))
            {
                int vkCode = Marshal.ReadInt32(lParam);
                bool isAlt = IsAltPressed();
                bool isCtrl = IsCtrlPressed();

                // 1. Alt Hotkeys:
                // - Brightness: Alt + (Left/Right)
                // - Volume: Alt + (+/- or Up/Down)
                if (isAlt && !isCtrl)
                {
                    if (vkCode == VK_RIGHT)
                    {
                        ThreadPool.QueueUserWorkItem(delegate { IncreaseBrightness(); });
                        return (IntPtr)1;
                    }
                    if (vkCode == VK_LEFT)
                    {
                        ThreadPool.QueueUserWorkItem(delegate { DecreaseBrightness(); });
                        return (IntPtr)1;
                    }
                    if (vkCode == VK_OEM_PLUS || vkCode == VK_ADD || vkCode == VK_UP)
                    {
                        ThreadPool.QueueUserWorkItem(delegate { IncreaseVolume(); });
                        return (IntPtr)1;
                    }
                    if (vkCode == VK_OEM_MINUS || vkCode == VK_SUBTRACT || vkCode == VK_DOWN)
                    {
                        ThreadPool.QueueUserWorkItem(delegate { DecreaseVolume(); });
                        return (IntPtr)1;
                    }
                }
                // 2. Ctrl Hotkeys:
                // - Brightness: Ctrl + (] / [)
                else if (isCtrl && !isAlt)
                {
                    if (vkCode == VK_OEM_6)
                    {
                        ThreadPool.QueueUserWorkItem(delegate { IncreaseBrightness(); });
                        return (IntPtr)1;
                    }
                    if (vkCode == VK_OEM_4)
                    {
                        ThreadPool.QueueUserWorkItem(delegate { DecreaseBrightness(); });
                        return (IntPtr)1;
                    }
                }
            }

            return CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        private bool IsAltPressed()
        {
            return (GetAsyncKeyState(VK_MENU) & 0x8000) != 0 ||
                   (GetAsyncKeyState(VK_LMENU) & 0x8000) != 0 ||
                   (GetAsyncKeyState(VK_RMENU) & 0x8000) != 0 ||
                   (GetAsyncKeyState(VK_LWIN) & 0x8000) != 0 ||
                   (GetAsyncKeyState(VK_RWIN) & 0x8000) != 0;
        }

        private bool IsCtrlPressed()
        {
            return (GetAsyncKeyState(VK_CONTROL) & 0x8000) != 0 ||
                   (GetAsyncKeyState(VK_LCONTROL) & 0x8000) != 0 ||
                   (GetAsyncKeyState(VK_RCONTROL) & 0x8000) != 0;
        }

        public void IncreaseVolume()
        {
            int current = _volumeControl.GetCurrentPercent();
            int newVal = Math.Min(100, current + _step);
            int res = _volumeControl.SetVolume(newVal);
            if (OnChange != null)
            {
                OnChange("volume", res);
            }
        }

        public void DecreaseVolume()
        {
            int current = _volumeControl.GetCurrentPercent();
            int newVal = Math.Max(0, current - _step);
            int res = _volumeControl.SetVolume(newVal);
            if (OnChange != null)
            {
                OnChange("volume", res);
            }
        }

        public void IncreaseBrightness()
        {
            int current = _brightnessControl.GetCurrent();
            int newVal = Math.Min(100, current + _step);
            _brightnessControl.Set(newVal);
            if (OnChange != null)
            {
                OnChange("brightness", newVal);
            }
        }

        public void DecreaseBrightness()
        {
            int current = _brightnessControl.GetCurrent();
            int newVal = Math.Max(0, current - _step);
            _brightnessControl.Set(newVal);
            if (OnChange != null)
            {
                OnChange("brightness", newVal);
            }
        }

        public void Dispose()
        {
            if (_hookId != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookId);
                _hookId = IntPtr.Zero;
            }
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);
    }
}
