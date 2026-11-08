# Xbox native shell (UWP)

This folder contains the first native Xbox application host for Retro Player.

## Architecture

- UWP app targeting the Xbox device family.
- WinUI 2 WebView2 control hosts the Retro Player Xbox interface.
- The web UI is loaded from the separately deployed project at https://retro-player-xbox.vercel.app/.
- Native UWP Gamepad polling forwards D-pad, A, and B actions into the page through the WebView2 scripting API.
- WebView2 is used rather than the older EdgeHTML WebView. Microsoft announced WebView2 support for Xbox apps in November 2023.

## Build

Open `RetroPlayerXbox.sln` in Visual Studio 2022 with the Universal Windows Platform development workload and Windows 10 SDK installed. First run `./GenerateAssets.ps1` in PowerShell to materialize the PNG package logos from the checked-in base64 source files. Restore NuGet packages, select `Release | x64`, and build the project.

The GitHub Actions workflow builds a signed sideload package and publishes the package plus its public development certificate as workflow artifacts.

## Install / test

The signed package is for personal Developer Mode testing only. The certificate must be trusted by the Xbox before installing the package. Use Xbox Device Portal on the same local network to install the app package and certificate as supported by the console's current Device Portal version.

## Current limitations

- This is the first native host, not a finished emulator.
- The app requires an internet connection to load the hosted interface.
- The emulator launch route is not connected yet. EmulatorJS/WebAssembly/WebGL, audio, file selection, memory, and sustained performance must be tested on a real Series S.
- No ROMs, BIOS files, copyrighted game images, or signing private keys are stored in this repository.
