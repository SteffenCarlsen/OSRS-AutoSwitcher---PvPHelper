## THIS PROJECT IS CURRENTLY BEING FULLY REWRITTEN - EXPECT A NEW FULLY FUNCTIONAL PVPHELPER SOON
# OSRS AutoSwitcher

## Description

![](https://i.imgur.com/2WYffe0.png)

The AutoSwitcher is written in C# and is meant to be used with Windows. The AutoSwitcher is meant to be used for OldSchool RuneScape to switch equipment more reliably and faster. I do not support nor endorse cheating in RuneScape, if you get banned it is NOT my fault.

A compiled version can be downloaded from the [release](https://github.com/SDCAAU/OSRS-AutoSwitcher/releases) section. Alternatively you can compile it yourself. It is tested to compile under Visual Studio 2017, is not tested anywhere else. 

![](https://i.imgur.com/LRaaAba.gif)

## Features

* AutoSwitching (Duhh)
* Changing speed
* Going back to old mouse postion after switching
* Hotkey selection
* Supporting Fixed/Resizeable and stretched Fixed (RuneLite)
* Saving and loading presets

## Built With

* [Newtonsoft.Json](https://www.newtonsoft.com/json) - For saving and loading user settings
* [MouseKeyHook](https://www.nuget.org/packages/MouseKeyHook) - For getting global hotkeys

## Issues
If there's any problems, feel free to open an Issue.

## License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details

## Changelog

### [1.0.1] - 21-09-2018
#### Added
- Special attack
- Prayer usage
- PK Helper(Eat/Potions/Prayer)
#### Changed
- Revamped hotkey implementation
- Improved AutoSwitch logic
- Added randomization to mouse movements and clicking
#### Known Errors
- WIP About-Page
- Saving and Reloading presets do not work
- Crashes during re-hooking of hotkeys
- Does not always properly perform actions before a second hotkey hook
