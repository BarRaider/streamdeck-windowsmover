# Windows Mover & Resizer for Elgato Stream Deck

Allows you to control the position and size of application windows on your Windows PC. Maximize/Minimize windows or change height, width and position. Supports moving applications across multiple screens.

**Author's website and contact information:** [https://barraider.com](https://barraider.com)

## New in v2.5
- Added `Virtual Desktop` support for new Windows 11 (22H2) version

## Features
- Multi-Screen support, move applications across multiple screens
- Change location and size of application window
- Supported in multi-action, one click can rearrange multiple applications
- Plugin will fill coordinates  with where the windows is currently located (and also height/width of window)
- Supports Minimizing/Maximizing windows
- Option to remove the screen's "Friendly name" in cases where you have multiple monitors of the same kind.
- `Auto Retry` feature will attempt to retry moving the window if the process is not yet loaded
- `Make Topmost `feature will attempt to make the window pop up to the front of the screen
- `Location Filter` allows to filter processes to a specific directory
- `Title Filter` allows to filter out processes that don't have certain words in the window title. **Note:** Chrome/Firefox act weird and the titles change based on the current open tab (your mileage may vary)
- `Only Make Topmost` feature allows moving an application to top, without moving or resizing it. Useful along with [SuperMacro](https://github.com/BarRaider/streamdeck-supermacro) to send key-presses in a Multi-Action.
- Virtual Desktop support (Pin/Unpin app, Move app across Virtual Desktops)

### Download

* [Download plugin](https://github.com/BarRaider/streamdeck-windowsmover/releases/)

## I found a bug, who do I contact?
For support please contact the developer. Contact information is available at https://barraider.com

## I have a feature request, who do I contact?
Please contact the developer. Contact information is available at https://barraider.com

## Dependencies
* Uses StreamDeck-Tools by BarRaider: [![NuGet](https://img.shields.io/nuget/v/streamdeck-tools.svg?style=flat)](https://www.nuget.org/packages/streamdeck-tools)
* Uses [Easy-PI](https://github.com/BarRaider/streamdeck-easypi) by BarRaider - Provides seamless integration with the Stream Deck PI (Property Inspector) 

# Change Log


## New in v2.4
- **Windows 11 Support** - All Virtual Desktop features (Pin/Unpin app, Move app across Virtual Desktops) should now work on both Win10 and Win11.
- 🆕 Support for moving/resizing **Apps running as administrator** 
      - Enabling the new `App is running as administrator` checkbox will elevate the permissions and allow you to control those apps too!

## New in v2.2
- :new: ***VIRTUAL DESKTOP SUPPORT***: 
    - New `Virtual Desktop Mover` action allows moving apps across Virtual Desktops. 
    - `Virtual Desktop Pin/Unpin` action  allows you to decide which apps are visible across all Virtual Desktops.
- Use along with Win Tools plugin to manage Virtual Desktops and then move the desired apps to there.
- Updated dependencies and improved load time

## New in v2.1
- `Reload Apps` button (in settings) allows to reload the most recent list of applications
- Improved Multi-Actions behavior
