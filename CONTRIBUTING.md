# Contributing to Ossuary

**English** · [Português (Brasil)](CONTRIBUTING.pt-BR.md)

Thanks for wanting to help. This guide explains **how the project is organised**, **what not to break**, and **how to add the most common things**
(spells, items, animations, translations) in a few lines. Code and commits may be in English or Portuguese; the documentation in `docs/` is in English.

> **Version and license.** The game is at **Version 16** (`0.16.0` in the manifests). The project is under the [MIT license](LICENSE): by contributing you agree
> that your code is distributed under it. The `unscii-16` font has its own attribution ([assets/fonts/NOTICE.md](assets/fonts/NOTICE.md)).

## Contents

1. [Before you start](#before-you-start)
2. [Set up](#set-up)
3. [Golden rules](#golden-rules)
4. [Workflow](#workflow)
5. [Recipes: adding things](#recipes-adding-things)
6. [Tests](#tests)
7. [Text and translations](#text-and-translations)
8. [Visuals](#visuals)
9. [Pull requests](#pull-requests)
10. [Bugs and ideas](#bugs-and-ideas)

## Before you start

- Read the [README](README.md), [`docs/game/overview.md`](docs/game/overview.md) and [`docs/tech/architecture.md`](docs/tech/architecture.md).
- **Read [`docs/todo.md`](docs/todo.md)**: the living backlog (what is missing, what is done). If your idea is there, great; if not, add it under the right category.
- [`AGENTS.md`](AGENTS.md) sums up the rules for coding agents; they apply to people too.
- For anything big (a new system, a rules change, a protocol change), **open an issue first**.

## Set up

Target: **Windows x64**. Scripts are PowerShell (`.ps1`).

| Tool | Notes |
|---|---|
| Git, PowerShell | use `-ExecutionPolicy Bypass` if scripts are blocked |
| Node.js | 22.12+ or 24+ |
| Rust | stable, MSVC toolchain |
| Visual Studio Build Tools | "Desktop development with C++" + Windows SDK |
| .NET 10 SDK | installed **locally** into `.tools/` by `setup` |

```powershell
git clone https://github.com/Bruno-BRG/ossuary.git
cd ossuary
.\desktop.ps1 setup     # local .NET 10 SDK + npm packages
.\desktop.ps1 dev       # Tauri app with frontend hot reload
.\desktop.ps1 web       # the same engine in a browser (loopback)
```

Details and troubleshooting: [INSTALL.md](INSTALL.md) and [`docs/tech/build-and-test.md`](docs/tech/build-and-test.md).
The engine and its tests (`engine/`) only need the .NET SDK, so you can work on game rules without Rust or Tauri.

## Golden rules

1. **The Core holds only simulation and UI-as-data.** `engine/Ossuary.Core` must not depend on Tauri, the DOM, rendering, the platform or windowing.
   Every verb lives in `Commands`; every screen is composed as a `TextBuilder`.
2. **Do not duplicate the simulation in TypeScript or Rust.** The front end only draws the grid the engine sends and plays animations the engine records.
3. **The seed is the save.** A save is seed + keys, so anything that affects the simulation must be deterministic: use the game's `Rng`, never `System.Random`, the clock or dictionary order.
   **Visual** variation uses `hash(x, y)` (see `Shapes.Hash`), never the simulation's `Rng`, otherwise recording an animation would change the game.
4. **Changed a rule, change the save.** If a change alters what a seed produces, bump `SaveData.Version` in `engine/Ossuary.Desktop/SaveStore.cs` (old saves stop loading on purpose).
5. **A new field in the JSON protocol** must be added on **all three sides**: `Request` in `engine/Ossuary.Desktop/Program.cs`, `desktop/src/protocol.ts`
   and the `Request` struct in `desktop/src-tauri/src/main.rs` (it uses `deny_unknown_fields`: a forgotten field makes the packaged app refuse every request).
   Fields only in the response *frame* (like `fx`) do not need the Rust side.
6. **stdout belongs to the protocol.** Diagnostics go to `stderr`.
7. **One action at a time.** The front end never advances turns when drawing, resizing or saving preferences.
8. **Preserve other people's work.** Do not revert changes you do not understand; ask.

## Workflow

1. Branch from `main` (`feature/…`, `fix/…`, `docs/…`).
2. Run **`.\headless.ps1 test` before** touching the Core (to know the baseline) and **after**.
3. Keep changes small and focused; type-check while editing (`.\fastcheck.ps1`).
4. Update the documentation when you change a system, controls, features or the pipeline (`docs/`).
5. **Update [`docs/todo.md`](docs/todo.md)** when done: mark `[x]` with a date and where it lives, `[~]` if partial, and note new ideas under the right category.
6. Run the full suite (`.\check.ps1`) before opening the PR, and a build (`.\desktop.ps1 build`) if you changed something that ships.

Code style: write like the neighbouring code (names, comment density, language). Comments explain **why**.

- `Game` is `partial`: a new mechanic gets a `Game.<Subject>.cs`.
- A new verb is a `case` in `Commands.Execute` plus a `Do*` method; keep strings short.
- `Input` is stateless; modals and input priority live in `Session.Key`.
- `Session.Draw` composes the frame; `TerminalRenderer.draw` only consumes the grid.
- A `uint64` seed travels in JSON as a **decimal string**, never a `Number`.

## Recipes: adding things

Everything is data wherever possible. The full catalogue and the details are in [`docs/design/spells-and-items.md`](docs/design/spells-and-items.md).

### A spell

1. One line in `engine/Ossuary.Core/Magic/Spells.*.cs`. Example:
   ```csharp
   S("cone-of-cold", "Cone of Cold", 4, E, 10, Cn, 5, "A cone of killing frost…", 5)
       .Dmg(Cold, 4, 6).Ride(Rider.Slow, 70, 8).Look(FxKind.Cone, Elem.Cold)
   ```
   `Dmg` (damage), `Ride` (status: hold, poison, fear…), `Aura` (buff), `Call` (summon), `Surf` (surface), `Shove`, `Drain`, `Chain`, `Scatter`, `Taint`, `Spec` (hand-coded effect) and `Look` (the animation).
2. The **animation**: `.Look(FxKind, Elem, glyph)`. See the result without opening the window: `.\headless.ps1 fx cone-of-cold`.
3. Add the id to a **book** (`Magic/Spells.Books.cs`) and the Portuguese **translation** in `Loc.Spells.cs`.
4. A new buff is one line in `Magic/SpellBuffs.cs` (plus its translation). A new summoned creature is an `Ally(...)` at the end of `Entities/Bestiary.cs`.

The `ArsenalTests` suite sweeps the catalogue: every spell is in a book, has an animation and a translation, casts without error and changes something in the world.
If your spell is legitimately silent in an empty arena (light, find traps…), add it to the test's `Quiet` list.

### An item

- **Base** (weapon, armour, ring…): `Items/Catalogue.More.cs`; bonus numbers in `Items/ItemEffects.cs`.
- **Affix** (prefix/suffix): `Items/Affixes.cs`. **Unique**: one line in `Items/Artifacts.More.cs` (branch, level, chance, spells it lends).
- **Wand, scroll or potion** that casts a spell: one line in `Items/ItemSpells.cs`.
- Which spells suit which kind of item (imbued items): `Magic/SpellFit.cs`.
- A new glyph goes through `GlyphSet` and the `GlyphCoverage` / `GlyphsInFont` tests.

### An animation

One piece in `FxLib` (`engine/Ossuary.Core/Fx.cs`) that writes steps of map cells and returns the step where it ends. Use the per-element ramps (`FxLib.Pal`),
never a fixed colour, and the shared geometry (`Shapes`) so what you see matches what was hit. Details in [`docs/design/spells-and-items.md`](docs/design/spells-and-items.md#animations).

### A monster, boss, god, race…

See [`docs/design/systems.md`](docs/design/systems.md) and [`docs/design/rpg.md`](docs/design/rpg.md): nearly everything is a table in `engine/Ossuary.Core/Entities/`.

## Tests

```powershell
.\fastcheck.ps1                  # type-check C# and TypeScript
.\headless.ps1 test              # simulation and desktop-flow suite (C#)
.\headless.ps1 dump panels       # layout as ASCII (also level|overworld|town|create)
.\headless.ps1 fx <spell|all>    # animations as ASCII
.\headless.ps1 soak 50 500       # random bot
.\headless.ps1 balance 8 2500    # balance bot per class × race (see docs/design/balance.md)
cd desktop; npm test             # frontend vitest
.\check.ps1                      # everything
```

- Every new system comes with a test (project standard). Luck-dependent tests need samples large enough not to be coin flips.
- `ARSENAL_TRACE=1 .\headless.ps1 test` prints stack traces for the arsenal tests.
- The `BalanceBand` test is a guard rail, not a target: if a class collapses, investigate before touching the limit.

## Text and translations

- Player-visible text **is born in English** and gets a Portuguese translation in `Loc.cs` (or `Loc.Spells.cs` for spells). `Say` and `TextBuilder` already translate; the Core never picks the language itself.
- Dynamic messages (names and numbers) match `Rx` patterns in `Loc.cs`. See [`docs/tech/languages.md`](docs/tech/languages.md).
- Portuguese spell and buff names must fit the list (≤ 26 columns); the tests check.

## Visuals

Art direction **"Phosphor & Bone"** ([`docs/tech/visual.md`](docs/tech/visual.md)): an 80s PC terminal, indigo background, coloured glyphs, sparing glow.

- Game colours live only in `engine/Ossuary.Core/Theme.cs` (and effect ramps in `FxLib`); CSS receives tokens from the frame. Do not create another palette.
- Fixed 8×16 bitmap font, no smoothing, integer scaling. A new font needs attribution and a licence in `docs/tech/visual.md` and `assets/fonts/NOTICE.md`.
- Each cell has a glyph, fg, bg and bold; the background should carry meaning.
- Theme, CRT and scale belong to the user (`DisplaySettings` + `localStorage`).

## Pull requests

- A clear imperative title; say **what** and **why**. Use the template GitHub loads.
- Tick what you tested (`headless test`, `check`, `fx`, screenshots if you touched visuals).
- One PR, one subject. If the change alters rules (and so saves), say so in the description.
- Be kind in reviews: the goal is a better game, not being right.

## Bugs and ideas

Use the [issues](https://github.com/Bruno-BRG/ossuary/issues) (there are templates). A good bug report has the game **version** (for example "Version 11"),
the **seed** (shown at the start of the journal and in the menu), what you did, what you expected and what happened. Since the seed is the save,
seed + keys reproduce the problem.
