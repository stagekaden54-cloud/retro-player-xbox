# Retro Player Xbox

A controller-first Xbox Developer Mode client for Retro Player.

## Project status

**Stage:** Interactive library prototype (0.2.0)

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

- Responsive, TV-friendly home screen.
- Sample catalog with search and Nintendo 64 / PlayStation / Game Boy Advance filters.
- Game detail dialog and locally saved favorites.
- Keyboard and browser Gamepad API focus navigation.
- Launch action is intentionally a preview message; no emulator is connected.

All displayed game entries are sample metadata, not installed content. No ROMs, BIOS files, or copyrighted artwork are included.

## Next development sequence

1. Test focus navigation and rendering on Xbox hardware.
2. Establish the Xbox app shell and package configuration.
3. Evaluate the supported WebView/emulator path on the console.
4. Add safe user-selected file access after platform compatibility is proven.
5. Integrate emulator launch and controller mapping only after compatibility is demonstrated.

See [docs/architecture.md](docs/architecture.md) for the initial design.
