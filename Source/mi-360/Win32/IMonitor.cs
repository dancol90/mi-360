using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mi360.Win32
{
    interface IMonitor : IDisposable
    {
        event EventHandler<string> DeviceAttached;
        event EventHandler<string> DeviceRemoved;

        void Start();
        void Stop();
    }
}
