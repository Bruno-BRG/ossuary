import { describe, it, expect } from 'vitest';
import { parseFont, layout } from './font';
import { validSeed } from './protocol';
import { readFileSync } from 'node:fs';

describe('bitmap terminal', () => {
  it('uses the real font with readable ASCII and box glyphs', () => {
    const font = parseFont(readFileSync('../assets/fonts/unscii-16.hex', 'utf8'));
    for (const glyph of '@?╔═╗╚╝█≈♦') expect(font.has(glyph.charCodeAt(0))).toBe(true);
    expect(font.get(32)!.every(row => row === 0)).toBe(true);
    expect(font.get(64)!.some(row => row !== 0)).toBe(true);
  });
  it('keeps device pixels integral at different viewport and DPI sizes', () => {
    for (const [w, h] of [[1280, 800], [1920, 1080], [2560, 1600], [840, 520], [1575, 1000]]) {
      const size = layout(w, h);
      expect(size.width).toBe(size.cols * 8 * size.scale);
      expect(size.height).toBe(size.rows * 16 * size.scale);
      expect(size.left).toBe(Math.floor(size.left));
      expect(size.width).toBeLessThanOrEqual(w);
      expect(size.height).toBeLessThanOrEqual(h);
      expect(size.cols).toBeGreaterThanOrEqual(84);
    }
  });
  it('does not lose uint64 precision to JavaScript numbers', () => {
    expect(validSeed('18446744073709551615')).toBe(true);
    expect(validSeed('18446744073709551616')).toBe(false);
    expect(validSeed('-1')).toBe(false);
    expect(validSeed('1.2')).toBe(false);
  });
});
