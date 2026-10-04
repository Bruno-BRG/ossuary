import { describe, expect, it } from 'vitest';
import { renderStinger, stingerInfo, stingerNames, toWav } from './stingers';

const SR = 22050;
const rms = (x: Float32Array, from: number, to: number) => {
  let s = 0; const a = Math.floor(from * SR), b = Math.min(x.length, Math.floor(to * SR));
  for (let i = a; i < b; i++) s += x[i]! * x[i]!;
  return Math.sqrt(s / Math.max(1, b - a));
};

describe('stingers', () => {
  const rendered = new Map(stingerNames.map(n => [n, renderStinger(n, SR)] as const));

  it('lasts as long as it says', () => {
    for (const n of stingerNames) expect(rendered.get(n)!.length, n).toBe(Math.floor(SR * stingerInfo[n].seconds));
  });

  it('is finite, never clips and is not silent', () => {
    for (const n of stingerNames) {
      const x = rendered.get(n)!;
      let peak = 0, bad = 0;
      for (let i = 0; i < x.length; i++) { if (!Number.isFinite(x[i]!)) bad++; peak = Math.max(peak, Math.abs(x[i]!)); }
      expect(bad, n).toBe(0);
      expect(peak, n).toBeGreaterThan(0.5);
      expect(peak, n).toBeLessThanOrEqual(0.86);
      expect(rms(x, 0, stingerInfo[n].seconds), n).toBeGreaterThan(0.01);
    }
  });

  it('ends without a click', () => {
    for (const n of stingerNames) {
      const x = rendered.get(n)!;
      expect(Math.abs(x[x.length - 1]!), n).toBeLessThan(0.01);
    }
  });

  it('is deterministic', () => {
    const again = renderStinger('quest-accepted', SR);
    expect(Array.from(again.slice(1000, 1010))).toEqual(Array.from(rendered.get('quest-accepted')!.slice(1000, 1010)));
  });

  it('keeps the quiet ones quiet and the big ones long', () => {
    expect(stingerInfo['quest-updated'].seconds).toBeLessThanOrEqual(2);
    expect(stingerInfo['ending-sealed'].seconds).toBe(25);
    // The sealed ending thins out: the last seconds carry less energy than the opening ones.
    const x = rendered.get('ending-sealed')!;
    expect(rms(x, 20, 23)).toBeLessThan(rms(x, 1, 4));
  });

  it('writes a valid wav', () => {
    const wav = toWav(rendered.get('trap')!, SR);
    const text = (o: number, n: number) => String.fromCharCode(...wav.slice(o, o + n));
    expect(text(0, 4)).toBe('RIFF');
    expect(text(8, 4)).toBe('WAVE');
    expect(wav.length).toBe(44 + rendered.get('trap')!.length * 2);
  });
});
