// All sound is synthesised here, with the Web Audio API and no files: square-wave bleeps for what the engine names
// (see Game.DrainCues), tiny UI blips for menus, and looping music played by a small step sequencer. The engine never
// plays anything; this file decides what each name sounds like and how loud it is.

/** [frequency in Hz, duration in seconds] steps played back to back; a frequency of 0 is a rest. */
export type Pattern = ReadonlyArray<readonly [number, number]>;

export const patterns: Readonly<Record<string, Pattern>> = {
  hit: [[180, 0.035], [120, 0.045]],
  hurt: [[110, 0.07], [80, 0.09]],
  kill: [[330, 0.04], [440, 0.04], [660, 0.07]],
  death: [[220, 0.12], [165, 0.14], [110, 0.18], [73, 0.3]],
  levelup: [[262, 0.07], [330, 0.07], [392, 0.07], [523, 0.16]],
  quest: [[392, 0.08], [0, 0.03], [392, 0.08], [523, 0.14]],
  magic: [[600, 0.03], [900, 0.03], [1200, 0.05]],
  warn: [[300, 0.05], [0, 0.03], [300, 0.05]],
  good: [[440, 0.04], [587, 0.07]],
  stairs: [[220, 0.06], [196, 0.06], [175, 0.06], [147, 0.14]],
  door: [[90, 0.05], [140, 0.04]],
  pickup: [[784, 0.03], [1047, 0.07]],
};

/** Menu blips, played by the front end itself (the engine does not know about them). */
export const uiPatterns: Readonly<Record<string, Pattern>> = {
  click: [[1800, 0.012]],
  move: [[1400, 0.018]],
  confirm: [[660, 0.035], [880, 0.06]],
  cancel: [[440, 0.04], [330, 0.06]],
};

/** Cues that are not square waves: a softer shape for friendly sounds, noise mixed under the blunt ones. */
const waves: Readonly<Record<string, OscillatorType>> = { good: 'triangle', pickup: 'triangle', stairs: 'triangle', levelup: 'triangle', quest: 'triangle' };
const noiseSeconds: Readonly<Record<string, number>> = { hit: 0.05, hurt: 0.08, kill: 0.06, door: 0.08, death: 0.2 };

/** Output level, 0..~0.15: master and effects volumes are 0..10, and the bleeps are kept polite. */
export function level(master: number, effects: number): number {
  const m = Math.max(0, Math.min(10, master)), e = Math.max(0, Math.min(10, effects));
  return (m / 10) * (e / 10) * 0.12;
}

/** Music bus level, 0..0.5. */
export function musicLevel(master: number, music: number): number {
  const m = Math.max(0, Math.min(10, master)), u = Math.max(0, Math.min(10, music));
  return (m / 10) * (u / 10) * 0.5;
}

export function cuesToPlay(cues: readonly string[] | undefined): Pattern[] {
  const out: Pattern[] = [];
  for (const cue of cues ?? []) { const p = patterns[cue]; if (p) out.push(p); }
  return out;
}

// ------------------------------------------------------------------------------------------------------ music

export type Voice = 'bell' | 'pulse' | 'tri' | 'cello' | 'pad' | 'tick' | 'thud' | 'drip';
/** One note: `b` is the start in beats from the top of the bar, `d` the length in beats, `m` a MIDI note. */
export interface Ev { v: Voice; b: number; d: number; m?: number; g?: number }
export interface Track { bpm: number; beats: number; bars: number; bar(i: number): Ev[] }
export type TrackName = 'title' | 'road' | 'town' | 'dungeon';

const hz = (m: number) => 440 * Math.pow(2, (m - 69) / 12);

interface Chord { r: number; a: [number, number, number] }
const Dm: Chord = { r: 38, a: [50, 53, 57] };
const Bb: Chord = { r: 34, a: [53, 58, 62] };
const Gm: Chord = { r: 31, a: [50, 55, 58] };
const A: Chord = { r: 33, a: [52, 57, 61] };
const Am: Chord = { r: 33, a: [52, 57, 60] };
const G: Chord = { r: 31, a: [55, 59, 62] };
const F: Chord = { r: 29, a: [53, 57, 60] };
const C: Chord = { r: 36, a: [52, 55, 60] };

