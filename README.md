# mi-360
Xbox360 controller emulation for Xiaomi Gamepad, with vibration support.

An application that runs in the tray and expose every Xiaomi Gamepad as a XInput-compatible device that can be used for every game or application that supports Xbox360 pads. Rumble works, too! 
The HID device will be hidden to the whole system, showing only the emulated Xbox one.

XInput emulation is provided by ViGem, by Benjamin Höglinger

## Easy install: pre-packaged setup

Download the latest version of the setup from the Releases page and run it.

## Custom build and setup

The solution can be built with Visual Studio 2017 Community.
No special dependencies are needed.

Some drivers must be installed before running the application.
You can find all the needed files inside the Drivers directory.

### Drivers installation instructions

- Install HidGuardian by following the instruction at https://docs.vigem.org/#!hidguardian-v1-installation.md
- Install ViGem by following one of this links:
    - Easy setup: https://docs.vigem.org/#!vigem-bus-driver-installation.md
    - Manual setup: https://docs.vigem.org/#!vigem-bus-driver-installation-caveman-edition.md

### Sources of the included files

- ViGem drivers v1.14.3.0: https://downloads.vigem.org/.stable/latest/windows/x86_64/ViGEmBus_signed_Win7-10_x86_x64_latest.zip
- devcon.exe utility: https://downloads.vigem.org/other/microsoft/devcon.zip

## HIDLibrary modifications

This repository contains a custom version of HIDLibrary by Mike O'Brien, with some changes that addresses a common problem.

The first change is the addition of a call to ``NativeMethods.CancelIo()`` in case of a timeout in ``ReadData()``.
Without that, every subsequent call to ReadData after a timeout will result in a timeout. Also, the library crashes when closing the device, making the whole application crash (at a very low level).

The second change consists in avoiding any call to `IsConnected` when reading a HID report. This property forces a re-enumeration of the devices, an activity that is pretty heavy when repeated several times per second.
