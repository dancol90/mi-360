using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HidLibrary;
using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Exceptions;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.Xbox360;

namespace mi360
{
    class MiGamepad : IDisposable
    {
        private static readonly Xbox360Buttons[][] HatSwitches = {
            new [] { Xbox360Buttons.Up },
            new [] { Xbox360Buttons.Up, Xbox360Buttons.Right },
            new [] { Xbox360Buttons.Right },
            new [] { Xbox360Buttons.Right, Xbox360Buttons.Down },
            new [] { Xbox360Buttons.Down },
            new [] { Xbox360Buttons.Down, Xbox360Buttons.Left },
            new [] { Xbox360Buttons.Left },
            new [] { Xbox360Buttons.Left, Xbox360Buttons.Up },
        };

        private readonly HidDevice _Device;
        private readonly Xbox360Controller _Target;
        private readonly Xbox360Report _Report;
        private readonly Thread _InputThread;
        private readonly CancellationTokenSource _CTS;

        public MiGamepad(string device, ViGEmClient client)
        {
            _Device = HidDevices.GetDevice(device);
            _Device.MonitorDeviceEvents = false;

            _Target = new Xbox360Controller(client);
            _Target.FeedbackReceived += Target_OnFeedbackReceived;

            // TODO mark the threads as background?
            _InputThread = new Thread(DeviceWorker);

            _CTS = new CancellationTokenSource();
            _Report = new Xbox360Report();

            LedNumber = 0xFF;
        }

        #region Properties

        public HidDevice Device => _Device;

        public int LedNumber { get; private set; }

        public bool IsActive => _InputThread.IsAlive;

        #endregion

        #region Methods

        public void Dispose()
        {
            if (_InputThread.IsAlive)
                Stop();

            _Device.Dispose();
            _CTS.Dispose();

            _Target.Dispose();
        }

        public void Start()
        {
            _InputThread.Start();
        }

        public void Stop()
        {
            _CTS.Cancel();
            _InputThread.Join();
        }

        private void DeviceWorker()
        {
            Console.WriteLine("Starting worker thread for {0}", _Device.ToString());

            // Open HID device to read input from the gamepad
            _Device.OpenDevice(DeviceMode.Overlapped, DeviceMode.Overlapped, ShareMode.ShareRead | ShareMode.ShareWrite);

            // Init Xiaomi Gamepad vibration
            _Device.WriteFeatureData(new byte[] {0x20, 0x00, 0x00});

            // Connect the virtual Xbox360 gamepad
            try
            {
                _Target.Connect();
            }
            catch (VigemAlreadyConnectedException e)
            {
                _Target.Disconnect();
                _Target.Connect();
            }

            HidReport hidReport;

            while (!_CTS.Token.IsCancellationRequested)
            {
                // Is device has been closed, exit the loop
                if (!_Device.IsOpen)
                    break;

                // Otherwise read a report
                hidReport = _Device.ReadReport(1000);

                if (hidReport.ReadStatus == HidDeviceData.ReadStatus.WaitTimedOut)
                    continue;
                else if (hidReport.ReadStatus != HidDeviceData.ReadStatus.Success)
                {
                    Console.WriteLine("Device {0}: error while reading HID report, {1}", _Device.ToString(), hidReport.ReadStatus.ToString());
                    break;
                }

                var data = hidReport.Data;

                lock (_Report)
                {
                    _Report.SetButtonState(Xbox360Buttons.A, GetBit(data[0], 0));
                    _Report.SetButtonState(Xbox360Buttons.B, GetBit(data[0], 1));
                    _Report.SetButtonState(Xbox360Buttons.X, GetBit(data[0], 3));
                    _Report.SetButtonState(Xbox360Buttons.Y, GetBit(data[0], 4));
                    _Report.SetButtonState(Xbox360Buttons.LeftShoulder, GetBit(data[0], 6));
                    _Report.SetButtonState(Xbox360Buttons.RightShoulder, GetBit(data[0], 7));

                    _Report.SetButtonState(Xbox360Buttons.Back, GetBit(data[1], 2));
                    _Report.SetButtonState(Xbox360Buttons.Start, GetBit(data[1], 3));
                    _Report.SetButtonState(Xbox360Buttons.LeftThumb, GetBit(data[1], 5));
                    _Report.SetButtonState(Xbox360Buttons.RightThumb, GetBit(data[1], 6));

                // Reset Hat switch status, as is set to 15 (all directions set, impossible state)
                    _Report.SetButtonState(Xbox360Buttons.Up, false);
                    _Report.SetButtonState(Xbox360Buttons.Left, false);
                    _Report.SetButtonState(Xbox360Buttons.Down, false);
                    _Report.SetButtonState(Xbox360Buttons.Right, false);

                if (data[3] < 8)
                {
                    var btns = HatSwitches[data[3]];
                    // Hat Switch is a number from 0 to 7, where 0 is Up, 1 is Up-Left, etc.
                        _Report.SetButtons(btns);
                }

                // Analog axis
                    _Report.SetAxis(Xbox360Axes.LeftThumbX, MapAnalog(data[4]));
                    _Report.SetAxis(Xbox360Axes.LeftThumbY, MapAnalog(data[5], true));
                    _Report.SetAxis(Xbox360Axes.RightThumbX, MapAnalog(data[6]));
                    _Report.SetAxis(Xbox360Axes.RightThumbY, MapAnalog(data[7], true));

                // Triggers
                    _Report.SetAxis(Xbox360Axes.LeftTrigger, data[10]);
                    _Report.SetAxis(Xbox360Axes.RightTrigger, data[11]);

                // Logo ("home") button
                    if (GetBit(data[19], 0))
                    {
                        _Report.SetButtonState((Xbox360Buttons)0x0400, true);
                        Task.Delay(200).ContinueWith(DelayedReleaseGuideButton);
                    }

                    _Target.SendReport(_Report);
                }
            }

            // Disconnect the virtual Xbox360 gamepad
            // Let Dispose handle that, otherwise it will rise a NotPluggedIn exception
            //_Target.Disconnect();

            // Close the HID device
            _Device.CloseDevice();

            Console.WriteLine("Exiting worker thread for {0}", _Device.ToString());
        }

        private bool GetBit(byte b, int bit)
        {
            return ((b >> bit) & 1) != 0;
        }

        private short MapAnalog(byte value, bool invert = false)
        {
            // Value has value in 0-255

            // Clip it in range -127;127
            var centered = Math.Max(-127, value - 128);

            if (invert)
                centered = -centered;

            return (short)(32767 * centered / 127);
        }

        private void DelayedReleaseGuideButton(Task t)
        {
            lock (_Report)
            {
                _Report.SetButtonState((Xbox360Buttons)0x0400, false);
                _Target.SendReport(_Report);
            }
        }

        #endregion

        #region Event Handlers

        private void Target_OnFeedbackReceived(object sender, Xbox360FeedbackReceivedEventArgs e)
        {
            byte[] data = { 0x20, e.SmallMotor, e.LargeMotor };

            Task.Run(() =>
            {
                if (_Device.IsOpen)
                    _Device.WriteFeatureData(data);
            });

            if (LedNumber != e.LedNumber)
            {
                LedNumber = e.LedNumber;
                // TODO raise event here
            }
        }

        #endregion

    }
}
