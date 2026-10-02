import type { Frame } from './protocol';
import { legend, t as tr, type Lang } from './i18n';

const logo = [
  ' ██████  ███████ ███████ ██    ██  █████  ██████  ██    ██',
  '██    ██ ██      ██      ██    ██ ██   ██ ██   ██  ██  ██ ',
  '██    ██ ███████ ███████ ██    ██ ███████ ██████    ████  ',
  '██    ██      ██      ██ ██    ██ ██   ██ ██   ██    ██   ',
  ' ██████  ███████ ███████  ██████  ██   ██ ██   ██    ██   ',
];
// Gold at the crown, ember at the base: the logo reads as lit from below.
const logoRamp = [0xFFE27A, 0xFBC53E, 0xF2A33D, 0xE7802B, 0xD65A26];

export const mix = (a: number, b: number, t: number) => {
  const k = Math.max(0, Math.min(1, t));
  const r = ((a >> 16) & 255) + ((((b >> 16) & 255) - ((a >> 16) & 255)) * k);
  const g = ((a >> 8) & 255) + ((((b >> 8) & 255) - ((a >> 8) & 255)) * k);
  const bl = (a & 255) + (((b & 255) - (a & 255)) * k);
  return (Math.round(r) << 16) | (Math.round(g) << 8) | Math.round(bl);
};

// Same integer hash family as the engine's Theme.Hash01: stable, no state, never shimmers.
export function hash01(x: number, y: number): number {
  let h = (Math.imul(x, 374761393) ^ Math.imul(y, 668265263) ^ 0x5bd1e995) >>> 0;
  h = Math.imul(h ^ (h >>> 13), 1274126177) >>> 0;
  h = (h ^ (h >>> 16)) >>> 0;
  return (h & 0xffffff) / 16777216;
}

// Smooth value noise in 2D, used for drifting fog.
export function noise(x: number, y: number): number {
  const xi = Math.floor(x), yi = Math.floor(y), xf = x - xi, yf = y - yi;
  const u = xf * xf * (3 - 2 * xf), v = yf * yf * (3 - 2 * yf);
  const a = hash01(xi, yi), b = hash01(xi + 1, yi), c = hash01(xi, yi + 1), d = hash01(xi + 1, yi + 1);
  return a + (b - a) * u + (c - a) * v + (a - b - c + d) * u * v;
}

