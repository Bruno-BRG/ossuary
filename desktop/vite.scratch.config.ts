import { defineConfig, type Plugin } from 'vite';
import { spawn, type ChildProcessWithoutNullStreams } from 'node:child_process';
import { createInterface } from 'node:readline';
import { existsSync, mkdirSync, copyFileSync, mkdtempSync, readdirSync, rmSync } from 'node:fs';
import { resolve } from 'node:path';
import { tmpdir } from 'node:os';
import { fileURLToPath } from 'node:url';

const root = fileURLToPath(new URL('..', import.meta.url));

// Browser development runs the exact same engine as the packaged desktop app.
function enginePlugin(): Plugin {
  let child: ChildProcessWithoutNullStreams | undefined;
  let pending: { resolve: (line: string) => void; reject: (error: Error) => void } | undefined;
  let chain = Promise.resolve();
  function start() {
    if (child) return;
    const dll = '/tmp/claude-0/-home-user-ossuary/bf6c2148-b596-57ff-87d3-637303f54e2b/scratchpad/w/engine/Ossuary.Desktop/bin/Release/net8.0/ossuary-engine.dll';
    if (!existsSync(dll)) throw new Error('Execute desktop.ps1 prepare primeiro.');
    const local = resolve(root, '.tools/dotnet/dotnet.exe');
    // A private snapshot prevents Windows from locking MSBuild's output during web development.
    const snapshot = mkdtempSync(resolve(tmpdir(), 'ossuary-preview-'));
    const sourceDir = resolve(dll, '..');
    for (const file of readdirSync(sourceDir, { withFileTypes: true })) {
      if (file.isFile() && /\.(dll|json)$/.test(file.name)) copyFileSync(resolve(sourceDir, file.name), resolve(snapshot, file.name));
    }
    const processHandle = spawn(existsSync(local) ? local : 'dotnet', [resolve(snapshot, 'ossuary-engine.dll')], { cwd: root, windowsHide: true });
    child = processHandle;
    createInterface({ input: processHandle.stdout }).on('line', line => { pending?.resolve(line); pending = undefined; });
    processHandle.stderr.on('data', data => process.stderr.write(data));
    processHandle.on('error', error => { pending?.reject(error); pending = undefined; if (child === processHandle) child = undefined; });
    processHandle.on('exit', () => { pending?.reject(new Error('O motor encerrou.')); pending = undefined; if (child === processHandle) child = undefined; });
    processHandle.on('close', () => rmSync(snapshot, { recursive: true, force: true, maxRetries: 5, retryDelay: 100 }));
  }
  return {
    name: 'ossuary-engine',
    buildStart() {
      mkdirSync(resolve(root, 'desktop/public'), { recursive: true });
      copyFileSync(resolve(root, 'assets/fonts/unscii-16.hex'), resolve(root, 'desktop/public/unscii-16.hex'));
    },
    configureServer(server) {
      server.middlewares.use('/__engine', (req, res, next) => {
        if (req.method !== 'POST') return next();
        // Development endpoint is same-origin, loopback only, and accepts no shell commands.
        const origin = req.headers.origin;
        if (origin && origin !== `http://${req.headers.host}`) { res.statusCode = 403; res.end(); return; }
        let body = '';
        req.on('data', data => { body += data; if (body.length > 8192) req.destroy(); });
        req.on('end', () => {
          chain = chain.then(async () => {
            try {
              JSON.parse(body); start();
              const result = await new Promise<string>((resolveLine, reject) => {
                const timer = setTimeout(() => { pending = undefined; child?.kill(); reject(new Error('O motor não respondeu em 10s.')); }, 10000);
                pending = { resolve: line => { clearTimeout(timer); resolveLine(line); }, reject: error => { clearTimeout(timer); reject(error); } };
                child!.stdin.write(body + '\n');
              });
              res.setHeader('Content-Type', 'application/json'); res.end(result);
            } catch (error) {
              res.statusCode = 500; res.end(JSON.stringify({ ok: false, error: String(error) }));
            }
          });
        });
      });
      server.httpServer?.on('close', () => child?.kill());
    },
  };
}

export default defineConfig({
  plugins: [enginePlugin()],
  clearScreen: false,
  server: { port: 1420, strictPort: true, host: '127.0.0.1', watch: { ignored: ['**/src-tauri/**'] } },
  envPrefix: ['VITE_', 'TAURI_ENV_*'],
  build: { target: 'es2022' },
});
