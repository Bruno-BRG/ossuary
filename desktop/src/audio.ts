// Sound effects: short square-wave bleeps, in the spirit of an 80s PC speaker. The engine only names what happened
// (see Game.DrainCues); this file decides what each name sounds like and how loud it is.

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
};

/** Output level, 0..~0.15: master and effects volumes are 0..10, and the bleeps are kept polite. */
export function level(master: number, effects: number): number {
  const m = Math.max(0, Math.min(10, master)), e = Math.max(0, Math.min(10, effects));
  return (m / 10) * (e / 10) * 0.12;
}

export function cuesToPlay(cues: readonly string[] | undefined): Pattern[] {
  const out: Pattern[] = [];
  for (const cue of cues ?? []) { const p = patterns[cue]; if (p) out.push(p); }
  return out;
}

let context: AudioContext | undefined;

/** Browsers only start audio after a gesture: call this from a key press. */
export function unlockAudio(): void {
  try {
    const Ctor = window.AudioContext ?? (window as unknown as { webkitAudioContext?: typeof AudioContext }).webkitAudioContext;
    if (!Ctor) return;
    context ??= new Ctor();
    if (context.state === 'suspended') void context.resume();
  } catch { /* No audio is a fine way to play. */ }
}

/** Plays the cues one after another, quietly, never throwing: sound must not be able to break the game. */
export function playCues(cues: readonly string[] | undefined, master: number, effects: number): void {
  const patternsToPlay = cuesToPlay(cues);
  const gain = level(master, effects);
  if (!context || context.state !== 'running' || gain <= 0 || patternsToPlay.length === 0) return;
  try {
    const ctx = context;
    let at = ctx.currentTime + 0.005;
    for (const pattern of patternsToPlay) {
      for (const [freq, seconds] of pattern) {
        if (freq > 0) {
          const osc = ctx.createOscillator();
          const amp = ctx.createGain();
          osc.type = 'square';
          osc.frequency.setValueAtTime(freq, at);
          amp.gain.setValueAtTime(gain, at);
          amp.gain.exponentialRampToValueAtTime(0.0001, at + seconds);
          osc.connect(amp).connect(ctx.destination);
          osc.start(at);
          osc.stop(at + seconds + 0.01);
        }
        at += seconds;
      }
      at += 0.02;
    }
  } catch { /* Ignore audio errors. */ }
}