/** Title scene. `tick` only drives the animation; nothing here touches the simulation. */
export function titleFrame(source: Frame, tick = 0): Frame {
  const { cols, rows } = source, n = cols * rows;
  const frame = { ...source,
    glyphs: Array<number>(n).fill(32),
    fg: Array<number>(n).fill(source.text),
    bg: Array<number>(n).fill(source.void),
    bold: Array<boolean>(n).fill(false),
  };
  const put = (x: number, y: number, ch: string, fg: number, bold = false, bg?: number) => {
    x = Math.round(x); y = Math.round(y);
    if (x < 0 || x >= cols || y < 0 || y >= rows) return;
    const i = y * cols + x;
    frame.glyphs[i] = ch.codePointAt(0)!; frame.fg[i] = fg; frame.bold[i] = bold;
    if (bg !== undefined) frame.bg[i] = bg;
  };
  const bgAt = (x: number, y: number) => frame.bg[y * cols + x];
  const tint = (x: number, y: number, color: number, amount: number) => {
    x = Math.round(x); y = Math.round(y);
    if (x < 0 || x >= cols || y < 0 || y >= rows) return;
    frame.bg[y * cols + x] = mix(bgAt(x, y), color, amount);
  };
  const write = (text: string, y: number, color: number, bold = false, bg?: number, x0?: number) => {
    const start = x0 ?? Math.floor((cols - text.length) / 2);
    for (let j = 0; j < text.length; j++) if (text[j] !== ' ' || bg !== undefined) put(start + j, y, text[j], color, bold, bg);
  };
  const t = tick;

  // Sky: void at the top, indigo haze lower down, a faint furnace glow at the horizon.
  const horizon = rows - 7;
  for (let y = 0; y < rows; y++) {
    const k = y / rows;
    const haze = mix(source.void, 0x1B1236, Math.pow(k, 1.6));
    const glow = y > horizon - 10 ? mix(haze, 0x3A1A0C, (y - (horizon - 10)) / 10 * 0.8) : haze;
    for (let x = 0; x < cols; x++) frame.bg[y * cols + x] = y >= horizon ? 0x07060B : glow;
  }

  // Moon, a slow crescent with a soft halo.
  const mx = Math.floor(cols * 0.8), my = Math.max(4, Math.floor(rows * 0.17)), mr = 5;
  for (let y = my - 9; y <= my + 9; y++) for (let x = mx - 24; x <= mx + 24; x++) {
    const dx = (x - mx) / 2, dy = y - my, dist = Math.hypot(dx, dy);
    if (dist < 9 && y < horizon - 4) tint(x, y, 0x5A5A9A, 0.20 * Math.pow(1 - dist / 9, 2));
  }
  const drift = Math.sin(t * 0.01) * 1.2;           // the shadow disc creeps: a slow phase
  for (let y = my - mr; y <= my + mr; y++) for (let x = mx - mr * 2; x <= mx + mr * 2; x++) {
    const dx = (x - mx) / 2, dy = y - my;
    if (dx * dx + dy * dy > mr * mr) continue;
    const sx = (x - (mx + 3 + drift * 2)) / 2, sy = y - (my - 1);
    if (sx * sx + sy * sy < (mr + 0.6) * (mr + 0.6)) continue;
    const edge = dx * dx + dy * dy > (mr - 1.2) * (mr - 1.2);
    put(x, y, edge ? '▒' : '█', edge ? 0xB8B4D8 : 0xE9E4CB);
  }

  // Stars twinkle on their own phase; a few go dark for a moment.
  for (let y = 0; y < horizon - 6; y++) for (let x = 0; x < cols; x++) {
    const h = hash01(x + 91, y + 13);
    if (h <= 0.985 || frame.glyphs[y * cols + x] !== 32) continue;
    const phase = 0.5 + 0.5 * Math.sin(t * (0.08 + h * 0.4) + h * 400);
    if (phase < 0.18) continue;
    put(x, y, h > 0.995 && phase > 0.7 ? '+' : h > 0.995 ? '•' : '·', mix(0x2A2745, 0xB4B0E8, (h - 0.985) / 0.015 * phase));
  }

  // Shooting star, every ~14 s: a head and a fading trail along a shallow diagonal.
  const cycle = t % 170;
  if (cycle < 14) {
    const sx = cols * 0.12 + (Math.floor(t / 170) % 5) * cols * 0.1, sy = 3 + (Math.floor(t / 170) % 3) * 2;
    for (let k = 0; k < 7; k++) {
      const age = cycle - k * 0.6;
      if (age < 0) continue;
      put(sx + age * 3, sy + age * 0.9, k === 0 ? '*' : k < 3 ? '·' : '.', mix(0xFFFFFF, 0x3A3A6A, k / 7), k === 0);
    }
  }

  // Bats, wings alternating, crossing the sky on a sine path.
  for (let b = 0; b < 3; b++) {
    const x = ((t * (0.5 + b * 0.18) + b * 47) % (cols + 24)) - 12;
    const y = Math.max(3, horizon - 14 - b * 3) + Math.sin(t * 0.09 + b * 2) * 2;
    put(x, y, (Math.floor(t / 2) + b) % 2 === 0 ? 'v' : '^', 0x4E4478, false);
    put(x - 1, y, (Math.floor(t / 2) + b) % 2 === 0 ? '\\' : '/', 0x4E4478);
    put(x + 1, y, (Math.floor(t / 2) + b) % 2 === 0 ? '/' : '\\', 0x4E4478);
  }

  // ---- Landscape, back to front: mountains, a ruined keep, a graveyard, ground and mist.
  const STONE = 0x0B0916;

  // Far mountains: a jagged ridge in two octaves, filled down to the ground.
  const ridge = (x: number) => horizon - 4 - Math.round(noise(x * 0.045 + 3, 1) * 7 + noise(x * 0.13 + 9, 2) * 3);
  for (let x = 0; x < cols; x++) {
    const top = ridge(x);
    for (let y = top; y < horizon; y++) put(x, y, y === top ? '▄' : y < top + 3 ? '▓' : '█', y === top ? 0x4A3F80 : y < top + 3 ? 0x2A2252 : 0x1E1840);
    // a little snow on the highest crests
    if (top < horizon - 9) { put(x, top, '▄', 0xB4AEE0); put(x, top + 1, '▓', 0x6A62A0); }
  }

  // The keep: tall tower, hall with battlements, a broken tower, a lit gate. h = available height.
  const drawKeep = (x0: number, h: number) => {
    const solid = (x: number, y: number) => put(x, y, '█', STONE);
    const window_ = (x: number, y: number) => {
      const f = hash01(x * 7, y + Math.floor(t / 7));
      put(x, y, '█', mix(0xB8641E, 0xFFC25A, f), true);
      tint(x - 1, y, 0x6A3A18, 0.25 * f); tint(x + 1, y, 0x6A3A18, 0.25 * f);
    };
    const hallW = 18, towerW = 7, hallH = Math.max(4, Math.round(h * 0.5)), brokenH = Math.max(4, Math.round(h * 0.75));
    // tall tower (left) with battlements and a pointed cap
    for (let k = 0; k < h; k++) for (let x = x0; x < x0 + towerW; x++) solid(x, horizon - 1 - k);
    for (let x = x0 - 1; x <= x0 + towerW; x++) if ((x - x0) % 2 === 0) solid(x, horizon - 1 - h);
    put(x0 + 3, horizon - 2 - h, '▲', STONE);
    // hall
    const hx = x0 + towerW;
    for (let k = 0; k < hallH; k++) for (let x = hx; x < hx + hallW; x++) solid(x, horizon - 1 - k);
    for (let x = hx; x < hx + hallW; x++) if ((x - hx) % 2 === 0) solid(x, horizon - 1 - hallH);
    // broken tower (right): the top is chewed away
    const bx = hx + hallW;
    for (let x = bx; x < bx + 6; x++) {
      const height = brokenH - Math.floor(hash01(x, 31) * 4);
      for (let k = 0; k < height; k++) solid(x, horizon - 1 - k);
    }
    // arched windows, each its own flame
    window_(x0 + 3, horizon - 1 - Math.round(h * 0.7));
    window_(x0 + 3, horizon - 1 - Math.round(h * 0.7) + 1);
    window_(x0 + 3, horizon - 1 - Math.round(h * 0.35));
    for (const off of [3, 8, 13]) { window_(hx + off, horizon - hallH); window_(hx + off, horizon - hallH + 1); }
    window_(bx + 2, horizon - 1 - Math.round(brokenH * 0.5));
    // the gate: a dark arch with a torch either side
    const gx = hx + Math.floor(hallW / 2);
    for (let k = 0; k < 3; k++) for (let x = gx - 1; x <= gx + 1; x++) put(x, horizon - 1 - k, ' ', 0x000000, false, 0x1A0C06);
    put(gx, horizon - 4, '∩', 0x3A2A22);
    for (const tx of [gx - 3, gx + 3]) {
      const f = hash01(tx, Math.floor(t / 2));
      put(tx, horizon - 2, '|', 0x4A3A30);
      put(tx, horizon - 3, f > 0.5 ? '♦' : '•', mix(0xFF7A2A, 0xFFD070, f), true);
      for (let dy = -3; dy <= 0; dy++) for (let dx = -3; dx <= 3; dx++)
        tint(tx + dx, horizon - 3 + dy, 0x6A3A18, 0.3 * f * Math.max(0, 1 - Math.hypot(dx / 1.5, dy) / 3));
    }
  };

  // The graveyard: crosses, headstones and one dead tree, with a low fence.
  const drawGraves = (x0: number, w: number) => {
    const treeX = x0 + Math.floor(w * 0.55);
    for (let k = 0; k < 7; k++) put(treeX, horizon - 1 - k, '│', STONE);
    for (let k = 1; k <= 4; k++) {
      put(treeX - k, horizon - 4 - k, '\\', STONE); put(treeX + k, horizon - 5 - k, '/', STONE);
      if (k === 2) { put(treeX - 4, horizon - 6, '\\', STONE); put(treeX + 4, horizon - 7, '/', STONE); }
    }
    for (let i = 0; i < Math.floor(w / 3); i++) {
      const gx = x0 + Math.floor(hash01(i, 11) * w);
      if (Math.abs(gx - treeX) < 2) continue;
      const kind = hash01(i, 12);
      if (kind < 0.45) { put(gx, horizon - 3, '│', STONE); put(gx, horizon - 2, '┼', STONE); put(gx, horizon - 1, '│', STONE); }
      else if (kind < 0.8) { put(gx, horizon - 2, '▄', STONE); put(gx, horizon - 1, '█', STONE); put(gx + 1, horizon - 1, '█', STONE); put(gx + 1, horizon - 2, '▄', STONE); }
      else { put(gx, horizon - 2, '∩', STONE); put(gx, horizon - 1, '█', STONE); }
    }
    for (let x = x0; x < x0 + w; x++) if (x % 2 === 0) put(x, horizon - 1, x % 4 === 0 ? '┬' : '┴', 0x1A1630);
  };

  const keepH = Math.max(0, Math.min(14, horizon - (Math.max(5, Math.floor(rows * 0.22)) + 14)));
  if (keepH >= 6) drawKeep(Math.floor(cols * 0.07), keepH);
  if (cols >= 110) drawGraves(Math.floor(cols * 0.70), Math.floor(cols * 0.24));

  // Ground: dark earth under a grass line, with tufts and pebbles.
  for (let x = 0; x < cols; x++) put(x, horizon, '▀', 0x16241A, false, 0x07060B);
  for (let y = horizon + 1; y < rows; y++) for (let x = 0; x < cols; x++) {
    const h = hash01(x, y + 200);
    if (h > 0.90) put(x, y, h > 0.97 ? '▒' : h > 0.94 ? ',' : '·', y === horizon + 1 ? 0x1E3324 : 0x15112A);
  }

  // Ground mist, thin and low, so it reads as a vapour hugging the soil.
  for (let y = horizon - 2; y <= horizon; y++) for (let x = 0; x < cols; x++) {
    const v = noise(x * 0.07 + t * 0.03, y * 1.1) * 0.65 + noise(x * 0.2 - t * 0.05, y * 2.3) * 0.35;
    if (v < 0.58 || frame.glyphs[y * cols + x] !== 32) continue;
    put(x, y, v > 0.7 ? '▒' : '░', mix(0x2A2448, 0x7A6AA8, (v - 0.58) * 3));
  }

  // Embers drifting up from the horizon on a gentle wind, flaring then dying.
  const embers = Math.max(22, Math.floor(cols / 3));
  for (let k = 0; k < embers; k++) {
    const speed = 0.35 + hash01(k, 3) * 0.9, phase = hash01(k, 5) * rows * 2;
    const life = ((t * speed * 0.12 + phase) % (rows * 0.9));
    const y = horizon - 1 - Math.floor(life);
    const x = Math.floor(hash01(k, 1) * cols + Math.sin(life * 0.5 + k) * 2 + life * 0.15 * Math.sin(t * 0.01 + k));
    const age = life / (rows * 0.9);
    const flare = hash01(k, Math.floor(t / 3)) > 0.9 && age < 0.6;
    const glyph = flare ? '*' : age < 0.35 ? '•' : age < 0.7 ? '·' : '.';
    put(x, y, glyph, mix(flare ? 0xFFE9A0 : 0xFFB04A, 0x4A2418, age), age < 0.4);
    if (age < 0.3) tint(x, y, 0x6A3A18, 0.25 * (1 - age / 0.3));
  }

  // Wordmark: drop shadow, ramp, and a highlight that sweeps across every few seconds.
  const top = Math.max(5, Math.floor(rows * 0.22));
  const lx = Math.floor((cols - logo[0].length) / 2);
  const breath = 0.5 + 0.5 * Math.sin(t * 0.07);
  for (let dy = -3; dy <= logo.length + 2; dy++) for (let dx = -8; dx < logo[0].length + 8; dx++)
    tint(lx + dx, top + dy, 0x5A3010, 0.10 * (0.6 + 0.4 * breath) * Math.max(0, 1 - Math.hypot(dx - logo[0].length / 2, (dy - 2) * 3) / (logo[0].length * 0.7)));
  logo.forEach((row, i) => {
    for (let j = 0; j < row.length; j++) if (row[j] !== ' ') put(lx + j + 1, top + i + 1, '▒', 0x4A2A12);
  });
  const sweepCycle = 150, sweep = (t % sweepCycle) / sweepCycle * (logo[0].length + 40) - 20;
  logo.forEach((row, i) => {
    for (let j = 0; j < row.length; j++) {
      if (row[j] === ' ') continue;
      const d = Math.abs(j + i * 1.5 - sweep);
      put(lx + j, top + i, row[j], d < 5 ? mix(0xFFFFFF, logoRamp[i], d / 5) : logoRamp[i], true);
    }
  });
  const lang = (source.lang as Lang) ?? 'pt';
  write('♦  ' + tr(lang, 'subtitle') + '  ♦', top - 3, mix(source.dim, 0xB8B0D8, breath));
  write('─────────  ◆  ─────────', top + 7, mix(source.rule, 0x8A6F30, 0.8));
  write(tr(lang, 'motto'), top + 9, 0xEFE6D2, true);
  write(tr(lang, 'tagline'), top + 11, source.dim);

  // Key legend as keycaps.
  const keys = legend[lang];
  const width = keys.reduce((sum, [k, l]) => sum + k.length + 2 + l.length + 3, -2);
  let x = Math.floor((cols - width) / 2);
  const y = rows - 3;
  for (const [key, label] of keys) {
    write(` ${key} `, y, source.title, true, source.rule, x); x += key.length + 3;
    write(label, y, source.text, false, undefined, x); x += label.length + 2;
  }
  return frame;
}

/** Draws the engine's menu panels (non-empty cells of `menu`) over the title scene. */
export function overlayMenu(scene: Frame, menu: Frame): Frame {
  const out = { ...scene, glyphs: scene.glyphs.slice(), fg: scene.fg.slice(), bg: scene.bg.slice(), bold: scene.bold.slice() };
  for (let i = 0; i < out.glyphs.length && i < menu.glyphs.length; i++) {
    if (menu.glyphs[i] === 32 && menu.bg[i] === menu.void) continue;
    out.glyphs[i] = menu.glyphs[i]; out.fg[i] = menu.fg[i]; out.bg[i] = menu.bg[i]; out.bold[i] = menu.bold[i];
  }
  return out;
}
