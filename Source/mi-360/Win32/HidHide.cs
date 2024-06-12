using mi360.Win32.Native;
using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace mi360.Win32
{
    class HidHide : IDisposable
    {
        private const uint IOCTL_GET_WHITELIST = 0x80016000;
        private const uint IOCTL_SET_WHITELIST = 0x80016004;
        private const uint IOCTL_GET_BLACKLIST = 0x80016008;
        private const uint IOCTL_SET_BLACKLIST = 0x8001600C;
        private const uint IOCTL_GET_ACTIVE = 0x80016010;
        private const uint IOCTL_SET_ACTIVE = 0x80016014;

        public HidHide()
        {
            Handle = Kernel32.CreateFile(@"\\.\HidHide", FileAccess.Read, FileShare.ReadWrite, IntPtr.Zero, FileMode.Open, FileAttributes.Normal, IntPtr.Zero);
        }

        private SafeFileHandle Handle { get; set; }

        public bool EnableHiding
        {
            get
            {
                var buffer = Marshal.AllocHGlobal(sizeof(bool));

                try
                {
                    Kernel32.DeviceIoControl(
                        Handle, IOCTL_GET_ACTIVE,
                        IntPtr.Zero, 0,   // Input: none
                        buffer, 1, out _, // Output: buffer of length 1
                        IntPtr.Zero
                    );

                    return Convert.ToBoolean(Marshal.ReadByte(buffer));
                }
                finally
                {
                    Marshal.FreeHGlobal(buffer);
                }
            }

            set
            {
                var buffer = Marshal.AllocHGlobal(sizeof(bool));

                // Enable blocking logic, if not enabled already
                try
                {
                    Marshal.WriteByte(buffer, Convert.ToByte(value));

                    // Check return value for success
                    Kernel32.DeviceIoControl(
                        Handle, IOCTL_SET_ACTIVE,
                        buffer, sizeof(bool),  // Input: 1 bool
                        IntPtr.Zero, 0, out _, // Output: ignored
                        IntPtr.Zero
                    );
                }
                finally
                {
                    Marshal.FreeHGlobal(buffer);
                }
            }
        }

        public bool SetDeviceHideStatus(string instance, bool state)
        {
            var buffer = IntPtr.Zero;

            try
            {
                var blacklist = GetBlacklist();
                var newlist = state ?
                    blacklist.Concat(new[] { instance }).Distinct() :
                    blacklist.Where(e => e != instance).Distinct();

                buffer = newlist.StringArrayToMultiSzPointer(out var length);

                // Submit new list
                // Check return value for success
                return Kernel32.DeviceIoControl(
                    Handle, IOCTL_SET_BLACKLIST,
                    buffer, length,
                    IntPtr.Zero, 0, out _,
                    IntPtr.Zero
                );
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        public bool WhitelistCurrentApplication()
        {
            var buffer = IntPtr.Zero;

            // Manipulate allow-list and submit it
            try
            {
                var appPath = Environment.ProcessPath;
                var dosPath = VolumeHelper.PathToDosDevicePath(appPath);

                var whitelist = GetWhitelist();

                if (whitelist.Contains(dosPath))
                    return true;
                
                buffer = whitelist
                    .Concat(new[] { dosPath })
                    .Distinct()
                    .StringArrayToMultiSzPointer(out var length);

                // Submit new list
                // Check return value for success
                return Kernel32.DeviceIoControl(
                    Handle,
                    IOCTL_SET_WHITELIST,
                    buffer, length,
                    IntPtr.Zero, 0, out _,
                    IntPtr.Zero
                );
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        private IEnumerable<string> GetBlacklist()
        {
            // List of blocked instances
            var buffer = IntPtr.Zero;

            // Get existing list of blocked instances
            // This is important to not discard entries other processes potentially made
            // Always get the current list before altering/submitting it
            try
            {
                // Get required buffer size
                Kernel32.DeviceIoControl(
                    Handle, IOCTL_GET_BLACKLIST,
                    IntPtr.Zero, 0,                   // Input: none
                    IntPtr.Zero, 0, out var required, // Output: buffer size
                    IntPtr.Zero
                );

                buffer = Marshal.AllocHGlobal(required);

                // Get actual buffer content
                Kernel32.DeviceIoControl(
                    Handle, IOCTL_GET_BLACKLIST,
                    IntPtr.Zero, 0,          // Input: none
                    buffer, required, out _, // Output: buffer of known length
                    IntPtr.Zero
                );

                // Store existing block-list in a more manageable "C#" fashion
                return buffer.MultiSzPointerToStringArray(required).ToList();
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        private IEnumerable<string> GetWhitelist()
        {
            // List of blocked instances
            var buffer = IntPtr.Zero;

            // Get existing list of blocked instances
            // This is important to not discard entries other processes potentially made
            // Always get the current list before altering/submitting it
            try
            {
                // Get required buffer size
                Kernel32.DeviceIoControl(
                    Handle, IOCTL_GET_WHITELIST,
                    IntPtr.Zero, 0,                   // Input: none
                    IntPtr.Zero, 0, out var required, // Output: buffer size
                    IntPtr.Zero
                );

                buffer = Marshal.AllocHGlobal(required);

                // Get actual buffer content
                Kernel32.DeviceIoControl(
                    Handle, IOCTL_GET_WHITELIST,
                    IntPtr.Zero, 0,          // Input: none
                    buffer, required, out _, // Output: buffer of known length
                    IntPtr.Zero
                );

                // Store existing block-list in a more manageable "C#" fashion
                return buffer.MultiSzPointerToStringArray(required).ToList();
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        public void Dispose()
        {
            Handle.Dispose();
        }
    }
}
