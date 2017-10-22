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
        private Dictionary<string, MiGamepad> _Gamepads;
        private ViGEmClient _ViGEmClient;

        public XInputManager()
        {
            _ViGEmClient = new ViGEmClient();
            _Gamepads = new Dictionary<string, MiGamepad>();
        }

        public void Dispose()
        {
            foreach (var device in _Gamepads.Values)
            {
                device.Stop();
                device.Dispose();
            }

            _ViGEmClient.Dispose();
        }

        #region Methods

        public bool AddAndStart(string device)
        {
            if (Contains(device))
                return false;

            var gamepad = new MiGamepad(device, _ViGEmClient);
            _Gamepads.Add(device, gamepad);

            gamepad.Start();

            return true;
        }

        public void StopAndRemove(string device)
        {
            if (!Contains(device))
                return;
            
            var gamepad = _Gamepads[device];

            gamepad.Stop();
            gamepad.Dispose();

            _Gamepads.Remove(device);
        }

        public bool Contains(string device)
        {
            return _Gamepads.ContainsKey(device);
        }

        #endregion
    }
}
