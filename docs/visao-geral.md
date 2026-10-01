# Visão geral — Ossuary

Roguelike de terminal: grade de glifos, turnos, morte permanente, seed como save.

## Inspirações (visão do projeto)

- **Dungeons: Rogue + NetHack** — geração procedural, escadas, armadilhas,
  fome, identificação, verbos de uma tecla, morte permanente.
- **Mundo: Fallout 1/2 + Caves of Qud** — overworld com regiões nomeadas,
  cidades-hub com lojas e NPCs, viagem com custo de tempo, encontros,
  facções/serviços; história emergente por simulação, não por script.
- **Visual: Dwarf Fortress** — ASCII denso e colorido, painéis cheios de
  informação, estética de terminal cru com CRT por cima.

## Pilares

1. **Tudo é turno.** Andar, pegar, ler, viajar no overworld — cada ação avança
   o relógio e dá vez aos monstros. Monstros rápidos agem mais vezes (medidor
   de energia, estilo NetHack).
2. **Tudo é seed.** `Game(seed)` gera dungeon + overworld + cidades. F5 mostra
   a seed = "save". Mesma seed, mesma masmorra.
3. **Tudo é testável sem Unity.** `Ossuary.Core` não referencia `UnityEngine`;
   a suite headless (`headless.ps1 test`) cobre geração, combate, lojas, UI.

## Loop do jogo (alpha)

1. Nasce em `The Dungeons:1` com adaga, armadura de couro, 3 rações, lock pick, 30 ouro.
2. Explora (FOV 10), luta ou desvia, pega loot, desce (`>`) até o fundo do branch.
3. Volta (`<` até a superfície) → overworld: estradas, regiões, dia/noite,
   encontros aleatórios, entradas de dungeon e cidades.
4. Cidades: praça, fonte, 8 lojas (arma, armadura, poção, pergaminho, varinha,
   comida, ferramenta, geral), guardas, NPCs. Compra/vende com ouro.
5. Morreu = `GameOver`, qualquer tecla recomeça. Sem vitória ainda (ver `alpha.md`).
