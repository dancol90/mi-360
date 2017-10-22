using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HidLibrary;

namespace mi360.Win32
{
    class HidMonitor : IMonitor
    {
        #region Constants & Fields

        public event EventHandler<string> DeviceAttached;
        public event EventHandler<string> DeviceRemoved;

        private Timer _MonitorTimer;
        private string _Filter;
        private string[] _SeenDevices;

        #endregion

        #region Constructors

        public HidMonitor(string filter)
        {
            _Filter = filter;
            _MonitorTimer = new Timer(SearchForDevice);

            _SeenDevices = new string[0];
        }

        #endregion

        #region Methods

        public void Start()
        {
            _MonitorTimer.Change(0, 5000);
        }

        public void Stop()
        {
            _MonitorTimer.Change(Timeout.Infinite, Timeout.Infinite);
        }

        private void SearchForDevice(object state)
        {
            var devices = HidDevices
                .EnumeratePaths(_Filter)
                .ToArray();

            // Get all the devices that has connected since the last check
            var newDevices = devices.Except(_SeenDevices);

            // Get all the device that has disconnected since the last check
            var removedDevices = _SeenDevices.Except(devices);

            foreach (var device in newDevices)
                DeviceAttached?.Invoke(this, device);

            foreach (var device in removedDevices)
                DeviceRemoved?.Invoke(this, device);

            _SeenDevices = devices;
        }

        #endregion

        #region IDisposable pattern

        public void Dispose()
        {
            _MonitorTimer.Dispose();
        }

        #endregion

    }
}
