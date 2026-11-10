# Retro Player Xbox architecture

## Target

Xbox Series S first, then Series X. Xbox One compatibility is a separate validation target.

## Native app shell

The first native host is a UWP app targeting the Windows.Xbox device family. It uses WinUI 2 and the Microsoft WebView2 control. Microsoft documents WebView2 for Xbox and provides a UWP WebView2 getting-started path.

The shell:
- loads the Retro Player Xbox web library from the separately deployed Vercel project;
- restricts top-level navigation to the Xbox UI site and the existing Retro Player site;
- polls the native UWP Gamepad API and forwards D-pad, left-stick navigation, A/B/X/Y, bumpers, and trigger-press actions to the dashboard;
- keeps web UI and emulator runtime code separate from the native host.

## Web library and emulator runtime

- Library UI: https://retro-player-xbox.vercel.app/
- Existing EmulatorJS player: https://ps1-player.vercel.app/

The library's initial launch action hands off to the existing EmulatorJS-based player in the same WebView2 control. This is not yet a per-game launch integration: sample catalog entries are metadata only and do not contain ROM files or direct file references.

## Controller

- D-pad and left stick: move dashboard focus, with a dead zone and paced repeat.
- A: activate the focused control; B: close the active dialog or clear focus.
- X: activate the favorite action when a game details dialog is open; Y: close that dialog.
- LB/RB and LT/RT are detected and forwarded as native actions for future bindings.
- On Xbox, the native shell owns dashboard navigation; the browser Gamepad API is not also used for dashboard input, avoiding duplicate activation. During emulation, native dashboard forwarding is intentionally not injected so the emulator can receive the browser's own gamepad input.

## Emulator integration boundary

Keep emulator launch behind an adapter. The first real runtime handoff uses the existing EmulatorJS-based site. The next phase must add:
- a real system/core mapping per library entry;
- user-selected game file access that works in the Xbox sandbox;
- a safe host-to-web file/launch contract;
- runtime ready/error reporting.

Do not assume a file picker or emulator core works on Xbox until tested on the actual Series S.

## Compatibility gates

1. UWP package installs and launches in Developer Mode.
2. WebView2 loads both sites.
3. WebAssembly and WebGL work in the player.
4. Audio and controller input work.
5. User-selected game files can be read without broad file-system access.
6. Memory and sustained frame pacing remain acceptable.

## Security and distribution

- Never commit signing keys, private certificates, tokens, ROMs, or BIOS files.
- The CI workflow generates a temporary development signing certificate for the package; only the public certificate is included in the artifact.
- Treat imported filenames and metadata as untrusted.
- This is an independent project and is not affiliated with Microsoft.
