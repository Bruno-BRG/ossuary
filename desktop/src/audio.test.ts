import { describe, expect, it } from 'vitest';
import { cueStinger, cuesToPlay, level, musicLevel, patterns, stingerLevel, trackFor, tracks, uiPatterns, uiSoundFor, type TrackName } from './audio';
import { stingerInfo } from './stingers';

describe('sound cues', () => {
  it('knows every cue the engine can name', () => {
    for (const cue of ['death', 'levelup', 'kill', 'hurt', 'hit', 'quest', 'magic', 'warn', 'good', 'stairs', 'door', 'pickup']) {
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

describe('stingers in the game', () => {
  it('maps engine cues to stingers that exist', () => {
    for (const [cue, name] of Object.entries(cueStinger)) expect(stingerInfo[name], cue).toBeDefined();
    expect(cueStinger['levelup']).toBe('level-up');
    expect(cueStinger['death']).toBe('death');
  });
  it('keeps the level polite and silences either volume at zero', () => {
    expect(stingerLevel(10, 10)).toBeLessThanOrEqual(0.55);
    expect(stingerLevel(0, 10)).toBe(0);
    expect(stingerLevel(10, 0)).toBe(0);
    expect(stingerLevel(5, 5)).toBeLessThan(stingerLevel(10, 10));
  });
});

describe('menu blips', () => {
  it('has a short pattern for every blip', () => {
    for (const name of ['click', 'move', 'confirm', 'cancel']) {
      expect(uiPatterns[name], name).toBeDefined();
      for (const [freq, seconds] of uiPatterns[name]!) { expect(freq).toBeGreaterThan(0); expect(seconds).toBeLessThan(0.1); }
    }
  });
  it('maps keys to the right blip', () => {
    expect(uiSoundFor('Escape')).toBe('cancel');
    expect(uiSoundFor('Enter')).toBe('confirm');
    expect(uiSoundFor('ArrowDown')).toBe('move');
    expect(uiSoundFor('KeyA')).toBe('click');
  });
});

describe('music', () => {
  const names = Object.keys(tracks) as TrackName[];
  it('writes every note inside its bar, in range and in time', () => {
    for (const name of names) {
      const t = tracks[name];
      expect(t.bpm).toBeGreaterThan(40);
      expect(t.bars).toBeGreaterThan(1);
      for (let i = 0; i < t.bars; i++) {
        for (const e of t.bar(i)) {
          expect(e.b, `${name}/${i}`).toBeGreaterThanOrEqual(0);
          expect(e.b, `${name}/${i}`).toBeLessThan(t.beats);
          expect(e.d).toBeGreaterThan(0);
          if (e.m !== undefined) { expect(e.m).toBeGreaterThanOrEqual(24); expect(e.m).toBeLessThanOrEqual(100); }
          if (e.v !== 'tick' && e.v !== 'thud') expect(e.m, `${name}/${i} ${e.v}`).toBeDefined();
        }
      }
    }
  });
  it('is deterministic, so a loop repeats exactly', () => {
    for (const name of names) expect(JSON.stringify(tracks[name].bar(3))).toBe(JSON.stringify(tracks[name].bar(3)));
  });
  it('plays the five-note theme at the start of the descent', () => {
    const bells = tracks.title.bar(2).filter(e => e.v === 'bell').map(e => e.m);
    expect(bells).toEqual([62, 65, 63, 62, 69]);
  });
  it('erases one note at a time in the seal', () => {
    const count = (bar: number) => tracks.title.bar(bar).filter(e => e.v === 'bell').length;
    expect([count(18), count(19), count(20), count(21)]).toEqual([5, 4, 3, 2]);
  });
  it('picks music by what is on screen', () => {
    expect(trackFor({ onTitle: true, intro: false, mode: 'Dungeon' })).toBe('title');
    expect(trackFor({ onTitle: false, intro: true, mode: 'Dungeon' })).toBe('intro');
    expect(trackFor({ onTitle: false, intro: false, mode: 'Dungeon', scene: 'combat' })).toBe('combat');
    expect(trackFor({ onTitle: false, intro: false, mode: 'Dungeon', scene: 'boss-gaoler' })).toBe('boss');
    expect(trackFor({ onTitle: false, intro: false, mode: 'Dungeon', scene: 'boss-stone-warden' })).toBe('boss-warden');
    expect(trackFor({ onTitle: false, intro: false, mode: 'Dungeon', scene: 'boss-rat-king' })).toBe('boss');
    expect(trackFor({ onTitle: false, intro: false, mode: 'TownMap', scene: 'tavern' })).toBe('tavern');
    expect(trackFor({ onTitle: false, intro: false, mode: 'TownMap', scene: 'town-night' })).toBe('town-night');
    expect(trackFor({ onTitle: false, intro: false, mode: 'Overworld', scene: 'road-night' })).toBe('road-night');
    expect(trackFor({ onTitle: false, intro: false, mode: 'Dungeon', scene: 'spire' })).toBe('spire');
    expect(trackFor({ onTitle: false, intro: false, mode: 'Dungeon', scene: 'something-new' })).toBe('dungeon');
    expect(trackFor({ onTitle: false, intro: false, mode: 'GameOver', scene: '' })).toBeNull();
    expect(trackFor({ onTitle: false, intro: false, mode: 'Dungeon' })).toBe('dungeon');
    expect(trackFor({ onTitle: false, intro: false, mode: 'Overworld' })).toBe('road');
    expect(trackFor({ onTitle: false, intro: false, mode: 'TownMap' })).toBe('town');
    expect(trackFor({ onTitle: false, intro: false, mode: 'GameOver' })).toBeNull();
  });
  it('keeps the music quiet and silences either volume at zero', () => {
    expect(musicLevel(10, 10)).toBeLessThanOrEqual(0.5);
    expect(musicLevel(0, 10)).toBe(0);
    expect(musicLevel(10, 0)).toBe(0);
    expect(musicLevel(5, 5)).toBeLessThan(musicLevel(10, 10));
  });
});
