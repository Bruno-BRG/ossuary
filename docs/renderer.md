# Renderer — Ossuary

O renderer em `desktop/src/renderer.ts` consome frames do motor e desenha
uma grade bitmap, com o filtro CRT em WebGL2 (e Canvas 2D como reserva). Ele não conhece mapa, combate ou regras do jogo.

## Fonte e células

Fonte canônica: `assets/fonts/unscii-16.hex`, unscii-16 de viznut, domínio
público. `desktop/src/font.ts` lê as linhas hex, retendo glifos BMP 8×16.
Cada glifo tem 16 bytes; o bit mais significativo representa a coluna esquerda.
O glifo `?` é o fallback. Nenhuma fonte dinâmica desenha o terminal.

A grade tem 84–240 colunas e 26–120 linhas. `layout` escolhe escala inteira
1×, 2× ou 3× em pixels físicos e centraliza a tela com letterbox. O DPI é
incluído no cálculo; o Canvas mantém suavização desativada. Uma escala fixa
maior que a disponível é reduzida para manter a grade jogável.

## Frame

`Session.Draw` percorre `TextBuilder` em ordem linha/coluna, exportando
codepoint, cor de frente, cor de fundo e negrito. Também envia tokens de cor,
dimensões, modo, turno, painel, opções e parâmetros do CRT.

O renderer rasteriza a grade em resolução nativa 8×16 por célula em duas
texturas: a imagem base e uma camada de emissão com os glifos brilhantes ou
em negrito. O look CRT é um fragment shader (`desktop/src/crt.ts`) sobre elas:

- curvatura de barril suave e vidro com cantos arredondados;
- *sharp bilinear*: texels nítidos, só a costura de 1 px é filtrada;
- scanlines (a parte baixa de cada linha de fonte escurece; em 1× alterna
  linhas de tela, bem leve) e máscara de fósforo RGB por pixel;
- bloom por mipmap só dos emissores e halação leve da imagem inteira;
- vinheta, flicker e zumbido muito sutis, e grão (hash inteiro, sem `sin`).

Intensidade de scanline, vinheta e glow continua vindo do Core
(`DisplaySettings.CrtParams`); curvatura, máscara e flicker derivam do nível
(`crtParams`). O brilho vaza como aura CSS ao redor do vidro. Sem WebGL2 o
renderer cai para Canvas 2D com blur + overlay CSS. A animação roda a ~30 fps
só com CRT ligado e é desativada com `prefers-reduced-motion`.

**Animações de magia** (`fx.ts`): o quadro pode trazer `fx` (um array por passo de [célula, glifo, fg, bg]) e `fxMs`; o front-end toca por cima do quadro
pronto, ~45 ms por passo, sem pedir turno ao motor, com a mesma regra de `prefers-reduced-motion` da água. Uma tecla nova ou um novo quadro corta a
animação. Ver [`magia-e-itens.md`](magia-e-itens.md#animações).

O título (`title.ts`) é cena só do cliente: céu, ruínas, brasas e logo com
gradiente; as brasas se movem por tick local e nunca consultam o motor.

## Paleta, luz e preferências

Cores vêm exclusivamente de `engine/Ossuary.Core/Theme.cs`. A luz de tocha,
memória, dia/noite e remap dos quatro presets são aplicados pela composição
Core antes do frame. O CSS recebe tokens, sem uma paleta paralela.

`DisplaySettings.Current` conserva tema, CRT e escala durante uma run e nos
reinícios. O frontend persiste essas opções em localStorage. `F2` abre opções,
`F3` alterna CRT e `F4` alterna tema; `F11` controla a janela em tela cheia.

## Como verificar

- `headless.ps1 test`: GlyphSet, fonte real, paleta, composição de todos os painéis.
- `headless.ps1 dump panels`: layout e conteúdo em ASCII.
- `npm test` em `desktop/`: fonte real, dimensões/DPI, sementes e IPC empacotado.
- `desktop.ps1 web`: inspeção visual com o mesmo motor da distribuição.
- `check.ps1`: validação completa.

Capturas reais ficam em `docs/shots/tauri-title.jpg`, `tauri-dungeon.jpg` e
`tauri-options.jpg`. A direção de arte está em [visual.md](visual.md).
