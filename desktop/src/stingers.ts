// Stingers: short musical signs rendered sample by sample, with no audio files. Each instrument is a small physical or
// additive model (bells ring in inharmonic partials, strings are plucked with a Karplus-Strong loop, the piano is a
// stack of slightly stretched partials, the cello is a bowed saw with a late vibrato, the organ breathes). Everything is
// deterministic, so the same call always produces the same samples, and it runs in the game and in a node script alike.

export type StingerName =
  | 'level-up' | 'quest-accepted' | 'quest-updated' | 'quest-complete' | 'item-found' | 'danger' | 'trap'
  | 'rest' | 'gate' | 'death' | 'victory' | 'ending-corrupted' | 'ending-sealed' | 'ending-free' | 'new-cycle';

export interface StingerInfo { seconds: number; title: string }

export const stingerInfo: Readonly<Record<StingerName, StingerInfo>> = {
  'level-up': { seconds: 4, title: 'Level Up' },
  'quest-accepted': { seconds: 4, title: 'Quest Accepted' },
  'quest-updated': { seconds: 2, title: 'Quest Updated' },
  'quest-complete': { seconds: 6, title: 'Quest Complete' },
  'item-found': { seconds: 3, title: 'Item Found' },
  'danger': { seconds: 3, title: 'Danger Spotted' },
  'trap': { seconds: 2, title: 'Trap Triggered' },
  'rest': { seconds: 8, title: 'Rest' },
  'gate': { seconds: 4, title: 'Gate' },
  'death': { seconds: 10, title: 'Death' },
  'victory': { seconds: 20, title: 'Victory' },
  'ending-corrupted': { seconds: 25, title: 'Ending: Corrupted' },
  'ending-sealed': { seconds: 25, title: 'Ending: Sealed' },
  'ending-free': { seconds: 25, title: 'Ending: Free' },
  'new-cycle': { seconds: 8, title: 'New Cycle' },
};

export const stingerNames = Object.keys(stingerInfo) as StingerName[];

const TAU = Math.PI * 2;
const hz = (m: number) => 440 * Math.pow(2, (m - 69) / 12);

/** A tiny seeded generator, so noise is the same every time. */
function rng(seed: number): () => number {
  let a = seed >>> 0;
  return () => {
    a = (a + 0x6D2B79F5) >>> 0;
    let t = a;
    t = Math.imul(t ^ (t >>> 15), t | 1);
    t ^= t + Math.imul(t ^ (t >>> 7), t | 61);
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
  };
}

const MOTIF = [62, 65, 63, 62, 69];   // D F Eb D A

// ------------------------------------------------------------------------------------------------ the mix

class Mix {
  readonly data: Float32Array;
  readonly sr: number;
  constructor(sr: number, seconds: number) { this.sr = sr; this.data = new Float32Array(Math.floor(sr * seconds)); }
  add(at: number, s: Float32Array, gain = 1) {
    const start = Math.floor(at * this.sr);
    for (let i = 0; i < s.length; i++) {
      const j = start + i;
      if (j >= 0 && j < this.data.length) this.data[j]! += s[i]! * gain;
    }
  }
}

/** Soft attack, steady hold and a smooth release over `n` samples. */
function shape(n: number, sr: number, attack: number, release: number): Float32Array {
  const e = new Float32Array(n);
  const a = Math.max(1, Math.floor(attack * sr)), r = Math.max(1, Math.floor(release * sr));
  for (let i = 0; i < n; i++) {
    let v = 1;
    if (i < a) v = Math.sin((i / a) * Math.PI / 2);
    const fromEnd = n - i;
    if (fromEnd < r) v *= Math.sin((fromEnd / r) * Math.PI / 2);
    e[i] = v;
  }
  return e;
}

function lowpass(x: Float32Array, sr: number, cutoff: number): Float32Array {
  const a = 1 - Math.exp(-TAU * cutoff / sr);
  let y = 0;
  for (let i = 0; i < x.length; i++) { y += a * (x[i]! - y); x[i] = y; }
  return x;
}

function highpass(x: Float32Array, sr: number, cutoff: number): Float32Array {
  const a = Math.exp(-TAU * cutoff / sr);
  let px = 0, py = 0;
  for (let i = 0; i < x.length; i++) { const v = x[i]!; py = a * (py + v - px); px = v; x[i] = py; }
  return x;
}

