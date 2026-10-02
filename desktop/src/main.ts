import './style.css';
import { layout, parseFont } from './font';
import { readDisplay, request, validSeed, type Frame, type Request } from './protocol';
import { TerminalRenderer } from './renderer';
import { overlayMenu, titleFrame } from './title';
import { bodyOf, introFrame, introHold, introSpeed, pageLength } from './intro';
import { t, type Lang } from './i18n';
import { playCues, unlockAudio } from './audio';

const canvas = document.querySelector<HTMLCanvasElement>('#screen')!;
const launch = document.querySelector<HTMLElement>('#launch')!;
const seed = document.querySelector<HTMLInputElement>('#seed')!;
const resumeButton = document.querySelector<HTMLButtonElement>('#continue')!;
const resumeInfo = document.querySelector<HTMLElement>('#continue-info')!;
const beginKey = document.querySelector<HTMLElement>('#begin-key')!;
const failure = document.querySelector<HTMLElement>('#failure')!;
const status = document.querySelector<HTMLElement>('#status')!;
let renderer: TerminalRenderer;
let frame: Frame;
let onTitle = true;
let busy = false;
let ready = false;
let resizePending = false;
let titleTick = 0;
let introPage = 0;
let introTyped = 0;
let introAge = 0;
let introWait = 0;
let introActive = false;
let lastStored = '';

const prefs = () => ({ theme: frame.theme, crt: frame.crt, scale: frame.scale, lang: frame.lang });
const langOf = (): Lang => (frame?.lang === 'en' ? 'en' : readDisplay().lang);

function applyLabels(lang: Lang) {
  document.documentElement.lang = lang === 'en' ? 'en' : 'pt-BR';
  document.querySelector('label[for="seed"]')!.innerHTML = `${t(lang, 'seed')} <span>${t(lang, 'optional')}</span>`;
  seed.placeholder = t(lang, 'random');
  resumeButton.querySelector('b')!.textContent = t(lang, 'continue');
  document.querySelector('#begin')!.firstChild!.textContent = t(lang, 'begin') + ' ';
  document.querySelector('#daily')!.firstChild!.textContent = t(lang, 'daily') + ' ';
  document.querySelector('#options')!.firstChild!.textContent = t(lang, 'options') + ' ';
  document.querySelector('#retry')!.textContent = t(lang, 'retry');
}

function size() { return layout(Math.floor(innerWidth * devicePixelRatio), Math.floor(innerHeight * devicePixelRatio), frame?.scale ?? readDisplay().scale); }

// While the title is up, the engine's menu panels are drawn over the title scene.
function menuOnTitle() { return onTitle && (frame.panel === 'Settings' || frame.panel === 'Controls'); }

function paint(next: Frame) {
  const wasIntro = introActive;
  frame = next;
  introActive = !!frame.intro;
  if (introActive && !wasIntro) { introPage = 0; introTyped = 0; introAge = 0; introWait = 0; }
  applyLabels(frame.lang);
  const scene = introActive ? introFrame(frame, frame.intro!, introPage, introTyped, titleTick, frame.lang, introAge) : onTitle ? titleFrame(frame, titleTick) : frame;
  renderer.draw(menuOnTitle() ? overlayMenu(scene, frame) : scene, size(), devicePixelRatio);
  if (!onTitle && !introActive) playCues(frame.sounds, frame.master, frame.effects);
  const stored = JSON.stringify({ theme: frame.theme, crt: frame.crt, scale: frame.scale, lang: frame.lang });
  if (stored !== lastStored) {
    lastStored = stored;
    try { localStorage.setItem('ossuary.display', stored); } catch { /* Display still works with storage disabled. */ }
  }
  launch.hidden = !onTitle || menuOnTitle();
  const canResume = frame.started || frame.hasSave;
  resumeButton.hidden = !canResume;
  beginKey.hidden = canResume;
  resumeInfo.textContent = frame.started ? t(frame.lang, 'inProgress') : frame.saveInfo;
  failure.hidden = true;
  status.hidden = true;
  canvas.setAttribute('aria-label', `Ossuary. ${frame.mode}. Turno ${frame.turn}. Painel ${frame.panel}.`);
}

function showError(error: unknown) {
  document.querySelector<HTMLElement>('#error')!.textContent = error instanceof Error ? error.message : String(error);
  failure.hidden = false;
}

async function send(message: Request) {
  busy = true;
  try {
    const next = await request(message);
    if (next.toTitle) onTitle = true;
    paint(next);
    if (next.exit && '__TAURI_INTERNALS__' in window) {
      const { getCurrentWindow } = await import('@tauri-apps/api/window');
      await getCurrentWindow().close();
    } else if (next.exit) {
      onTitle = true;
      const s = size();
      paint(await request({ op: 'new', cols: s.cols, rows: s.rows, theme: next.theme, crt: next.crt, scale: next.scale, lang: next.lang }));
    }
  } catch (error) { showError(error); if (message.op === 'load') onTitle = true; }
  finally { busy = false; }
}

async function resize() {
  if (!ready || busy) { resizePending = true; return; }
  resizePending = false;
  const s = size();
  if (s.cols !== frame.cols || s.rows !== frame.rows) await send({ op: 'resize', cols: s.cols, rows: s.rows });
  else paint(frame);
}

