# AGENTS.md — Ossuary

Dark-fantasy ASCII roguelike. **Tauri 2 + Rust + TypeScript/Canvas** app in `desktop/`.
Independent C# simulation in `engine/Ossuary.Core`, self-contained local .NET 10 host in
`engine/Ossuary.Desktop`, tests in `engine/Ossuary.Headless`.

## Golden rule

The Core contains only simulation and UI as data. It cannot depend on Tauri, the DOM,
rendering, the platform or window services. Every verb lives in `Commands`, and every screen
composition produces a `TextBuilder`.

## Layout

- `engine/Ossuary.Core/`: Game partial, Commands, Ui, maps, combat, items, entities, RPG, quest,
  generation, FOV, pathfinding, RNG and themes.
- `engine/Ossuary.Desktop/`: Input, Session, JSON protocol over stdin/stdout.
- `engine/Ossuary.Headless/`: console test/dump/soak and Tests.
- `desktop/src/`: frontend, bitmap terminal, title, grid fitting and preferences.
- `desktop/src-tauri/`: Rust window, local transport, process lifecycle and packaging.
- `assets/fonts/unscii-16.hex`: canonical 8×16 font; attribution in NOTICE.md.
- `docs/`: documentation in English, by topic: `game/`, `design/`, `tech/`, `audio/`, `roadmap/`; index in `docs/README.md`; real captures in `docs/shots/`.
- `docs/todo.md`: backlog and progress tracking, by topic (combat, magic, items, crafting, economy, towns, world...), one line per item.

## Commands

| Script | Purpose |
|---|---|
| `desktop.ps1 setup` | prepares the local .NET 10 SDK and npm dependencies |
| `desktop.ps1 dev` | opens the Tauri app with frontend reload |
| `desktop.ps1 web` | loopback browser with the same real engine |
| `fastcheck.ps1` | C# and TypeScript type-check |
| `headless.ps1 test` | simulation and desktop-flow suite |
| `headless.ps1 dump [level\|overworld\|panels\|town]` | ASCII frames |
| `headless.ps1 fx <spell>` | a spell animation as ASCII (`fx list`, `fx all`) |
| `headless.ps1 soak <seeds> <turns>` | random bot |
| `headless.ps1 balance <seeds> <turns> [dive]` | class×race balance bot (see docs/design/balance.md) |
| `check.ps1` | full suite, frontend, packaged IPC and Rust |
| `test.ps1` / `run-tests.ps1` | headless test aliases |
| `desktop.ps1 build` | Windows x64 executable + NSIS installer |

MSBuild is incremental and detects source changes. The web preview runs a private temporary
copy of the assemblies so it does not block compilation on Windows.
The package ships `ossuary.exe` and the engine `ossuary-engine.exe` side by side.

## Conventions

- `Game` is partial: new mechanics get `Game.<Subject>.cs`.
- New verb: a case in `Commands.Execute` + a `Do*` method; keep strings short.
- `Input` is stateless; modals and input priority live in `Session.Key`.
- `Session.Draw` composes the frame; `TerminalRenderer.draw` only consumes the grid.
- The frontend does not advance turns on draw, resize or persisting preferences.
- One action in flight per window; do not accumulate keyboard autorepeat.
- A uint64 seed travels in the JSON as a decimal string, never converted to Number.
- A new field in the JSON protocol requires updating all three sides: `Request` in `engine/Ossuary.Desktop/Program.cs`,
  `desktop/src/protocol.ts` and the `Request` struct in `desktop/src-tauri/src/main.rs` (it uses `deny_unknown_fields`:
  a forgotten field makes the packaged app refuse every request).
- Player-visible text is born in English and gets a PT translation in `Loc.cs`; `Say` and
  `TextBuilder` already translate (see docs/tech/languages.md). The Core never picks the language by itself.
- Diagnostic errors go to stderr; stdout is exclusive to the protocol.
- Do not duplicate the simulation in TypeScript or Rust.

## Visual: Phosphor & Bone

- 80s PC terminal, indigo background, colored glyphs, sparing glow.
- Game colors only in `engine/Ossuary.Core/Theme.cs`, except bestiary content colors.
  The CSS receives tokens from the frame; do not create another hardcoded palette.
- Each cell holds glyph, fg, bg and bold. The background should carry meaning.
- Fixed 8×16 bitmap font, no smoothing, integer scale in physical pixels.
- A new glyph goes through GlyphSet and the GlyphCoverage/GlyphsInFont tests.
- Visual variants are hash(x,y) at draw time, never simulation RNG consumption.
- Theme, CRT and scale belong to the user: DisplaySettings + localStorage.
- A new font requires attribution and license in docs/tech/visual.md and assets/fonts/NOTICE.md.

## Work and validation

0. Before starting, read `docs/todo.md`; when finished, update it in its one format (`[~]` with *has / left* when partial,
   a finished item moves to *Done* with its date, new ideas go in their topic in one line). It is our progress tracking.
1. Type-check while editing.
2. Headless suite before and after Core changes; dump panels for layout.
3. Full suite before closing and a build after distributable changes.
4. Update the documentation when a system, controls, resources or the pipeline change.
5. Preserve other sessions' local changes; do not commit without being asked.
