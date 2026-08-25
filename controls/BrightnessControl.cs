// ---
// Summary:
// - Purpose: Windows screen brightness controller.
// - Role: Display hardware abstraction.
// - Used by: HotkeyManager and MainForm.
// - Depends on: System.Management, dxva2.dll, gdi32.dll, user32.dll.
// - Key Responsibilities: Query and update monitor brightness percentage.
// - Notes: Uses WMI for integrated panels, DXVA2 for DDC/CI external displays, and Gamma Ramp as universal fallback.
// ---

using System;
using System.Management;
using System.Runtime.InteropServices;

namespace KeyboardControl.Controls
{
    public class BrightnessControl
    {
        private int _cachedBrightness = 50;
        private readonly object _lockObj = new object();

        [DllImport("user32.dll")]
        private static extern IntPtr GetDesktopWindow();

        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

        [DllImport("dxva2.dll", SetLastError = true)]
        private static extern bool GetNumberOfPhysicalMonitorsFromHMONITOR(IntPtr hMonitor, out uint pdwNumberOfPhysicalMonitors);

        [DllImport("dxva2.dll", SetLastError = true)]
        private static extern bool GetPhysicalMonitorsFromHMONITOR(IntPtr hMonitor, uint dwPhysicalMonitorArraySize, [Out] PHYSICAL_MONITOR[] pPhysicalMonitorArray);

        [DllImport("dxva2.dll", SetLastError = true)]
        private static extern bool DestroyPhysicalMonitors(uint dwPhysicalMonitorArraySize, [In] PHYSICAL_MONITOR[] pPhysicalMonitorArray);

        [DllImport("dxva2.dll", SetLastError = true)]
        private static extern bool GetMonitorBrightness(IntPtr hMonitor, out uint pdwMinimumBrightness, out uint pdwCurrentBrightness, out uint pdwMaximumBrightness);

        [DllImport("dxva2.dll", SetLastError = true)]
        private static extern bool SetMonitorBrightness(IntPtr hMonitor, uint dwNewBrightness);

        [DllImport("gdi32.dll")]
        private static extern bool SetDeviceGammaRamp(IntPtr hdc, ref RAMP lpRamp);

        [DllImport("gdi32.dll")]
        private static extern bool GetDeviceGammaRamp(IntPtr hdc, ref RAMP lpRamp);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct PHYSICAL_MONITOR
        {
            public IntPtr hPhysicalMonitor;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string szPhysicalMonitorDescription;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        private struct RAMP
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
            public ushort[] Red;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
            public ushort[] Green;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
            public ushort[] Blue;
        }

        public BrightnessControl()
        {
            int? hwBri = QueryHardwareBrightness();
            if (hwBri.HasValue)
            {
                _cachedBrightness = hwBri.Value;
            }
        }

        public int GetCurrent()
        {
            lock (_lockObj)
            {
                return _cachedBrightness;
            }
        }

        public int? Set(int brightness)
        {
            int clamped = Math.Max(0, Math.Min(100, brightness));

            lock (_lockObj)
            {
                _cachedBrightness = clamped;
            }

            // Apply hardware change
            try
            {
                // 1. WMI (Laptop/Integrated panels)
                if (!SetWmiBrightness(clamped))
                {
                    // 2. DXVA2 (DDC/CI External monitors)
                    if (!SetDxvaBrightness((uint)clamped))
                    {
                        // 3. Gamma Ramp Fallback (Universal)
                        SetGammaBrightness(clamped);
                    }
                }
            }
            catch
            {
            }

            return clamped;
        }

        public int? QueryHardwareBrightness()
        {
            // 1. Try WMI
            try
            {
                using (var searcher = new ManagementObjectSearcher(@"root\wmi", "SELECT CurrentBrightness FROM WmiMonitorBrightness"))
                using (var collection = searcher.Get())
                {
                    foreach (ManagementObject obj in collection)
                    {
                        var val = obj["CurrentBrightness"];
                        if (val != null)
                        {
                            int res = Convert.ToInt32(val);
                            lock (_lockObj)
                            {
                                _cachedBrightness = res;
                            }
                            return res;
                        }
                    }
                }
            }
            catch
            {
            }

            // 2. Try DXVA2
            int? dxva = GetDxvaBrightness();
            if (dxva.HasValue)
            {
                lock (_lockObj)
                {
                    _cachedBrightness = dxva.Value;
                }
                return dxva.Value;
            }

            return null;
        }

