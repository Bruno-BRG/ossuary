import { afterAll, describe, expect, it } from 'vitest';
import { spawn } from 'node:child_process';
import { createInterface } from 'node:readline';
import { mkdtempSync, rmdirSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { resolve, join } from 'node:path';
import type { Frame, Request } from './protocol';

// End-to-end test of the actual self-contained executable, from an empty cwd.
// No dotnet command, editor installation, development HTTP server or mock engine.
const cwd = mkdtempSync(join(tmpdir(), 'ossuary-ipc-'));
const child = spawn(resolve('src-tauri/binaries/ossuary-engine-x86_64-pc-windows-msvc.exe'), [], { cwd, windowsHide: true });
let pending: { resolve: (line: string) => void; reject: (error: Error) => void } | undefined;
const lines = createInterface({ input: child.stdout });
lines.on('line', line => { pending?.resolve(line); pending = undefined; });
child.on('error', error => { pending?.reject(error); pending = undefined; });
child.stderr.on('data', () => { /* Expected validation errors go to stderr, never corrupt stdout. */ });
async function raw(message: string) {
  return new Promise<{ ok: boolean; frame: Frame; error?: string }>((resolveLine, reject) => {
    const timer = setTimeout(() => reject(new Error('Engine IPC timed out')), 5000);
    pending = { resolve: line => { clearTimeout(timer); resolveLine(JSON.parse(line)); }, reject: error => { clearTimeout(timer); reject(error); } };
    child.stdin.write(message + '\n');
  });
}
async function send(request: Request) { return raw(JSON.stringify(request)); }
afterAll(async () => {
  const exited = new Promise<void>(resolveExit => child.once('exit', () => resolveExit()));
  child.stdin.end();
  await exited;
  lines.close();
  rmdirSync(cwd); // Only the empty temporary working directory created by this test.
});

describe('packaged engine protocol', () => {
  it('starts and draws without an installed SDK or editor', async () => {
    const response = await send({ op: 'new', seed: '31337', cols: 110, rows: 36 });
    expect(response.ok).toBe(true);
    expect(response.frame.seed).toBe('31337');
    expect(response.frame.glyphs.length).toBe(3960);
    expect(response.frame.glyphs).toContain(64);
  });
  it('preserves modal input and consumes exactly one turn for wait', async () => {
    const inventory = await send({ op: 'key', code: 'KeyI' });
    expect(inventory.frame.panel).toBe('Inventory');
    const close = await send({ op: 'key', code: 'ArrowRight' });
    expect(close.frame.panel).toBe('None');
    expect(close.frame.turn).toBe(0);
    const wait = await send({ op: 'key', code: 'Period', key: '.' });
    expect(wait.frame.turn).toBe(1);
  });
  it('recovers from malformed input without restarting the session', async () => {
    expect((await raw('{invalid')).ok).toBe(false);
    expect((await raw('{"op":"shell"}')).ok).toBe(false);
    const response = await send({ op: 'frame' });
    expect(response.ok).toBe(true);
    expect(response.frame.turn).toBe(1);
  });
  it('keeps all uint64 seed bits and display options across new runs', async () => {
    const response = await send({ op: 'new', seed: '18446744073709551615', theme: 2, crt: 0, scale: 2 });
    expect(response.frame.seed).toBe('18446744073709551615');
    expect(response.frame.theme).toBe(2);
    expect(response.frame.glow).toBe(0);
    expect(response.frame.scale).toBe(2);
  });
  it('replays input deterministically at the frame boundary', async () => {
    async function replay() {
      await send({ op: 'new', seed: '777', cols: 110, rows: 36 });
      for (const code of ['ArrowRight', 'ArrowDown', 'Period', 'KeyS', 'KeyI', 'Escape']) await send({ op: 'key', code });
      return (await send({ op: 'frame' })).frame;
    }
    const a = await replay(), b = await replay();
    expect(a).toEqual(b);
  });
});
