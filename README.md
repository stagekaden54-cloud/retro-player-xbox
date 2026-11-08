# Retro Player Xbox

A controller-first Xbox Developer Mode client for Retro Player.

## Project status

**Stage:** Foundation / architecture

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

## Initial development sequence

1. Establish the Xbox app shell and package configuration.
2. Implement gamepad-first navigation and focus behavior.
3. Add the library view and local metadata model.
4. Evaluate the supported WebView/emulator path on real Xbox hardware.
5. Integrate emulator launch and controller mapping only after compatibility is demonstrated.

See [docs/architecture.md](docs/architecture.md) for the initial design.