/** A falling arpeggio, `steps` notes per bar, high to low and back: the footsteps on the spiral stair. */
function arp(c: Chord, steps: number, beats: number, g: number): Ev[] {
  const order = [2, 1, 0, 1];
  const out: Ev[] = [];
  for (let i = 0; i < steps; i++) out.push({ v: 'pulse', m: c.a[order[i % 4]!]!, b: i * beats / steps, d: beats / steps * 0.9, g });
  return out;
}

/** The theme's five notes: D F Eb D A, over one 6/4 bar. `step` is the third note's distance from the root: 1 gives the Eb of the minor, 2 the E of Dorian. */
function motif(root: number, step: number, g = 1, v: Voice = 'bell', drop = 0): Ev[] {
  const notes = [root, root + 3, root + step, root, root + 7];
  const times = [0, 1, 2, 3, 4];
  const lens = [1, 1, 1, 1, 2];
  const out: Ev[] = [];
  for (let i = 0; i < 5 - drop; i++) out.push({ v, m: notes[i]! + 0, b: times[i]!, d: lens[i]! * 1.6, g });
  return out;
}
const shifted = (evs: Ev[], by: number): Ev[] => evs.map(e => ({ ...e, m: (e.m ?? 0) + by }));

const bass = (c: Chord, g = 1): Ev[] => [{ v: 'tri', m: c.r, b: 0, d: 3, g }, { v: 'tri', m: c.r + (c === Dm ? 7 : 0), b: 3, d: 3, g: g * 0.8 }];
const ticks = (): Ev[] => [{ v: 'tick', b: 0, d: 0.2 }, { v: 'tick', b: 3, d: 0.2, g: 0.7 }];

const titleChords: Chord[] = [
  Dm, Dm, Dm, Dm, Dm, Dm, Bb, Bb, A, Dm,      // 0-9: the descent
  Bb, Gm, A, Dm,                              // 10-13: the hall of bones
  Dm, Bb, Dm, Dm,                             // 14-17: the cello
  Dm, Dm, Dm, Dm, Dm, Dm,                     // 18-23: the seal and the shutdown
];

/** The main theme, "Phosphor & Bone": 24 bars of 6/4 at 66 BPM, a procession that loops back into the first bar. */
const title: Track = {
  bpm: 66, beats: 6, bars: 24,
  bar(i) {
    const c = titleChords[i]!;
    const out: Ev[] = [];
    if (i === 0) out.push({ v: 'pad', m: 38, b: 0, d: 6, g: 0.8 });
    else if (i === 1) { out.push({ v: 'pad', m: 38, b: 0, d: 6, g: 0.8 }, { v: 'bell', m: 81, b: 0, d: 8, g: 0.7 }); }
    else if (i <= 9) {
      // The descent: the motif, the stair-step arpeggio from bar 2, the bone-tick from bar 4, a drip every eighth bar.
      out.push({ v: 'pad', m: 38, b: 0, d: 6.5, g: 0.5 }, ...motif(62, 1), ...arp(c, 12, 6, i < 4 ? 0.5 : 0.8), ...bass(c, 0.9));
      if (i >= 4) out.push(...ticks());
      if (i === 8) out.push({ v: 'drip', m: 96, b: 0, d: 1, g: 1 });
    } else if (i <= 13) {
      // The hall of bones: organ and choir, the motif in parallel fifths, the arpeggio twice as fast.
      out.push({ v: 'pad', m: 50, b: 0, d: 6.5, g: 1 }, { v: 'pad', m: 57, b: 0, d: 6.5, g: 0.9 }, { v: 'pad', m: 62, b: 0, d: 6.5, g: 0.7 },
        ...motif(62, 1), ...shifted(motif(62, 1, 0.7), -7), ...arp(c, 24, 6, 0.55), ...bass(c, 1), ...ticks());
      if (i === 10) out.push({ v: 'pad', m: 65, b: 0, d: 6.5, g: 0.7 }, { v: 'pad', m: 69, b: 0, d: 6.5, g: 0.6 });
    } else if (i <= 17) {
      // The cello and the lie: one octave up and ornamented; bar 16 holds a bright E natural, then the Eb comes back.
      const mel: Ev[] = [
        { v: 'cello', m: 74, b: 0, d: 1.4 }, { v: 'cello', m: 77, b: 1, d: 1.4 }, { v: 'cello', m: i === 16 ? 76 : 75, b: 2, d: 1.4 },
        { v: 'cello', m: 74, b: 3, d: 1.4 }, { v: 'cello', m: 81, b: 4, d: 2.2 },
      ];
      out.push({ v: 'pad', m: 38, b: 0, d: 6.5, g: 0.6 }, ...mel, ...bass(c, 0.9));
      if (i === 17) out.pop();   // silence under the last bar of the lie
    } else if (i <= 21) {
      // The seal: a heartbeat, an empty fifth, and the motif losing a note each time it comes back.
      out.push({ v: 'pad', m: 38, b: 0, d: 6.5, g: 1 }, { v: 'pad', m: 45, b: 0, d: 6.5, g: 0.9 }, { v: 'pad', m: 50, b: 0, d: 6.5, g: 0.6 },
        { v: 'thud', b: 0, d: 0.3 }, { v: 'thud', b: 0.5, d: 0.3, g: 0.4 }, { v: 'thud', b: 2, d: 0.3, g: 0.8 }, { v: 'thud', b: 2.5, d: 0.3, g: 0.3 },
        { v: 'thud', b: 4, d: 0.3, g: 0.6 }, ...motif(62, 1, 1, 'bell', i - 18));
    } else if (i === 22) out.push({ v: 'bell', m: 69, b: 0, d: 10, g: 0.9 }, { v: 'tick', b: 5, d: 0.2, g: 0.4 });
    return out;
  },
};