async function initialize() {
  if (busy) return;
  busy = true;
  try {
    const fontResponse = await fetch('/unscii-16.hex');
    if (!fontResponse.ok) throw new Error('Não foi possível carregar a fonte unscii-16.');
    const glyphs = parseFont(await fontResponse.text());
    renderer = new TerminalRenderer(canvas, glyphs, document.querySelector<HTMLElement>('#crt')!);
    const s = size();
    paint(await request({ op: 'new', cols: s.cols, rows: s.rows, ...readDisplay() }));
    paint(await request({ op: 'title' }));
    ready = true;
    if ('__TAURI_INTERNALS__' in window) {
      const { invoke } = await import('@tauri-apps/api/core');
      await invoke('startup_ready', { cols: frame.cols, rows: frame.rows, cells: frame.glyphs.length, fontGlyphs: glyphs.size, painted: renderer.painted });
    }
  } catch (error) { showError(error); }
  finally { busy = false; }
}

async function resume() {
  if (!ready || busy || !(frame.started || frame.hasSave)) return;
  onTitle = false;
  if (frame.started) { paint(await request({ op: 'play' })); }
  else {
    const s = size();
    await send({ op: 'load', cols: s.cols, rows: s.rows, ...prefs() });
  }
  canvas.focus();
}

async function begin() {
  if (!ready || busy) return;
  const value = seed.value.trim();
  if (value && !validSeed(value)) { showError(new Error(t(langOf(), 'badSeed'))); return; }
  onTitle = false;
  // Every new expedition starts at character creation (name, race, class).
  const s = size();
  await send({ op: 'new', create: true, seed: value || undefined, cols: s.cols, rows: s.rows, ...prefs() });
  canvas.focus();
}

async function beginDaily() {
  if (!ready || busy) return;
  onTitle = false;
  // The daily challenge fixes the seed and the hero by the UTC date, so it skips creation.
  const s = size();
  await send({ op: 'new', daily: true, cols: s.cols, rows: s.rows, ...prefs() });
  canvas.focus();
}

document.querySelector('#begin')!.addEventListener('click', () => void begin());
document.querySelector('#daily')!.addEventListener('click', () => void beginDaily());
document.querySelector('#options')!.addEventListener('click', () => { if (ready && !busy) void send({ op: 'key', code: 'Escape' }); });
resumeButton.addEventListener('click', () => void resume());
document.querySelector('#retry')!.addEventListener('click', () => { failure.hidden = true; if (!ready) void initialize(); else void send({ op: 'frame' }); });

window.addEventListener('keydown', async event => {
  unlockAudio();
  if (event.code === 'F11') {
    event.preventDefault();
    if ('__TAURI_INTERNALS__' in window) {
      const { getCurrentWindow } = await import('@tauri-apps/api/window');
      const win = getCurrentWindow(); await win.setFullscreen(!await win.isFullscreen());
    } else if (document.fullscreenElement) await document.exitFullscreen();
    else await document.documentElement.requestFullscreen();
    return;
  }
  if (event.altKey || event.metaKey || !ready) return;
  if (introActive) {
    event.preventDefault();
    if (event.repeat || busy || event.code.startsWith('Control') || event.code.startsWith('Shift')) return;
    const pages = frame.intro!;
    if (event.code === 'Escape') { await send({ op: 'key', code: 'Enter' }); return; }
    if (event.code !== 'Enter' && event.code !== 'NumpadEnter' && event.code !== 'Space') return;
    if (introTyped < pageLength(bodyOf(pages[introPage]!))) introTyped = 1 << 20;
    else if (introPage < pages.length - 1) { introPage++; introTyped = 0; introAge = 0; introWait = 0; }
    else { await send({ op: 'key', code: 'Enter' }); return; }
    paint(frame);
    return;
  }
  if (onTitle && !menuOnTitle()) {
    if (event.code === 'Escape' && !busy) { event.preventDefault(); void send({ op: 'key', code: 'Escape' }); return; }
    if (event.code === 'Enter' || event.code === 'NumpadEnter') {
      if (event.target instanceof HTMLButtonElement && event.target.id === 'begin') return;   // the button handles its own Enter
      event.preventDefault(); void (frame.started || frame.hasSave ? resume() : begin());
    }
    else if ((event.code === 'F3' || event.code === 'F4') && !busy) {
      event.preventDefault(); await send({ op: 'key', code: event.code });
    }
    return;
  }
  // Single outstanding action; holding a key never creates an unbounded turn queue. Autorepeat is sent flagged,
  // and the engine honours it only for calm walking (no hostile in view, nothing underfoot, no damage).
  if (event.code === 'Tab' || event.code.startsWith('Control') || event.code.startsWith('Shift')) return;
  event.preventDefault();
  if (busy) return;
  await send({ op: 'key', code: event.code, key: event.key, shift: event.shiftKey, ctrl: event.ctrlKey, repeat: event.repeat });
  await resize();
});

let resizeTimer: ReturnType<typeof setTimeout>;
window.addEventListener('resize', () => { clearTimeout(resizeTimer); resizeTimer = setTimeout(() => void resize(), 80); });
// Resizes requested during IPC are applied after the request, without simulation ticks.
setInterval(() => { if (resizePending && !busy && ready) void resize(); }, 100);
// The title scene is client-only: its embers move without asking the engine for anything.
setInterval(() => {
  if ((!onTitle && !introActive) || !ready || busy || document.hidden) return;
  const calm = matchMedia('(prefers-reduced-motion: reduce)').matches;
  if (introActive) {
    const pages = frame.intro!;
    if (calm) { introTyped = 1 << 20; introAge = 1 << 12; paint(frame); return; }
    introAge++;
    if (introTyped < pageLength(bodyOf(pages[introPage]!))) introTyped += introSpeed;
    // A finished page is held for a few seconds and then the story moves on by itself; the last one waits for you.
    else if (introPage < pages.length - 1 && ++introWait > introHold) { introPage++; introTyped = 0; introAge = 0; introWait = 0; }
  }
  else if (calm) return;
  titleTick++; paint(frame);
}, 80);
void initialize();
