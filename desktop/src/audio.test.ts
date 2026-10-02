import { describe, expect, it } from 'vitest';
import { cuesToPlay, level, patterns } from './audio';

describe('sound cues', () => {
  it('knows every cue the engine can name', () => {
    for (const cue of ['death', 'levelup', 'kill', 'hurt', 'hit', 'quest', 'magic', 'warn', 'good']) {
      expect(patterns[cue], cue).toBeDefined();
      for (const [freq, seconds] of patterns[cue]!) {
        expect(freq).toBeGreaterThanOrEqual(0);
        expect(seconds).toBeGreaterThan(0);
        expect(seconds).toBeLessThan(0.5);
      }
    }
  });
  it('ignores cues it has never heard of, and missing ones', () => {
    expect(cuesToPlay(['hit', 'nope', 'kill'])).toHaveLength(2);
    expect(cuesToPlay(undefined)).toHaveLength(0);
  });
  it('keeps the level polite and silences either volume at zero', () => {
    expect(level(10, 10)).toBeLessThanOrEqual(0.15);
    expect(level(0, 10)).toBe(0);
    expect(level(10, 0)).toBe(0);
    expect(level(99, 99)).toBe(level(10, 10));
    expect(level(5, 5)).toBeLessThan(level(10, 10));
  });
});
