# Visual — Phosphor & Bone

An 80s PC terminal: indigo background, bone-colored text, colored glyphs and sparing glow. The current implementation uses Tauri, Canvas and a bitmap font; see [renderer.md](renderer.md) and [desktop.md](desktop.md).

## Font and license

The shipped font is **unscii-16**, an 8×16 bitmap by viznut, in the public domain. File: `assets/fonts/unscii-16.hex`; credit and origin in `assets/fonts/NOTICE.md`. We do not use the `unscii-16-full` variant. A new font requires recording its origin and license here.

The Canvas uses integer scale, no smoothing, with letterboxing. The big logo is composed from blocks in `desktop/src/title.ts`. New glyphs enter `engine/Ossuary.Core/GlyphSet.cs` first and must pass the font coverage test. The font lacks `☼ ✗ ⚔ ☠ ♜ ⌂ ✚ ✦ ∙ ›`; use the existing alternatives.

## "Ossuary" palette (semantic tokens)

Near-black indigo background instead of neutral gray. Text is bone-colored,
amber marks titles, cyan marks labels and magenta/violet marks magic.

**UI**

| Token | Hex | Use |
|---|---|---|
| `Void` | `#07060B` | Letterbox, outside the map |
| `Background` | `#0D0B14` | Map floor and base of everything |
| `Panel` | `#16121F` | Sidebar, log and modals |
| `PanelHi` | `#211B30` | Panel header, selected row |
| `Rule` | `#3A3150` | Single frames and separators |
| `Frame` | `#8A6F30` | Modal double frame (old gold) |
| `Text` | `#D8CFC0` | Text (bone) |
| `Dim` | `#7D7590` | Secondary text and old messages |
| `Label` | `#5FCDE4` | Labels (cyan) |
| `Title` | `#F2B33D` | Titles and shortcut keys (amber) |
| `Accent` | `#FBF236` | Rare highlight (selection, `@`) |

**States and messages**

| Token | Hex | Token | Hex |
|---|---|---|---|
| `Good` | `#99E550` | `Bad` | `#D95763` |
| `Warn` | `#DF7126` | `Danger` | `#FF3B4F` |
| `Gold` | `#FBD94A` | `Magic` | `#B57EDC` |
| `Info` | `#639BFF` | `Quest` | `#5FE4C0` |
| `Narrative` | `#CBB3E8` | `Memory` | `#2E2A45` |

**Map: every tile has fg *and* bg**

| Tile | Glyph | fg | bg |
|---|---|---|---|
| Floor | `·` | `#4A4360` | `Background` |
| Floor alt | `·` / `,` | `#6B5440` | `#120F18` |
| Wall | `#` | `#9A90AE` | `#2A2438` |
| Brick wall | `#` | `#B0705A` | `#341C1C` |
| Rock | `#` / `▓` | `#6A6880` | `#1E1B2A` |
| Door | `+` / `'` | `#DF7126` | `#2E1A10` |
| Locked door | `+` | `#FBD94A` | `#2E1A10` |
| Stairs | `>` `<` | `#FFFFFF` (bold) | `#3F3F74` |
| Portal | `^` | `#D77BBA` | `#2A1238` |
| Fountain | `{` | `#5FCDE4` | `#0E2A40` |
| Altar | `_` | `#EAE4F4` | `#2A2438` |
| Rubble | `"` / `▒` | `#8F7A5E` | `Background` |

**Overworld: CP437 vocabulary + colored background**

