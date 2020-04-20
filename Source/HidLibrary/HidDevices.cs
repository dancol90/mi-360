using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace HidLibrary
{
    public class HidDevices
    {
        private static Guid _hidClassGuid = Guid.Empty;

        public static bool IsConnected(string devicePath)
        {
            return EnumerateDevices().Any(x => x.Path == devicePath);
        }

        public static HidDevice GetDevice(string devicePath)
        {
            return Enumerate(devicePath).FirstOrDefault();
        }

        public static IEnumerable<HidDevice> Enumerate()
        {
            return EnumerateDevices().Select(x => new HidDevice(x.Path, x.Description));
        }

        public static IEnumerable<HidDevice> Enumerate(string devicePath)
        {
            return EnumerateDevices().Where(x => x.Path == devicePath).Select(x => new HidDevice(x.Path, x.Description));
        }

        public static IEnumerable<HidDevice> Enumerate(int vendorId, params int[] productIds)
        {
            return EnumerateDevices().Select(x => new HidDevice(x.Path, x.Description)).Where(x => x.Attributes.VendorId == vendorId && 
                                                                                  productIds.Contains(x.Attributes.ProductId));
        }

        public static IEnumerable<HidDevice> Enumerate(int vendorId, int productId, ushort UsagePage)
        {
            return EnumerateDevices().Select(x => new HidDevice(x.Path, x.Description)).Where(x => x.Attributes.VendorId == vendorId &&
                                                                                  productId == (ushort)x.Attributes.ProductId && (ushort)x.Capabilities.UsagePage == UsagePage);
        }

        public static IEnumerable<HidDevice> Enumerate(int vendorId)
        {
            return EnumerateDevices().Select(x => new HidDevice(x.Path, x.Description)).Where(x => x.Attributes.VendorId == vendorId);
        }

        public static IEnumerable<string> EnumeratePaths(string filter)
        {
            var f = filter.ToLower();

            return EnumerateDevices()
                .Select(x => x.Path.ToLower())
                .Where(x => x.Contains(f));
        }

        public static bool SetDeviceState(string interfacePath, bool state)
        {
            var iface = EnumerateDevicesInterfaces(false)
                .Where(ei =>
                {
                    var devicePath = GetDevicePath(ei.DeviceInfoSet, ei.DeviceInterfaceData);
                    return devicePath.ToLower() == interfacePath.ToLower();
                })
                .FirstOrDefault();

            if (iface == null)
                return false;

            var header = new NativeMethods.SP_CLASSINSTALL_HEADER();
            header.cbSize = (int)Marshal.SizeOf(header);
            header.InstallFunction = NativeMethods.DIF_PROPERTYCHANGE;

            var propchangeparams = new NativeMethods.SP_PROPCHANGE_PARAMS
            {
                ClassInstallHeader = header,
                StateChange = state ? NativeMethods.DICS_ENABLE : NativeMethods.DICS_DISABLE,
                Scope = NativeMethods.DICS_FLAG_GLOBAL,
                HwProfile = 0
            };

            NativeMethods.SetupDiSetClassInstallParams(iface.DeviceInfoSet, ref iface.DeviceInfoData, ref propchangeparams, (UInt32)Marshal.SizeOf(propchangeparams));

            if (Marshal.GetLastWin32Error() != 0)
                return false;

            NativeMethods.SetupDiChangeState(iface.DeviceInfoSet, ref iface.DeviceInfoData);
            
            if (Marshal.GetLastWin32Error() != 0)
                return false;

            return true;
        }

        internal class DeviceInfo { public string Path { get; set; } public string Description { get; set; } }

        internal class DeviceInterfaceInfo
        {
            internal IntPtr DeviceInfoSet;
            internal NativeMethods.SP_DEVINFO_DATA DeviceInfoData;
            internal NativeMethods.SP_DEVICE_INTERFACE_DATA DeviceInterfaceData;
            internal int DeviceInterfaceIndex;
        };

        internal static IEnumerable<DeviceInfo> EnumerateDevices()
        {
            return EnumerateDevicesInterfaces().Select(ei => { 
                var devicePath = GetDevicePath(ei.DeviceInfoSet, ei.DeviceInterfaceData);
                var description = GetBusReportedDeviceDescription(ei.DeviceInfoSet, ref ei.DeviceInfoData) ??
                                  GetDeviceDescription(ei.DeviceInfoSet, ref ei.DeviceInfoData);
                return new DeviceInfo { Path = devicePath, Description = description };
            });
        }

        internal static IEnumerable<DeviceInterfaceInfo> EnumerateDevicesInterfaces(bool presentOnly = true)
        {
            var hidClass = HidClassGuid;
            var flags = NativeMethods.DIGCF_DEVICEINTERFACE | (presentOnly ? NativeMethods.DIGCF_PRESENT : 0);
            var deviceInfoSet = NativeMethods.SetupDiGetClassDevs(ref hidClass, null, 0, flags);

            if (deviceInfoSet.ToInt64() != NativeMethods.INVALID_HANDLE_VALUE)
            {
                var deviceInfoData = CreateDeviceInfoData();
                var deviceIndex = 0;

                while (NativeMethods.SetupDiEnumDeviceInfo(deviceInfoSet, deviceIndex, ref deviceInfoData))
                {
                    deviceIndex += 1;

                    var deviceInterfaceData = new NativeMethods.SP_DEVICE_INTERFACE_DATA();
                    deviceInterfaceData.cbSize = Marshal.SizeOf(deviceInterfaceData);
                    var deviceInterfaceIndex = 0;

                    while (NativeMethods.SetupDiEnumDeviceInterfaces(deviceInfoSet, ref deviceInfoData, ref hidClass, deviceInterfaceIndex, ref deviceInterfaceData))
                    {
                        deviceInterfaceIndex++;
                        yield return new DeviceInterfaceInfo {
                            DeviceInfoSet = deviceInfoSet,
                            DeviceInfoData = deviceInfoData,
                            DeviceInterfaceData = deviceInterfaceData,
                            DeviceInterfaceIndex = deviceInterfaceIndex
                        };
                    }
                }
                NativeMethods.SetupDiDestroyDeviceInfoList(deviceInfoSet);
            }
        }

        private static NativeMethods.SP_DEVINFO_DATA CreateDeviceInfoData()
        {
            var deviceInfoData = new NativeMethods.SP_DEVINFO_DATA();

            deviceInfoData.cbSize = Marshal.SizeOf(deviceInfoData);
            deviceInfoData.DevInst = 0;
            deviceInfoData.ClassGuid = Guid.Empty;
            deviceInfoData.Reserved = IntPtr.Zero;

            return deviceInfoData;
        }

        private static string GetDevicePath(IntPtr deviceInfoSet, NativeMethods.SP_DEVICE_INTERFACE_DATA deviceInterfaceData)
        {
            var bufferSize = 0;
            var interfaceDetail = new NativeMethods.SP_DEVICE_INTERFACE_DETAIL_DATA { Size = IntPtr.Size == 4 ? 4 + Marshal.SystemDefaultCharSize : 8 };

            NativeMethods.SetupDiGetDeviceInterfaceDetailBuffer(deviceInfoSet, ref deviceInterfaceData, IntPtr.Zero, 0, ref bufferSize, IntPtr.Zero);

            return NativeMethods.SetupDiGetDeviceInterfaceDetail(deviceInfoSet, ref deviceInterfaceData, ref interfaceDetail, bufferSize, ref bufferSize, IntPtr.Zero) ? 
                interfaceDetail.DevicePath : null;
        }

        private static Guid HidClassGuid
        {
            get
            {
                if (_hidClassGuid.Equals(Guid.Empty)) NativeMethods.HidD_GetHidGuid(ref _hidClassGuid);
                return _hidClassGuid;
            }
        }

        private static string GetDeviceDescription(IntPtr deviceInfoSet, ref NativeMethods.SP_DEVINFO_DATA devinfoData)
        {
            var descriptionBuffer = new byte[1024];

            var requiredSize = 0;
            var type = 0;

            NativeMethods.SetupDiGetDeviceRegistryProperty(deviceInfoSet,
                                                            ref devinfoData,
                                                            NativeMethods.SPDRP_DEVICEDESC,
                                                            ref type,
                                                            descriptionBuffer,
                                                            descriptionBuffer.Length,
                                                            ref requiredSize);

            return descriptionBuffer.ToUTF8String();
        }

        private static string GetBusReportedDeviceDescription(IntPtr deviceInfoSet, ref NativeMethods.SP_DEVINFO_DATA devinfoData)
        {
            var descriptionBuffer = new byte[1024];

            if (Environment.OSVersion.Version.Major > 5)
            {
                ulong propertyType = 0;
                var requiredSize = 0;

                var _continue = NativeMethods.SetupDiGetDeviceProperty(deviceInfoSet,
                                                                        ref devinfoData,
                                                                        ref NativeMethods.DEVPKEY_Device_BusReportedDeviceDesc,
                                                                        ref propertyType,
                                                                        descriptionBuffer,
                                                                        descriptionBuffer.Length,
                                                                        ref requiredSize,
                                                                        0);

                if (_continue) return descriptionBuffer.ToUTF16String();
            }
            return null;
        }
    }
}
