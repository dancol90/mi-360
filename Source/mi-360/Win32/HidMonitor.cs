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
        private string _Filter;
        private string[] _SeenDevices;

        #endregion

        #region Constructors

        public HidMonitor(string filter)
        {
            _Logger.Information("Initializing HID device monitor with filter {Filter}", filter);

            _Filter = filter;
            _MonitorTimer = new Timer(SearchForDevice);

            _SeenDevices = new string[0];
        }

        #endregion

        #region Methods

        public void Start()
        {
            _Logger.Information("Start monitoring for filter {Filter}", _Filter);
            _MonitorTimer.Change(0, 5000);
        }

        public void Stop()
        {
            _Logger.Information("Stop monitoring for filter {Filter}", _Filter);
            _MonitorTimer.Change(Timeout.Infinite, Timeout.Infinite);
        }

        private void SearchForDevice(object state)
        {
            var filter = _Filter.ToLower();
            var devices = HidDevices
                .EnumerateDevices()
                .Select(d => d.Path)
                .Where(p => p.ToLower().Contains(filter))
                .ToArray();

            // Get all the devices that has connected since the last check
            var newDevices = devices.Except(_SeenDevices);

            // Get all the device that has disconnected since the last check
            var removedDevices = _SeenDevices.Except(devices);

            foreach (var device in newDevices)
            {
                _Logger.Information("Detected attached HID devices matching filter {Filter}", _Filter);
                DeviceAttached?.Invoke(this, device);
            }

            foreach (var device in removedDevices)
            {
                _Logger.Information("Detected removed HID devices matching filter {Filter}", _Filter);
                DeviceRemoved?.Invoke(this, device);
            }

            _SeenDevices = devices;
        }

        #endregion

        #region IDisposable pattern

        public void Dispose()
        {
            _Logger.Information("Deinitilizing HID monitor for {Filter}", _Filter);
            _MonitorTimer.Dispose();
        }

        #endregion

    }
}
