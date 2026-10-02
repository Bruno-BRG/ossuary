import type { Frame } from './protocol';
import { t, type Key, type Lang } from './i18n';
import { hash01, mix, noise } from './title';

/** Characters typed per animation tick (80 ms). */
export const introSpeed = 2;
/** Ticks a finished page is held before the story moves on by itself. */
export const introHold = 56;

/** A page is its heading (first line) followed by its text. */
export const headingOf = (page: string[]): string => page[0] ?? '';
export const bodyOf = (page: string[]): string[] => page.slice(1);
/** Total characters of a text block, so the caller knows when the typewriter has finished. */
export function pageLength(lines: string[]): number { return lines.reduce((n, l) => n + l.length + 1, 0); }

const SKY = 0x14102A;

interface Scene {
  cols: number; rows: number;
  x0: number; x1: number;      // horizontal extent of the picture
  top: number; bot: number;    // vertical extent
  ground: number;              // the surface row
  cx: number;                  // the pit's axis
  age: number; tick: number;
  lang: Lang;
  put(x: number, y: number, ch: string, fg: number, bold?: boolean, bg?: number): void;
  tint(x: number, y: number, color: number, amount: number): void;
  text(x: number, y: number, s: string, fg: number, bold?: boolean): void;
}

/**
 * Opening story: seven pages, each an era, each drawn as a cross-section of the world from the sky down
 * through the Ossuary: the pit, the kings' floors, the sunken layers, Yendor's seal, the Spire's fire, the
 * present, and the road you arrive on. Client-only; `age` is how long the page has been showing and drives
 * its animation, `typed` the typewriter. The engine owns the words.
 */
export function introFrame(source: Frame, pages: string[][], page: number, typed: number, tick: number, lang: Lang, age: number): Frame {
  const { cols, rows } = source, n = cols * rows;
  const frame = { ...source,
    glyphs: Array<number>(n).fill(32), fg: Array<number>(n).fill(source.text),
    bg: Array<number>(n).fill(source.void), bold: Array<boolean>(n).fill(false) };
  const inside = (x: number, y: number) => x >= 0 && x < cols && y >= 0 && y < rows;
  const put: Scene['put'] = (x, y, ch, fg, bold = false, bg) => {
    x = Math.round(x); y = Math.round(y);
    if (!inside(x, y)) return;
    const i = y * cols + x;
    frame.glyphs[i] = ch.codePointAt(0)!; frame.fg[i] = fg; frame.bold[i] = bold;
    if (bg !== undefined) frame.bg[i] = bg;
  };
  const tint: Scene['tint'] = (x, y, color, amount) => {
    x = Math.round(x); y = Math.round(y);
    if (!inside(x, y)) return;
    const i = y * cols + x; frame.bg[i] = mix(frame.bg[i], color, amount);
  };
  const text: Scene['text'] = (x, y, s, fg, bold = false) => { for (let j = 0; j < s.length; j++) put(x + j, y, s[j]!, fg, bold); };

  const last = pages.length - 1;
  const textTop = Math.floor(rows * 0.6);
  const top = 4, bot = Math.max(top + 8, textTop - 2);
  const ground = top + Math.max(4, Math.floor((bot - top) * 0.4));
  const scene: Scene = { cols, rows, x0: 2, x1: cols - 3, top, bot, ground, cx: Math.floor(cols / 2), age, tick, lang, put, tint, text };

  // Sky and haze behind everything; each scene repaints what it needs.
  for (let y = 0; y < rows; y++) for (let x = 0; x < cols; x++)
    frame.bg[y * cols + x] = mix(source.void, SKY, 0.35 * noise(x * 0.05 + tick * 0.008, y * 0.25 + 3) * (1 - y / rows));

  const draw = scenes[Math.min(page, scenes.length - 1)]!;
  draw(scene);

  // Timeline across the top, the era's heading under it.
  const span = Math.min(cols - 8, pages.length * 8);
  const tx = Math.floor((cols - span) / 2);
  for (let x = 0; x < span; x++) put(tx + x, 1, '─', mix(source.rule, source.dim, x / span < (page + 0.5) / pages.length ? 0.9 : 0.1));
  pages.forEach((_, i) => {
    const x = tx + Math.round((i + 0.5) * span / pages.length);
    put(x, 1, i === page ? '◆' : i < page ? '◇' : '·', i === page ? source.title : i < page ? source.text : source.dim, i === page);
  });
  const heading = headingOf(pages[page] ?? []);
  text(Math.floor((cols - heading.length) / 2), 2, heading, source.title, true);

  // Text, typed out, centred, with a soft cursor.
  const lines = bodyOf(pages[page] ?? []);
  let remaining = typed, y = textTop;
  const finalLine = page === last ? lines.length - 1 : -1;
  lines.forEach((line, li) => {
    const before = remaining, shown = Math.max(0, Math.min(line.length, remaining));
    remaining -= line.length + 1;
    const x0 = Math.floor((cols - line.length) / 2);
    const gold = li === finalLine || (page === last && li === finalLine - 2);
    for (let j = 0; j < shown; j++) put(x0 + j, y, line[j]!, gold ? 0xFBC53E : 0xEFE6D2, gold);
    if (before >= 0 && before <= line.length && shown < line.length + 1 && typed < pageLength(lines) && Math.floor(tick / 5) % 2 === 0) put(x0 + shown, y, '▌', 0xFBC53E);
    y++;
  });

  const hint = t(lang, page === last ? 'introBegin' : 'introHint');
  const hx = Math.floor((cols - hint.length) / 2);
  const pulse = 0.55 + 0.45 * Math.sin(tick * 0.12);
  for (let j = 0; j < hint.length; j++) put(hx + j, rows - 2, hint[j]!, mix(source.dim, source.title, pulse));
  return frame;
}