function noise(n: number, rnd: () => number): Float32Array {
  const x = new Float32Array(n);
  for (let i = 0; i < n; i++) x[i] = rnd() * 2 - 1;
  return x;
}

function reversed(x: Float32Array): Float32Array { return Float32Array.from(x).reverse(); }

// ------------------------------------------------------------------------------------------- instruments

/** A handbell or church bell: inharmonic partials, the high ones dying first. `size` stretches the ring. */
function bell(sr: number, f: number, dur: number, rnd: () => number, size = 1): Float32Array {
  const n = Math.floor(sr * dur), out = new Float32Array(n);
  const parts: [number, number, number][] = [[0.5, 0.45, 1.2], [1, 1, 1], [1.2, 0.35, 0.8], [2.0, 0.5, 0.6], [2.76, 0.55, 0.45], [4.07, 0.3, 0.3], [5.4, 0.22, 0.22], [8.93, 0.1, 0.12]];
  for (const [ratio, amp, decay] of parts) {
    const fr = f * ratio;
    if (fr > sr * 0.45) continue;
    const tau = dur * 0.3 * decay * size;
    const ph = rnd() * TAU;
    for (let i = 0; i < n; i++) out[i]! += amp * Math.exp(-i / sr / tau) * Math.sin(TAU * fr * i / sr + ph);
  }
  const strike = lowpass(noise(Math.floor(sr * 0.02), rnd), sr, 5000);
  for (let i = 0; i < strike.length; i++) out[i]! += strike[i]! * 0.5 * (1 - i / strike.length);
  return out;
}

/** A celesta: a pure, fragile tone with a quiet bell partial that vanishes fast. */
function celesta(sr: number, f: number, dur: number, rnd: () => number): Float32Array {
  const n = Math.floor(sr * dur), out = new Float32Array(n);
  const ph = rnd() * TAU;
  for (let i = 0; i < n; i++) {
    const t = i / sr;
    out[i] = Math.exp(-t / (dur * 0.35)) * Math.sin(TAU * f * t + ph)
      + 0.25 * Math.exp(-t / 0.18) * Math.sin(TAU * f * 4 * t) + 0.08 * Math.exp(-t / 0.08) * Math.sin(TAU * f * 9.2 * t);
  }
  return out;
}

/** A piano: slightly stretched partials, two strings a hair apart, and a soft felt thump for the hammer. */
function piano(sr: number, f: number, dur: number, rnd: () => number, vel = 0.8): Float32Array {
  const n = Math.floor(sr * dur), out = new Float32Array(n);
  const base = Math.min(dur, 9) * Math.max(0.25, Math.min(1.2, Math.pow(130 / f, 0.35)));
  const B = 0.0003;
  for (let k = 1; k <= 12; k++) {
    const fk = k * f * Math.sqrt(1 + B * k * k);
    if (fk > sr * 0.45) break;
    const amp = vel / Math.pow(k, 1.15) * (k === 2 ? 1.1 : 1);
    const tau = base / Math.pow(k, 0.7) * 0.55;
    for (const det of [-0.6, 0.6]) {
      const ph = rnd() * TAU;
      const fd = fk * (1 + det * 0.0006);
      for (let i = 0; i < n; i++) out[i]! += 0.5 * amp * Math.exp(-i / sr / tau) * Math.sin(TAU * fd * i / sr + ph);
    }
  }
  const thump = lowpass(noise(Math.floor(sr * 0.03), rnd), sr, 900);
  for (let i = 0; i < thump.length; i++) out[i]! += thump[i]! * 0.5 * vel * (1 - i / thump.length);
  const fade = shape(n, sr, 0.002, Math.min(0.4, dur * 0.4));
  for (let i = 0; i < n; i++) out[i]! *= fade[i]!;
  return out;
}

/** A plucked string (harp, pizzicato) with the Karplus-Strong loop: noise in a damped delay line. */
function pluck(sr: number, f: number, dur: number, rnd: () => number, damp = 0.9965): Float32Array {
  const n = Math.floor(sr * dur), out = new Float32Array(n);
  const len = Math.max(2, Math.round(sr / f));
  const line = lowpass(noise(len, rnd), sr, 6000);
  let idx = 0;
  for (let i = 0; i < n; i++) {
    const next = (idx + 1) % len;
    const v = damp * 0.5 * (line[idx]! + line[next]!);
    out[i] = line[idx]!;
    line[idx] = v;
    idx = next;
  }
  const fade = shape(n, sr, 0.001, Math.min(0.3, dur * 0.3));
  for (let i = 0; i < n; i++) out[i]! *= fade[i]!;
  return out;
}

