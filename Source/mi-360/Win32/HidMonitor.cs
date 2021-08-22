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
    public class DeviceEventArgs : EventArgs
    {
        internal DeviceEventArgs(HidDevices.DeviceInfo info) : base()
        {
            Path = info.Path;
            Description = info.Description;
            InstanceID = info.InstanceID;
        }

        public string Path { get; set; }
        public string Description { get; set; }
        public string InstanceID { get; set; }
    }

    class DeviceInfoEqualityComparer : IEqualityComparer<HidDevices.DeviceInfo>
    {
        public bool Equals(HidDevices.DeviceInfo x, HidDevices.DeviceInfo y) => x.Path == y.Path;
        public int GetHashCode(HidDevices.DeviceInfo di) => di.Path.GetHashCode();
    }

    class HidMonitor
    {
        #region Constants & Fields

        public event EventHandler<DeviceEventArgs> DeviceAttached;
        public event EventHandler<DeviceEventArgs> DeviceRemoved;

        private ILogger _Logger = Log.ForContext<HidMonitor>();

        private Timer _MonitorTimer;
        private string _Filter;
        private HidDevices.DeviceInfo[] _SeenDevices;

        #endregion

        #region Constructors

        public HidMonitor(string filter)
        {
            _Logger.Information("Initializing HID device monitor with filter {Filter}", filter);

            _Filter = filter;
            _MonitorTimer = new Timer(SearchForDevice);

            _SeenDevices = new HidDevices.DeviceInfo[0];
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
                .Where(p => p.Path.ToLower().Contains(filter))
                .ToArray();

            var comp = new DeviceInfoEqualityComparer();

            // Get all the devices that has connected since the last check
            var newDevices = devices.Except(_SeenDevices, comp);

            // Get all the device that has disconnected since the last check
            var removedDevices = _SeenDevices.Except(devices, comp);

            foreach (var device in newDevices)
            {
                _Logger.Information("Detected attached HID devices matching filter {Filter}", _Filter);
                DeviceAttached?.Invoke(this, new DeviceEventArgs(device));
            }

            foreach (var device in removedDevices)
            {
                _Logger.Information("Detected removed HID devices matching filter {Filter}", _Filter);
                DeviceRemoved?.Invoke(this, new DeviceEventArgs(device));
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
