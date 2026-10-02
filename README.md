# Ossuary — Phosphor & Bone

A dark-fantasy ASCII roguelike. The world threw its dead, its kings and its gods into one
pit, the **Ossuary**, and now a nobody climbs down to steal what is left. Descend, take the
Amulet of Yendor, and bring it back to daylight.

It is a **Tauri 2 + Rust** desktop app with a **TypeScript/Canvas** bitmap terminal. The
simulation is an independent **C# (.NET 10)** engine, packaged as a self-contained executable
and run as a private local process.

## Features

- Turn-based dungeon crawling across five branches, with an overworld of nine regions,
  roads, a day/night clock, random road encounters and travel.
- **Vertical towns**: walled settlements with towers, lofts, crypts and cellars (use `<` and
  `>` on stairs). Smith, armourer, alchemist, mage tower, tavern, inn, temple, guild,
  library, barracks, market stalls — and the people who live in them. Shops, healing,
  rest, appraisal, weapon honing, rumours.
- Seven races, eight classes, 39 spells, abilities, perks, six gods (with rivals, trials and sacrifices) and altars.
- Five reachable branches plus an optional portal branch (**The Annex**), branch bosses, native monsters with habits, monster factions, vaults, rigged caches and brass keys.
- **Corruption and mutations**, hired companions, light crafting (molotovs, bone blades), artifact sets and corrupting relics.
- A living world: reputation with four houses, Guild jobs, road events, townsfolk who keep hours and remember you.
- Auto-explore, travel to stairs/altars, rest until healed, held-key walking, stealth and noise, found traps you can disarm.
- Modes: Normal, Classic (no hunger), Hardcore, Dive, Naked, Trained; a daily challenge with a local board; achievements, morgue files, past runs and bones of dead heroes.
- Short square-wave sound effects, animated water and optional square tiles.
- Animated opening story, from the first pit to your arrival.
- Portuguese (Brazil) and English, switchable at any time (`F2`).
- Deterministic by seed; the seed is the save.
- CRT / amber / green-phosphor themes, fixed 8x16 bitmap font, integer scaling.

## Install (players)

Download the installer `Ossuary_0.1.0_x64-setup.exe` (Windows x64) from the build output or a
release, and run it. No SDK is needed. WebView2 is required (preinstalled on Windows 11).

## Build from source

See [INSTALL.md](INSTALL.md) for the full guide. Quick start (Windows x64):

```powershell
.\desktop.ps1 setup    # local .NET 10 SDK + npm packages
.\desktop.ps1 dev      # run the Tauri app
.\desktop.ps1 build    # release executable + NSIS installer
```

## Controls

Arrow keys, numpad or `hjkl` move; `yubn` are diagonals. `i` inventory, `c` character,
`g` pick up, `>` / `<` stairs, `?` help, `F2` options, `F3` CRT, `F4` palette, `F11` fullscreen.

- In towns, bump into a person to talk, and into a counter, notice board or altar to trade.
- When a monster blocks the road: `Enter`, `Space`, `K` or `F` fight; `R` or `<` flee.

See [docs/controles.md](docs/controles.md) (Portuguese) for the complete list.

## Project layout

- `engine/Ossuary.Core`: simulation and screen composition as data (no UI/platform code).
- `engine/Ossuary.Desktop`: local host, input, modals, JSON protocol over stdin/stdout.
- `engine/Ossuary.Headless`: tests, ASCII dumps and soak/balance bots.
- `desktop`: Tauri window, frontend and distribution pipeline.
- `assets/fonts`: canonical bitmap font and attribution.
- `docs`: architecture, systems, controls, lore (Portuguese).

## Testing

```powershell
.\fastcheck.ps1                 # type-check C# and TypeScript
.\headless.ps1 test             # simulation + desktop-flow suite
.\headless.ps1 dump town        # ASCII dump of a town, every floor, service panel
.\headless.ps1 soak 50 500      # random-bot soak
.\check.ps1                     # everything: engine, frontend, packaged IPC, Rust
```

## Credits

Font **unscii-16** by viznut, public domain: see [assets/fonts/NOTICE.md](assets/fonts/NOTICE.md).