/** A bowed string: a saw built from harmonics, a vibrato that opens late, bow noise and a darkening low-pass. */
function bowed(sr: number, f: number, dur: number, rnd: () => number, opts: { vib?: number; attack?: number; bright?: number; release?: number } = {}): Float32Array {
  const n = Math.floor(sr * dur), out = new Float32Array(n);
  const vib = opts.vib ?? 0.008, attack = opts.attack ?? 0.14, bright = opts.bright ?? 2600, release = opts.release ?? 0.5;
  const harm = Math.max(2, Math.min(26, Math.floor((sr * 0.45) / f)));
  let phase = 0;
  for (let i = 0; i < n; i++) {
    const t = i / sr;
    const depth = vib * Math.min(1, t / Math.max(0.5, dur * 0.5));
    phase += TAU * f * (1 + depth * Math.sin(TAU * 5.1 * t)) / sr;
    let s = 0;
    for (let k = 1; k <= harm; k++) s += Math.sin(k * phase) / Math.pow(k, 0.95);
    out[i] = s;
  }
  const bow = highpass(lowpass(noise(n, rnd), sr, 3500), sr, 400);
  for (let i = 0; i < n; i++) out[i]! += bow[i]! * 0.06;
  lowpass(out, sr, bright);
  const env = shape(n, sr, attack, Math.min(release, dur * 0.5));
  for (let i = 0; i < n; i++) out[i]! *= env[i]! * 0.45;
  return out;
}

/** A small old pipe organ, reeds half closed: a few drawbars, breath, a slow swell and a slow release. */
function organ(sr: number, f: number, dur: number, rnd: () => number, detune = 0): Float32Array {
  const n = Math.floor(sr * dur), out = new Float32Array(n);
  const bars: [number, number][] = [[1, 1], [2, 0.55], [3, 0.35], [4, 0.2], [6, 0.1], [0.5, 0.45]];
  for (const [ratio, amp] of bars) {
    const fr = f * ratio * (1 + detune);
    if (fr > sr * 0.45) continue;
    const ph = rnd() * TAU;
    for (let i = 0; i < n; i++) out[i]! += amp * Math.sin(TAU * fr * i / sr + ph);
  }
  const breath = lowpass(noise(n, rnd), sr, 1800);
  for (let i = 0; i < n; i++) out[i]! += breath[i]! * 0.05;
  lowpass(out, sr, 1400);
  const env = shape(n, sr, Math.min(1.2, dur * 0.3), Math.min(1.5, dur * 0.45));
  for (let i = 0; i < n; i++) out[i]! *= env[i]! * 0.5;
  return out;
}

/** A clarinet: odd harmonics only, a breath of noise, a gentle swell. */
function clarinet(sr: number, f: number, dur: number, rnd: () => number): Float32Array {
  const n = Math.floor(sr * dur), out = new Float32Array(n);
  let phase = 0;
  for (let i = 0; i < n; i++) {
    const t = i / sr;
    phase += TAU * f * (1 + 0.004 * Math.min(1, t / 0.8) * Math.sin(TAU * 4.6 * t)) / sr;
    let s = 0;
    for (const k of [1, 3, 5, 7, 9]) if (f * k < sr * 0.45) s += Math.sin(k * phase) / Math.pow(k, 1.1);
    out[i] = s;
  }
  const breath = highpass(lowpass(noise(n, rnd), sr, 4000), sr, 1500);
  for (let i = 0; i < n; i++) out[i]! += breath[i]! * 0.05;
  lowpass(out, sr, 2200);
  const env = shape(n, sr, 0.09, Math.min(0.4, dur * 0.4));
  for (let i = 0; i < n; i++) out[i]! *= env[i]! * 0.5;
  return out;
}

