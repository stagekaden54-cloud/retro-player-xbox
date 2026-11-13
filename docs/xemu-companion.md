# Original Xbox companion: xemu

## Current feature

Retro Player Xbox includes an informational assistant for the open-source original Xbox emulator [xemu](https://xemu.app/). The assistant:

- Links to xemu's official system requirements, compatibility information, and source repository.
- Presents a five-step PC-readiness checklist.
- Saves checklist selections in this device's local browser storage.
- Opens only HTTPS links to `xemu.app` and the exact `github.com/xemu-project/xemu` repository through the native shell's external-link handler.

The assistant is a guide, not a playable emulator integration. It does not install or bundle xemu, accept original Xbox game images, launch xemu on Xbox, pair with a PC, or stream gameplay.

## Why it is not running on Xbox yet

The [official xemu project](https://xemu.app/docs/about/) documents support for Windows, macOS, and Linux and requires an OpenGL 4.0-compatible GPU. The current Retro Player Xbox native app is a Windows.Xbox UWP AppContainer that hosts a web interface in WebView2. That architecture does not provide a supported direct launch path for xemu's desktop application.

A true native Xbox port would be a separate engineering project. It would need an Xbox-compatible build target, graphics and input integration, file/storage handling, and testing on actual Series X|S hardware. No such port is included here.

A different future direction would be a PC-hosted xemu companion that streams to the Xbox client. That would require a separately installed PC host, a secure pairing protocol, video/audio transport, controller forwarding, and latency testing. This repository does not implement that streaming bridge.

## xemu requirements and compatibility

The official xemu documentation lists Windows, macOS, and Linux support and an OpenGL 4.0-compatible GPU requirement. Game compatibility is title-specific and changes as xemu develops. Always check the official compatibility information instead of assuming every game works.

xemu requires system files and game images that are not included with the emulator. Retro Player does not provide BIOS, firmware, or copyrighted game content. Only use files you are authorized to use.

## Licensing boundary

The xemu source repository identifies the project as GPL-2.0-only. Retro Player Xbox currently has a separate All Rights Reserved project license. This integration therefore uses links and explanatory material only; it does not copy xemu source code or redistribute xemu binaries.

Any future plan to modify, bundle, link, or redistribute xemu components must first receive a license-compliance review and preserve all applicable notices and source obligations. This document is not legal advice.

## External-link safety

The web client intercepts the assistant's resource links and sends a narrow message to the native shell. The native shell opens only HTTPS URLs on `xemu.app` or within `github.com/xemu-project/xemu`. It rejects other hosts and non-HTTPS schemes.
