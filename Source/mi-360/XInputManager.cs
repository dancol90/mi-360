using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HidLibrary;
using Nefarius.ViGEm.Client;

namespace mi360
{
    class XInputManager : IDisposable
    {
        public event EventHandler GamepadRunning;
        public event EventHandler GamepadRemoved;

        private Dictionary<string, MiGamepad> _Gamepads;
        private ViGEmClient _ViGEmClient;

        public XInputManager()
        {
            _ViGEmClient = new ViGEmClient();
            _Gamepads = new Dictionary<string, MiGamepad>();
        }

        public void Dispose()
        {
            // When calling Stop() the device will get removed from the dictionary, do this to avoid exceptions in enumeration
            var devices = _Gamepads.Values.ToArray();

            foreach (var device in devices)
            {
                device.Stop();
                device.Dispose();
            }

            _ViGEmClient.Dispose();
        }

        public Dictionary<ushort, ushort> DeviceStatus => _Gamepads.ToDictionary(g => g.Value.LedNumber, g => g.Value.BatteryLevel);

        #region Methods

        public bool AddAndStart(string device)
        {
            if (Contains(device))
                return false;

            var gamepad = new MiGamepad(device, _ViGEmClient);
            _Gamepads.Add(device, gamepad);

            gamepad.Started += Gamepad_Started;
            gamepad.Ended += Gamepad_Ended;

            gamepad.Start();

            return true;
        }

        public void StopAndRemove(string device)
        {
            if (!Contains(device))
                return;
            
            var gamepad = _Gamepads[device];

            if (gamepad.IsActive)
                gamepad.Stop();

            gamepad.Started -= Gamepad_Started;
            gamepad.Ended -= Gamepad_Ended;

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
            var gamepad = sender as MiGamepad;
            StopAndRemove(gamepad.Device.DevicePath);
            GamepadRemoved?.Invoke(this, EventArgs.Empty);
        }

        private void Gamepad_Started(object sender, EventArgs eventArgs)
        {
            GamepadRunning?.Invoke(this, EventArgs.Empty);
        }
    }
}
