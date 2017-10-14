using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using HidLibrary;
using mi360.Properties;
using Nefarius.ViGEm.Client;

namespace mi360
{
    class Mi360Application : ApplicationContext
    {
        private static int XiaomiGamepadVid = 0x2717;
        private static int XiaomiGamepadPid = 0x3144;
        private static string XiaomiGamepadHardwareId = "HID\\{00001124-0000-1000-8000-00805f9b34fb}_VID&00022717_PID&3144";

        private NotifyIcon _NotifyIcon;
        private System.Threading.Timer _MonitorTimer;
        private Dictionary<string, MiGamepad> _Gamepads;
        private ViGEmClient _ViGEmClient;

        public Mi360Application()
        {
            InitializeComponents();

            EnableHidGuardian();

            _ViGEmClient = new ViGEmClient();

            _Gamepads = new Dictionary<string, MiGamepad>();
            _MonitorTimer = new System.Threading.Timer(SearchForDevice, null, 0, 5000);
        }

        #region Initialization/Cleanup methods

        private void InitializeComponents()
        {
            Application.ApplicationExit += Application_OnApplicationExit;

            _NotifyIcon = new NotifyIcon()
            {
                Icon = Resources.ApplicationIcon,
                Visible = true,
                Text = "Xiaomi Gamepad XInput manager",
                ContextMenuStrip = new ContextMenuStrip()
                {
                    Items =
                    {
                        new ToolStripMenuItem("Exit", null, Exit_OnClick)
                    }
                }
            };
        }

        protected override void Dispose(bool disposing)
        {
            _MonitorTimer.Dispose();

            foreach (var gamepad in _Gamepads)
            {
                gamepad.Value.Dispose();
            }

            base.Dispose(disposing);
        }

        #endregion

        private void SearchForDevice(object state)
        {
            var devices = HidDevices.Enumerate(XiaomiGamepadVid, XiaomiGamepadPid);
            var connectedPaths = new List<string>();

            foreach (var device in devices)
            {
                connectedPaths.Add(device.DevicePath);

                // If the device is already running, go on
                if (_Gamepads.ContainsKey(device.DevicePath))
                    continue;

                // This is a new gamepad
                ShowNotification("Gamepad connected", "A new gamepad is up and running.");

                var gamepad = new MiGamepad(device, _ViGEmClient);
                _Gamepads.Add(device.DevicePath, gamepad);
                gamepad.Start();
            }

            // Stop and remove any disconnected gamepad
            var runningPaths = _Gamepads.Keys.ToArray();

            foreach (var path in runningPaths)
            {
                if (!connectedPaths.Contains(path))
                {
                    var gamepad = _Gamepads[path];

                    ShowNotification("Gamepad disconnected", "A gamepad disconnected and is not available any more.");
                    gamepad.Stop();
                    gamepad.Dispose();
                    _Gamepads.Remove(path);
                }
            }
        }

        private void ShowNotification(string title, string message, int timeout = 2000)
        {
            _NotifyIcon.BalloonTipTitle = title;
            _NotifyIcon.BalloonTipText = message;
            _NotifyIcon.ShowBalloonTip(timeout);
        }

        #region Hardware utilities

        private void EnableHidGuardian()
        {
            // Temp
            //HidGuardian.ClearWhitelistedProcesses();
            //HidGuardian.ClearAffectedDevices();

            HidGuardian.AddDeviceToAffectedList(XiaomiGamepadHardwareId);
            HidGuardian.AddToWhitelist(Process.GetCurrentProcess().Id);

            // Disable and reenable the device to let the driver hide the HID gamepad and show Xbox360 one
            DisableEnableGamepads();
        }

        private void DisableHidGuardian()
        {
            HidGuardian.RemoveDeviceFromAffectedList(XiaomiGamepadHardwareId);
            HidGuardian.RemoveFromWhitelist(Process.GetCurrentProcess().Id);

            // Disable and reenable the device to let the driver hide the emulated gamepad and show the HID one again
            DisableEnableGamepads();
        }

        private void DisableEnableGamepads()
        {
            try
            {
                Win32Api.DisableDevice(XiaomiGamepadHardwareId, true);
            }
            catch (Win32Exception e)
            {
                Console.WriteLine(e);
            }

            try
            {
                Win32Api.DisableDevice(XiaomiGamepadHardwareId, false);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        #endregion

        #region Event Handlers

        private void Exit_OnClick(object sender, EventArgs eventArgs)
        {
            Application.Exit();
        }

        private void Application_OnApplicationExit(object sender, EventArgs eventArgs)
        {
            _NotifyIcon.Visible = false;
            Dispose(true);
            DisableHidGuardian();
        }

        #endregion

    }
}
