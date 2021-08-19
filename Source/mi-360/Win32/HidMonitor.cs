using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using HidLibrary;

namespace mi360.Win32
{
    class HidMonitor : IMonitor
    {
        #region Constants & Fields

        public event EventHandler<string> DeviceAttached;
        public event EventHandler<string> DeviceRemoved;

        private ILogger _Logger = Log.ForContext<HidMonitor>();

        private Timer _MonitorTimer;
        private int _VID, _PID;
        private string[] _SeenDevices;

        #endregion

        #region Constructors

        public HidMonitor(int vid, int pid)
        {
            _Logger.Information("Initializing HID device monitor with filter {VID}:{PID}", vid, pid);

            _VID = vid;
            _PID = pid;
            _MonitorTimer = new Timer(SearchForDevice);

            _SeenDevices = new string[0];
        }

        #endregion

        #region Methods

        public void Start()
        {
            _Logger.Information("Start monitoring for filter {VID}:{PID}", _VID, _PID);
            _MonitorTimer.Change(0, 5000);
        }

        public void Stop()
        {
            _Logger.Information("Stop monitoring for filter {VID}:{PID}", _VID, _PID);
            _MonitorTimer.Change(Timeout.Infinite, Timeout.Infinite);
        }

        private void SearchForDevice(object state)
        {
            var devices = HidDevices
                .Enumerate(_VID, _PID)
                .Select(d => d.DevicePath)
                .ToArray();

            // Get all the devices that has connected since the last check
            var newDevices = devices.Except(_SeenDevices);

            // Get all the device that has disconnected since the last check
            var removedDevices = _SeenDevices.Except(devices);

            foreach (var device in newDevices)
            {
                _Logger.Information("Detected attached HID devices matching filter {VID}:{PID}", _VID, _PID);
                DeviceAttached?.Invoke(this, device);
            }

            foreach (var device in removedDevices)
            {
                _Logger.Information("Detected removed HID devices matching filter {VID}:{PID}", _VID, _PID);
                DeviceRemoved?.Invoke(this, device);
            }

            _SeenDevices = devices;
        }

        #endregion

        #region IDisposable pattern

        public void Dispose()
        {
            _Logger.Information("Deinitilizing HID monitor for {VID}:{PID}", _VID, _PID);
            _MonitorTimer.Dispose();
        }

        #endregion

    }
}
