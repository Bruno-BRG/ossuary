# Alpha v0.1 — Ossuary (jogável)

Build: `unity/Builds/StandaloneWindows64/Ossuary.exe` (rebuild 30/09/2026,
`Succeeded, 0 errors`). Suite: **30/30 PASS** (`headless.ps1 test -Rebuild`):
núcleo 24 + loja + papéis/level-ups (3) + vitória (5 asserts via `WinTests`)
+ cobertura de glifos (1). Shots: `unity/Builds/shots/` (pendente regenerar).

## Status honesto (01/10/2026)

- Simulação: 100% jogável e verificada (testes + dumps ASCII perfeitos).
- Renderer na tela: causa raiz encontrada. O charset do atlas tinha 400+
  codepoints (braille, clima, box duplo, moedas) e estourava o atlas dinâmico
  da fonte — Unity evicta glifos e as UVs lidas viram lixo (captures pretos /
  hieróglifos). Fix aplicado: charset enxuto (~140, canônico em
  `Core/GlyphSet.cs`), vocabulário 100% ASCII (`Tile.cs`, `OverworldGen`),
  layout sem sobreposição msg/mapa (`Ui.cs`), minimap sem off-by-one, CRT
  desligado por padrão (`GameApp`). Falta validar com GPU: `RenderDiagnostics`
  + `CaptureFrames` frescos e um screenshot legível. **Alpha só será declarado
  jogável de verdade com esse screenshot.**
- Suspeita antiga de `FitCamera` (z=+10) descartada: a câmera vai p/ z=-10
  com rotação identidade, correto.

## Como jogar (quando o renderer estiver ok)

Abra o exe, use o teclado (lista completa em `controles.md`):
`h j k l` anda, `>`/`<` escada, `g` pegar, `i` inventário, `?` ajuda,
`Ctrl-Q` 2x sai. Morreu = qualquer tecla recomeça.

## O que tem

Movimento 8-dir, FOV, 5 branches (~54 níveis, 6 estilos), 25+ monstros c/
energia/velocidade e 3 AIs, combate corpo-a-corpo + tiro/varinhas,
armadilhas, portas secretas/trancadas, fome, status (veneno, cega, confuso…),
loot/cadáveres, overworld 96×60 c/ dia-noite/estradas/encontros, cidades c/
8 lojas + guardas, loja jogável (comprar `Enter`/`B`, vender `S`), papéis
iniciais + level-ups gastáveis, objetivo final (Amuleto de Yendor no fundo,
extração = vitória), HUD + 10 painéis, CRT, seed como save.

## Limites conhecidos (roadmap)

- Renderer na tela: fix aplicado mas ainda não validado visualmente (falta
  GPU p/ `CaptureFrames`) — ver "Status honesto". É o único item bloqueando
  o alpha jogável.
- Sem save em arquivo (só seed), sem som, sem mouse, balanceamento só via soak.
- `CaptureFrames` e `RenderDiagnostics` precisam de GPU; rodar com
  `unity-run.ps1 -Graphics` (sem `-nographics`).
