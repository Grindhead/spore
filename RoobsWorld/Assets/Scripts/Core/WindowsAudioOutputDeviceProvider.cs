using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace RoobsWorld.Core
{
    /// <summary>
    /// Enumerates active Windows audio output endpoints (speakers/headsets) via Core Audio MMDevice API.
    /// </summary>
    public static class WindowsAudioOutputDeviceProvider
    {
        public static string[] GetAvailableOutputs()
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            try
            {
                var names = EnumerateWindowsRenderDeviceNames();
                if (names.Count > 0)
                    return names.ToArray();
            }
            catch
            {
                // Fall through to default when COM lookup fails.
            }
#endif
            return new[] { Localization.Get("settings.audio_output_system_default") };
        }

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        private static List<string> EnumerateWindowsRenderDeviceNames()
        {
            const int DEVICE_STATE_ACTIVE = 0x00000001;
            const int EDataFlow_eRender = 0;
            const int STGM_READ = 0x00000000;
            const int PKEY_Device_FriendlyName_PID = 14;

            var names = new List<string>();
            IMMDeviceEnumerator enumerator = null;
            IMMDeviceCollection collection = null;
            try
            {
                var type = Type.GetTypeFromCLSID(new Guid("BCDE0395-E52F-467C-8E3D-C4579291692E"));
                enumerator = (IMMDeviceEnumerator)Activator.CreateInstance(type);
                Marshal.ThrowExceptionForHR(enumerator.EnumAudioEndpoints(EDataFlow_eRender, DEVICE_STATE_ACTIVE, out collection));
                Marshal.ThrowExceptionForHR(collection.GetCount(out uint count));

                var key = new PROPERTYKEY
                {
                    fmtid = new Guid("A45C254E-DF1C-4EFD-8020-67D146A850E0"),
                    pid = PKEY_Device_FriendlyName_PID
                };

                for (uint i = 0; i < count; i++)
                {
                    IMMDevice device = null;
                    IPropertyStore props = null;
                    try
                    {
                        Marshal.ThrowExceptionForHR(collection.Item(i, out device));
                        Marshal.ThrowExceptionForHR(device.OpenPropertyStore(STGM_READ, out props));
                        Marshal.ThrowExceptionForHR(props.GetValue(ref key, out PROPVARIANT value));
                        string name = value.GetValueAsString();
                        value.Clear();

                        if (!string.IsNullOrWhiteSpace(name))
                            names.Add(name);
                    }
                    finally
                    {
                        if (props != null) Marshal.ReleaseComObject(props);
                        if (device != null) Marshal.ReleaseComObject(device);
                    }
                }
            }
            finally
            {
                if (collection != null) Marshal.ReleaseComObject(collection);
                if (enumerator != null) Marshal.ReleaseComObject(enumerator);
            }

            return names;
        }

        [ComImport]
        [Guid("A95664D2-9614-4F35-A746-DE8DB63617E6")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IMMDeviceEnumerator
        {
            int NotImpl1();
            int EnumAudioEndpoints(int dataFlow, int dwStateMask, out IMMDeviceCollection ppDevices);
            int GetDefaultAudioEndpoint(int dataFlow, int role, out IMMDevice ppEndpoint);
            int NotImpl2();
            int NotImpl3();
            int NotImpl4();
            int NotImpl5();
        }

        [ComImport]
        [Guid("0BD7A1BE-7A1A-44DB-8397-CC5392387B5E")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IMMDeviceCollection
        {
            int GetCount(out uint pcDevices);
            int Item(uint nDevice, out IMMDevice ppDevice);
        }

        [ComImport]
        [Guid("D666063F-1587-4E43-81F1-B948E807363F")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IMMDevice
        {
            int Activate(ref Guid iid, int dwClsCtx, IntPtr pActivationParams, [MarshalAs(UnmanagedType.IUnknown)] out object ppInterface);
            int OpenPropertyStore(int stgmAccess, out IPropertyStore ppProperties);
            int GetId([MarshalAs(UnmanagedType.LPWStr)] out string ppstrId);
            int GetState(out int pdwState);
        }

        [ComImport]
        [Guid("886d8eeb-8cf2-4446-8d02-cdba1dbdcf99")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IPropertyStore
        {
            int GetCount(out uint cProps);
            int GetAt(uint iProp, out PROPERTYKEY pkey);
            int GetValue(ref PROPERTYKEY key, out PROPVARIANT pv);
            int SetValue(ref PROPERTYKEY key, ref PROPVARIANT propvar);
            int Commit();
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PROPERTYKEY
        {
            public Guid fmtid;
            public uint pid;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PROPVARIANT
        {
            private ushort vt;
            private ushort wReserved1;
            private ushort wReserved2;
            private ushort wReserved3;
            private IntPtr p;
            private int p2;

            public string GetValueAsString()
            {
                const ushort VT_LPWSTR = 31;
                return vt == VT_LPWSTR ? Marshal.PtrToStringUni(p) : null;
            }

            public void Clear()
            {
                PropVariantClear(ref this);
            }

            [DllImport("ole32.dll")]
            private static extern int PropVariantClear(ref PROPVARIANT pvar);
        }
#endif
    }
}
