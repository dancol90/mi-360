# mi-360
Xbox360 controller emulation for Xiaomi Gamepad, with vibration support.

An application that runs in the tray and expose every Xiaomi Gamepad as a XInput-compatible device that can be used for every game or application that supports Xbox360 pads. Rumble works, too! 
The HID device will be hidden to the whole system, showing only the emulated Xbox one.

XInput emulation is provided by [ViGEm Bus](https://github.com/ViGEm/ViGEmBus), by Benjamin Höglinger
HID cloacking is provided by [HidHide](https://github.com/ViGEm/HidHide), by Benjamin Höglinger

## Prerequisites

If any version of mi-360 prior to version 0.5 is installed, please uninstall it before updating.

ViGEm bus is required to be installed to use this utility. You can find the auto-updating installer [here](https://github.com/ViGEm/ViGEmBus/releases).

## Installation

1- Download and install the most recent release of [ViGEm Bus](https://github.com/ViGEm/ViGEmBus/releases/latest)
2- **(Optional)** Download and install the most recent release of [HidHide](https://github.com/ViGEm/HidHide/releases/latest)
3- Download the latest version of the setup from the Releases page and run it.

## Custom build and setup

The solution can be built with Visual Studio 2022 Community.
No special dependencies are needed.
