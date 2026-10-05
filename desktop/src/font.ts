import { readDisplay } from './protocol';
import { t } from './i18n';

export function parseFont(text: string): Map<number, Uint8Array> {
  const glyphs = new Map<number, Uint8Array>();
  for (const line of text.split(/\r?\n/)) {
    const match = /^([0-9A-Fa-f]{4,6}):([0-9A-Fa-f]{32})$/.exec(line.trim());
    if (!match) continue;
    const cp = parseInt(match[1], 16);
    if (cp < 32 || cp > 65535 || (cp >= 0xd800 && cp <= 0xdfff)) continue;
    const rows = new Uint8Array(16);
    for (let y = 0; y < 16; y++) rows[y] = parseInt(match[2].slice(y * 2, y * 2 + 2), 16);
    glyphs.set(cp, rows);
  }
  if (!glyphs.has(63)) throw new Error(t(readDisplay().lang, 'fontIncomplete'));
  return glyphs;
}

export interface Layout { cols: number; rows: number; scale: number; width: number; height: number; left: number; top: number }
export function layout(width: number, height: number, preferredScale = 0): Layout {
  const fit = Math.max(1, Math.min(3, Math.floor(width / (84 * 8)), Math.floor(height / (26 * 16))));
  const scale = preferredScale === 0 ? fit : Math.max(1, Math.min(preferredScale, fit));
  const cols = Math.max(84, Math.min(240, Math.floor(width / (8 * scale))));
  const rows = Math.max(26, Math.min(120, Math.floor(height / (16 * scale))));
  const w = cols * 8 * scale, h = rows * 16 * scale;
  return { cols, rows, scale, width: w, height: h, left: Math.floor((width - w) / 2), top: Math.floor((height - h) / 2) };
}
