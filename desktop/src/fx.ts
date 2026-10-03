import type { Frame } from './protocol';

// Spell animations. The engine resolves a cast at once and sends, with that frame, a short script of overlay cells
// (Frame.fx: one array per step, [cell, glyph, fg, bg] quadruples, bg -1 = keep). This plays it over the finished frame on a
// timer, between engine frames, without asking the engine for anything; a new frame from the engine cuts it short.

/** A copy of the frame with one step of the script drawn over it. Returns the frame itself for an empty or missing step. */
export function applyFx(frame: Frame, step: readonly number[] | undefined): Frame {
  if (!step || step.length === 0) return frame;
  const glyphs = frame.glyphs.slice(), fg = frame.fg.slice(), bg = frame.bg.slice(), bold = frame.bold.slice();
  for (let i = 0; i + 3 < step.length; i += 4) {
    const cell = step[i]!;
    if (cell < 0 || cell >= glyphs.length) continue;
    glyphs[cell] = step[i + 1]!;
    fg[cell] = step[i + 2]!;
    if (step[i + 3]! >= 0) bg[cell] = step[i + 3]!;
    bold[cell] = true;
  }
  return { ...frame, glyphs, fg, bg, bold };
}

export interface FxPlayer {
  /** Starts playing a frame's script (cancelling any running one). Returns false when there is nothing to play. */
  start(frame: Frame): boolean;
  /** Stops without drawing anything further. */
  stop(): void;
  readonly playing: boolean;
}

/** Plays scripts with a timer. `draw` paints a frame; `done` is called once when a script ends on its own (not when stopped). */
export function createFxPlayer(draw: (f: Frame) => void, done: () => void, schedule: (fn: () => void, ms: number) => unknown = (fn, ms) => setInterval(fn, ms), cancel: (h: unknown) => void = h => clearInterval(h as ReturnType<typeof setInterval>)): FxPlayer {
  let handle: unknown = null;
  let playing = false;
  const stop = () => { if (handle !== null) cancel(handle); handle = null; playing = false; };
  return {
    get playing() { return playing; },
    stop,
    start(frame: Frame) {
      stop();
      const script = frame.fx;
      if (!script || script.length === 0) return false;
      let i = 0;
      playing = true;
      const tick = () => {
        if (i >= script.length) { stop(); done(); return; }
        draw(applyFx(frame, script[i++]));
      };
      tick();
      handle = schedule(tick, Math.max(16, frame.fxMs ?? 45));
      return true;
    },
  };
}
