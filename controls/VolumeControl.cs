// ---
// Summary:
// - Purpose: Master volume controller via Windows Core Audio API.
// - Role: Audio hardware abstraction.
// - Used by: HotkeyManager and MainForm.
// - Depends on: System.Runtime.InteropServices.
// - Key Responsibilities: Query and set master volume scalar and mute state.
// - Notes: Implements IMMDeviceEnumerator and IAudioEndpointVolume COM interfaces.
// ---

using System;
using System.Runtime.InteropServices;

namespace KeyboardControl.Controls
{
    public class VolumeControl : IDisposable
    {
        private IAudioEndpointVolume _volumeEndpoint;

        public VolumeControl()
        {
            InitializeEndpoint();
        }

        private void InitializeEndpoint()
        {
            var enumerator = (IMMDeviceEnumerator)new MMDeviceEnumerator();
            IMMDevice device;
            enumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia, out device);
            var iid = typeof(IAudioEndpointVolume).GUID;
            object endpointObj;
            device.Activate(ref iid, 1, IntPtr.Zero, out endpointObj);
            _volumeEndpoint = (IAudioEndpointVolume)endpointObj;
        }

        public int GetCurrentPercent()
        {
            try
            {
                if (_volumeEndpoint == null)
                {
                    InitializeEndpoint();
                }
                float level;
                _volumeEndpoint.GetMasterVolumeLevelScalar(out level);
                return (int)Math.Round(level * 100f);
            }
            catch
            {
                return 0;
            }
        }

        public int SetVolume(int volumePercent)
        {
            var clamped = Math.Max(0, Math.Min(100, volumePercent));
            try
            {
                if (_volumeEndpoint == null)
                {
                    InitializeEndpoint();
                }
                var scalar = clamped / 100f;
                var guid = Guid.Empty;
                _volumeEndpoint.SetMasterVolumeLevelScalar(scalar, ref guid);
                return clamped;
            }
            catch
            {
                return clamped;
            }
        }

        public bool IsMuted()
        {
            try
            {
                if (_volumeEndpoint == null)
                {
                    InitializeEndpoint();
                }
                bool isMuted;
                _volumeEndpoint.GetMute(out isMuted);
                return isMuted;
            }
            catch
            {
                return false;
            }
        }

        public bool ToggleMute()
        {
            try
            {
                if (_volumeEndpoint == null)
                {
                    InitializeEndpoint();
                }
                var currentMute = IsMuted();
                var guid = Guid.Empty;
                _volumeEndpoint.SetMute(!currentMute, ref guid);
                return !currentMute;
            }
            catch
            {
                return false;
            }
        }

        public void Dispose()
        {
            if (_volumeEndpoint != null)
            {
                Marshal.ReleaseComObject(_volumeEndpoint);
                _volumeEndpoint = null;
            }
        }

        [ComImport]
        [Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
        private class MMDeviceEnumerator
        {
        }

        private enum EDataFlow
        {
            eRender = 0,
            eCapture = 1,
            eAll = 2
        }

        private enum ERole
        {
            eConsole = 0,
            eMultimedia = 1,
            eCommunications = 2
        }

        [Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IMMDeviceEnumerator
        {
            int EnumAudioEndpoints(EDataFlow dataFlow, int stateMask, out IntPtr devices);
            int GetDefaultAudioEndpoint(EDataFlow dataFlow, ERole role, out IMMDevice endpoint);
            int GetDevice(string pwstrId, out IMMDevice endpoint);
            int RegisterEndpointNotificationCallback(IntPtr client);
            int UnregisterEndpointNotificationCallback(IntPtr client);
        }

        [Guid("D666063F-1587-4E43-81F1-B948E807363F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IMMDevice
        {
            int Activate(ref Guid id, int clsCtx, IntPtr activationParams, [MarshalAs(UnmanagedType.IUnknown)] out object interfacePointer);
            int OpenPropertyStore(int stgmAccess, out IntPtr properties);
            int GetId([MarshalAs(UnmanagedType.LPWStr)] out string strId);
            int GetState(out int state);
        }

        [Guid("5CDF2C82-841E-4546-9722-0CF74078229A"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IAudioEndpointVolume
        {
            int RegisterControlChangeNotify(IntPtr notify);
            int UnregisterControlChangeNotify(IntPtr notify);
            int GetChannelCount(out uint channelCount);
            int SetMasterVolumeLevel(float levelDb, ref Guid eventContextGuid);
            int SetMasterVolumeLevelScalar(float level, ref Guid eventContextGuid);
            int GetMasterVolumeLevel(out float levelDb);
            int GetMasterVolumeLevelScalar(out float level);
            int SetChannelVolumeLevel(uint channelNumber, float levelDb, ref Guid eventContextGuid);
            int SetChannelVolumeLevelScalar(uint channelNumber, float level, ref Guid eventContextGuid);
            int GetChannelVolumeLevel(uint channelNumber, out float levelDb);
            int GetChannelVolumeLevelScalar(uint channelNumber, out float level);
            int SetMute([MarshalAs(UnmanagedType.Bool)] bool isMuted, ref Guid eventContextGuid);
            int GetMute([MarshalAs(UnmanagedType.Bool)] out bool isMuted);
            int GetVolumeStepInfo(out uint step, out uint stepCount);
            int VolumeStepUp(ref Guid eventContextGuid);
            int VolumeStepDown(ref Guid eventContextGuid);
            int QueryHardwareSupport(out uint hardwareSupportMask);
            int GetVolumeRange(out float volumeMinDb, out float volumeMaxDb, out float volumeIncrementDb);
        }
    }
}
