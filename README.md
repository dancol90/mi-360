# mi-360
Xbox360 controller emulation for Xiaomi Gamepad, with vibration support.

An application that runs in the tray and expose every Xiaomi Gamepad as a XInput-compatible device that can be used for every game or application that supports Xbox360 pads. Rumble works, too! 
The HID device will be hidden to the whole system, showing only the emulated Xbox one.

XInput emulation is provided by ViGem, by Benjamin Höglinger

## Prerequisites

If any version of mi-360 prior to version 0.5 is installed, please uninstall it before updating.

ViGEm bus is required to be installed to use this utility. You can find the auto-updating installer [here](https://github.com/ViGEm/ViGEmBus/releases).

## Easy install: pre-packaged setup

Download the latest version of the setup from the Releases page and run it.

## Custom build and setup

The solution can be built with Visual Studio 2017 Community.
No special dependencies are needed.

## HIDLibrary modifications

This repository contains a custom version of HIDLibrary by Mike O'Brien, with some changes that addresses a common problem:

- Added a call to ``NativeMethods.CancelIo()`` in case of a timeout in ``ReadData()``.
Without that, every subsequent call to ReadData after a timeout will result in a timeout. Also, the library crashes when closing the device, making the whole application crash (at a very low level).
- Avoided any call to `IsConnected` when reading a HID report. This property forces a re-enumeration of the devices, an activity that is pretty heavy when repeated several times per second.
- Added some methods to enable/disable an HID device through Windows API.