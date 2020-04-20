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
        private static string XiaomiGamepadHardwareFilter = @"VID&00022717_PID&3144";

        private NotifyIcon _NotifyIcon;
        private IMonitor _Monitor;
        private XInputManager _Manager;

        public Mi360Application()
        {
            InitializeComponents();

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

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

        #region Event Handlers

        private void Exit_OnClick(object sender, EventArgs eventArgs)
        {
            Application.Exit();
        }

        private void Application_ApplicationExit(object sender, EventArgs eventArgs)
        {
            _NotifyIcon.Visible = false;
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
            if (_Manager == null)
                return;

            var lines = new List<string> { "mi-360" };

            if (_Manager.DeviceStatus.Count == 1)
            {
                var s = _Manager.DeviceStatus.First();

                if (s.Key <= 4)
                {
                    var led = $"{ new string('\u25CB', s.Key) }\u25C9{ new string('\u25CB', 3 - s.Key) }";
                    var batt = s.Value > 0 ? $"{ s.Value }%" : "N/A";

                    lines.Add($"{ led } - Battery { batt }");
                }
            }
            else
            {
                foreach (var s in _Manager.DeviceStatus)
                {
                    var batt = s.Value > 0 ? $"{ s.Value }%" : "N/A";
                    lines.Add($"{ s.Key }: { batt }");
                }
            }

            _NotifyIcon.Text = String.Join(Environment.NewLine, lines);
        }

        #endregion
    }
}
