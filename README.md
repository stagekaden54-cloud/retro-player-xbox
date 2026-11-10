# Retro Player Xbox

A controller-first Xbox Developer Mode client for Retro Player.

## Project status

**Stage:** Native Xbox UWP shell scaffold + EmulatorJS web handoff (0.4.0)

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
- Search and sorting for imported local games (recently added, recently played, title, and file size).

- Responsive, TV-friendly home screen.
- Sample catalog with search and Nintendo 64 / PlayStation / Game Boy Advance filters.
- Game detail dialog and locally saved favorites.
- Keyboard and browser Gamepad API focus navigation.
- The native shell hosts the Xbox library in WebView2 and forwards D-pad / A / B from the UWP Gamepad API.
- The library can hand off to the existing EmulatorJS-based Retro Player website. The handoff is not yet a per-game ROM launch.

All displayed game entries are sample metadata, not installed content. No ROMs, BIOS files, or copyrighted artwork are included.

## Native Xbox shell

The `XboxShell/` folder contains a UWP + WinUI 2 WebView2 host and a GitHub Actions workflow for building a Developer Mode package. See [XboxShell/README.md](XboxShell/README.md) and [docs/emulator-runtime.md](docs/emulator-runtime.md).

## Next development sequence

1. Complete a successful Windows CI build of the signed x64 UWP package.
2. Install and test the shell on a real Xbox Series S in Developer Mode.
3. Validate the existing EmulatorJS site inside Xbox WebView2.
4. Add per-game core mapping and user-selected file access after the runtime checks pass.

See [docs/architecture.md](docs/architecture.md) for the initial design.
