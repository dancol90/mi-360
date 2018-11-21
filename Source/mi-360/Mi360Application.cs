using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using mi360.Properties;
using mi360.Win32;

namespace mi360
{
    class Mi360Application : ApplicationContext
    {
        private static string XiaomiGamepadHardwareId = @"HID\{00001124-0000-1000-8000-00805f9b34fb}_VID&00022717_PID&3144";
        private static string XiaomiGamepadHardwareFilter = @"VID&00022717_PID&3144";

        private NotifyIcon _NotifyIcon;
        private IMonitor _Monitor;
        private XInputManager _Manager;

        public Mi360Application()
        {
            InitializeComponents();

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            EnableHidGuardian();

            _Manager = new XInputManager();
            _Manager.GamepadRunning += Manager_GamepadRunning;
            _Manager.GamepadRemoved += Manager_GamepadRemoved;

            _Monitor = new HidMonitor(XiaomiGamepadHardwareFilter);
            _Monitor.DeviceAttached += Monitor_DeviceAttached;
            _Monitor.DeviceRemoved += Monitor_DeviceRemoved;
            _Monitor.Start();
        }

        #region Initialization/Cleanup methods

        private void InitializeComponents()
        {
            Application.ApplicationExit += Application_ApplicationExit;

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

            _NotifyIcon.MouseMove += _NotifyIcon_MouseMove;
        }

        protected override void Dispose(bool disposing)
        {
            _Monitor.Stop();
            _Monitor.Dispose();

            _Manager.Dispose();

            base.Dispose(disposing);
        }

        #endregion

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
            HidGuardian.ClearWhitelistedProcesses();
            HidGuardian.ClearAffectedDevices();

            HidGuardian.AddDeviceToAffectedList(XiaomiGamepadHardwareId);
            HidGuardian.AddToWhitelist(Process.GetCurrentProcess().Id);

            // Disable and reenable the device to let the driver hide the HID gamepad and show Xbox360 one
            DeviceStateManager.DisableReEnableDevice(XiaomiGamepadHardwareId);
        }

        private void DisableHidGuardian()
        {
            HidGuardian.RemoveDeviceFromAffectedList(XiaomiGamepadHardwareId);
            HidGuardian.RemoveFromWhitelist(Process.GetCurrentProcess().Id);

            // Disable and reenable the device to let the driver hide the emulated gamepad and show the HID one again
            DeviceStateManager.DisableReEnableDevice(XiaomiGamepadHardwareId);
        }

        #endregion

        #region Event Handlers

        private void Exit_OnClick(object sender, EventArgs eventArgs)
        {
            Application.Exit();
        }

        private void Application_ApplicationExit(object sender, EventArgs eventArgs)
        {
            _NotifyIcon.Visible = false;
            DisableHidGuardian();
        }

        private void Monitor_DeviceAttached(object sender, string s)
        {
            Console.WriteLine("HID Connected: " + s);
            _Manager.AddAndStart(s);
        }

        private void Monitor_DeviceRemoved(object sender, string s)
        {
            Console.WriteLine("HID Disconnected: " + s);
            _Manager.StopAndRemove(s);
        }

        private void Manager_GamepadRemoved(object sender, EventArgs eventArgs)
        {
            ShowNotification("Gamepad disconnected", "A gamepad has disconnected and is not available anymore.");
        }

        private void Manager_GamepadRunning(object sender, EventArgs eventArgs)
        {
            ShowNotification("Gamepad connected", "A new gamepad is now available as XInput device.");
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs args)
        {
            string message;

            if (args.ExceptionObject is Exception)
                message = (args.ExceptionObject as Exception).Message;
            else
                message = args.ExceptionObject.ToString();

            MessageBox.Show("mi-360 has stopped working. The cause of the problem is:\n\n" + message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void _NotifyIcon_MouseMove(object sender, MouseEventArgs e)
        {
            var lines = new List<string> { "Xiaomi Gamepad XInput manager" };

            foreach (var s in _Manager.DeviceStatus)
            {
                if (s.Key > 4)
                    continue;

                var led = $"{ new string('\u25CB', s.Key) }\u25C9{ new string('\u25CB', 3 - s.Key) }";
                var batt = s.Value > 0 ? $"{ s.Value }%" : "N/A";

                lines.Add($"{ led } - Battery { batt }");
            }

            _NotifyIcon.Text = String.Join(Environment.NewLine, lines);
        }

        #endregion
    }
}
