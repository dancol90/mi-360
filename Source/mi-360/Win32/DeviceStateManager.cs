using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using static mi360.Win32.Native.SetupApi;

namespace mi360.Win32
{
    // Source: https://stackoverflow.com/questions/4097000/how-do-i-disable-a-system-device-programatically
    public static class DeviceStateManager
    {
        public static void ChangeDeviceState(string filter, bool disable)
        {
            IntPtr info = IntPtr.Zero;
            Guid NullGuid = Guid.Empty;
            try
            {
                info = SetupDiGetClassDevsW(ref NullGuid, null, IntPtr.Zero, DIGCF_ALLCLASSES);
                CheckError("SetupDiGetClassDevs");

                SP_DEVINFO_DATA devdata = new SP_DEVINFO_DATA();
                devdata.cbSize = (UInt32) Marshal.SizeOf(devdata);

                // Get first device matching device criterion.
                for (uint i = 0;; i++)
                {
                    SetupDiEnumDeviceInfo(info, i, out devdata);
                    // if no items match filter, throw
                    if (Marshal.GetLastWin32Error() == ERROR_NO_MORE_ITEMS)
                        CheckError("No device found matching filter.", 0xcffff);
                    CheckError("SetupDiEnumDeviceInfo");

                    string devicepath = GetStringPropertyForDevice(info, devdata, 1); // SPDRP_HARDWAREID

                    if (devicepath != null && devicepath.Contains(filter))
                        break;
                }

                SP_CLASSINSTALL_HEADER header = new SP_CLASSINSTALL_HEADER();
                header.cbSize = (UInt32)Marshal.SizeOf(header);
                header.InstallFunction = DIF_PROPERTYCHANGE;

                SP_PROPCHANGE_PARAMS propchangeparams = new SP_PROPCHANGE_PARAMS
                {
                    ClassInstallHeader = header,
                    StateChange = disable ? DICS_DISABLE : DICS_ENABLE,
                    Scope = DICS_FLAG_GLOBAL,
                    HwProfile = 0
                };

                SetupDiSetClassInstallParams(info, ref devdata, ref propchangeparams, (UInt32)Marshal.SizeOf(propchangeparams));
                CheckError("SetupDiSetClassInstallParams");

                SetupDiChangeState(info, ref devdata);
                //SetupDiCallClassInstaller(DIF_PROPERTYCHANGE, info, ref devdata);
                CheckError("SetupDiChangeState");
            }
            finally
            {
                if (info != IntPtr.Zero)
                    SetupDiDestroyDeviceInfoList(info);
            }
        }

        private static void CheckError(string message, int lasterror = -1)
        {
            int code = lasterror == -1 ? Marshal.GetLastWin32Error() : lasterror;
            if (code != 0)
                throw new Win32Exception(code, $"An API call returned an error: {message}");
        }

        private static string GetStringPropertyForDevice(IntPtr info, SP_DEVINFO_DATA devdata, uint propId)
        {
            uint proptype, outsize;
            IntPtr buffer = IntPtr.Zero;
            try
            {
                uint buflen = 1024;
                buffer = Marshal.AllocHGlobal((int) buflen);
                outsize = 0;

                SetupDiGetDeviceRegistryPropertyW(info, ref devdata, propId, out proptype, buffer, buflen, ref outsize);

                byte[] lbuffer = new byte[outsize];
                Marshal.Copy(buffer, lbuffer, 0, (int) outsize);

                int errcode = Marshal.GetLastWin32Error();

                if (errcode == ERROR_INVALID_DATA)
                    return null;

                CheckError("SetupDiGetDeviceProperty", errcode);
                return Encoding.Unicode.GetString(lbuffer);
            }
            finally
            {
                if (buffer != IntPtr.Zero)
                    Marshal.FreeHGlobal(buffer);
            }
        }

        public static bool DisableReEnableDevice(string filter)
        {
            Win32Exception ex = null;

            try { ChangeDeviceState(filter, true); }
            catch(Win32Exception e) { ex = e; }

            try { ChangeDeviceState(filter, false); }
            catch (Win32Exception e) { ex = e; }

            return ex != null;
        }
    }
}