// ------------------------------------------------------------------ pieces

const flicker = (tick: number, k: number) => 0.5 + 0.5 * Math.sin(tick * 0.35 + k * 1.7 + hash01(k, 3) * 6);
const ease = (v: number) => Math.max(0, Math.min(1, v));

function sky(s: Scene, from: number, to: number) {
  for (let y = s.top - 1; y < s.ground; y++) for (let x = 0; x < s.cols; x++)
    s.tint(x, y, mix(from, to, (y - s.top) / Math.max(1, s.ground - s.top)), 1);
}

function stars(s: Scene, amount: number) {
  for (let k = 0; k < 40; k++) {
    const x = hash01(k, 11) * s.cols, y = s.top + hash01(k, 12) * (s.ground - s.top - 1);
    if (hash01(k, 13) > amount) continue;
    s.put(x, y, hash01(k, 14) > 0.7 ? '•' : '·', mix(0x5A5A7A, 0xE8E0FF, flicker(s.tick, k)));
  }
}

/** The ground: grass on top, strata below, shaded by depth. `tone` tints the rock. Low contrast on purpose: it is a backdrop. */
function earth(s: Scene, tone = 0x3A2C22) {
  for (let y = s.ground; y <= s.bot; y++) for (let x = s.x0; x <= s.x1; x++) {
    const d = (y - s.ground) / Math.max(1, s.bot - s.ground);
    if (y === s.ground) { s.put(x, y, hash01(x, 2) > 0.6 ? ',' : '"', 0x58B04A, false, 0x10240E); continue; }
    const strata = noise(x * 0.07, y * 0.9 + 3), h = hash01(x, y + 40);
    const bg = mix(mix(tone, 0x000000, 0.7), 0x050408, d * 0.8);
    s.put(x, y, h > 0.93 ? '▪' : h > 0.8 ? '·' : ' ', mix(tone, 0x120E1A, d * 0.6), false, mix(bg, tone, 0.18 * strata * (1 - d)));
  }
}

/** A shaft with a ragged edge, dark inside. `reach` is how many rows deep it goes, `half` half its width. */
function pit(s: Scene, reach: number, half: number, glow = 0) {
  for (let r = 1; r <= reach; r++) {
    const y = s.ground + r;
    const w = half - Math.floor(r * 0.12) + Math.floor(hash01(r, 7) * 2);
    for (let x = s.cx - w; x <= s.cx + w; x++) {
      const edge = x === s.cx - w || x === s.cx + w;
      s.put(x, y, edge ? '▐' : ' ', edge ? 0x5A4A6A : 0, false, edge ? 0x0A0810 : mix(0x020204, 0x40206A, glow * (0.4 + 0.6 * hash01(x, y))));
      if (x === s.cx + w) s.put(x, y, '▌', 0x5A4A6A, false, 0x0A0810);
    }
  }
}

function house(s: Scene, x: number, lit = 0x0F0D0A, window = 0xF2C060) {
  s.put(x + 1, s.ground - 2, '◢', 0x7A4A3A); s.put(x + 2, s.ground - 2, '◣', 0x7A4A3A);
  s.put(x, s.ground - 1, '█', 0x6A5A4A); s.put(x + 1, s.ground - 1, '■', window, false, lit); s.put(x + 2, s.ground - 1, '■', window, false, lit); s.put(x + 3, s.ground - 1, '█', 0x6A5A4A);
}

