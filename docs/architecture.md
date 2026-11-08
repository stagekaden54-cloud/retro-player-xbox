# Initial architecture

## Target

Xbox Series S first, with Xbox Series X compatibility considered alongside it. Xbox One compatibility is not assumed; it will be evaluated separately.

## Layers

### 1. Xbox application shell

A native Xbox-compatible application owns startup, navigation, focus, settings, and controller input. It must be packaged and signed for Developer Mode deployment.

### 2. Retro Player presentation

The existing website at https://ps1-player.vercel.app/ is a separate web product. Reuse of its UI or emulator loader is conditional on the Xbox runtime supporting the required browser APIs, graphics features, audio behavior, and input APIs.

### 3. Game library

The library stores user-managed entries and metadata. It should not bundle ROMs, firmware, BIOS images, or copyrighted artwork.

### 4. Emulator integration

Keep emulator launch behind an interface so we can evaluate:
- an embedded web runtime, if its APIs and performance are sufficient;
- a native emulator core or separately packaged emulator, where supported and legally distributable.

Do not assume an emulator can be launched from the app merely because a web version runs on a PC.

## Controller-first interaction

- D-pad and left stick: move focus.
- A: activate the focused control.
- B: go back or close a dialog.
- Menu: open the application menu.
- Every screen must expose a visible focus indicator.
- No essential action may require a mouse or touchscreen.

## Compatibility gates

Before choosing the emulator architecture, test on actual Xbox Series S hardware:
1. App installation and launch in Developer Mode.
2. Controller input and focus navigation.
3. WebView graphics support, including WebGL requirements if using EmulatorJS.
4. Audio output and lifecycle behavior.
5. Memory use and sustained performance.

## Security and distribution

- Never commit signing keys, certificates, tokens, ROMs, or BIOS files.
- Treat imported filenames and metadata as untrusted input.
- Keep any future file access scoped to user-selected locations.
- This is an independent community project and is not affiliated with Microsoft.
