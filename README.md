# OSRS AutoSwitcher v1.2

## Description

![](https://i.imgur.com/zvplCXk.png)

The AutoSwitcher is written in C# and is meant to be used with Windows. The AutoSwitcher is meant to be used for OldSchool RuneScape to switch equipment more reliably and faster. I do not support nor endorse cheating in RuneScape, if you get banned it is NOT my fault.

A compiled version can be downloaded from the [release](https://github.com/SDCAAU/OSRS-AutoSwitcher/releases) section. Alternatively you can compile it yourself. It is tested to compile under Visual Studio 2017, is not tested anywhere else. 

![](https://i.imgur.com/LRaaAba.gif)

## Features

* AutoSwitching (Duhh)
* Changing speed
* Going back to old mouse postion after switching
* Hotkey selection
* Supporting Fixed/Resizeable and stretched Fixed (RuneLite) //Temporarily not supported
* Prayer switching on hotkey
* Automatically special attack on target at mouse position
* Saving and loading presets

## Built With

* [Newtonsoft.Json](https://www.newtonsoft.com/json) - For saving and loading user settings

## Issues
If there's any problems, feel free to open an Issue.

## License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details

## Changelog
### [1.3] - 10-03-2020 - WIP
In order to accomodate for mouse input (mouse4/5) and allow modification of existing configurations, I'm changing to a Low level input library and adding an extra option in the menu.
- Added the option to change an existing configuration
- Changing from GetAsyncKeyState to low level Input manager to support all types of input (WIP)
### [1.2] - 23-02-2019
Decided to rework the entire project since it started to get some attention from interested users. This is a WIP to rewrite the entire thing into an overall better application. Due to previous testing of functionaility being tedious, I decided to skip out on implementing a regular user interface and keep it console for now.
#### Added/edited
- Complete new Console UI
- Reworked special attacking to supported weapons with no special attack bar
- Reworked prayer helper to reliably change the correct prayers
- All hotkeys are now done using GetAsyncKeyState for more reliable hotkeys and not relying on a library(That had wonky bugs anyway)
#### Removed
- Temporarily removed the PK Helper(Eating/Potions) till the rest of the functionality is improved
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