/** The road: the same five notes in D Dorian (a natural E), quicker, plucked, with no choir. */
const road: Track = {
  bpm: 84, beats: 6, bars: 8,
  bar(i) {
    const c = [Dm, G, Dm, Am][i % 4]!;
    const phrase = i % 2 === 0 ? motif(62, 2, 0.8) : [
      { v: 'bell', m: 69, b: 0, d: 1.5, g: 0.8 }, { v: 'bell', m: 67, b: 1, d: 1.5, g: 0.8 }, { v: 'bell', m: 65, b: 2, d: 1.5, g: 0.8 },
      { v: 'bell', m: 64, b: 3, d: 1.5, g: 0.8 }, { v: 'bell', m: 62, b: 4, d: 2.5, g: 0.8 },
    ] as Ev[];
    return [...phrase, ...arp(c, 12, 6, 0.45), ...bass(c, 0.8), { v: 'tick', b: 0, d: 0.2, g: 0.6 }];
  },
};

/** A town: the motif in F major, on a music box. The only place the bright note is allowed to stay. */
const town: Track = {
  bpm: 72, beats: 6, bars: 8,
  bar(i) {
    const c = [F, C, Dm, Bb][i % 4]!;
    const box = shifted([{ v: 'bell', m: 65, b: 0, d: 1 }, { v: 'bell', m: 69, b: 1, d: 1 }, { v: 'bell', m: 67, b: 2, d: 1 }, { v: 'bell', m: 65, b: 3, d: 1 }, { v: 'bell', m: 72, b: 4, d: 2 }], 12)
      .map(e => ({ ...e, g: 0.45, d: 0.7 }));
    return [...box, ...arp(c, 6, 6, 0.3), ...bass(c, 0.6)];
  },
};

/** Below ground: a drone, a drip, and now and then three notes of the theme, half remembered. */
const dungeon: Track = {
  bpm: 54, beats: 6, bars: 8,
  bar(i) {
    const out: Ev[] = [{ v: 'pad', m: 26, b: 0, d: 6.5, g: 0.9 }, { v: 'pad', m: 33, b: 0, d: 6.5, g: 0.5 }];
    if (i % 4 === 0) out.push({ v: 'pulse', m: 62, b: 1, d: 1.2, g: 0.35 }, { v: 'pulse', m: 65, b: 2.5, d: 1.2, g: 0.35 }, { v: 'pulse', m: 63, b: 4, d: 1.6, g: 0.3 });
    if (i % 4 === 2) out.push({ v: 'tick', b: 2, d: 0.2, g: 0.5 });
    if (i === 3 || i === 6) out.push({ v: 'drip', m: i === 3 ? 96 : 91, b: 1 + (i % 3), d: 1, g: 0.9 });
    return out;
  },
};

export const tracks: Readonly<Record<TrackName, Track>> = { title, road, town, dungeon };

