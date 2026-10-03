import type { Lang } from './i18n';
export interface Display { theme: number; crt: number; scale: number; square: number; lang: Lang }
export interface Frame extends Display {
  cols: number; rows: number;
  glyphs: number[]; fg: number[]; bg: number[]; bold: boolean[];
  seed: string; turn: number; mode: string; panel: string; exit: boolean;
  void: number; text: number; dim: number; title: number;
  rule: number; panelColor: number; bad: number;
  scanline: number; vignette: number; glow: number;
  master: number; music: number; effects: number;
  started: boolean; toTitle: boolean; hasSave: boolean; saveInfo: string;
  intro: string[][] | null;
  sounds?: string[];
  anim?: number[];
  fx?: number[][];
  fxMs?: number;
}
export interface Request {
  op: 'new' | 'load' | 'title' | 'play' | 'key' | 'resize' | 'display' | 'frame';
  seed?: string; code?: string; key?: string; shift?: boolean; ctrl?: boolean; repeat?: boolean;
  cols?: number; rows?: number; theme?: number; crt?: number; scale?: number; square?: number; create?: boolean; daily?: boolean; lang?: Lang;
}
interface Response { ok: boolean; frame?: Frame; error?: string }

export async function request(message: Request): Promise<Frame> {
  let response: Response;
  if ('__TAURI_INTERNALS__' in window) {
    const { invoke } = await import('@tauri-apps/api/core');
    response = await invoke<Response>('game_request', { request: message });
  } else {
    const result = await fetch('/__engine', {
      method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(message),
    });
    response = await result.json() as Response;
  }
  if (!response.ok || !response.frame) throw new Error(response.error ?? 'O motor não devolveu uma tela.');
  const f = response.frame;
  const n = f.cols * f.rows;
  if (f.glyphs.length !== n || f.fg.length !== n || f.bg.length !== n || f.bold.length !== n) {
    throw new Error('Tela incompleta recebida do motor.');
  }
  return f;
}

export function validSeed(value: string): boolean {
  if (!/^\d{1,20}$/.test(value)) return false;
  return BigInt(value) <= 18446744073709551615n;
}

export function cssColor(color: number): string { return '#' + color.toString(16).padStart(6, '0'); }

export function readDisplay(): Display {
  try {
    const data = JSON.parse(localStorage.getItem('ossuary.display') ?? '{}') as Partial<Display>;
    const bound = (v: unknown, max: number, fallback: number) => typeof v === 'number' && Number.isInteger(v) && v >= 0 && v <= max ? v : fallback;
    return { theme: bound(data.theme, 3, 0), crt: bound(data.crt, 2, 1), scale: bound(data.scale, 3, 0), square: bound(data.square, 1, 0), lang: data.lang === 'en' ? 'en' : 'pt' };
  } catch { return { theme: 0, crt: 1, scale: 0, square: 0, lang: 'pt' }; }
}