function castle(s: Scene, x: number, w: number, height: number, flag = true) {
  for (let r = 1; r <= height; r++) for (let j = 0; j < w; j++) {
    const edge = j === 0 || j === w - 1;
    s.put(x + j, s.ground - r, r === height ? (j % 2 === 0 ? '▀' : ' ') : r % 3 === 0 && !edge && j % 3 === 1 ? '■' : '█', r % 3 === 0 && !edge && j % 3 === 1 ? 0xF2C060 : 0x7A7490);
  }
  if (flag) s.put(x + Math.floor(w / 2), s.ground - height - 1, flicker(s.tick, x) > 0.5 ? '►' : '▶', 0xD04A4A, true);
}

function walker(s: Scene, x: number, y: number, ch = '☺', color = 0xE8D8B8) { s.put(x, y, ch, color, true); }

// ------------------------------------------------------------------- scenes

const scenes: Array<(s: Scene) => void> = [
  // I. The pit: villages bring their dead to a hole in the ground.
  (s) => {
    sky(s, 0x4A3050, 0x1A1430);
    s.put(s.x0 + 12 + ease(s.age / 120) * 14, s.ground - 1 - ease(s.age / 120) * (s.ground - s.top - 3), '●', 0xFBD94A, true);
    earth(s);
    const half = Math.max(5, Math.floor(s.cols * 0.07)), reach = 5;
    pit(s, reach, half);
    house(s, s.x0 + 4); house(s, s.x0 + 10); house(s, s.x1 - 14); house(s, s.x1 - 8);
    const gap = s.cx - half - s.x0 - 18;
    for (let k = 0; k < 4; k++) {
      const p = ((s.age * 0.16 + k * 7) % Math.max(10, gap)) / Math.max(10, gap);
      walker(s, s.x0 + 16 + p * gap, s.ground - 1);
      s.put(s.x0 + 16 + p * gap, s.ground - 2, '=', 0xC8B890);
      walker(s, s.x1 - 16 - p * gap, s.ground - 1);
      s.put(s.x1 - 16 - p * gap, s.ground - 2, '=', 0xC8B890);
    }
    for (let k = 0; k < 5; k++) {   // the dead, falling in
      const fall = (s.age * 0.35 + k * 3.1) % reach;
      s.put(s.cx - half + 2 + hash01(k, 1) * (half * 2 - 3), s.ground + 1 + fall, '†', 0xD8D0E8);
    }
    const heap = Math.floor(ease(s.age / 90) * half * 2 * 0.9);
    for (let j = 0; j < heap; j++) s.put(s.cx - half + 1 + (j % (half * 2 - 1)), s.ground + reach - Math.floor(j / (half * 2 - 1)), hash01(j, 4) > 0.5 ? '·' : '†', 0xB8B0C8);
  },
  // II. The kings and the archives dig floors under the pit, one after another.
  (s) => {
    sky(s, 0x3A3A6A, 0x1A1838);
    earth(s);
    castle(s, s.x0 + 3, 11, 4); castle(s, s.x1 - 14, 9, 3, false);
    const half = Math.max(7, Math.floor(s.cols * 0.1)), floors = Math.min(Math.floor((s.bot - s.ground - 1) / 2), 1 + Math.floor(s.age / 9));
    pit(s, s.bot - s.ground - 1, half);
    for (let i = 0; i < floors; i++) {
      const y = s.ground + 2 + i * 2, fresh = ease(1 - (s.age - i * 9) / 8);
      for (let x = s.cx - half + 1; x < s.cx + half; x++) s.put(x, y, '═', mix(0x8A7A6A, 0xFFE27A, fresh), false, 0x120E1A);
      const items = ['♦', '■', '☺', '■', '♦'];
      for (let j = 0; j < 4; j++) s.put(s.cx - half + 2 + j * Math.floor((half * 2 - 3) / 3), y - 1, items[(i + j) % items.length]!, j % 2 ? 0xF2C060 : 0xB8A8D0, j % 2 === 1);
    }
    if (floors < 6) {    // a digger on the newest floor
      const y = s.ground + 1 + floors * 2;
      walker(s, s.cx - half + 3 + (s.age % 10) * 0.9, y, '☺', 0xE8C060); s.put(s.cx - half + 4 + (s.age % 10) * 0.9, y, flicker(s.tick, 3) > 0.5 ? '/' : '|', 0xB0B0C0);
    }
  },
  // III. The floors pile up until they are geology: five layers, five ages.
  (s) => {
    sky(s, 0x2A2A52, 0x16122A);
    earth(s, 0x2E2438);
    const rowsDeep = s.bot - s.ground, band = Math.max(2, Math.floor(rowsDeep / 5));
    const names: Key[] = ['bDungeons', 'bMines', 'bWarrens', 'bVaults', 'bSpire'];
    const tones = [0x6A6A8A, 0x8A6A40, 0x7A5A3A, 0x2A5A9A, 0xB04A28];
    const shown = Math.min(5, 1 + Math.floor(s.age / 12));
    for (let b = 0; b < shown; b++) {
      const y0 = s.ground + 1 + b * band, y1 = Math.min(s.bot, y0 + band - 1);
      for (let y = y0; y <= y1; y++) for (let x = s.x0; x <= s.x1; x++) s.tint(x, y, tones[b]!, 0.28 + 0.25 * ease((s.age - b * 12) / 10));
      const label = t(s.lang, names[b]!);
      s.text(s.x1 - label.length - 1, y0 + Math.floor((band - 1) / 2), label, mix(tones[b]!, 0xFFFFFF, 0.55), true);
    }
    pit(s, rowsDeep - 1, 3);
    for (let k = 0; k < 24; k++) {
      const y = s.ground + 1 + hash01(k, 21) * (rowsDeep - 1), x = s.x0 + 2 + hash01(k, 22) * (s.cx - s.x0 - 8);
      if (y <= s.ground + 1 + (shown * band)) s.put(x, y, hash01(k, 23) > 0.5 ? '†' : '·', 0xA8A0C0);
    }
  },
  // IV. Yendor goes down with the seal; the black water rises behind him; the dead do not stay buried.
  (s) => {
    sky(s, 0x20203E, 0x120E22);
    earth(s, 0x2E2438);
    const rowsDeep = s.bot - s.ground;
    pit(s, rowsDeep - 1, 3, 0.2);
    const p = ease(s.age / 70), y = s.ground + 1 + p * (rowsDeep - 2);
    const x = s.cx + Math.round(Math.sin(p * 9) * 1.5);
    for (let r = s.ground + 1; r < y; r += 1) s.put(s.cx + Math.round(Math.sin(((r - s.ground) / (rowsDeep - 2)) * 9) * 1.5), r, '·', 0xB89A3A);
    s.put(x, y, '@', 0xFBC53E, true);
    s.put(x + 1, y, '◊', mix(0xFFE27A, 0xFFFFFF, flicker(s.tick, 1)), true);
    if (p >= 1) { const r = Math.floor((s.age - 70) / 3) % 4; s.put(x - 2 - r, y, '*', 0xFFE27A); s.put(x + 3 + r, y, '*', 0xFFE27A); }
    const water = ease((s.age - 78) / 90) * (rowsDeep - 2);   // black water climbing the shaft
    for (let r = 0; r < water; r++) {
      const wy = s.bot - r;
      for (let wx = s.cx - 3; wx <= s.cx + 3; wx++) s.put(wx, wy, flicker(s.tick, wx + wy) > 0.5 ? '≈' : '~', 0x3A6AB0, false, 0x061428);
    }
    for (let k = 0; k < 6 && water > 3; k++) s.put(s.cx - 2 + hash01(k, 31) * 4, s.bot - ((s.age * 0.12 + k * 2) % Math.max(2, water)), '☻', 0xC8C0D8);
  },
  // V. The Spire burns what climbs out; then the living; the kingdoms fall.
  (s) => {
    sky(s, 0x6A1A12, 0x1C0A0C);
    for (let x = 0; x < s.cols; x++) s.tint(x, s.ground - 1, 0xC8481C, 0.35 * flicker(s.tick, x));
    earth(s, 0x3A241C);
    castle(s, s.x0 + 3, 11, 4); castle(s, s.x1 - 14, 9, 3, false);
    for (const cx of [s.x0 + 3, s.x1 - 14]) for (let j = 0; j < 9; j++) s.put(cx + j, s.ground - 5 - Math.floor(hash01(j, cx) * 2), flicker(s.tick, cx + j) > 0.4 ? '▲' : '^', mix(0xFF6020, 0xFFC040, hash01(j, 9)), true);
    const rowsDeep = s.bot - s.ground;
    pit(s, rowsDeep - 1, 4, 0.5);
    for (let r = 0; r < Math.min(rowsDeep - 2, 4 + Math.floor(s.age / 6)); r++) {   // the Spire: upside down, hanging from the rim
      const w = Math.max(0, 3 - Math.floor(r / 2));
      for (let j = -w; j <= w; j++) s.put(s.cx + j, s.ground + 1 + r, r === rowsDeep - 3 ? '▼' : '▓', mix(0x8A2A1A, 0xE0602A, flicker(s.tick, r + j)), false, 0x2A0A08);
    }
    for (let k = 0; k < 26; k++) {
      const life = (s.age * 0.5 + k * 3.3) % 16, ex = s.cx + Math.sin(k * 2.1 + life * 0.3) * (2 + life * 0.5), ey = s.ground - life * 0.45;
      if (ey >= s.top) s.put(ex, ey, hash01(k, 41) > 0.5 ? '*' : '·', mix(0xFFC040, 0xB84A18, life / 16));
    }
  },
  // VI. Today: walled villages, a bounty on the board, a pit that smokes.
  (s) => {
    sky(s, 0x2A2050, 0x6A3A4A);
    stars(s, 0.5);
    earth(s);
    const half = Math.max(5, Math.floor(s.cols * 0.06)), rowsDeep = Math.min(7, s.bot - s.ground - 1);
    pit(s, rowsDeep, half, 0.35);
    for (let k = 0; k < 5; k++) s.put(s.cx - half + 2 + hash01(k, 51) * (half * 2 - 3), s.ground - 1 - ((s.age * 0.18 + k * 2.2) % 7), '░', mix(0x6A5A8A, 0x20182E, ((s.age * 0.18 + k * 2.2) % 7) / 7));
    for (const x0 of [s.x0 + 2, s.x1 - 22]) {    // two walled villages
      for (let j = 0; j < 20; j++) s.put(x0 + j, s.ground - 1, j % 5 === 2 ? '▀' : '█', 0x5A5470);
      house(s, x0 + 3, 0x0F0D0A, mix(0x806030, 0xF2C060, flicker(s.tick, x0))); house(s, x0 + 9, 0x0F0D0A, mix(0x806030, 0xF2C060, flicker(s.tick, x0 + 5)));
      castle(s, x0 + 14, 5, 3, false);
    }
    // The bounty board, between the left village and the pit.
    const bx = s.x0 + 25;
    if (bx + 12 < s.cx - half) {
      s.put(bx, s.ground - 4, '┌', 0xB09060); s.put(bx + 11, s.ground - 4, '┐', 0xB09060);
      s.text(bx + 1, s.ground - 4, '──────────', 0xB09060);
      s.text(bx, s.ground - 3, '│ 30 ◆    │', 0xF2D77A, true); s.text(bx, s.ground - 2, '│ ★ 5000 ◆│', mix(0xB09060, 0xFFE27A, flicker(s.tick, 2)), true);
      s.put(bx, s.ground - 1, '└', 0xB09060); s.put(bx + 11, s.ground - 1, '┘', 0xB09060); s.text(bx + 1, s.ground - 1, '──────────', 0xB09060);
    }
  },
  // VII. You: one more, on the road, walking toward the rim. Everyone before you is buried by it.
  (s) => {
    sky(s, 0x120E26, 0x2E1A3A);
    stars(s, 0.8);
    earth(s, 0x2E2438);
    const half = Math.max(5, Math.floor(s.cols * 0.06)), rowsDeep = s.bot - s.ground - 1;
    const glow = 0.15 + 0.2 * ease(s.age / 80);
    pit(s, rowsDeep, half, glow);
    for (let k = 0; k < 28; k++) {    // the ones who tried before you
      const x = k % 2 ? s.cx + half + 3 + hash01(k, 61) * (s.x1 - s.cx - half - 4) : s.x0 + hash01(k, 62) * (s.cx - half - s.x0 - 14);
      s.put(x, s.ground - 1, '†', mix(0x5A5470, 0xA09ABE, hash01(k, 63)));
    }
    const reach = s.cx - half - 3 - (s.x0 + 2), walk = Math.min(1, s.age / 100);
    const hx = s.x0 + 2 + walk * reach;
    for (let x = s.x0 + 2; x < hx; x += 3) s.put(x, s.ground - 1, '·', 0x8A7A5A);
    s.put(hx, s.ground - 1, '@', mix(0xFBC53E, 0xFFFFFF, flicker(s.tick, 4) * (walk >= 1 ? 1 : 0)), true);
    const eyes = Math.floor(s.tick / 14) % 6 !== 0;    // two eyes, deep in the dark, that blink
    if (eyes) { s.put(s.cx - 2, s.ground + rowsDeep - 1, '●', 0xC03030, true); s.put(s.cx + 2, s.ground + rowsDeep - 1, '●', 0xC03030, true); }
  },
];
