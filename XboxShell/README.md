# Xbox native shell (UWP)

This folder contains the native Xbox application host for Retro Player.

## Native Xbox 0.2.0 upgrades

- Native Xbox toolbar with **Import game**, **Home**, **Reload**, and **About** actions.
- Native status strip showing shell/network state and controller connection state.
- Native UWP file picker accepts PS1 images (`.bin`, `.iso`, `.img`, `.pbp`, `.chd`) and Nintendo 64 ROM formats (`.z64`, `.n64`, `.v64`).
- Selected files are staged temporarily in the app's private local folder, exposed to the hosted interface through a WebView2 virtual-host mapping, imported into the existing IndexedDB library, then removed from the staging folder after the web client acknowledges completion.
- Native controller polling forwards D-pad, left-stick, A/B/X/Y, shoulder, and trigger input to the interface. The browser Gamepad API remains available for emulator gameplay.
- The WebView2 navigation allowlist remains restricted to the Retro Player and PS1 Player HTTPS origins.
- Package and assembly version are now 0.2.0.

## Architecture

- UWP app targeting the Xbox device family.
- WinUI 2 WebView2 control hosts the Retro Player Xbox interface.
- The web UI is loaded from the separately deployed project at https://retro-player-xbox.vercel.app/.
- Native UWP Gamepad polling forwards controller actions into the page through the WebView2 scripting API.
- The native file picker uses the system picker permission and copies a selected file into the app-private LocalFolder; it does not request broad filesystem access.
- WebView2 is used rather than the older EdgeHTML WebView.

## Build

Open `RetroPlayerXbox.sln` in Visual Studio 2022 with the Universal Windows Platform development workload and Windows 10 SDK installed. First run `./GenerateAssets.ps1` in PowerShell to materialize the PNG package logos from the checked-in base64 source files. Restore NuGet packages, select `Release | x64`, and build the project.

The GitHub Actions workflow builds a signed sideload package and publishes the package plus its public development certificate as workflow artifacts.

## Install / test

The signed package is for personal Developer Mode testing only. The certificate must be trusted by the Xbox before installing the package. Use Xbox Device Portal on the same local network to install the app package and certificate as supported by the console's current Device Portal version.

## Current limitations

- This is a native application shell around the hosted web interface, not a native emulator implementation.
- The app requires an internet connection to load the hosted interface and EmulatorJS runtime.
- The native file picker and import bridge compile in CI but still require physical Xbox Series S testing.
- EmulatorJS/WebAssembly/WebGL, audio, file selection, save states, memory use, and sustained performance must be tested on a real console.
- Controller navigation and gameplay input must be checked with a physical Xbox controller.
- No ROMs, BIOS files, copyrighted game images, or signing private keys are stored in this repository.


## xemu companion

The dashboard includes an Original Xbox information panel linking to xemu's official requirements, compatibility list, and source repository. Its checklist is stored in local browser storage.

This does not bundle xemu, launch a desktop executable, accept Original Xbox game images, or provide PC-to-Xbox streaming. xemu is a separate desktop project; see [../docs/xemu-companion.md](../docs/xemu-companion.md) before considering a deeper integration.
