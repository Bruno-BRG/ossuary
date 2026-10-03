# Architecture — Ossuary

Ossuary is a Tauri 2 application. Rust controls the window and a private .NET 10 process; TypeScript draws the terminal and delivers input.
C# holds the complete simulation and composes the screens as data, in a single independent library.

```text
KeyboardEvent / resize / preferences
                 │
                 ▼
        desktop/src (TypeScript)
                 │ invoke game_request
                 ▼
        desktop/src-tauri (Rust)
                 │ UTF-8 JSON over stdin/stdout
                 ▼
        engine/Ossuary.Desktop
        Input → Session → Commands → Game
                               │
                         GameHud / Ui
                               │
                          TextBuilder
                               │ Frame (+ sounds, water cells, spell animation)
                 ◄─────────────┘
        8×16 Canvas + glow + CRT
```

## Projects

| Project | Responsibility |
|---|---|
| `engine/Ossuary.Core` | simulation, entities, generation, RPG, combat, UI as data |
| `engine/Ossuary.Desktop` | .NET 10 host, physical keyboard, modals, protocol |
| `engine/Ossuary.Headless` | tests, ASCII dumps, soak and balance bots, animation viewer |
| `desktop/src` | renderer, bitmap font, title, layout, preferences, animation player |
| `desktop/src-tauri` | native window, process lifecycle, IPC validation and packaging |
| `assets/fonts` | the canonical font and its attribution |

## Dependencies

The Core does not depend on the window, the DOM, the transport or the renderer. New mechanics live in `Game.<Subject>.cs`; verbs belong to `Commands`.
`Ui.Draw` composes glyphs, fg/bg and bold into a `TextBuilder`. The host converts the grid into a frame; the front end never reimplements gameplay rules.

`Session.Key` keeps the priority: display shortcuts, death/victory, item selection, options, travel, targeting, shop, other panels and normal play.
`Input` translates `KeyboardEvent.code` with shift/ctrl and keeps no state.

## Transport

One request and one response per UTF-8 line. Operations: `new`, `load`, `title`, `play`, `key`, `resize`, `display`, `frame`. The response is `{ok,frame}` or `{ok:false,error}`.
Diagnostics go to stderr. Frames carry grids in row/column order, dimensions, the decimal seed, the turn, mode, panel, display tokens, and:
`sounds` (cues of the turn), `anim` (water cells) and `fx`/`fxMs` (a spell animation: one array per step of `[cell, glyph, fg, bg]` quadruples).

The uint64 seed is a decimal string. No file path, shell command or network operation can be requested through this protocol. Rust validates the
operations, serialises the requests and applies a 10-second timeout. Waiting happens off the graphics thread. Quitting the app reaps the engine.

The package uses private IPC and works locally. The web development mode uses a Vite endpoint on loopback only, with the same engine and a private copy of the
assemblies so the build stays available during the preview.

## Sound

Everything is synthesised in `desktop/src/audio.ts` with the Web Audio API: no audio files, no licences. The Core plays nothing: `Game.Cue/DrainCues` keep names
(`hit`, `kill`, `hurt`, `death`, `levelup`, `quest`, `magic`, `warn`, `good`, `stairs`, `door`, `pickup`), the host puts them in `Frame.Sounds` and the front end turns them into
square/triangle bleeps with a little noise. Menu blips (`click`, `move`, `confirm`, `cancel`) are the front end's own: they play when a key is pressed inside a panel.

Music is a score as data (`tracks`: bars of notes in beats and MIDI numbers) played by a look-ahead step sequencer with five voices (FM bell, 12.5% pulse, triangle bass, bowed
saw "cello", detuned-saw pad) plus noise ticks and thuds, all through one shared reverb. `trackFor` picks the track from what is on screen: **title** ("Phosphor & Bone", 24 bars of 6/4
at 66 BPM in D minor, which also plays during the intro), **dungeon**, **road** (overworld, D Dorian) and **town** (F major); a dead hero hears nothing. Tracks fade in and out. The scores are
deterministic and the sequencer never touches the simulation RNG. Master × music and master × effects volumes come from the frame. Audio only unlocks after a key press or click (browser policy).

## Animation

The engine resolves an effect at once and only *records* how it should look (`Fx.cs`, `Game.Fx.cs`): a timeline of steps, each a set of overlay cells in map coordinates, with no `Rng` and no
state, so recording never changes the game. `Session.BuildFx` maps those cells to the screen grid (camera, square tiles, theme) and `desktop/src/fx.ts` plays them over the finished frame, about 45 ms a step,
without asking the engine for anything. A new key or frame cuts an animation short. See [`spells-and-items.md`](spells-and-items.md#animations).

## Turns and display

Only simulation commands advance turns. Resizing, rendering, persisting options and checking window state do not change the game. The interface accepts one action in flight at a time and never queues turns.
The auto-repeat of a held key is sent with `repeat: true`; the engine (`Session.KeyRepeat`) honours it only for calm walking (`Game.CanKeepWalking`: no hostile in view, nothing underfoot,
no damage or message on the previous step) and ignores the rest.
Theme, CRT and scale are user data, persisted in localStorage; starting a run from the title or restarting after death keeps these preferences.

## Core data

- `Game.Mode`: Dungeon, Overworld, TownMap, GameOver, Won.
- `Game.Map`: dungeon/town; the regional world is in `Game.World`.
- `Player`: HP, hunger, status effects, equipment, skills, gold and inventory.
- Monsters: energy, speed, alertness, perception and AI.
- `UiState`: panels, targeting/travel/shop cursors and item selection.
- `Log` and `Transcript`: recent messages and history.
- `Rng`: deterministic generation and mechanics for the same seed and input.

Operational reference: [desktop.md](desktop.md).