        private bool SetWmiBrightness(int targetBrightness)
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher(@"root\wmi", "SELECT * FROM WmiMonitorBrightnessMethods"))
                using (var collection = searcher.Get())
                {
                    bool applied = false;
                    foreach (ManagementObject obj in collection)
                    {
                        using (var inParams = obj.GetMethodParameters("WmiSetBrightness"))
                        {
                            inParams["Timeout"] = (uint)1;
                            inParams["Brightness"] = (byte)targetBrightness;
                            obj.InvokeMethod("WmiSetBrightness", inParams, null);
                            applied = true;
                        }
                    }
                    return applied;
                }
            }
            catch
            {
                return false;
            }
        }

        private int? GetDxvaBrightness()
        {
            try
            {
                IntPtr hDesktop = GetDesktopWindow();
                IntPtr hMonitor = MonitorFromWindow(hDesktop, 1 /* MONITOR_DEFAULTTOPRIMARY */);
                if (hMonitor == IntPtr.Zero)
                {
                    return null;
                }

                uint count;
                if (!GetNumberOfPhysicalMonitorsFromHMONITOR(hMonitor, out count) || count == 0)
                {
                    return null;
                }

                var monitors = new PHYSICAL_MONITOR[count];
                if (!GetPhysicalMonitorsFromHMONITOR(hMonitor, count, monitors))
                {
                    return null;
                }

                try
                {
                    uint min, current, max;
                    if (GetMonitorBrightness(monitors[0].hPhysicalMonitor, out min, out current, out max))
                    {
                        return (int)current;
                    }
                }
                finally
                {
                    DestroyPhysicalMonitors(count, monitors);
                }
            }
            catch
            {
            }

            return null;
        }

        private bool SetDxvaBrightness(uint value)
        {
            try
            {
                IntPtr hDesktop = GetDesktopWindow();
                IntPtr hMonitor = MonitorFromWindow(hDesktop, 1 /* MONITOR_DEFAULTTOPRIMARY */);
                if (hMonitor == IntPtr.Zero)
                {
                    return false;
                }

                uint count;
                if (!GetNumberOfPhysicalMonitorsFromHMONITOR(hMonitor, out count) || count == 0)
                {
                    return false;
                }

                var monitors = new PHYSICAL_MONITOR[count];
                if (!GetPhysicalMonitorsFromHMONITOR(hMonitor, count, monitors))
                {
                    return false;
                }

                bool success = false;
                try
                {
                    for (int i = 0; i < count; i++)
                    {
                        if (SetMonitorBrightness(monitors[i].hPhysicalMonitor, value))
                        {
                            success = true;
                        }
                    }
                }
                finally
                {
                    DestroyPhysicalMonitors(count, monitors);
                }

                return success;
            }
            catch
            {
                return false;
            }
        }

        private bool SetGammaBrightness(int brightness)
        {
            try
            {
                IntPtr hdc = GetDC(IntPtr.Zero);
                if (hdc == IntPtr.Zero) return false;

                try
                {
                    var ramp = new RAMP();
                    ramp.Red = new ushort[256];
                    ramp.Green = new ushort[256];
                    ramp.Blue = new ushort[256];

                    // Brightness multiplier: 0 to 100 -> factor 0.2 to 1.8
                    double factor = (brightness + 40) / 140.0;

                    for (int i = 0; i < 256; i++)
                    {
                        int val = (int)(i * 256 * factor);
                        if (val > 65535) val = 65535;
                        if (val < 0) val = 0;
                        ramp.Red[i] = (ushort)val;
                        ramp.Green[i] = (ushort)val;
                        ramp.Blue[i] = (ushort)val;
                    }

                    return SetDeviceGammaRamp(hdc, ref ramp);
                }
                finally
                {
                    ReleaseDC(IntPtr.Zero, hdc);
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
