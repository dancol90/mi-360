# mi-360
Xbox360 controller emulation for Xiaomi Gamepad, with vibration support.

An application that runs in the tray and expose every Xiaomi Gamepad as a XInput-compatible device that can be used for every game or application that supports Xbox360 pads. Rumble works, too!
The HID device will be hidden to the whole system, showing only the emulated Xbox one.
XInput emulation is provied by ViGem, by Benjamin Höglinger

## HIDLibrary

This repository contains a custom version of HIDLibrary by Mike O'Brien, with some changes that addresses a common problem.
The first change is the addition of a call to ``NativeMethods.CancelIo()`` in case of a timeout in ``ReadData()``.
Without that, every subsequent call to ReadData after a timeout will result in a timeout. Also, the library crashes when closing the device, making the whole application crash (at a very low level).
The second change consists in avoiding any call to `IsConnected` when reading a HID report. This property forces a re-enumeration of the devices, an activity that is pretty heavy when repeated several times per second.

## Installation

- Download the ViGem drivers from the project page (tested on version v1.13.0.0)
- Get a copy of copy of devcon.exe (https://superuser.com/questions/1002950/quick-method-to-install-devcon-exe)
- Install HidGuardian by following the instruction at https://github.com/nefarius/ViGEm/tree/master/Sys/HidGuardian
- Install ViGem by following the instruction at https://github.com/nefarius/ViGEm/tree/master/Sys/ViGEmBus
- Download latest version of mi-360 from releases or build it from scratch.
- Execute it and give administation rights
- Enjoy your games!
