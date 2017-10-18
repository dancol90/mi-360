using System;
using System.Runtime.InteropServices;

namespace mi360.Win32.Native
{
    public class DBT
    {
        #region Contants

        public const int WM_DEVICECHANGE = 0x0219; // device state change
        public const int DBT_DEVICEARRIVAL = 0x8000; // detected a new device
        public const int DBT_DEVICEQUERYREMOVE = 0x8001; // preparing to remove
        public const int DBT_DEVICEREMOVECOMPLETE = 0x8004; // removed 
        public const int DBT_DEVNODES_CHANGED = 0x0007; //A device has been added to or removed from the system.

        public const int DBT_DEVTYP_DEVICEINTERFACE = 0x00000005;
        public const int DBT_DEVTYP_HANDLE = 0x00000006;
        public const int DBT_DEVTYP_OEM = 0x00000000;
        public const int DBT_DEVTYP_PORT = 0x00000003;
        public const int DBT_DEVTYP_VOLUME = 0x00000002;

        #endregion

        #region Enums

        [Flags]
        public enum DEVICE_NOTIFY : uint
        {
            DEVICE_NOTIFY_WINDOW_HANDLE = 0x00000000,
            DEVICE_NOTIFY_SERVICE_HANDLE = 0x00000001,
            DEVICE_NOTIFY_ALL_INTERFACE_CLASSES = 0x00000004
        }

        #endregion

        #region Structures

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct DEV_BROADCAST_DEVICEINTERFACE
        {
            public int dbcc_size;
            public int dbcc_devicetype;
            public int dbcc_reserved;
            //public IntPtr dbcc_handle;
            //public IntPtr dbcc_hdevnotify;
            [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U1, SizeConst = 16)]
            public byte[] dbcc_classguid;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)]
            public Char[] dbcc_name;
            //public byte dbcc_data;
            //public byte dbcc_data1; 
        }

        #endregion

        #region Methods imports

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr RegisterDeviceNotification(IntPtr intPtr, IntPtr notificationFilter, uint flags);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool UnregisterDeviceNotification(IntPtr Handle);

        #endregion
    }
}