export interface SoundScene { onTitle: boolean; intro: boolean; mode: string }

/** Which music suits what is on screen; null is silence (the hero is dead, or the run is over). */
export function trackFor(s: SoundScene): TrackName | null {
  if (s.onTitle || s.intro) return 'title';
  if (s.mode === 'Dungeon') return 'dungeon';
  if (s.mode === 'Overworld') return 'road';
  if (s.mode === 'TownMap') return 'town';
  return null;
}

// -------------------------------------------------------------------------------------------------- synthesis

let context: AudioContext | undefined;
let reverbIn: GainNode | undefined;
let noiseBuffer: AudioBuffer | undefined;
let pulseWave: PeriodicWave | undefined;

function setup(ctx: AudioContext) {
  // A cold hall: two seconds and a half of decaying noise, one shared by every voice.
  const len = Math.floor(ctx.sampleRate * 2.6);
  const impulse = ctx.createBuffer(2, len, ctx.sampleRate);
  for (let c = 0; c < 2; c++) {
    const d = impulse.getChannelData(c);
    for (let i = 0; i < len; i++) d[i] = (Math.random() * 2 - 1) * Math.pow(1 - i / len, 3.2);
  }
  const convolver = ctx.createConvolver();
  convolver.buffer = impulse;
  const wet = ctx.createGain(); wet.gain.value = 0.55;
  reverbIn = ctx.createGain();
  reverbIn.connect(convolver).connect(wet).connect(ctx.destination);

  noiseBuffer = ctx.createBuffer(1, ctx.sampleRate, ctx.sampleRate);
  const n = noiseBuffer.getChannelData(0);
  for (let i = 0; i < n.length; i++) n[i] = Math.random() * 2 - 1;

  // A pulse with a 12.5% duty cycle: thin and nasal, the sound of a PC speaker with a soul.
  const real = new Float32Array(33), imag = new Float32Array(33);
  for (let k = 1; k < 33; k++) imag[k] = (2 / (k * Math.PI)) * Math.sin(k * Math.PI * 0.125);
  pulseWave = ctx.createPeriodicWave(real, imag);
}

/** Browsers only start audio after a gesture: call this from a key press or a click. */
export function unlockAudio(): void {
  try {
    const Ctor = window.AudioContext ?? (window as unknown as { webkitAudioContext?: typeof AudioContext }).webkitAudioContext;
    if (!Ctor) return;
    if (!context) { context = new Ctor(); setup(context); }
    if (context.state === 'suspended') void context.resume().then(syncMusic);
    else syncMusic();
  } catch { /* No audio is a fine way to play. */ }
}

function running(): AudioContext | undefined { return context && context.state === 'running' ? context : undefined; }

function envelope(ctx: AudioContext, dest: AudioNode, at: number, attack: number, hold: number, release: number, peak: number): GainNode {
  const g = ctx.createGain();
  g.gain.setValueAtTime(0.0001, at);
  g.gain.exponentialRampToValueAtTime(Math.max(peak, 0.0002), at + attack);
  g.gain.setValueAtTime(Math.max(peak, 0.0002), at + attack + hold);
  g.gain.exponentialRampToValueAtTime(0.0001, at + attack + hold + release);
  g.connect(dest);
  return g;
}

function send(ctx: AudioContext, from: AudioNode, amount: number) {
  if (!reverbIn || amount <= 0) return;
  const s = ctx.createGain(); s.gain.value = amount;
  from.connect(s).connect(reverbIn);
}

function noiseBurst(ctx: AudioContext, dest: AudioNode, at: number, seconds: number, peak: number, filter: BiquadFilterType, freq: number) {
  const src = ctx.createBufferSource();
  src.buffer = noiseBuffer!;
  const f = ctx.createBiquadFilter(); f.type = filter; f.frequency.value = freq;
  const g = envelope(ctx, dest, at, 0.002, 0, seconds, peak);
  src.connect(f).connect(g);
  src.start(at, Math.random() * 0.5, seconds + 0.05);
}

