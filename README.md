<div align="center">

# 🦴 Ossuary — Phosphor & Bone

**A dark-fantasy ASCII roguelike.** The world threw its dead, its kings and its gods into one pit.
You climb down to steal what is left.

![Version 12](https://img.shields.io/badge/version-12-c8a050?style=flat-square)
![License](https://img.shields.io/badge/license-MIT-5fa85f?style=flat-square)
![Platform](https://img.shields.io/badge/platform-Windows%20x64-4a6ea8?style=flat-square)
![Engine](https://img.shields.io/badge/engine-C%23%20.NET%2010-7c4dff?style=flat-square)
![Shell](https://img.shields.io/badge/shell-Tauri%202%20%2B%20Rust-e0802a?style=flat-square)
![Tests](https://img.shields.io/badge/tests-headless%20%2B%20vitest-5fa85f?style=flat-square)

**English** · [Português (Brasil)](README.pt-BR.md) · [Contributing](CONTRIBUTING.md) · [Changelog](CHANGELOG.md) · [Docs](docs/README.md)

<img src="docs/shots/tauri-dungeon.jpg" alt="Ossuary: a dungeon level in the phosphor terminal" width="760">

</div>

---

## The game

Descend through the Ossuary, take the **Amulet of Yendor**, and bring it back to daylight. Everything is drawn with one
fixed 8×16 bitmap font on a CRT-style terminal; the world is a turn-based dungeon crawl with a living overworld above it.
The seed *is* the save: same seed, same keys, same run.

### What is in **version 12**

| | |
|---|---|
| ✨ **Spells you can see** | 322 spells in 8 schools, and every one is animated: a fireball is a ball of fire that flies and bursts, lightning forks, meteors fall. |
| 📖 **55 spellbooks** | Wizard, necromancer, cleric/paladin, ranger (nature) and rogue (shadow) each have about fifty spells to find, in five depth tiers. |
| ⚔️ **Items with a spell inside** | Random magic gear is imbued with a spell that fits what it is: swords carry attacks, armour carries wards, boots carry leaps. They fire on their own, too. |
| 🗡️ **43 unique items & 4 sets** | Named relics across every branch, many lending you a spell while you hold them. |
| 🧪 **Wands, scrolls and potions that are spells** | Same effects, same animations, no mana. |
| 🏰 **The world** | Five branches and a portal branch, a nine-region overworld, vertical towns, bosses, factions, reputation, corruption and mutations, companions, crafting. |
| 🕯️ **A world that remembers** | Townsfolk with personalities and memory, dialogue, quests with deadlines (journal on `F7`), crime and the Watch, rumours, road travellers and the main questline *The Seal*, with six endings. |
| 🌍 **Two languages** | Portuguese (Brazil) and English, switchable any time with `F2`. |

<details>
<summary><b>Everything else</b></summary>

- Turn-based dungeon crawling across five branches, with an overworld of nine regions, roads, a day/night clock, random road encounters and travel.
- **Vertical towns**: walled settlements with towers, lofts, crypts and cellars. Smith, armourer, alchemist, mage tower, tavern, inn, temple, guild, library, barracks, market stalls, and the people who live in them.
- Seven races, eight classes, abilities, perks, six gods (with rivals, trials and sacrifices) and altars.
- Branch bosses, native monsters with habits, monster factions, vaults, rigged caches and brass keys; the optional portal branch **The Annex**.
- **Corruption and mutations**, hired companions, light crafting (molotovs, bone blades), artifact sets and corrupting relics.
- A living world: reputation with four houses, Guild jobs, road events, townsfolk who keep hours and remember you.
- Auto-explore, travel to stairs/altars, rest until healed, held-key walking, stealth and noise, traps you can find and disarm.
- Modes: Normal, Classic (no hunger), Hardcore, Dive, Naked, Trained; a daily challenge with a local board; achievements, morgue files, past runs and bones of dead heroes.
- Short square-wave sound effects, animated water, optional square tiles, an animated opening story.
- CRT / amber / green-phosphor themes, fixed 8×16 bitmap font, integer scaling.

</details>

<div align="center">
<img src="docs/shots/tauri-title.jpg" alt="Title screen" width="370"> <img src="docs/shots/tauri-options.jpg" alt="Options" width="370">
</div>

## Install (players)

Download the installer `Ossuary_0.12.0_x64-setup.exe` (Windows x64) from a [release](https://github.com/Bruno-BRG/ossuary/releases)
or the build output, and run it. No SDK is needed. WebView2 is required (preinstalled on Windows 11).

> The public name of a release is just **Version N** (this one is **Version 12**). Installers and manifests use the matching
> `0.N.0`, because Windows installers and npm/Cargo want three numbers.

## Build from source

The full guide is in [INSTALL.md](INSTALL.md) ([pt-BR](INSTALL.pt-BR.md)). Quick start (Windows x64):

```powershell
.\desktop.ps1 setup    # local .NET 10 SDK + npm packages
.\desktop.ps1 dev      # run the Tauri app with frontend hot reload
.\desktop.ps1 web      # same engine in a browser, loopback only
.\desktop.ps1 build    # release executable + NSIS installer
```

## Controls

Arrow keys, numpad or `hjkl` move; `yubn` are diagonals. `i` inventory, `c` character, `g` pick up, `>` / `<` stairs,
`Shift+Z` spells, `r` read, `z` zap a wand, `q` quaff, `P` put on a ring or amulet, `?` help, `F2` options, `F3` CRT, `F4` palette, `F11` fullscreen.

- In towns, bump into a person to talk, and into a counter, notice board or altar to trade.
- In the spell list, `←`/`→` switch school, a letter or `Enter` casts, `◆` marks spells lent by your gear.
- When a monster blocks the road: `Enter`, `Space`, `K` or `F` fight; `R` or `<` flee.

The complete list is in [docs/controls.md](docs/controls.md).

## How it is built

```
engine/Ossuary.Core      simulation + screens as data   (no UI, no platform: the "golden rule")
engine/Ossuary.Desktop   local host: input, modals, JSON protocol over stdin/stdout
engine/Ossuary.Headless  tests, ASCII dumps, soak and balance bots
desktop/                 Tauri window (Rust), TypeScript/Canvas terminal, packaging
assets/fonts             the canonical bitmap font and its attribution
docs/                    architecture, systems, spells and items, lore
```

The simulation lives only in C#; the front end draws the grid the engine sends and plays animations the engine records.
Start with [docs/architecture.md](docs/architecture.md) and [docs/spells-and-items.md](docs/spells-and-items.md).

## Testing

```powershell
.\fastcheck.ps1                 # type-check C# and TypeScript
.\headless.ps1 test             # simulation + desktop-flow suite
.\headless.ps1 fx fireball      # a spell's animation, step by step, in ASCII
.\headless.ps1 dump panels      # UI as ASCII
.\headless.ps1 soak 50 500      # random-bot soak
.\check.ps1                     # everything: engine, frontend, packaged IPC, Rust
```

## Contributing

Bug reports, spells, items, translations and balance notes are welcome. Read [CONTRIBUTING.md](CONTRIBUTING.md) first;
it explains the layout, the rules and how to add a spell or an item in a few lines.

## License

The code is released under the [MIT license](LICENSE). The **unscii-16** font (viznut, public domain) has its own attribution in [assets/fonts/NOTICE.md](assets/fonts/NOTICE.md).
