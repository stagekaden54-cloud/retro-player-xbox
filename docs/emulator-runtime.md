# Emulator runtime integration plan

## First real runtime handoff

The native Xbox shell loads the Retro Player Xbox library from the Vercel project. The library's **Open Retro Player** action navigates the same WebView2 instance to the existing EmulatorJS-based player:

- Library UI: https://retro-player-xbox.vercel.app/
- Existing emulator website: https://ps1-player.vercel.app/

The Xbox host allowlists both HTTPS origins. The native controller bridge forwards dashboard navigation and supported button actions to the web page. The native shell owns dashboard input on Xbox to avoid duplicate browser/native activations. During emulation, the shell does not inject dashboard input; the emulator is expected to read the controller through the WebView2 browser Gamepad API.

This is an initial runtime handoff, not per-title launch integration. The demo catalog entries are metadata only and do not point to ROM files.

## What must be validated on a Series S

1. The Xbox WebView2 runtime can load both sites.
2. EmulatorJS initializes without browser API errors.
3. WebAssembly and WebGL are available to the player.
4. Audio output works after the user starts a game.
5. The native dashboard controller bridge and the emulator's browser Gamepad API do not conflict.
6. User-selected game files can be read by the player in the Xbox WebView2 environment.
7. Memory use and frame pacing remain stable during longer sessions.

## Next integration step

After those checks pass, replace the generic handoff with a real launch contract:

- game metadata stores the emulator system/core identifier;
- the user chooses a local game file through a supported Xbox file picker;
- the host passes a scoped file handle or supported stream to the player;
- the player starts the selected EmulatorJS core and reports ready/error state to the library.

Do not bundle copyrighted ROMs or BIOS files. Keep user-provided game content outside the source repository.


## Original Xbox: separate xemu-uwp runtime

Original Xbox games launch in the separately installed [xemu-uwp](https://github.com/skitzellez/xemu-uwp) Xbox Dev Mode app, not inside EmulatorJS or the Retro Player WebView2 process.

The native shell opens a file picker for `.iso` / `.xiso` images and validates that the selected path belongs to removable USB storage. It launches the xemu app with its documented URI format:

```text
xemu:?disc=<URL-escaped USB path>&launchOnExit=<URL-escaped return URI>
```

The return URI is `retroplayerxbox:resume`; the Retro Player package registers that protocol and `App.OnActivated` restores the native UI after xemu exits.

The package declares UWP's `removableStorage` capability with `.iso` and `.xiso` file-type associations so the image remains on USB and is visible to both apps. App-private staged imports are not suitable for this handoff. Do not add broad filesystem access.

## What still needs physical-console validation

1. Install the separate xemu UWP package on a Series S in Developer Mode.
2. Verify the selected USB `.iso` / `.xiso` is visible to xemu and starts.
3. Verify xemu's quit / `launchOnExit` route returns to Retro Player.
4. Check controller handoff, suspension/resume behavior, and sustained performance per game.
5. Verify failure messaging when xemu is not installed or the user cancels the picker.

No Original Xbox FPS, compatibility percentage, or Series S performance result is claimed before those tests pass. The xemu port is experimental; check its own release notes and compatibility reports.
