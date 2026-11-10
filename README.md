# Retro Player Xbox

A controller-first Xbox Developer Mode client for Retro Player.

## Project status

**Stage:** EmulatorJS web client with PS1 and Nintendo 64 file-to-core routing, paged library, and optional runtime monitor (0.6.0)

This repository is separate from the existing Retro Player website. The first target is Xbox Series X|S in Developer Mode.

## Goals

- A television-friendly interface.
- Full navigation using an Xbox controller.
- A game library with metadata and artwork.
- A clear emulator integration boundary.
- Reuse compatible parts of the existing Retro Player web experience where technically practical.

## Important technical boundaries

- This is a sideloaded development application, not an Xbox Store release.
- The Xbox app shell and existing Vercel website are separate deliverables.
- Emulator compatibility must be tested on the target console. A web page working in a desktop browser does not prove it will work in the Xbox app's WebView.
- The project will not include game ROMs or copyrighted game assets. Users must provide content they are authorized to use.
- Never commit signing certificates, passwords, tokens, ROMs, BIOS files, or other private material.

## Current prototype features

- Settings dashboard with display preferences, local library/storage estimates, and controller connection status.
- Fullscreen TV mode with a layout-only fallback when the host does not support the Fullscreen API.
- Persistent large-interface and reduced-motion preferences.

- Recently played shelf with per-game session counts.
- Large personal libraries render 24 games at a time, with cached IndexedDB reads and delegated actions.
- Optional emulator runtime monitor for display-cadence estimates, EmulatorJS core initialization time, and browser-exposed JavaScript heap usage.
- Native controller actions are batched into one WebView2 message per poll instead of separate script calls for each button.
- Search and sorting for imported local games (recently added, recently played, title, and file size).

- Responsive, TV-friendly home screen.
- Sample catalog with search and Nintendo 64 / PlayStation / Game Boy Advance filters.
- Game detail dialog and locally saved favorites.
- Keyboard and browser Gamepad API focus navigation.
- The native shell hosts the Xbox library in WebView2 and forwards D-pad / A / B from the UWP Gamepad API.
- The web client imports single-file PS1 images (BIN, ISO, IMG, PBP, CHD) and N64 ROM images (Z64, N64, V64) into local IndexedDB storage.
- Saved games retain their system type and launch with the matching EmulatorJS core (`psx` or `n64`).
- Existing library entries without a saved system field continue to launch as PlayStation games.

All displayed game entries are sample metadata, not installed content. No ROMs, BIOS files, or copyrighted artwork are included.

## Performance update (0.6.0)

See [docs/performance.md](docs/performance.md) for implementation details and the Series X|S benchmark checklist. The runtime monitor is diagnostic only: its display-cadence figure is not EmulatorJS's internal core FPS, and JavaScript heap data is reported only when the browser exposes it. No Xbox performance result is claimed before testing on the console.

## Native Xbox shell

The `XboxShell/` folder contains a UWP + WinUI 2 WebView2 host and a GitHub Actions workflow for building a Developer Mode package. See [XboxShell/README.md](XboxShell/README.md) and [docs/emulator-runtime.md](docs/emulator-runtime.md).

## Next development sequence

1. Complete a successful Windows CI build of the signed x64 UWP package.
2. Install and test the shell on a real Xbox Series S in Developer Mode.
3. Test PS1 and N64 EmulatorJS cores inside Xbox WebView2, including controller input, audio, and save states.
4. Tune N64 compatibility and performance on the physical console.

See [docs/architecture.md](docs/architecture.md) for the initial design.
