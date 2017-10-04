using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace mi360
{
    public static class HidGuardian
    {
        private static string HidGuardianRegistryKeyBase = @"SYSTEM\CurrentControlSet\Services\HidGuardian\Parameters";
        private static string HidWhitelistRegistryKeyBase = $"{HidGuardianRegistryKeyBase}\\Whitelist";

        private static readonly Regex HardwareIdRegex =
            new Regex(@"HID\\[{(]?[0-9A-Fa-z]{8}[-]?([0-9A-Fa-z]{4}[-]?){3}[0-9A-Fa-z]{12}[)}]?|HID\\VID_[a-zA-Z0-9]{4}&PID_[a-zA-Z0-9]{4}");
        
        #region Whitelist

        public static IEnumerable<int> GetWhitelistedProcesses()
        {
            var wlKey = Registry.LocalMachine.OpenSubKey(HidWhitelistRegistryKeyBase);
            var list = wlKey?.GetSubKeyNames();
            wlKey?.Close();

            return list.Select(int.Parse);
        }

        public static void ClearWhitelistedProcesses()
        {
            var wlKey = Registry.LocalMachine.OpenSubKey(HidWhitelistRegistryKeyBase);

            foreach (var subKeyName in wlKey.GetSubKeyNames())
                Registry.LocalMachine.DeleteSubKey($"{HidWhitelistRegistryKeyBase}\\{subKeyName}");
        }

        public static void AddToWhitelist(int id)
        {
            Registry.LocalMachine.CreateSubKey($"{HidWhitelistRegistryKeyBase}\\{id}");
        }

        public static void RemoveFromWhitelist(int id)
        {
            Registry.LocalMachine.DeleteSubKey($"{HidWhitelistRegistryKeyBase}\\{id}");
        }

        #endregion

        #region Affected

        public static IEnumerable<string> GetAffectedDevices()
        {
            var wlKey = Registry.LocalMachine.OpenSubKey(HidGuardianRegistryKeyBase);
            var affected = wlKey?.GetValue("AffectedDevices") as string[];
            wlKey?.Close();

            return affected ?? new string[]{};
        }

        public static void ClearAffectedDevices()
        {
            var wlKey = Registry.LocalMachine.OpenSubKey(HidGuardianRegistryKeyBase, true);
            wlKey?.SetValue("AffectedDevices", new string[] { }, RegistryValueKind.MultiString);
            wlKey?.Close();
        }

        public static void AddDeviceToAffectedList(string hwid)
        {
            // Get existing Hardware IDs
            var wlKey = Registry.LocalMachine.OpenSubKey(HidGuardianRegistryKeyBase, true);
            var affected = GetAffectedDevices().ToList();

            if (!HardwareIdRegex.IsMatch(hwid))
                return;

            // Add the divec to the list
            affected.Add(hwid);

            // Write back to registry
            wlKey?.SetValue("AffectedDevices", affected.Distinct().ToArray(), RegistryValueKind.MultiString);
            wlKey?.Close();
        }

        public static void RemoveDeviceFromAffectedList(string hwid)
        {
            // Get existing Hardware IDs
            var wlKey = Registry.LocalMachine.OpenSubKey(HidGuardianRegistryKeyBase, true);
            var affected = GetAffectedDevices();

            if (!HardwareIdRegex.IsMatch(hwid))
                return;

            // Remove provided device
            affected = affected.Where(id => !id.Contains(hwid));

            // Write back to registry
            wlKey?.SetValue("AffectedDevices", affected.ToArray(), RegistryValueKind.MultiString);
            wlKey?.Close();
        }

        #endregion
    }
}
