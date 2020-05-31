using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Serilog;
using Nefarius.ViGEm.Client;

namespace mi360
{
    class XInputManager : IDisposable
    {
        public event EventHandler GamepadRunning;
        public event EventHandler GamepadRemoved;

        private ILogger _Logger = Log.ForContext<XInputManager>();

        private Dictionary<string, MiGamepad> _Gamepads;
        private ViGEmClient _ViGEmClient;

        private SynchronizationContext _SyncContext;

        public XInputManager()
        {
            _Logger.Information("Initializing ViGEm client");
            _ViGEmClient = new ViGEmClient();
            _Gamepads = new Dictionary<string, MiGamepad>();

            _SyncContext = SynchronizationContext.Current;
        }

        public void Dispose()
        {
            _Logger.Information("Cleaning up running gamepads");

            // When calling Stop() the device will get removed from the dictionary, do this to avoid exceptions in enumeration
            var devices = _Gamepads.Values.ToArray();

            foreach (var device in devices)
            {
                device.Stop();
                device.Dispose();
            }

            _Logger.Information("Deinitializing ViGEm client");
            _ViGEmClient.Dispose();
        }

        public Dictionary<ushort, ushort> DeviceStatus => _Gamepads.ToDictionary(g => g.Value.LedNumber, g => g.Value.BatteryLevel);

        #region Methods

        public bool AddAndStart(string device)
        {
            if (Contains(device))
            {
                _Logger.Warning("Requested additiong of already existing device {Device}", device);
                return false;
            }

            _Logger.Information("Adding device {Device}", device);
            var gamepad = new MiGamepad(device, _ViGEmClient);
            _Gamepads.Add(device, gamepad);

            gamepad.Started += Gamepad_Started;
            gamepad.Ended += Gamepad_Ended;

            _Logger.Information("Starting {Device}", device);
            gamepad.Start();

            return true;
        }

        public void StopAndRemove(string device)
        {
            if (!Contains(device))
            {
                _Logger.Warning("Requested removal of non-existing device {Device}", device);
                return;
            }
            
            var gamepad = _Gamepads[device];

            _Logger.Information("Stopping device {Device}", device);
            if (gamepad.IsActive)
                gamepad.Stop();

            gamepad.Started -= Gamepad_Started;
            gamepad.Ended -= Gamepad_Ended;

            _Logger.Information("Deinitializing and removing device {Device}", device);
            gamepad.Dispose();

            _Gamepads.Remove(device);
        }

        public bool Contains(string device)
        {
            return _Gamepads.ContainsKey(device);
        }

        #endregion

        private void Gamepad_Ended(object sender, EventArgs eventArgs)
        {
            _SyncContext.Post(o =>
            {
                var gamepad = sender as MiGamepad;
                StopAndRemove(gamepad.Device.DevicePath);

                GamepadRemoved?.Invoke(this, EventArgs.Empty);
            }, null);
        }

        private void Gamepad_Started(object sender, EventArgs eventArgs)
        {
            _SyncContext.Post(o => GamepadRunning?.Invoke(this, EventArgs.Empty), null);
        }
    }
}
