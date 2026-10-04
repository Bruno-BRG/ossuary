// Renders every stinger to a 16-bit mono WAV in assets/audio/stingers/.  Usage: npm run stingers
import { mkdirSync, writeFileSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';
import { renderStinger, stingerInfo, stingerNames, toWav } from '../src/stingers.ts';

const out = join(dirname(fileURLToPath(import.meta.url)), '..', '..', 'assets', 'audio', 'stingers');
mkdirSync(out, { recursive: true });
const rate = 44100;
stingerNames.forEach((name, i) => {
  const started = Date.now();
  const wav = toWav(renderStinger(name, rate), rate);
  const file = join(out, `S${String(i + 1).padStart(2, '0')}-${name}.wav`);
  writeFileSync(file, wav);
  console.log(`${stingerInfo[name].title.padEnd(20)} ${stingerInfo[name].seconds}s  ${(wav.length / 1024).toFixed(0)} KB  (${Date.now() - started} ms)  ${file}`);
});
