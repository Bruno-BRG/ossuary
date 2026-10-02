# Visual — Fósforo & Osso

Terminal de PC dos anos 80: fundo índigo, texto cor de osso, glifos coloridos e brilho escasso. A implementação atual usa Tauri, Canvas e fonte bitmap; veja [renderer.md](renderer.md) e [desktop.md](desktop.md).

## Fonte e licença

A fonte distribuída é **unscii-16**, bitmap 8×16 de viznut, em domínio público. Arquivo: `assets/fonts/unscii-16.hex`; crédito e origem em `assets/fonts/NOTICE.md`. Não usamos a variante `unscii-16-full`. Uma fonte nova exige registrar origem e licença aqui.

O Canvas usa escala inteira, sem suavização, com letterbox. O logo grande é composto em blocos em `desktop/src/title.ts`. Novos glifos entram primeiro em `engine/Ossuary.Core/GlyphSet.cs` e precisam passar no teste de cobertura da fonte. A fonte não contém `☼ ✗ ⚔ ☠ ♜ ⌂ ✚ ✦ ∙ ›`; use as alternativas existentes.
## Paleta "Ossuary" (tokens semânticos)

Fundo índigo quase preto, em vez de cinza neutro. O texto é cor de osso,
âmbar marca títulos, ciano marca rótulos e magenta/violeta marca magia.

**UI**

| Token | Hex | Uso |
|---|---|---|
| `Void` | `#07060B` | Letterbox, fora do mapa |
| `Background` | `#0D0B14` | Chão do mapa e base de tudo |
| `Panel` | `#16121F` | Sidebar, log e modais |
| `PanelHi` | `#211B30` | Cabeçalho de painel, linha selecionada |
| `Rule` | `#3A3150` | Molduras simples e separadores |
| `Frame` | `#8A6F30` | Moldura dupla de modal (ouro velho) |
| `Text` | `#D8CFC0` | Texto (osso) |
| `Dim` | `#7D7590` | Texto secundário e mensagens antigas |
| `Label` | `#5FCDE4` | Rótulos (ciano) |
| `Title` | `#F2B33D` | Títulos e teclas de atalho (âmbar) |
| `Accent` | `#FBF236` | Destaque raro (seleção, `@`) |

**Estados e mensagens**

| Token | Hex | Token | Hex |
|---|---|---|---|
| `Good` | `#99E550` | `Bad` | `#D95763` |
| `Warn` | `#DF7126` | `Danger` | `#FF3B4F` |
| `Gold` | `#FBD94A` | `Magic` | `#B57EDC` |
| `Info` | `#639BFF` | `Quest` | `#5FE4C0` |
| `Narrative` | `#CBB3E8` | `Memory` | `#2E2A45` |

**Mapa: cada tile tem fg *e* bg**

| Tile | Glifo | fg | bg |
|---|---|---|---|
| Chão | `·` | `#4A4360` | `Background` |
| Chão alt | `·` / `,` | `#6B5440` | `#120F18` |
| Parede | `#` | `#9A90AE` | `#2A2438` |
| Parede tijolo | `#` | `#B0705A` | `#341C1C` |
| Rocha | `#` / `▓` | `#6A6880` | `#1E1B2A` |
| Porta | `+` / `'` | `#DF7126` | `#2E1A10` |
| Porta trancada | `+` | `#FBD94A` | `#2E1A10` |
| Escada | `>` `<` | `#FFFFFF` (negrito) | `#3F3F74` |
| Portal | `^` | `#D77BBA` | `#2A1238` |
| Fonte | `{` | `#5FCDE4` | `#0E2A40` |
| Altar | `_` | `#EAE4F4` | `#2A2438` |
| Entulho | `"` / `▒` | `#8F7A5E` | `Background` |

**Overworld: vocabulário CP437 + fundo colorido**

