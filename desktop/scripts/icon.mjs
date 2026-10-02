// App icon derived from the existing bitmap @ and the canonical theme tokens.
import { readFileSync, writeFileSync, mkdirSync } from 'node:fs';
const core = readFileSync('../engine/Ossuary.Core/Theme.cs', 'utf8');
const color = token => '#' + new RegExp(`${token} = [BF]\\(0x([0-9A-Fa-f]{6})\\)`).exec(core)[1];
const font = readFileSync('../assets/fonts/unscii-16.hex', 'utf8');
const bits = /^0*40:([0-9A-Fa-f]{32})\r?$/m.exec(font)[1];
let pixels = '';
for (let y = 0; y < 16; y++) {
  const row = parseInt(bits.slice(y * 2, y * 2 + 2), 16);
  for (let x = 0; x < 8; x++) {
    if (row & (128 >> x)) pixels += `<rect x="${4 + x}" y="${y}" width="1" height="1"/>`;
  }
}
mkdirSync('src-tauri/icons', { recursive: true });
writeFileSync('src-tauri/icons/source.svg', `<svg xmlns="http://www.w3.org/2000/svg" width="256" height="256" viewBox="0 0 16 16"><rect width="16" height="16" fill="${color('Void')}"/><g fill="${color('Accent')}">${pixels}</g></svg>`);