/** A timpani or bass drum: a sine whose pitch falls fast, and a muffled thud of noise. */
function drum(sr: number, f: number, dur: number, rnd: () => number): Float32Array {
  const n = Math.floor(sr * dur), out = new Float32Array(n);
  let phase = 0;
  for (let i = 0; i < n; i++) {
    const t = i / sr;
    phase += TAU * f * (1 + 0.6 * Math.exp(-t / 0.05)) / sr;
    out[i] = Math.exp(-t / (dur * 0.28)) * Math.sin(phase);
  }
  const thud = lowpass(noise(Math.floor(sr * 0.06), rnd), sr, 500);
  for (let i = 0; i < thud.length; i++) out[i]! += thud[i]! * 0.7 * (1 - i / thud.length);
  return out;
}

/** A dull wooden knock or click. */
function knock(sr: number, rnd: () => number, f = 320, dur = 0.07): Float32Array {
  const n = Math.floor(sr * dur), out = new Float32Array(n);
  const ph = rnd() * TAU;
  for (let i = 0; i < n; i++) {
    const t = i / sr;
    out[i] = Math.exp(-t / 0.012) * (Math.sin(TAU * f * t + ph) * 0.7 + (rnd() * 2 - 1) * 0.35);
  }
  return out;
}

/** A single water drop landing in a cistern. */
function drip(sr: number, f: number, rnd: () => number): Float32Array {
  const n = Math.floor(sr * 0.35), out = new Float32Array(n);
  let phase = rnd() * TAU;
  for (let i = 0; i < n; i++) {
    const t = i / sr;
    phase += TAU * (f * (1 + 1.8 * Math.exp(-t / 0.025))) / sr;
    out[i] = Math.exp(-t / 0.09) * Math.sin(phase);
  }
  return out;
}

/** Wind or a rush of cold air: band-limited noise with a slow swell. */
function wind(sr: number, dur: number, rnd: () => number, lo = 200, hi = 1400): Float32Array {
  const n = Math.floor(sr * dur);
  const x = highpass(lowpass(noise(n, rnd), sr, hi), sr, lo);
  const env = shape(n, sr, dur * 0.4, dur * 0.4);
  for (let i = 0; i < n; i++) x[i]! *= env[i]!;
  return x;
}

/** A dry scrape of bow, iron or stone: noise that sweeps down. */
function scrape(sr: number, dur: number, rnd: () => number, from = 3000, to = 500): Float32Array {
  const n = Math.floor(sr * dur), out = new Float32Array(n);
  let y = 0;
  for (let i = 0; i < n; i++) {
    const fc = from + (to - from) * (i / n);
    const a = 1 - Math.exp(-TAU * fc / sr);
    y += a * ((rnd() * 2 - 1) - y);
    out[i] = y * Math.exp(-i / sr / (dur * 0.5));
  }
  return out;
}

// ------------------------------------------------------------------------------------------------ space

/** Schroeder reverb: four combs and two all-passes, tuned by the length of the tail. */
function reverb(x: Float32Array, sr: number, rt60: number, wet: number): Float32Array {
  const delays = [0.0297, 0.0371, 0.0411, 0.0437].map(d => Math.floor(d * sr));
  const out = new Float32Array(x.length);
  for (const d of delays) {
    const fb = Math.pow(10, -3 * (d / sr) / rt60);
    const buf = new Float32Array(d);
    let idx = 0, damp = 0;
    for (let i = 0; i < x.length; i++) {
      const y = buf[idx]!;
      damp = damp * 0.35 + y * 0.65;
      buf[idx] = x[i]! + damp * fb;
      out[i]! += y * 0.25;
      idx = (idx + 1) % d;
    }
  }
  for (const [dt, g] of [[0.005, 0.7], [0.0017, 0.7]] as const) {
    const d = Math.floor(dt * sr), buf = new Float32Array(d);
    let idx = 0;
    for (let i = 0; i < out.length; i++) {
      const v = buf[idx]!;
      const y = -g * out[i]! + v;
      buf[idx] = out[i]! + g * y;
      out[i] = y;
      idx = (idx + 1) % d;
    }
  }
  const res = new Float32Array(x.length);
  for (let i = 0; i < x.length; i++) res[i] = x[i]! * (1 - wet) + out[i]! * wet;
  return res;
}

/** Scales to a steady peak and fades the last few milliseconds so no sting ever ends on a click. */
function finish(x: Float32Array, sr: number, peak = 0.85): Float32Array {
  let max = 0;
  for (let i = 0; i < x.length; i++) max = Math.max(max, Math.abs(x[i]!));
  const g = max > 0 ? peak / max : 1;
  const fade = Math.floor(sr * 0.05);
  for (let i = 0; i < x.length; i++) {
    let v = x[i]! * g;
    const fromEnd = x.length - i;
    if (fromEnd < fade) v *= fromEnd / fade;
    x[i] = v;
  }
  return x;
}

