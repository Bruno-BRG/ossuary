# Tauri desktop — Ossuary

The app is Tauri 2 with Rust, a TypeScript frontend and a bitmap Canvas terminal.
The independent C# engine runs as a private, self-contained .NET 10 executable
packaged with the app. All simulation lives in `engine/Ossuary.Core`;
input and modals in `engine/Ossuary.Desktop`; validation in `engine/Ossuary.Headless`.

## Final structure

```text
engine/
  Ossuary.Core/        simulation and UI as data
  Ossuary.Desktop/     host, Session, Input, protocol
  Ossuary.Headless/    console and Tests
desktop/
  src/                bitmap renderer, title, layout, preferences
  src-tauri/          Rust shell, window, IPC and packaging
assets/fonts/         unscii-16.hex and NOTICE.md
docs/                 documentation and screenshots
```

## Operation

```powershell
.\desktop.ps1 setup
.\desktop.ps1 dev
.\desktop.ps1 web
.\fastcheck.ps1
.\headless.ps1 test
.\headless.ps1 dump panels
.\headless.ps1 soak 50 500
.\check.ps1
.\desktop.ps1 build
```

Setup installs the local .NET 10 SDK and npm dependencies. Publishing creates a
self-contained engine: players install neither .NET nor development tools.
Tauri uses the Windows WebView2. The validated distribution target is Windows x64.

## Contract

The frontend sends physical keys and receives complete frames. Rust accepts only
game operations, validates the uint64 seed and serializes requests to the process.
The protocol is UTF-8 JSON over stdin/stdout. The package does not depend on an HTTP
server; the Vite endpoint only exists during web development on loopback.

The grid holds codepoint, fg, bg and bold. Palette tokens and CRT parameters
come from the Core. Seeds are decimal strings; JavaScript never converts
uint64 to Number. Drawing and resizing do not advance turns.

The transport timeout is 10 seconds. Failures surface as errors and never
silently start a new game. Closing the window kills and reaps the engine.
The frontend gets no shell permission or general filesystem access.

## Resources

Font: `assets/fonts/unscii-16.hex`, 8×16 bitmap by viznut, public domain.
Vite copies the font to the public assets; the headless suite also gets a
copy. Its coverage test fails if the resource is missing.
The icon is generated from the `@` glyph using canonical palette tokens.

## Distribution

The build produces `ossuary.exe`, `ossuary-engine.exe` and the NSIS installer in
`desktop/src-tauri/target/release/bundle/nsis/`. Distribute the installer or the
two executables side by side. The self-contained runtime may extract native
libraries into the user's temp directory on first run.

## Verification

The suite covers generation, combat, items, economy, RPG and victory; panel flow,
choices, aiming, travel, shop, options and restart; font/DPI/uint64;
IPC of the published engine and deterministic frame comparison; Rust validation and
transport. Real frontend captures live in `docs/shots/`.

The engine is the definitive implementation of the rules, with no competing copy
in another language. The split of responsibilities is in
[architecture.md](architecture.md).

## Native startup

After packaging, `desktop.ps1 build` launches the executable with `--smoke-test` in a hidden window. The test requires the bitmap font loaded, a frame received over IPC and pixels drawn on the WebView2 Canvas. The app exits with code 0 on confirmation; a startup failure exits with 2 after 20 seconds. The pipeline also enforces a 30-second limit and reaps the process on timeout.

## Protocol: save and title

Operations: `new`, `load`, `key`, `resize`, `display`, `frame`. `load` rebuilds the saved
run (seed + key log) and returns the frame. The frame carries `started` (a run is in
progress), `hasSave`, `saveInfo`, `toTitle` (a single frame, after "Main menu") and the
`master`/`music`/`effects` volumes, plus `sounds` (the turn's cues), `anim` (water cells) and `fx`/`fxMs` (spell animation: one array per step of
[cell, glyph, fg, bg] quadruples, see `spells-and-items.md`). Rust only relays; save, binds and menu logic live in
the Core and `engine/Ossuary.Desktop` (`Session`, `SaveStore`).
