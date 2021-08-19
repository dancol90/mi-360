# HIDLibrary

## Modified from [HIDLibrary by Mike O'Brien](https://github.com/mikeobrien/HidLibrary)

### Custom changes:

- Added a call to ``NativeMethods.CancelIo()`` in case of a timeout in ``ReadData()``.
Without that, every subsequent call to ReadData after a timeout will result in a timeout. Also, the library crashes when closing the device, making the whole application crash (at a very low level).
- Avoided any call to `IsConnected` when reading a HID report. This property forces a re-enumeration of the devices, an activity that is pretty heavy when repeated several times per second.
- Added some methods to enable/disable an HID device through Windows API.