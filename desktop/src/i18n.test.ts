import { readFileSync, readdirSync } from 'node:fs';
import { join } from 'node:path';
import { describe, expect, it } from 'vitest';
import { strings } from './i18n';

// The front end speaks one language at a time: everything the player reads comes from this table.
// A Portuguese string hardcoded anywhere else is shown in English mode (and the other way round).

const ACCENTS = /[áàâãäéèêëíìîïóòôõöúùûüçñÁÀÂÃÄÉÈÊËÍÌÎÏÓÒÔÕÖÚÙÛÜÇÑ]/;
const TRANSLATED = 'i18n.ts';

describe('interface language', () => {
  it('has every string in both languages, and never the same text twice', () => {
    for (const [key, value] of Object.entries(strings)) {
      expect(value.pt, key).toBeTruthy();
      expect(value.en, key).toBeTruthy();
      expect(value.pt, key).not.toBe(value.en);
    }
  });

  it('keeps Portuguese out of the sources that are not the language table', () => {
    const dir = new URL('.', import.meta.url).pathname.replace(/^\/([A-Za-z]:)/, '$1');
    const offenders: string[] = [];
    for (const file of readdirSync(dir)) {
      if (!file.endsWith('.ts') || file.endsWith('.test.ts') || file === TRANSLATED) continue;
      const source = readFileSync(join(dir, file), 'utf8');
      source.split(/\r?\n/).forEach((line, index) => {
        if (line.trimStart().startsWith('//') || line.trimStart().startsWith('*')) return;   // a comment may be written in any language
        for (const literal of line.matchAll(/'([^'\\]*)'|"([^"\\]*)"|`([^`\\]*)`/g)) {
          const text = literal[1] ?? literal[2] ?? literal[3] ?? '';
          if (ACCENTS.test(text)) offenders.push(`${file}:${index + 1} ${text}`);
        }
      });
    }
    expect(offenders, offenders.join('\n')).toEqual([]);
  });
});
