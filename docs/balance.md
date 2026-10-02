# Balanceamento — método e medidas

O sistema RPG (raças, classes, magia, perks, itens, deuses, superfícies) foi balanceado com um **bot
determinístico** em vez de números de cabeça. Este documento diz como rodar, o que ele mede, o que foi
ajustado e o que ele **não** consegue dizer.

## Como rodar

```powershell
.\headless.ps1 balance 10 2500            # 10 sementes por combinação, 2500 turnos, política "cautious"
.\headless.ps1 balance 10 2500 dive       # política "dive": desce sem se preocupar com nível
.\headless.ps1 balance 4 3000 role=wizard race=elf trace   # uma combinação, com log a cada 50 turnos
```

560 execuções (8 classes × 7 raças × 10 sementes) levam cerca de um minuto. A saída mostra sobrevivência,
profundidade média, nível, mortes e morte-mais-comum, por classe, por raça e na matriz classe × raça.

## O bot (`engine/Ossuary.Headless/Balance.cs`)

Igual para todos, de propósito: estuda os livros que tem, gasta perks por uma lista por classe, equipa a
melhor armadura e arma que acha, lê *enchant*, bebe poções de ganho, cura quando o HP cai de 40%, invoca e
usa buff antes de lutar, usa a melhor magia ou habilidade da classe (Cleave com 2+ vizinhos, Fireball em
grupos, Aimed Shot a distância), descansa antes de seguir (HP 75%, Mp 80%), come, recolhe itens e desce.
Anda pelo mapa real (BFS, abre portas com o verbo de porta). **Não** reza em altares, não compra nada, não
usa varinhas nem pergaminhos desconhecidos e não troca de branch: mede classe, raça, nível e habilidades,
não a esperteza de quem joga.

`BalanceBand` (nos testes) é uma trava, não uma meta: nenhuma classe pode sobreviver menos de 50% nem ficar
abaixo de 60% da profundidade da melhor.

## Linha de base (10 sementes, 2500 turnos, "cautious")

| Classe | Sobrev. | Prof. | Nível | Mortes de monstros |
|---|---|---|---|---|
| fighter | 100% | 7.7 | 7.8 | 62.6 |
| ranger | 100% | 7.6 | 7.8 | 62.4 |
| cleric | 100% | 7.5 | 7.6 | 60.4 |
| paladin | 100% | 7.4 | 7.4 | 60.4 |
| adventurer | 100% | 7.0 | 7.1 | 56.3 |
| rogue | 100% | 7.0 | 7.2 | 56.4 |
| wizard | 100% | 6.9 | 7.0 | 52.5 |
| necromancer | 100% | 6.5 | 6.6 | 46.5 |

Média geral: profundidade 7.2, nível 7.3. As classes ficam dentro de −10% / +7% da média; as raças dentro de
±5% (human/orc 7.4, dwarf 7.0). Na política "dive" a média chega a 9.1 de 10 com 99% de sobrevivência.

## O que foi ajustado (e por quê)

- **O bot veio primeiro.** As primeiras execuções "morriam" 15–40% por defeitos do bot (ficava preso em portas,
  ignorava lesmas, achava que estava preso em combate). Só depois de corrigir é que os números dizem algo.
- **Casters morriam e andavam devagar**: Wizard 3→4 HP/nível, Mp base 6→8; Necromancer 3→4 HP/nível, Mp base 5→9,
  começa com *Drain Life* e *Raise Skeleton* (antes só *Sleep*, sem ataque); *Drain Life* 6→5 Mp e
  *Raise Skeleton* 7→6. Regeneração de Mp mais rápida (`14 − stat − Magic/15`, antes `16 − stat − Magic/20`).
- **Adventurer**: 4→5 HP/nível e começa com *Power Strike* (era "média em tudo" sem nenhuma habilidade).
- **Fighter**: 6→5 HP/nível (dominava com 98–100% e a maior profundidade).
- **Regeneração de HP** mais lenta (6–20 → 10–30 turnos por ponto): descansar custa comida de verdade.
- **Densidade de monstros**: `área/90 + prof/2` → `área/70 + prof×2/3` por nível.
- **Poções e pergaminhos** eram quase inúteis: poções não podiam ser bebidas (agora `Shift+Q`, 14 efeitos) e
  tanto poções quanto pergaminhos tinham 4–12 "cargas" (um *enchant* dava +12). Agora são de uso único.

## O que isto NÃO mede

- **Dificuldade absoluta.** Um bot que descansa sempre e cura a 40% quase nunca morre nos Dungeons (10 níveis).
  A pressão real vem da **fome** (cerca de 2400 turnos de ração, mais o que se acha) e dos branches fundos.
  Falta um bot que entre nos outros branches e uma revisão de combate (monstros com status, à distância,
  veneno/sangramento). Isso é a próxima volta de balanceamento, e só jogando de verdade dá para decidir o tom.
- **Altares, lojas, varinhas e artefatos.** Fora da política do bot.
- **Dados são 0-based.** `Rng.Dice(n)` devolve `0..n−1`, então `NdS` rende em média `N×(S−1)/2`, não
  `N×(S+1)/2`. É a convenção do motor desde o começo (acerto e CA já a assumem); os números deste documento
  e das magias foram calibrados com ela.

## Botões de ajuste (onde mexer)

| O quê | Onde |
|---|---|
| HP/nível, Mp base e por nível, tetos de skill, perks e magias iniciais | `Entities/Roles.cs` |
| Atributos, HP/nível, resistências e traços da raça | `Entities/Races.cs` |
| Custo, alcance e dados de cada magia | `Magic/Spells.cs`, `Game.Magic.Effects.cs` |
| Custo e efeito de habilidades; Vigor | `Entities/Abilities.cs`, `Game.Abilities.cs`, `Player.RecomputeMaxVigor` |
| Regeneração de HP, Mp e Vigor | `Player.HpRegenInterval/MpRegenInterval/VigorRegenInterval` |
| Chance de itens mágicos e afixos | `Items/Affixes.cs` (`ItemRoller`) |
| Monstros por nível e espalhamento | `LevelBuilder.SpawnMonsters`, `Entities/Bestiary.cs` |
| Piedade, dádivas e bônus dos deuses | `Entities/Gods.cs`, `Game.Gods.cs` |
