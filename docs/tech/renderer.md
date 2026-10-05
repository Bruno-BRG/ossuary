# Renderer — Ossuary

The renderer in `desktop/src/renderer.ts` consumes engine frames and draws
a bitmap grid, with the CRT filter in WebGL2 (and Canvas 2D as a fallback). It knows nothing about the map, combat or game rules.

## Font and cells

Canonical font: `assets/fonts/unscii-16.hex`, viznut's unscii-16, public
domain. `desktop/src/font.ts` reads the hex lines, keeping BMP 8×16 glyphs.
Each glyph is 16 bytes; the most significant bit is the left column.
The `?` glyph is the fallback. No dynamic font draws the terminal.

The grid is 84–240 columns by 26–120 rows. `layout` picks an integer scale
of 1×, 2× or 3× in physical pixels and centers the screen with letterboxing. DPI is
part of the calculation; the Canvas keeps smoothing off. A fixed scale
larger than what is available is reduced to keep the grid playable.

## Frame

`Session.Draw` walks `TextBuilder` in row/column order, exporting
codepoint, foreground color, background color and bold. It also sends color tokens,
dimensions, mode, turn, panel, options and CRT parameters.

The renderer rasterizes the grid at native 8×16 resolution per cell into two
textures: the base image and an emission layer with the bright or bold glyphs.
The CRT look is a fragment shader (`desktop/src/crt.ts`) over them:

- soft barrel curvature and glass with rounded corners;
- *sharp bilinear*: crisp texels, only the 1 px seam is filtered;
- scanlines (the lower part of each font row darkens; at 1× it alternates
  screen rows, very light) and a per-pixel RGB phosphor mask;
- mipmap bloom of the emitters only and a light halation of the whole image;
- very subtle vignette, flicker and hum, and grain (integer hash, no `sin`).

Scanline, vignette and glow intensity still come from the Core
(`DisplaySettings.CrtParams`); curvature, mask and flicker derive from the level
(`crtParams`). The glow leaks as a CSS aura around the glass. Without WebGL2 the
renderer falls back to Canvas 2D with blur + a CSS overlay. The animation runs at ~30 fps
only with the CRT on and is disabled with `prefers-reduced-motion`.

**Spell animations** (`fx.ts`): a frame may carry `fx` (one array per step of [cell, glyph, fg, bg]) and `fxMs`; the frontend plays it over the finished
frame, ~45 ms per step, without asking the engine for a turn, under the same `prefers-reduced-motion` rule as water. A new key or a new frame cuts the
animation short. See [`spells-and-items.md`](../design/spells-and-items.md#animations).

The title (`title.ts`) is a client-only scene: sky, ruins, embers and a gradient
logo; the embers move on a local tick and never query the engine.

## Palette, light and preferences

Colors come exclusively from `engine/Ossuary.Core/Theme.cs`. Torch light,
memory, day/night and the remap of the four presets are applied by the Core
composition before the frame. The CSS receives tokens, with no parallel palette.

`DisplaySettings.Current` keeps theme, CRT and scale during a run and across
restarts. The frontend persists these options in localStorage. `F2` opens options,
`F3` toggles the CRT and `F4` toggles the theme; `F11` controls fullscreen.

## How to verify

- `headless.ps1 test`: GlyphSet, real font, palette, composition of every panel.
- `headless.ps1 dump panels`: layout and content as ASCII.
- `npm test` in `desktop/`: real font, dimensions/DPI, seeds and packaged IPC.
- `desktop.ps1 web`: visual inspection with the same engine as the distribution.
- `check.ps1`: full validation.

Real captures live in `docs/shots/tauri-title.jpg`, `tauri-dungeon.jpg` and
`tauri-options.jpg`. The art direction is in [visual.md](visual.md).