| Terrain | Glyphs (variant by hash x,y) | fg | bg |
|---|---|---|---|
| Deep water | `≈` | `#306082` | `#0A1830` |
| Water | `≈` `~` | `#5B6EE1` | `#10224A` |
| Shallows | `~` | `#639BFF` | `#16305A` |
| Sand | `·` `·` `░` | `#D9A066` | `#2A2014` |
| Grass | `"` `'` `,` `·` | `#6ABE30` | `#0F1A0C` |
| Forest | `♣` `♠` `↑` | `#37946E` | `#0A1610` |
| Hills | `∩` `ⁿ` | `#8F974A` | `#16180C` |
| Mountain | `▲` | `#9BADB7` | `#1E2028` |
| Swamp | `⌠` `"` `,` | `#4B692F` | `#10140A` |
| Snow | `·` `*` | `#EAF2FF` | `#2A3040` |
| Ashes | `·` `·` | `#696A6A` | `#141414` |
| Road | `·` `═` `║` | `#8A6F30` | `#1A140C` |
| Town | `■` / `♦` | `#FBF236` | `#3A2A08` |
| Dungeon | `▼` | `#FF3B4F` | `#2A0A0E` |
| Ruin | `π` | `#9A9488` | — |
| Cave | `Ω` | `#B08050` | — |
| Mine | `¥` | `#C09060` | — |
| Fortress | `Π` | `#C0B0A0` | — |
| Shrine | `‡` / `†` | `#5FE4C0` | — |
| Bridge | `═` / `║` | `#A0A0B0` | `#10224A` |

Glyphs only count after passing `GlyphsInFont` against the chosen font. A glyph
outside the font renders as `?` and fails the test.

## Light and composition

The palette implemented in `engine/Ossuary.Core/Theme.cs` is the source of truth. The UI uses semantic tokens; terrain colors receive light before the preset remap. Monster colors are content and go through `Theme.Mon`.

- Torchlight darkens cells with distance; memory outside the FOV uses a bluish ramp.
- Day/night changes overworld lighting.
- Variants and jitter use a coordinate hash at draw time, never consuming the simulation RNG.
- White, Accent and bold are reserved for the player, stairs, items and danger.
- Compact header, sidebar with Nearby/equipment/minimap, colored log and double frames on modals.
- Two-column inventory; title with logo and seed; death with RIP and cause; victory with the Amulet.

## Preferences

`F2` opens options, `F3` toggles the CRT and `F4` cycles Ossuary, Amber, Phosphor and CGA. The CRT combines scanlines, vignette and glow in the frontend, in three levels. Preferences persist in `localStorage` and reach the engine as `DisplaySettings` data.

## Future extensions

Ambient water animation and a square tileset are visual-evolution options that do not alter turns. The current runtime is complete without them. Captures of the current client are in `docs/shots/tauri-*.jpg`.

## Light, ambience and HUD (redesign)

**Torchlight.** `Theme.Shade` warms glyphs and backgrounds near the player (a
`#6A4220` pool on the background), with a soft falloff up to `TorchRadius`. Monsters and items tint
their own background with the glyph color so they pop from the plan. The `@` sits in a
warm pool.

**Depth mood.** `Theme.DepthTint` pushes stone and background toward a
color per band of 3 levels: crypt (indigo), catacombs (moss), flooded vaults
(blue), bone pits (amber) and the mouth of hell (crimson). The band name
appears in the header. A secret door stays identical to the wall around it.

**Texture.** Floor/wall variants and tone jitter come only from `hash(x,y)`;
unexplored rock has sparse grain, and maps smaller than the window are centered.

**HUD.** Header with the `♦ OSSUARY` seal, depth chip and clock with a day/dusk/night
icon; rounded map frame with title and coordinates;
solid half-block bars (HP/EN/XP); colored attributes; sections
`NEARBY`, `IN VIEW` (live legend), `WORN`, `VITALS`, `MAP`; journal with a marker
per type and fade by age; status bar with what is underfoot;
shortcuts as keys; modal panels with a drop shadow and ornamented title.

## Animated water and square tiles

- Visible water is marked by the engine (`Frame.Anim`) and the frontend alternates glyph/brightness between frames (`desktop/src/anim.ts`); none of this consumes a turn or Rng.
- **Tiles** option (menu): `DisplaySettings.Square` draws each map cell in two 8×16 columns, making it square on screen. The Core still counts in cells.
