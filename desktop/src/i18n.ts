export type Lang = 'pt' | 'en';

const strings = {
  seed: { pt: 'SEMENTE', en: 'SEED' },
  optional: { pt: 'opcional', en: 'optional' },
  random: { pt: 'Aleatória', en: 'Random' },
  continue: { pt: 'CONTINUAR', en: 'CONTINUE' },
  begin: { pt: 'NOVA EXPEDIÇÃO', en: 'NEW EXPEDITION' },
  options: { pt: 'OPÇÕES', en: 'OPTIONS' },
  retry: { pt: 'Tentar novamente', en: 'Try again' },
  preparing: { pt: 'Preparando o terminal…', en: 'Preparing the terminal…' },
  inProgress: { pt: 'expedição em andamento', en: 'expedition in progress' },
  badSeed: { pt: 'Use uma semente inteira entre 0 e 18446744073709551615.', en: 'Use an integer seed between 0 and 18446744073709551615.' },
  motto: { pt: 'Sob a terra, toda coroa é osso.', en: 'Beneath the earth, every crown is bone.' },
  tagline: { pt: 'Uma expedição. Um destino. Nenhuma segunda vida.', en: 'One expedition. One fate. No second life.' },
  subtitle: { pt: 'F Ó S F O R O  &  O S S O', en: 'P H O S P H O R  &  B O N E' },
  introHint: { pt: 'Enter avança   Esc pula', en: 'Enter advances   Esc skips' },
  bDungeons: { pt: 'As Masmorras', en: 'The Dungeons' },
  bMines: { pt: 'As Minas de Dwarfdeep', en: 'The Mines of Dwarfdeep' },
  bWarrens: { pt: 'As Tocas', en: 'The Warrens' },
  bVaults: { pt: 'Os Cofres Afundados', en: 'The Sunken Vaults' },
  bSpire: { pt: 'A Torre de Cinza', en: 'The Ashen Spire' },
  introBegin: { pt: 'Enter: começar a expedição', en: 'Enter: begin the expedition' },
} as const;

export type Key = keyof typeof strings;
export function t(lang: Lang, key: Key): string { return strings[key][lang]; }

export const legend: Record<Lang, [string, string][]> = {
  pt: [['←↑↓→', 'mover'], ['I', 'inventário'], ['?', 'ajuda'], ['Esc', 'opções'], ['F3', 'fósforo'], ['F4', 'paleta'], ['F11', 'tela cheia']],
  en: [['←↑↓→', 'move'], ['I', 'inventory'], ['?', 'help'], ['Esc', 'options'], ['F3', 'phosphor'], ['F4', 'palette'], ['F11', 'fullscreen']],
};
