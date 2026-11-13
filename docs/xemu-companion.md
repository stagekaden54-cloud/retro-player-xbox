# Original Xbox: xemu-uwp integration

## Selected emulator

Retro Player Xbox uses [skitzellez/xemu-uwp](https://github.com/skitzellez/xemu-uwp), an unofficial UWP port of the open-source xemu Original Xbox emulator for Xbox Series X|S Developer Mode.

- [Latest xemu-uwp releases](https://github.com/skitzellez/xemu-uwp/releases/latest)
- [Xbox UWP setup and compatibility notes](https://github.com/skitzellez/xemu-uwp/blob/uwp/README-UWP.md)
- [Upstream xemu project](https://xemu.app/)

The emulator is a separate app. Retro Player does not copy xemu source code, build its binaries, or redistribute its AppX package. This keeps the integration boundary separate from Retro Player's All Rights Reserved codebase and the emulator's GPL-2.0 license.

## How launching works

1. Put an Original Xbox `.iso` or `.xiso` image that you are authorized to use on a USB drive.
2. Install xemu-uwp separately on the Xbox through Device Portal while the console is in Developer Mode.
3. In Dev Home, set xemu's app type to **Game**, as its maintainer documents.
4. Open the Original Xbox panel in Retro Player and choose **Choose Xbox disc and play**.
5. Retro Player's native UWP picker selects a file under removable storage. The shell checks that the selected path is inside a removable-storage root.
6. The shell launches xemu through its documented URI format:

```text
xemu:?disc=<URL-escaped path>&launchOnExit=<URL-escaped URI>
```

The disc path is URL-escaped before it is placed in the query. The return URI is `retroplayerxbox:resume`. Retro Player registers that protocol in `Package.appxmanifest` and handles it in `App.OnActivated`, restoring the library when xemu exits.

The game image remains on USB. It is not copied into Retro Player's private app folder or imported into the existing PS1/N64 IndexedDB library. The Retro Player package declares UWP's `removableStorage` capability and the `.iso` / `.xiso` file-type associations required for access; it does not request `broadFileSystemAccess`.

## Requirements and limitations

- The Xbox must be in Developer Mode, and xemu-uwp must be installed separately.
- Use a removable USB drive visible to both packages. The native picker is restricted to `.iso` and `.xiso`.
- Follow xemu-uwp's release notes for system-file setup, supported image formats, controls, and known game issues.
- The emulator fork is experimental. Its detailed technical README describes results observed on Series X; Series S performance must be validated on the actual console and per game.
- Original Xbox images are not yet cataloged in the persistent Retro Player library. This first implementation provides a direct picker-to-emulator handoff.
- If xemu is not installed or does not register the `xemu:` URI scheme, Retro Player displays an install prompt instead of claiming the game launched.

## Licensing and distribution boundary

xemu-uwp is an unofficial fork of xemu/QEMU under GPL-2.0. Retro Player only calls its public URI activation contract and links to its project resources. No xemu source, emulator binaries, BIOS files, firmware, or game images are committed or bundled here.

Any future plan to copy, modify, link, package, or redistribute xemu components must receive a separate license-compliance review and preserve all applicable notices and source obligations. This document is not legal advice.

## External-link safety

The web client intercepts only its authored resource links. The native shell opens HTTPS resources on `xemu.app` or within the exact GitHub repositories `xemu-project/xemu` and `skitzellez/xemu-uwp`. It rejects other hosts and non-HTTPS schemes.
