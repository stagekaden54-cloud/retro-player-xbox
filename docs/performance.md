# Retro Player Xbox performance update (0.6.0)

## Changes

- The personal library caches its IndexedDB result until a write or delete invalidates it.
- The personal library renders 24 entries at a time and exposes a Load more control. Search still checks the full cached collection.
- Game-card actions use delegated listeners on the library containers rather than creating new listeners after every render.
- Personal-library search is debounced to avoid repeating filtering and DOM work on every keystroke.
- On the native Xbox shell, the dashboard Gamepad API animation loop stops after the host flag is enabled. The shell continues to poll the native controller itself.
- The UWP shell collects button, trigger, and stick-edge actions for a controller poll and sends them in one WebView2 script call.
- An optional performance panel samples browser animation-frame cadence only while open, reports the documented EmulatorJS EJS_ready callback timing, and shows JavaScript heap usage only when the browser exposes performance.memory.

## Measurement limits

The display-cadence estimate is the browser's observed requestAnimationFrame cadence while the monitor is open. It is not EmulatorJS's internal frame counter and must not be presented as a guaranteed game FPS value. The extra sampling loop adds a small amount of work, which is why it is opt-in.

The JavaScript heap estimate is not total process or console memory. Some WebView2 environments do not expose it; the UI then reports that it is unavailable.

The core initialization timer ends when EmulatorJS calls its documented EJS_ready callback. The script-download event is reported separately and is not treated as proof that the core is ready.

## Xbox Series X|S test checklist

Test from a real Developer Mode installation before drawing conclusions:

1. Compare opening the library, searching, sorting, and loading more games with a small collection and a large collection.
2. Hold and rapidly tap D-pad, A/B/X/Y, bumpers, triggers, and diagonal stick directions. Confirm no missed or duplicated dashboard actions.
3. Launch one PS1 game and one N64 game. Record core initialization time and the monitor's displayed cadence with the panel closed and open.
4. Test a 10–15 minute session for input responsiveness, audio stability, visual stutter, and memory growth.
5. Confirm the local library still exists after leaving the emulator and reopening the app.

Do not enable EmulatorJS threading as a generic performance tweak: its documented requirements include cross-origin isolation headers and SharedArrayBuffer, which must be verified for the deployed host first.
