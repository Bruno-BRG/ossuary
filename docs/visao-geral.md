# Visão geral — Ossuary

Runtime desktop atual em Tauri 2, com shell Rust, terminal Canvas bitmap e
simulação C# independente. Veja [`desktop.md`](desktop.md).

Roguelike de terminal: grade de glifos, turnos, morte permanente, geração reproduzível por seed.

## Inspirações (visão do projeto)

- **Dungeons: Rogue + NetHack** — geração procedural, escadas, armadilhas,
  fome, identificação, verbos de uma tecla, morte permanente.
- **Mundo: Fallout 1/2 + Caves of Qud** — overworld com regiões nomeadas,
  cidades-hub com lojas e NPCs, viagem com custo de tempo, encontros,
  facções/serviços; história emergente por simulação, não por script.
- **Visual: Dwarf Fortress** — ASCII denso e colorido, painéis cheios de
  informação, estética de terminal cru com CRT por cima.
  Direção de arte detalhada ("Fósforo & Osso", anos 80, UI escura +
  glifos coloridos): [`visual.md`](visual.md).

## Pilares

1. **Tudo é turno.** Andar, pegar, ler, viajar no overworld — cada ação avança
   o relógio e dá vez aos monstros. Monstros rápidos agem mais vezes (medidor
   de energia, estilo NetHack).
2. **Tudo é seed.** `Game(seed)` gera dungeon + overworld + cidades. F5 mostra
   a seed inicial. Mesma seed, mesma masmorra; ela não salva o progresso.
3. **Tudo é testável sem interface gráfica.** `Ossuary.Core` contém apenas simulação e UI como dados;
   a suite headless (`headless.ps1 test`) cobre geração, combate, lojas, UI.

## Loop do jogo (alpha)

1. Nasce em `The Dungeons:1` com adaga, armadura de couro, 3 rações, lock pick, 30 ouro.
2. Explora (FOV 10), luta ou desvia, pega loot, desce (`>`) até o fundo do branch.
3. Volta (`<` até a superfície) → overworld: estradas, regiões, dia/noite,
   encontros aleatórios, entradas de dungeon e cidades.
4. Cidades verticais: muralha, praça, fonte, ferraria, alquimista, magos, taverna,
   estalagem, templo, guilda, biblioteca, quartel… com torres, sótãos, criptas e
   porões (escadas `<`/`>`), moradores que falam, serviços pagos. Compra/vende com ouro.
5. Morreu = `GameOver`, qualquer tecla recomeça. Levar o Amuleto à superfície encerra a missão com vitória (ver `alpha.md`).
