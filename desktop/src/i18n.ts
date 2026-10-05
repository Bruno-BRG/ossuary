export type Lang = 'pt' | 'en';

export const strings = {
  seed: { pt: 'SEMENTE', en: 'SEED' },
  optional: { pt: 'opcional', en: 'optional' },
  random: { pt: 'Aleatória', en: 'Random' },
  continue: { pt: 'CONTINUAR', en: 'CONTINUE' },
  begin: { pt: 'NOVA EXPEDIÇÃO', en: 'NEW EXPEDITION' },
  daily: { pt: 'DESAFIO DIÁRIO', en: 'DAILY CHALLENGE' },
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
  fontFailed: { pt: 'Não foi possível carregar a fonte unscii-16.', en: 'Could not load the unscii-16 font.' },
  engineNoFrame: { pt: 'O motor não devolveu uma tela.', en: 'The engine did not return a screen.' },
  enginePartial: { pt: 'Tela incompleta recebida do motor.', en: 'An incomplete screen came back from the engine.' },
  turn: { pt: 'Turno', en: 'Turn' },
  panel: { pt: 'Painel', en: 'Panel' },
  terminal: { pt: 'Terminal do jogo Ossuary', en: 'Ossuary game terminal' },
  screen: { pt: 'Mapa e interface ASCII', en: 'ASCII map and interface' },
  newExpedition: { pt: 'Nova expedição', en: 'New expedition' },
  fontIncomplete: { pt: 'A fonte bitmap está incompleta.', en: 'The bitmap font is incomplete.' },
  canvasMissing: { pt: 'Canvas indisponível.', en: 'Canvas unavailable.' },
  crtMissing: { pt: 'WebGL2 CRT indisponível, usando Canvas 2D:', en: 'WebGL2 CRT unavailable, using Canvas 2D:' },
} as const;

export type Key = keyof typeof strings;
export function t(lang: Lang, key: Key): string { return strings[key][lang]; }

export const legend: Record<Lang, [string, string][]> = {
  pt: [['←↑↓→', 'mover'], ['I', 'inventário'], ['?', 'ajuda'], ['Esc', 'opções'], ['F3', 'fósforo'], ['F4', 'paleta'], ['F11', 'tela cheia']],
  en: [['←↑↓→', 'move'], ['I', 'inventory'], ['?', 'help'], ['Esc', 'options'], ['F3', 'phosphor'], ['F4', 'palette'], ['F11', 'fullscreen']],
};