/** One note of the score, built from oscillators and thrown away when it ends. */
function play(ctx: AudioContext, bus: GainNode, e: Ev, at: number, spb: number) {
  const dur = e.d * spb, g = e.g ?? 1, f = e.m !== undefined ? hz(e.m) : 0;
  switch (e.v) {
    case 'bell': {
      // FM: a sine carrier bent by a modulator at 3.5 times its pitch, the bend dying away.
      const car = ctx.createOscillator(), mod = ctx.createOscillator(), idx = ctx.createGain();
      car.frequency.value = f; mod.frequency.value = f * 3.5;
      idx.gain.setValueAtTime(f * 2.2, at); idx.gain.exponentialRampToValueAtTime(f * 0.05, at + dur);
      mod.connect(idx).connect(car.frequency);
      const amp = envelope(ctx, bus, at, 0.004, 0, dur, 0.26 * g);
      car.connect(amp); send(ctx, amp, 0.8);
      car.start(at); mod.start(at); car.stop(at + dur + 0.05); mod.stop(at + dur + 0.05);
      break;
    }
    case 'pulse': {
      const o = ctx.createOscillator(); o.setPeriodicWave(pulseWave!); o.frequency.value = f;
      const amp = envelope(ctx, bus, at, 0.005, dur * 0.3, dur * 0.7, 0.055 * g);
      o.connect(amp); send(ctx, amp, 0.25);
      o.start(at); o.stop(at + dur + 0.05);
      break;
    }
    case 'tri': {
      const o = ctx.createOscillator(); o.type = 'triangle'; o.frequency.value = f;
      const amp = envelope(ctx, bus, at, 0.02, dur * 0.5, dur * 0.5, 0.2 * g);
      o.connect(amp);
      o.start(at); o.stop(at + dur + 0.05);
      break;
    }
    case 'cello': {
      // A bowed string: slow attack, a low-passed saw, a vibrato that opens up after a moment.
      const o = ctx.createOscillator(); o.type = 'sawtooth'; o.frequency.value = f;
      const lp = ctx.createBiquadFilter(); lp.type = 'lowpass'; lp.frequency.value = 1500; lp.Q.value = 0.7;
      const lfo = ctx.createOscillator(), depth = ctx.createGain();
      lfo.frequency.value = 5.2; depth.gain.setValueAtTime(0, at); depth.gain.linearRampToValueAtTime(f * 0.012, at + dur * 0.7);
      lfo.connect(depth).connect(o.frequency);
      const amp = envelope(ctx, bus, at, 0.18, dur * 0.6, dur * 0.35, 0.13 * g);
      o.connect(lp).connect(amp); send(ctx, amp, 0.7);
      o.start(at); lfo.start(at); o.stop(at + dur + 0.4); lfo.stop(at + dur + 0.4);
      break;
    }
    case 'pad': {
      // Organ and choir: two detuned saws behind a low-pass, a very slow swell, and a great deal of hall.
      const lp = ctx.createBiquadFilter(); lp.type = 'lowpass'; lp.frequency.value = 520; lp.Q.value = 0.4;
      const amp = envelope(ctx, bus, at, dur * 0.35, dur * 0.3, dur * 0.35, 0.05 * g);
      lp.connect(amp); send(ctx, amp, 1);
      for (const cents of [-7, 7]) {
        const o = ctx.createOscillator(); o.type = 'sawtooth'; o.frequency.value = f; o.detune.value = cents;
        o.connect(lp); o.start(at); o.stop(at + dur + 0.1);
      }
      break;
    }
    case 'tick': noiseBurst(ctx, bus, at, 0.03, 0.06 * g, 'highpass', 3500); break;
    case 'thud': noiseBurst(ctx, bus, at, 0.12, 0.5 * g, 'lowpass', 160); break;
    case 'drip': {
      const o = ctx.createOscillator(); o.type = 'sine';
      o.frequency.setValueAtTime(f * 1.6, at); o.frequency.exponentialRampToValueAtTime(f, at + 0.05);
      const amp = envelope(ctx, bus, at, 0.002, 0, 0.18, 0.12 * g);
      o.connect(amp); send(ctx, amp, 1.2);
      o.start(at); o.stop(at + 0.3);
      break;
    }
  }
}

interface Running { name: TrackName; bus: GainNode; timer: ReturnType<typeof setInterval>; stop(): void }
let current: Running | undefined;
let wanted: { track: TrackName | null; master: number; music: number } = { track: null, master: 0, music: 0 };

