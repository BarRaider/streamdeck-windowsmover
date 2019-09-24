# Windows Mover & Resizer for Elgato Stream Deck

Allows you to control the position and size of application windows on your Windows PC. Maximize/Minimize windows or change height, width and position. Supports moving applications across multiple screens.

**Author's website and contact information:** [https://barraider.github.io](https://barraider.github.io)

## New in v1.9
- Instead of choosing a specific application, you can now choose to manipulate the current focused window. This allows you to "snap"/"resize" the current focused app to various areas on the screen.

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

### Download

* [Download plugin](https://github.com/BarRaider/streamdeck-windowsmover/releases/)

## I found a bug, who do I contact?
For support please contact the developer. Contact information is available at https://barraider.github.io

## I have a feature request, who do I contact?
Please contact the developer. Contact information is available at https://barraider.github.io

## Dependencies
* Uses StreamDeck-Tools by BarRaider: [![NuGet](https://img.shields.io/nuget/v/streamdeck-tools.svg?style=flat)](https://www.nuget.org/packages/streamdeck-tools)
* Uses [Easy-PI](https://github.com/BarRaider/streamdeck-easypi) by BarRaider - Provides seamless integration with the Stream Deck PI (Property Inspector) 