| Terreno | Glifos (variante por hash x,y) | fg | bg |
|---|---|---|---|
| Água funda | `≈` | `#306082` | `#0A1830` |
| Água | `≈` `~` | `#5B6EE1` | `#10224A` |
| Raso | `~` | `#639BFF` | `#16305A` |
| Areia | `·` `·` `░` | `#D9A066` | `#2A2014` |
| Grama | `"` `'` `,` `·` | `#6ABE30` | `#0F1A0C` |
| Floresta | `♣` `♠` `↑` | `#37946E` | `#0A1610` |
| Colinas | `∩` `ⁿ` | `#8F974A` | `#16180C` |
| Montanha | `▲` | `#9BADB7` | `#1E2028` |
| Pântano | `⌠` `"` `,` | `#4B692F` | `#10140A` |
| Neve | `·` `*` | `#EAF2FF` | `#2A3040` |
| Cinzas | `·` `·` | `#696A6A` | `#141414` |
| Estrada | `·` `═` `║` | `#8A6F30` | `#1A140C` |
| Cidade | `■` / `♦` | `#FBF236` | `#3A2A08` |
| Masmorra | `▼` | `#FF3B4F` | `#2A0A0E` |
| Ruína | `π` | `#9A9488` | — |
| Caverna | `Ω` | `#B08050` | — |
| Mina | `¥` | `#C09060` | — |
| Fortaleza | `Π` | `#C0B0A0` | — |
| Santuário | `‡` / `†` | `#5FE4C0` | — |
| Ponte | `═` / `║` | `#A0A0B0` | `#10224A` |

Os glifos só valem depois de passar pelo `GlyphsInFont` contra a fonte
escolhida. Glifo fora da fonte renderiza como `?` e falha o teste.

## Luz e composição

A paleta implementada em `engine/Ossuary.Core/Theme.cs` é a fonte da verdade. A UI usa tokens semânticos; cores de terreno recebem luz antes do remapeamento do preset. Cores de monstros são conteúdo e passam por `Theme.Mon`.

- Tocha escurece células com a distância; memória fora do FOV usa uma rampa azulada.
- Dia/noite altera a iluminação do overworld.
- Variantes e jitter usam hash de coordenadas no desenho, sem consumir o RNG da simulação.
- Branco, Accent e negrito ficam reservados ao jogador, escadas, itens e perigo.
- Cabeçalho compacto, sidebar com Nearby/equipamento/minimapa, log colorido e molduras duplas nos modais.
- Inventário em duas colunas; título com logo e seed; morte com RIP e causa; vitória com o Amuleto.

## Preferências

`F2` abre opções, `F3` alterna CRT e `F4` alterna Ossuary, Amber, Phosphor e CGA. O CRT combina scanlines, vinheta e glow no frontend, em três níveis. As preferências persistem em `localStorage` e chegam ao motor como dados de `DisplaySettings`.

## Extensões futuras

Animação ambiente de água e tileset quadrado são opções de evolução visual, sem alterar turnos. O runtime atual está completo sem essas extensões. Capturas do cliente atual estão em `docs/shots/tauri-*.jpg`.


## Luz, ambientação e HUD (redesenho)

**Luz de tocha.** `Theme.Shade` aquece glifos e fundos perto do jogador (poça
`#6A4220` no fundo), com queda suave até `TorchRadius`. Monstros e itens tingem o
próprio fundo com a cor do glifo, para saltarem da planta. O `@` fica numa poça
quente.

**Humor por profundidade.** `Theme.DepthTint` empurra pedra e fundo para uma
cor por faixa de 3 níveis: cripta (índigo), catacumbas (musgo), cofres alagados
(azul), fossas de osso (âmbar) e boca do inferno (rubro). O nome da faixa
aparece no cabeçalho. Porta secreta continua idêntica à parede ao redor.

**Textura.** Variantes de chão/parede e jitter de tom saem só de `hash(x,y)`;
rocha inexplorada tem grão esparso, e mapas menores que a janela são centralizados.

**HUD.** Cabeçalho com selo `♦ OSSUARY`, chip de profundidade e relógio com ícone
de dia/dusk/noite; moldura arredondada do mapa com título e coordenadas;
barras sólidas de meio bloco (HP/EN/XP); atributos coloridos; seções
`NEARBY`, `IN VIEW` (legenda viva), `WORN`, `VITALS`, `MAP`; diário com marcador
por tipo e esmaecimento por idade; barra de status com o que está sob os pés;
atalhos como teclas; painéis modais com sombra projetada e título ornamentado.


## Água animada e tiles quadrados

- Água visível é marcada pelo motor (`Frame.Anim`) e o front-end alterna glifo/brilho entre frames (`desktop/src/anim.ts`); nada disso consome turno nem Rng.
- Opção **Tiles** (menu): `DisplaySettings.Square` desenha cada célula do mapa em duas colunas 8×16, ficando quadrada na tela. O Core continua contando em células.