function startTrack(ctx: AudioContext, name: TrackName, gain: number): Running {
  const track = tracks[name];
  const bus = ctx.createGain();
  bus.gain.setValueAtTime(0.0001, ctx.currentTime);
  bus.gain.linearRampToValueAtTime(gain, ctx.currentTime + 1.2);
  bus.connect(ctx.destination);
  if (reverbIn) bus.connect(reverbIn);
  const spb = 60 / track.bpm;
  let bar = 0, next = ctx.currentTime + 0.15;
  const pump = () => {
    try {
      while (next < ctx.currentTime + 0.6) {
        for (const e of track.bar(bar % track.bars)) play(ctx, bus, e, next + e.b * spb, spb);
        next += track.beats * spb; bar++;
      }
    } catch { /* A missed note is not worth a crash. */ }
  };
  pump();
  const timer = setInterval(pump, 150);
  return {
    name, bus, timer,
    stop() {
      clearInterval(timer);
      try { bus.gain.cancelScheduledValues(ctx.currentTime); bus.gain.setTargetAtTime(0.0001, ctx.currentTime, 0.3); } catch { /* ignore */ }
      setTimeout(() => { try { bus.disconnect(); } catch { /* ignore */ } }, 2500);
    },
  };
}

/** Starts, changes or fades out the music so it matches `wanted`; safe to call as often as you like. */
function syncMusic(): void {
  const ctx = running();
  if (!ctx) return;
  try {
    const gain = musicLevel(wanted.master, wanted.music);
    const track = gain > 0 ? wanted.track : null;
    if (current && current.name !== track) { current.stop(); current = undefined; }
    if (!current && track) current = startTrack(ctx, track, gain);
    else if (current) current.bus.gain.setTargetAtTime(gain, ctx.currentTime, 0.15);
  } catch { /* Ignore audio errors. */ }
}

/** Says which music should be playing and how loud (volumes 0..10). Cheap enough to call on every frame. */
export function setMusic(track: TrackName | null, master: number, music: number): void {
  if (wanted.track === track && wanted.master === master && wanted.music === music) return;
  wanted = { track, master, music };
  syncMusic();
}

function bleep(ctx: AudioContext, pattern: Pattern, gain: number, at: number, wave: OscillatorType, noise: number): number {
  if (noise > 0) noiseBurst(ctx, ctx.destination, at, noise, gain * 2.5, 'lowpass', 1800);
  for (const [freq, seconds] of pattern) {
    if (freq > 0) {
      const osc = ctx.createOscillator();
      const amp = ctx.createGain();
      osc.type = wave;
      osc.frequency.setValueAtTime(freq, at);
      amp.gain.setValueAtTime(gain, at);
      amp.gain.exponentialRampToValueAtTime(0.0001, at + seconds);
      osc.connect(amp).connect(ctx.destination);
      osc.start(at);
      osc.stop(at + seconds + 0.01);
    }
    at += seconds;
  }
  return at + 0.02;
}

/** Plays the cues one after another, quietly, never throwing: sound must not be able to break the game. */
export function playCues(cues: readonly string[] | undefined, master: number, effects: number): void {
  const gain = level(master, effects);
  const ctx = running();
  if (!ctx || gain <= 0 || !cues?.length) return;
  try {
    let at = ctx.currentTime + 0.005;
    for (const cue of cues) {
      const p = patterns[cue];
      if (p) at = bleep(ctx, p, gain, at, waves[cue] ?? 'square', noiseSeconds[cue] ?? 0);
    }
  } catch { /* Ignore audio errors. */ }
}

/** A menu blip: `click`, `move`, `confirm` or `cancel`. */
export function playUi(name: string, master: number, effects: number): void {
  const gain = level(master, effects) * 0.7;
  const ctx = running();
  const p = uiPatterns[name];
  if (!ctx || gain <= 0 || !p) return;
  try { bleep(ctx, p, gain, ctx.currentTime + 0.002, 'square', 0); } catch { /* Ignore audio errors. */ }
}

/** Which blip a key makes inside a menu panel. */
export function uiSoundFor(code: string): string {
  if (code === 'Escape') return 'cancel';
  if (code === 'Enter' || code === 'NumpadEnter' || code === 'Space') return 'confirm';
  if (/^(Arrow|Page|Home|End|Numpad[0-9])/.test(code)) return 'move';
  return 'click';
}