// ------------------------------------------------------------------------------------------------ scores

type Score = (sr: number, rnd: () => number) => Float32Array;

const scores: Record<StingerName, Score> = {
  'level-up': (sr, rnd) => {
    const m = new Mix(sr, 4);
    [74, 77, 81, 86].forEach((n, i) => m.add(0.15 + i * 0.2, celesta(sr, hz(n), 2.6, rnd), 0.6));
    m.add(0.1, organ(sr, hz(38), 3.6, rnd), 0.7);
    m.add(0.95, bell(sr, hz(86), 2.5, rnd, 0.6), 0.12);
    return reverb(m.data, sr, 1.1, 0.22);
  },
  'quest-accepted': (sr, rnd) => {
    const m = new Mix(sr, 4);
    m.add(0.1, piano(sr, hz(67), 2.4, rnd, 0.8), 0.8);
    m.add(0.7, piano(sr, hz(63), 2.4, rnd, 0.7), 0.7);
    for (const n of [48, 55, 63]) m.add(1.3, bowed(sr, hz(n), 2.4, rnd, { attack: 0.3, release: 0.8 }), 0.45);
    return reverb(m.data, sr, 1.0, 0.2);
  },
  'quest-updated': (sr, rnd) => {
    const m = new Mix(sr, 2);
    m.add(0.05, piano(sr, hz(69), 1.8, rnd, 0.8), 0.8);
    m.add(0.12, pluck(sr, hz(38), 1.8, rnd, 0.998), 0.55);
    return reverb(m.data, sr, 0.8, 0.16);
  },
  'quest-complete': (sr, rnd) => {
    const m = new Mix(sr, 6);
    const times = [0.1, 0.95, 1.8, 2.65, 3.5];
    MOTIF.forEach((n, i) => m.add(times[i]!, piano(sr, hz(n), i === 4 ? 2.5 : 1.3, rnd, 0.75), 0.8));
    m.add(3.5, bowed(sr, hz(50), 2.5, rnd, { attack: 0.35, release: 0.8 }), 0.5);
    m.add(3.9, organ(sr, hz(50), 2.1, rnd), 0.5);
    m.add(3.9, organ(sr, hz(57), 2.1, rnd), 0.45);
    m.add(3.9, bowed(sr, hz(81), 2.1, rnd, { attack: 0.4, release: 0.9, bright: 3500, vib: 0.006 }), 0.18);
    return reverb(m.data, sr, 1.6, 0.25);
  },
  'item-found': (sr, rnd) => {
    const m = new Mix(sr, 3);
    [74, 77, 81, 84, 88].forEach((n, i) => m.add(0.05 + i * 0.09, pluck(sr, hz(n), 1.6, rnd, 0.996), 0.55));
    m.add(0.62, celesta(sr, hz(93), 2.2, rnd), 0.5);
    return reverb(m.data, sr, 1.0, 0.2);
  },
  'danger': (sr, rnd) => {
    const m = new Mix(sr, 3);
    m.add(0.0, drum(sr, hz(38), 1.1, rnd), 0.95);
    m.add(0.38, drum(sr, hz(39), 1.1, rnd), 0.95);
    m.add(0.0, bowed(sr, hz(26), 1.6, rnd, { attack: 0.05, release: 0.5, bright: 900 }), 0.7);
    m.add(1.15, scrape(sr, 0.18, rnd, 4500, 1500), 0.5);
    for (const n of [62, 63]) m.add(1.15, bowed(sr, hz(n), 0.35, rnd, { attack: 0.01, release: 0.2, vib: 0 }), 0.5);
    return reverb(m.data, sr, 0.6, 0.1);
  },
  'trap': (sr, rnd) => {
    const m = new Mix(sr, 2);
    for (const n of [50, 51, 52, 53]) m.add(0.0, piano(sr, hz(n), 1.2, rnd, 0.9), 0.5);
    m.add(0.0, drum(sr, hz(36), 0.7, rnd), 0.9);
    m.add(0.04, scrape(sr, 0.5, rnd, 4000, 400), 0.4);
    return reverb(m.data, sr, 0.5, 0.08);
  },
  'rest': (sr, rnd) => {
    const m = new Mix(sr, 8);
    m.add(0.0, bowed(sr, hz(38), 7.5, rnd, { attack: 1.2, release: 2.4, vib: 0.004, bright: 1400 }), 0.55);
    m.add(0.2, organ(sr, hz(26), 7.4, rnd), 0.45);
    const times = [0.8, 2.1, 3.4, 4.7, 5.8];
    MOTIF.forEach((n, i) => m.add(times[i]!, piano(sr, hz(n), i === 4 ? 2.4 : 1.8, rnd, 0.5 - i * 0.05), 0.7));
    return reverb(m.data, sr, 2.4, 0.3);
  },
  'gate': (sr, rnd) => {
    const m = new Mix(sr, 4);
    const boom = lowpass(noise(Math.floor(sr * 1.4), rnd), sr, 260);
    for (let i = 0; i < boom.length; i++) boom[i]! *= Math.exp(-i / sr / 0.35);
    m.add(0.0, boom, 1.4);
    m.add(0.0, drum(sr, hz(31), 1.6, rnd), 0.8);
    m.add(0.25, wind(sr, 2.6, rnd, 250, 1800), 0.3);
    m.add(0.5, bell(sr, hz(50), 3.4, rnd, 1.4), 0.35);
    return reverb(m.data, sr, 2.0, 0.28);
  },
  'death': (sr, rnd) => {
    const m = new Mix(sr, 10);
    m.add(0.0, organ(sr, hz(38), 9.8, rnd), 0.8);
    m.add(0.1, organ(sr, hz(45), 9.6, rnd), 0.35);
    const notes = [68, 71, 69, 68, 75];            // the motif a tritone away
    const times = [0.6, 1.9, 3.2, 4.5, 5.8];
    notes.forEach((n, i) => m.add(times[i]!, piano(sr, hz(n), i === 4 ? 3.8 : 2.2, rnd, 0.7), 0.75));
    m.add(0.0, bell(sr, hz(50), 6, rnd, 1.6), 0.4);
    m.add(5.0, bell(sr, hz(50), 5, rnd, 1.6), 0.3);
    m.add(7.0, drip(sr, 520, rnd), 0.2);
    return reverb(m.data, sr, 3.0, 0.32);
  },
  'victory': (sr, rnd) => {
    const m = new Mix(sr, 20);
    const times = [0.5, 2.5, 4.5, 6.5, 8.5];
    MOTIF.forEach((n, i) => {
      m.add(times[i]!, piano(sr, hz(n), i === 4 ? 4 : 2.6, rnd, 0.6), 0.7);
      m.add(times[i]!, bowed(sr, hz(n - 12), i === 4 ? 4 : 2.4, rnd, { attack: 0.5, release: 1 }), 0.4);
    });
    m.add(0.0, organ(sr, hz(38), 19.5, rnd), 0.55);
    m.add(12.5, piano(sr, hz(66), 5, rnd, 0.55), 0.7);          // F sharp: the true major third
    m.add(12.5, bowed(sr, hz(54), 5.2, rnd, { attack: 0.8, release: 1.5 }), 0.45);
    m.add(11.5, bowed(sr, hz(93), 8, rnd, { attack: 1.8, release: 3, bright: 3800, vib: 0.005 }), 0.12);
    m.add(15.0, bell(sr, hz(50), 5, rnd, 1.8), 0.35);
    m.add(15.0, bell(sr, hz(57), 5, rnd, 1.8), 0.3);
    return reverb(m.data, sr, 3.2, 0.34);
  },
  'ending-corrupted': (sr, rnd) => {
    const m = new Mix(sr, 25);
    const times = [0.5, 2.6, 4.7, 6.8, 8.9];
    MOTIF.forEach((n, i) => m.add(times[i]!, bowed(sr, hz(n - 12), i === 4 ? 4.4 : 2.4, rnd, { attack: 0.6, release: 1.2, vib: 0.01 }), 0.55));
    m.add(4.0, reversed(piano(sr, hz(50), 6, rnd, 0.7)), 0.7);
    m.add(9.0, reversed(piano(sr, hz(53), 6, rnd, 0.7)), 0.6);
    m.add(0.0, organ(sr, hz(38), 24.5, rnd, -0.012), 0.55);
    m.add(15.0, organ(sr, hz(50), 9.5, rnd, -0.015), 0.5);   // the chord turns cold and flat
    m.add(15.0, organ(sr, hz(53), 9.5, rnd, -0.012), 0.45);
    m.add(15.0, organ(sr, hz(57), 9.5, rnd, -0.02), 0.4);
    [11, 16.5, 21].forEach(t => m.add(t, drip(sr, 300, rnd), 0.45));
    m.add(18, scrape(sr, 4, rnd, 900, 150), 0.3);
    return reverb(m.data, sr, 3.5, 0.36);
  },
  'ending-sealed': (sr, rnd) => {
    const m = new Mix(sr, 25);
    let t = 1;
    for (let pass = 0; pass < 5; pass++) {
      const count = 5 - pass;
      for (let i = 0; i < count; i++) {
        const last = i === count - 1;
        m.add(t + i * 1.0, piano(sr, hz(MOTIF[i]!), last ? 3.5 : 1.6, rnd, 0.7 - pass * 0.08), 0.75);
      }
      t += count * 1.0 + 1.6;
    }
    m.add(0.0, organ(sr, hz(38), 24, rnd), 0.35);
    m.add(24.2, knock(sr, rnd, 260, 0.12), 0.9);
    return reverb(m.data, sr, 3.0, 0.3);
  },
  'ending-free': (sr, rnd) => {
    const m = new Mix(sr, 25);
    const road = [[62, 1], [65, 1], [63, 1], [62, 1], [69, 3], [67, 1], [65, 1], [63, 1], [62, 4]] as const;
    let t = 1;
    for (const [n, beats] of road) {
      m.add(t, piano(sr, hz(n), beats * 1.4 + 0.8, rnd, 0.65), 0.7);
      if (beats >= 3) m.add(t, clarinet(sr, hz(n + 12), beats * 1.4, rnd), 0.35);
      t += beats * 1.4;
    }
    m.add(0.0, organ(sr, hz(38), 24.5, rnd), 0.4);
    m.add(0.5, bowed(sr, hz(38), 24, rnd, { attack: 2, release: 4, vib: 0.003, bright: 1200 }), 0.4);
    [60, 64, 67, 72].forEach((n, i) => m.add(8 + i * 1.7, pluck(sr, hz(n), 3, rnd, 0.9975), 0.3));
    m.add(18, organ(sr, hz(50), 6.5, rnd), 0.45);
    m.add(18, organ(sr, hz(57), 6.5, rnd), 0.4);
    return reverb(m.data, sr, 2.2, 0.28);
  },
  'new-cycle': (sr, rnd) => {
    const m = new Mix(sr, 8);
    m.add(0.0, organ(sr, hz(26), 7.8, rnd), 0.8);
    m.add(0.2, wind(sr, 5, rnd, 150, 900), 0.12);
    m.add(2.6, bell(sr, hz(70), 5, rnd, 1.4), 0.5);            // B flat: a half step above the first time
    return reverb(m.data, sr, 2.4, 0.3);
  },
};

