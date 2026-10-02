import { describe, expect, it } from 'vitest';
import { shimmer } from './anim';
import type { Frame } from './protocol';

const base = (anim: number[] | undefined): Frame => ({
  cols: 4, rows: 1, glyphs: [0x2248, 65, 0x2248, 66], fg: [0x204060, 0xffffff, 0x204060, 0xffffff], bg: [0, 0, 0, 0], bold: [false, false, false, false], anim,
} as unknown as Frame);

describe('water shimmer', () => {
  it('returns the very same frame when there is no water', () => {
    const f = base(undefined);
    expect(shimmer(f, 3)).toBe(f);
    expect(shimmer(base([]), 3).glyphs).toEqual(f.glyphs);
  });
  it('only ever touches the marked cells and never mutates the original', () => {
    const f = base([0, 2]);
    const g = shimmer(f, 5);
    expect(g).not.toBe(f);
    expect(f.glyphs).toEqual([0x2248, 65, 0x2248, 66]);
    expect(g.glyphs[1]).toBe(65);
    expect(g.glyphs[3]).toBe(66);
    expect(g.fg[1]).toBe(0xffffff);
  });
  it('moves: some tick changes a water cell, and every cell stays a wave glyph', () => {
    const f = base([0, 2]);
    const seen = new Set<number>();
    for (let t = 0; t < 8; t++) { const g = shimmer(f, t); seen.add(g.glyphs[0]!); expect([0x2248, 0x7e, 0x2d]).toContain(g.glyphs[0]); }
    expect(seen.size).toBeGreaterThan(1);
  });
  it('ignores indices outside the frame', () => {
    expect(() => shimmer(base([-1, 99]), 1)).not.toThrow();
  });
});
