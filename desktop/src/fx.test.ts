import { describe, expect, it } from 'vitest';
import { applyFx, createFxPlayer } from './fx';
import type { Frame } from './protocol';

const base = (fx?: number[][]): Frame => ({
  cols: 4, rows: 1, glyphs: [65, 66, 67, 68], fg: [1, 2, 3, 4], bg: [10, 20, 30, 40], bold: [false, false, false, false], fx, fxMs: 45,
} as unknown as Frame);

describe('spell animation overlay', () => {
  it('returns the same frame for an empty step', () => {
    const f = base();
    expect(applyFx(f, undefined)).toBe(f);
    expect(applyFx(f, [])).toBe(f);
  });
  it('draws only the marked cells, keeps the background when it is -1, and never mutates the original', () => {
    const f = base();
    const g = applyFx(f, [1, 42, 0xff0000, -1, 3, 43, 0x00ff00, 0x112233]);
    expect(g.glyphs).toEqual([65, 42, 67, 43]);
    expect(g.fg[1]).toBe(0xff0000);
    expect(g.bg).toEqual([10, 20, 30, 0x112233]);
    expect(g.bold).toEqual([false, true, false, true]);
    expect(f.glyphs).toEqual([65, 66, 67, 68]);
    expect(f.bg).toEqual([10, 20, 30, 40]);
  });
  it('ignores cells outside the frame and a trailing partial quadruple', () => {
    const f = base();
    const g = applyFx(f, [-1, 1, 1, 1, 99, 1, 1, 1, 2, 7]);
    expect(g.glyphs).toEqual(f.glyphs);
  });
});

describe('animation player', () => {
  it('plays each step in order, then calls done exactly once', () => {
    const drawn: number[] = [];
    let ticks: (() => void) | null = null; let cancelled = 0; let done = 0;
    const p = createFxPlayer(f => drawn.push(f.glyphs[0]!), () => done++, fn => { ticks = fn; return 1; }, () => { cancelled++; });
    expect(p.start(base([[0, 70, 0, -1], [0, 71, 0, -1], [0, 72, 0, -1]]))).toBe(true);
    expect(p.playing).toBe(true);
    expect(drawn).toEqual([70]);
    ticks!(); ticks!();
    expect(drawn).toEqual([70, 71, 72]);
    expect(done).toBe(0);
    ticks!();
    expect(done).toBe(1);
    expect(p.playing).toBe(false);
    expect(cancelled).toBeGreaterThan(0);
  });
  it('does nothing without a script, and stop() ends a run without calling done', () => {
    let done = 0;
    const p = createFxPlayer(() => {}, () => done++, () => 1, () => {});
    expect(p.start(base())).toBe(false);
    expect(p.start(base([]))).toBe(false);
    expect(p.start(base([[0, 70, 0, -1], [0, 71, 0, -1]]))).toBe(true);
    p.stop();
    expect(p.playing).toBe(false);
    expect(done).toBe(0);
  });
});
