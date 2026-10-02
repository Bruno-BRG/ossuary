import type { Frame } from './protocol';

// Moving water. The engine marks the cells (Frame.anim); the front end alternates their glyph and brightness on a timer,
// between frames, so a pool never looks frozen and the engine is never asked for a turn.

const WAVE = [0x2248, 0x7e, 0x2248, 0x2d] as const;   // ≈ ~ ≈ -

function hash(i: number): number {
  let h = Math.imul(i ^ 0x9e3779b9, 0x85ebca6b);
  h ^= h >>> 13;
  h = Math.imul(h, 0xc2b2ae35);
  return (h ^ (h >>> 16)) >>> 0;
}

function lift(color: number, amount: number): number {
  const r = Math.min(255, ((color >> 16) & 255) + amount), g = Math.min(255, ((color >> 8) & 255) + amount), b = Math.min(255, (color & 255) + amount);
  return (r << 16) | (g << 8) | b;
}

/** A copy of the frame with the water cells in the given phase. Returns the frame itself when there is no water. */
export function shimmer(frame: Frame, tick: number): Frame {
  const cells = frame.anim;
  if (!cells || cells.length === 0) return frame;
  const glyphs = frame.glyphs.slice(), fg = frame.fg.slice();
  for (const i of cells) {
    if (i < 0 || i >= glyphs.length) continue;
    const phase = (hash(i) + tick) % WAVE.length;
    glyphs[i] = WAVE[phase]!;
    if (phase === 1) fg[i] = lift(fg[i]!, 28);
    else if (phase === 3) fg[i] = lift(fg[i]!, -14);
  }
  return { ...frame, glyphs, fg };
}