/** Renders one stinger as mono samples at the given sample rate, normalised and faded out. Same input, same output. */
export function renderStinger(name: StingerName, sampleRate = 44100): Float32Array {
  const score = scores[name];
  const seed = 1000 + stingerNames.indexOf(name) * 97;
  return finish(score(sampleRate, rng(seed)), sampleRate);
}

/** A 16-bit mono WAV file for the given samples. */
export function toWav(samples: Float32Array, sampleRate = 44100): Uint8Array {
  const bytes = new Uint8Array(44 + samples.length * 2);
  const dv = new DataView(bytes.buffer);
  const text = (o: number, s: string) => { for (let i = 0; i < s.length; i++) dv.setUint8(o + i, s.charCodeAt(i)); };
  text(0, 'RIFF'); dv.setUint32(4, 36 + samples.length * 2, true); text(8, 'WAVE');
  text(12, 'fmt '); dv.setUint32(16, 16, true); dv.setUint16(20, 1, true); dv.setUint16(22, 1, true);
  dv.setUint32(24, sampleRate, true); dv.setUint32(28, sampleRate * 2, true); dv.setUint16(32, 2, true); dv.setUint16(34, 16, true);
  text(36, 'data'); dv.setUint32(40, samples.length * 2, true);
  for (let i = 0; i < samples.length; i++) dv.setInt16(44 + i * 2, Math.max(-1, Math.min(1, samples[i]!)) * 32767, true);
  return bytes;
}
