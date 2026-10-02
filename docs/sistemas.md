# Sistemas — Ossuary

Onde mora cada um (tudo sob `engine/Ossuary.Core/`):

## Dungeon (`Dungeon.cs`, `Gen/`, `GameMap.cs`, `Tile.cs`)

- 5 branches, ~54 níveis no total (teste `AllBranchDepths` garante).
- Estilos: Rooms, Cave, Maze, Barracks, Warrens, Fort (`DungeonGen` +
  `LevelBuilder.Populate` p/ monstros/loot). Todo nível: 1 escada sobe + 1
  desce, tudo alcançável (testes por estilo, 12 seeds cada).
- Tiles: parede, chão, portas (fechada/aberta/trancada/secreta), escadas,
  altar, fonte, rubble, armadilhas (`TrapTable`: spike, hole, dart, teleport,
  alarm, web, fire). Interagir com parede resolve porta/rubble/altar/fonte.
- Vocabulário de glifos (NetHack clássico, 100% ASCII; lista canônica em
  `Core/GlyphSet.cs`, coberta pelo teste `GlyphCoverage`). Vocabulário alvo
  com CP437 (`·` `▲` `♣` `≈`…) e fg+bg por tile: `visual.md` §3.2, fase F2:
  `#` parede (tom distingue pedra/tijolo/rocha), `.` chão, `,` chão alt,
  `I` pilar, `+` porta fechada/trancada (cor distingue), `'` porta aberta,
  `>`/`<` escada desce/sobe, `^` portal, `"` rubble, `_` altar, `{` fonte.
  Porta secreta desenha `#` (parede) até ser encontrada.
- Overworld: `~` água, `.` areia/neve/cinza/estrada (cor distingue), `"`
  grama, `t` floresta, `h` colina, `M` montanha, `,` pântano, `#` ruína de
  terreno; features em maiúsculas: `+` cidade, `^` dungeon, `R` ruína,
  `C` caverna, `m` mina, `K` fortaleza, `S` santuário, `=` ponte.
- UI: `@` jogador, `✚` mira, `✦` cursor de viagem, `▶` cursor de lista,
  `▼` marcador de encontro, `█`/`░` barras, `─│╭╮╰╯` molduras.

## Turnos e monstros (`Game.cs`)

- Ação do jogador → `EndPlayerTurn()`: turno++, fome, status, monstros por
  energia (`Speed`, 12 p/ agir), FOV, morte.
- Fome estilo NetHack: pool `Nutrient` drena/turno; comer enche; zerou =
  aviso → dano de starving. (`EatFood`, `ProcessHunger`).
- Monstro a 1 de distância ataca (alguns explodem); visão por `Def.Vision`;
  AI Ambush/Guard/Territorial patrulha ou persegue; rubble pode ser esmagado.

## Combate (`Combat/`)

- `Battles.PlayerMelee` / `Battles.MeleeAttack`: hit, dano, crit, morte.
  Kill dá XP (`AddXp`), skill Combat, dropa ouro/inventário e às vezes cadáver.
- Status: sono, stun, confusão, cegueira (FOV=1), alucinação, veneno,
  amulet of strangulation.

## Itens (`Items/`, `Game.Items.cs`)

- Kinds: Weapon, Armor, Shield, potion, Scroll, Wand, Food, Tool, Ring,
  Amulet, Gold, Rock, Corpse, Book, Gem, Ornament.
- Verbos: `g` pegar tudo na célula, `d` drop, `w`/`W` empunhar/vestir,
  `T` tirar armadura, `P`/`R` anel on/off, `r` ler, `z` zapar, `e` comer,
  `a` aplicar ferramenta (pick-axe→cavar, lock pick→abrir).
- Escolha modal (`PushChoice`) quando há >1 candidato.

## Poções e pergaminhos (`Game.Potions.cs`, `Game.Items.cs`)

- **Beber** (`Shift+Q`, `Commands.DoQuaff`): healing (6d4), extra healing (6d8), full healing (cura tudo, +2 HP máx),
  poison, sleeping (3–6 turnos indefeso), confusion, hallucination, speed (buff `haste` 25), levitation (buff, ignora
  armadilhas 40 turnos), acid, oil (derrama óleo em 3×3), see invisible, gain ability (+1 atributo), gain level.
- Poções e pergaminhos são **de uso único** (`Charges = 1`; o pergaminho lido some). Só varinhas têm cargas.
- Dados do motor são **0-based** (`Rng.Dice(n)` = `0..n−1`); ver [balance.md](balance.md).

## Superfícies e status elementais (`Surfaces.cs`, `Game.Surfaces.cs`)

- **Camada de superfície** (`GameMap.Surfaces`, esparsa; só em chão `Floor/FloorAlt`): `Water` (`≈`), `Ice` (`≡`),
  `Fire` (`▲`/`^`, cintila por hash(x,y,turno)), `Oil` (`,`), `Grass`/brush (`"`). Cores/glifos em `Theme.SurfaceStyle`
  (fundo carrega significado). O chão sob um jogador/monstro é descrito em `l`.
- **Regras** (`Game.PutSurface`): fogo não pega em água (vira vapor e molha), derrete gelo em água, queima brush
  (≥5 turnos) e óleo (≥10); gelo só congela água; água apaga fogo.
- **Tick** (`TickSurfaces`, todo turno, só no nível atual): fogo exposto a quem está nele, espalha para vizinhos
  cardinais inflamáveis (brush 70%, óleo 85%), derrete gelo vizinho e se apaga; gelo derrete em água (40 turnos);
  poças criadas por magia secam (60).
- **Condições** (`Actor.BurnTurns/WetTurns`; pílulas BURNING/WET): em chamas = 1d3 de fogo por 4 turnos (resistência
  conta; imunes e molhados não pegam fogo; água apaga); molhado (em água) = frio e raio ×1,5.
  **Gelo**: pisar escorrega ~30% (perde um turno; monstros também). Monstros evitam pisar em fogo.
- **Elementos** (`Game.Magic.Effects.cs`, `Aftermath`): dano de fogo acende o alvo; frio retarda (e congela água sob o
  alvo, prendendo-o); raio em água **conduz** pela poça conectada (até 4 casas) e atinge todos, inclusive o conjurador.
  Também vale para dados elementais de armas. `MonsterResist`: mortos imunes a veneno e meio imunes a frio; fire giant/
  tiamat imunes a fogo; ice troll/lich/wraith imunes a frio; fraquezas de −50%.
- **Magias**: *Wall of Fire* (cruz de chamas, 8 turnos), *Create Water* (5×5, 60 turnos). Fireball queima brush/óleo
  no raio; Meteor incendeia o chão todo. Armadilha de fogo agora deixa chamas e acende o jogador.
- **Geração** (`LevelBuilder.PlaceSurfaces`): poças nos Sunken Vaults e (1) nas Mines, brush nos Warrens, óleo na Spire,
  e um pouco de óleo/brush nos Dungeons. Saves passam à versão 7.

## Deuses, piedade e altares (`Entities/Gods.cs`, `Game.Gods.cs`)

- **5 deuses** (`Gods.All`): **Aurel**, a Última Lamparina (luz, misericórdia), **Khorr**, o Martelo Abaixo
  (guerra), **Veyra**, Mãe das Cinzas (fogo), **Nhal**, o Rei Afogado (morte), **Sylk**, o Quieto (sombra).
  Cada um tem gostos, desgostos, uma **dádiva** de oração e dois bônus permanentes (piedade 50 e 100).
- **Altar**: andar contra um `_` abre o menu (grátis, não gasta turno; `Panel.Altar`). O deus do altar sai de
  `Gods.AtAltar(mapa, x, y)` (função pura do lugar, nada guardado). Linhas: *Swear* (sem deus; após renunciar
  custa 150 ouro × renúncias), *Pray*, *Offer gold* (até 100 ouro → +ouro/20), *Offer an item* (valor/40,
  artefato +25), *Renounce* (pede confirmação), *Forsake X, swear to Y* (tributo 150 × (renúncias+1)).
  Cleric e Paladin começam seguindo Aurel com piedade 30.
- **Piedade** 0–200 (`Player.Piety`, `God`, `PrayerTimer`, `Renounced`): −1 a cada 250 turnos. Ganhos/perdas
  (`GodsOnKill/OnCast/OnAbility`):
  - Aurel: +2 morto-vivo morto, +1 magia Sacred; −4 matar inofensivo (nível 0), −3 necromancia.
  - Khorr: +1 por morte (+3 se o alvo é ≥2 níveis acima), −1 ilusão, −2 Vanish.
  - Veyra: +2 morte por fogo, +1 Fireball/Meteor, −1 morte por gelo.
  - Nhal: +2 morte necrótica, +1 necromancia, +1 morte por aliado, −2 magia Sacred.
  - Sylk: +2 morte furtiva (alvo dormindo/desatento/fugindo), +1 ilusão, −2 War Cry.
- **Oração** (`Pray`): em apuros (HP <⅓) cura tudo por 20 de piedade (se piedade ≥10); senão gasta a dádiva do
  deus (custo 30–60) e inicia 400 turnos de espera; cedo demais = −20 piedade e um empurrão (nunca mata).
  Dádivas: Aurel cura+mana+limpa; Khorr +1 de encantamento na arma; Veyra buff `flame` (+1d4 fogo nos golpes,
  300 turnos); Nhal 2 esqueletos aliados (200); Sylk invisibilidade 100 + Vigor cheio.
- **Bônus permanentes**: Aurel 50 regen de HP mais rápida, 100 resistência necrótica 30%; Khorr 50 +1 acerto,
  100 +2 dano; Veyra 50/100 fogo 30%/60% (100: golpes queimam); Nhal 50 necrótico 30%, 100 cura 2 HP por morte;
  Sylk 50 evasão +2, 100 monstros notam 1 casa a menos. Painel Character mostra a fé; saves v6.

## Itens mágicos (`Items/Affixes.cs`, `Items/Item.cs`)

- **Slots de armadura**: corpo (`WornArmor`), escudo, **elmo, luvas, botas, capa** (`ItemKind.Helm/Gloves/Boots/Cloak`;
  listas `Catalogue.Helms/Gloves/Boots/Cloaks`). `Player.Wear/TakeOff/WornPieces`; `W` veste na vaga
  certa (devolve a peça trocada à mochila), `T` pergunta qual tirar quando há várias. Escudo e arma de
  duas mãos não convivem. CA = soma de `Item.TotalAc` das peças.
- **Raridade** (`Item.Rarity`): Common, Magic (1 afixo ou +N), Rare (prefixo + sufixo + encantamento),
  Artifact. `ItemRoller.Roll` (no `RollLoot`): chance de Rare 2+prof% (máx 15), Magic 10+3×prof% (máx 45);
  só afeta arma/armadura/escudo/elmo/luvas/botas/capa. Cores: azul / amarelo / laranja (`Theme.ItemRaw`).
- **Afixos** (`Affixes.All`, 20): prefixos de arma (keen, brutal, flaming, frozen, venomous, vampiric), de armadura
  (sturdy, fire/frost/storm-warded, venom-proof) e sufixos (of the fox/bear/owl/sage/mage, of life, of vigor,
  of shadows, of ruin). Tudo vira um `ItemMods` somado em `Player.Gear` e aplicado por
  `Player.RefreshGear()` (atributos entram como diferença, então treino e equipamento não se sobrescrevem;
  também alimenta HP, Mp, Vigor, evasão e resistências). Dado elemental da arma e lifesteal rodam em
  `Game.MeleeProcs` (corpo a corpo e habilidades).
- **Identificação**: item mágico achado aparece como *magical long sword*; empunhar/vestir revela nome, bônus e
  (artefato) a lore; o pergaminho de identify revela tudo que se usa.
- **Enchant**: *scroll of enchant weapon* (+1 na arma) e *scroll of enchant armour* (+1 na peça menos
  encantada), teto +5. Valor de loja: `Item.TradeValue`.
- **Artefatos** (`Artifacts.All`, um por branch, nível fixo): Veil of the First Cell (Dungeons 8), Dwarfdeep Cleaver
  (Mines 6), Rat King's Tooth (Warrens 7), Crown of the Drowned King (Vaults 10), Ashfall (Spire 13).
  `LevelBuilder.PlaceLoot` os coloca; ficam sem identificar até serem usados.
- Saves passam à versão 5.

## Personagem (`Entities/Player.cs`, `Entities/Roles.cs`, `Entities/Progression.cs`, `Game.Rpg.cs`)

- **Criação** (`Panel.Create`, `Session.CreateKey`): nome (≤16 ASCII,
  `Heroes.CleanName`), raça, classe, confirmação. Toda expedição nova abre aqui
  (op `new` com `create`); `Game.NewHero(seed, nome, raça, papel)` monta o herói.
  Nome/raça/papel vão no `SaveData` (não são teclas do replay); saves antigos
  carregam como Wanderer humano aventureiro.
- Raças (`Entities/Races.cs`): Human, Dwarf, Elf, Halfling, Orc, Gnome, Ashen —
  viés de atributo (sobre a mesma rolagem), HP/nível, ouro, alinhamento e
  skills iniciais e **traços ativos** (dados em `RaceDef`): Human +1 avanço
  grátis; Dwarf veneno 50%; Elf Mp +25%, evasão +1; Halfling evasão +2; Orc
  regen ×2, veneno 25%; Gnome Mp +15%; Ashen fogo 50%, gelo −25%.
  `Player.CharName` é o nome; `Name` segue "you" no log.
- **Atributos derivados** (`Entities/Stats.cs`, `Player`, `Game.Rpg.cs`):
  - `DamageType` (Physical/Fire/Cold/Lightning/Poison/Necrotic/Holy) e
    `Player.ResistPct`/`ResistDamage` (hoje: armadilhas de fogo/dardo e veneno).
  - **Mp**: `RoleDef.MpBase/MpPerLevel/MpStat` + atributo acima de 10 + Magic/10,
    × `MpPct` da raça. Sobe e enche no level-up; barra `MP` no sidebar. Ainda não
    há magias para gastá-lo (parte 3).
  - **Regeneração** (`Game.Regenerate`, sem RNG): HP 1 a cada 6–20 turnos
    (nível/Con, ÷ `RegenPct`); parada por fome ou veneno. Mp 1 a cada 4–20 (Int/Wis/Magic).
  - **Graus de skill** (`SkillRanks`): Novice 0, Trained 25, Skilled 50, Expert 75,
    Master 100. Combat dá +grau no acerto; Dodging +grau na evasão.
    **Teto por classe** (`RoleDef.SkillCaps`, ex.: Fighter Magic 40) aplicado em `GainSkill`.
  - Saves passam a versão 3 (replay depende das regras e do catálogo de itens); saves antigos deixam de carregar (versão atual: 8).
- Papéis: Adventurer (padrão, kit clássico preservado), Fighter, Rogue,
  Cleric, Wizard, Ranger, Paladin, Necromancer — viés de atributos sobre a mesma rolagem base (mesma seed,
  mesma base), skills iniciais, kit/ ouro/ rações e HP/nível próprios
  (3–6), 4 títulos por nível (ex.: Apprentice→Archmage).
- `Game(seed)` = adventurer (compatível); `Game(seed, roleId)` ou
  `NewGameWithRole` p/ os demais. Título acompanha o nível via
  `Roles.TitleFor`.
- **Level-up** (`Entities/Progression.cs`): base (HP/XP/cura) aplica na hora e cada
  nível enfileira 1 pick (`PendingAdvances`; humanos começam com 1). O painel
  **Advancement** (`Shift+C`, abre sozinho ao subir de nível) lista só os perks
  disponíveis (`Progression.Available`): filtrados por classe, nível mínimo, skill
  mínima, perk pré-requisito, Mp (perks de magia) e rank máximo. Setas ou letra, Enter pega.
  - Atributos (ranks até 4–5): tough, mighty, agile, hale, learned, devout, focused.
  - Gerais: lucky (evasão), iron-will (veneno), gourmand (fome ÷2), quick-learner (+15% XP),
    vigorous (+Vigor e regen).
  - Marciais: weapon-master (+1 acerto/dano por rank), shield-wall, aura (Paladin),
    light-feet (monstros notam de mais perto), keen-eye (mísseis).
  - Magia: mind-expansion (+6 Mp), focus (−5% falha), spell-power (+2 dano), quick-recovery.
  - Perks que **ensinam habilidades** (abaixo).
- **Vigor** (`Player.Vigor/VigorMax`, barra `VG` quando há habilidades): 8 + 2×nível + (Con−10)/2
  (+8 por *vigorous*); 1 ponto a cada 5 turnos (menos com *vigorous*); enche ao subir de nível.
- **Habilidades ativas** (`Abilities.cs`, `Game.Abilities.cs`; `Shift+V`): Power Strike (×2),
  Backstab (×3 em alvo dormindo/fugindo/confuso/desatento, senão ×2), Holy Strike (+2d6 sagrado,
  morto-vivo ×2), Shield Bash (exige escudo; alvo perde ~2 turnos), Cleave (todos adjacentes),
  Aimed Shot (+4 acerto, ×2 dano, alcance 8), Second Wind (¼ do HP), Lay on Hands (½ do HP e
  limpa veneno), War Cry (vivos a 6 casas fogem), Vanish (invisível 12 turnos). Sem Vigor ou
  alvo inválido: recusa **sem gastar turno**. Cada classe marcial começa com uma
  (`RoleDef.StartPerks`: Fighter power-strike, Rogue backstab, Ranger aimed-shot, Paladin lay-on-hands).
- Painel Character mostra perks com rank; saves passam à versão 4.

## Magia (`Magic/Spells.cs`, `Game.Magic.cs`)

- `SpellDef` é dado (id, nível 1–5, escola, custo Mp, alvo, alcance, raio, invocações);
  efeitos em `Game.Magic.Effects.cs` por id (`ApplySpell`). **32 magias em 6 escolas**:
  - **Evocation**: Magic Missile, Shocking Grasp (adjacente), Frost Ray, Fireball
    (área r2), Lightning Bolt (perfura a linha), Chain Lightning (salta até 3), Meteor (área r3).
  - **Conjuration**: Familiar, Summon Beast (escala com nível), Blink, Teleport.
  - **Alteration**: Ward (AC+3), Haste (monstros agem em turnos alternados), Slow,
    Clairvoyance (revela o mapa), Stone Skin (AC+6).
  - **Illusion**: Sleep, Confuse, Invisibility, Charm Monster (vira aliado temporário).
  - **Necromancy**: Drain Life (cura metade), Raise Skeleton, Fear, Finger of Death, Army of Bones (3 esqueletos).
  - **Sacred**: Cure Wounds, Bless (+2 acerto), Smite (morto-vivo ×2), Cleanse, Turn Undead, Greater Heal, Revive (desfaz a próxima morte, 300 turnos).
  - Alvos: `Self`, `Monster` (hostil), `Cell` (célula vazia), `Area` (célula visível),
    `Line` (a partir de você, parado por paredes). Mortos-vivos ignoram dano necrótico.
- **Buffs** (`Player.Buffs`, id → turnos): ward, stone-skin, haste, bless,
  invisibility, revive; aparecem como pílulas na barra de status.
- **Aliados** (`Monster.Ally`, `SummonTurns`; `AllyTurn`): atacam o hostil visível
  mais próximo, senão seguem você; andar contra um aliado troca de lugar; hostis adjacentes a um
  aliado (e longe de você) atacam o aliado. Invocados somem ao expirar (`charmed` volta hostil);
  sumem ao mudar de nível. Magias hostis recusam aliados. Kills de aliados dão XP ao jogador.
- **Medo/lentidão** em monstros (`FearTurns`, `SlowTurns`): fugir do jogador / velocidade pela metade.
- `Player.Spells` guarda as magias aprendidas; cada papel tem `StartSpells`
  (Wizard: magic-missile, ward; Cleric/Paladin: cure-wounds; Necromancer: sleep).
- **Conjurar**: `Shift+Z` abre o painel (`Panel.Spells`; setas ou letra a–z, Enter,
  Esc). Magia com alvo abre a mira (`TargetingMode.Cast`) já no monstro visível
  mais próximo. Sem mana, alvo inválido ou sem linha: recusa **sem gastar turno**.
- **Falha** (`Spells.FailPct`): 20 + 10×nível − 3×(Int/Wis−10) − Magic/4 + 2×AC da
  armadura + 3×AC do escudo (0–95%). Falhou: gasta metade do Mp, 1 turno.
- **Aprender** (`r` num livro → `StudyBook`): uma tentativa por magia
  desconhecida, 2×nível turnos cada; chance `Spells.LearnPct`; falha = tontura
  (confusão), nunca dano. Recusa com inimigo à vista; classes sem Mp não aprendem.
  10 livros (`Spells.Books`): *a spellbook* (inicial do mago), *a book of prayers*,
  *a book of shadows*, e como loot *a tome of evocation*, *a codex of storms*, *a tome of conjuration*,
  *a book of wards*, *a book of illusions*, *a grimoire of the dead*, *a book of mercy*.
- Teclas do painel de magias passam **sem** o mapa de atalhos (letras viram
  seleção, não movimento) e entram no log do replay normalmente.

## Andar sozinho (`Game.Explore.cs`, `Game.Repeat.cs`, `Commands.DoAutoWalk`)

- **Explorar** (`t`): BFS sobre células já vistas até o objetivo mais próximo: pilha de itens não visitada ou
  célula andável com vizinho nunca visto (`ExploreGoal`). Células servidas entram em `_exploreDone`, o que garante
  término. **Escada** (`` ` ``): `StairsStep`. **Descanso** (`Shift+S`): `Wait` até HP/Mp cheios.
- Cada um é um laço de turnos normais (limite 600; descanso 3000) que para com hostil à vista, dano, qualquer
  `Say` novo (`Game.Said`), item/escada sob os pés ou troca de nível. Sem RNG próprio: o replay reproduz.
- **Tecla segurada** (`Game.CanKeepWalking`, `Session.KeyRepeat`): ver `arquitetura.md`.

## Fim de run: causa, morgue e histórico (`Morgue.cs`, `Game.Death.cs`, `SaveStore`)

- `Game.HurtBy(causa)` marca quem feriu o jogador por último (monstros, armadilhas, veneno, fogo, fome…);
  `CheckDeath` grava `DeathCause`. `quit` marca `Abandoned`.
- `Morgue.Summarize/Text` são funções puras do jogo terminado. `Session.RecordRun` (uma vez por run, nunca em
  replay) grava `morgue/*.txt` e anexa a `history.json` no diretório de dados (`OSSUARY_DATA` nos testes).

## Cemitério (`Bones.cs`)

- Morrer em dungeon, nível ≥ 2 (`Game.LeaveBones`), grava `Bones` via `SaveStore.WriteBones`. `Game.Graveyard` é fixado
  no início da run (e vai no save), então o replay encontra as mesmas sombras. `RaiseBones` roda só na primeira geração
  do nível, com Rng privado (`seed ^ hash(branch, depth)`): 60% de chance, longe da entrada. A sombra é um
  `wandering wraith` reescalado (`BonesKey` marca quem é); destruí-la entra em `Game.LaidToRest` e o host remove o arquivo.

## FOV / pathfinding (`Fov.cs`, `Pathfinder.cs`)

- FOV com sombra (raio 10, +2 com ring of warning), simétrico, testado.
  Ray casting simétrico + revelação de canto — ver
  [`renderer.md`](renderer.md) p/ por que não é shadowcasting com slope.
- `FindPath` (A*) + `FlowField`; porta vira passagem, bolso selado = sem rota.

## Overworld (`World/`, `Game.Overworld.cs`)

- 96×60, regiões nomeadas com perigo/profundidade, estradas conectando,
  ≥3 cidades, 1 dungeon por região, dia/noite (`AdvanceTime`).
- Andar revela (`Discover`); `O` + Enter = viagem longa (custa horas, pode
  encontrar monstro ou chegar em cidade/dungeon).
- Encontro (monstro bloqueia a estrada): **Enter / Espaço / `K` / `F` atacam,
  `R` ou `<` fogem**. `K` sem shift é "andar para norte" nas teclas vi, então
  `Session.KeyCore` remapeia pelo caractere digitado (o código já foi
  reescrito pelos key bindings: `KeyK` chega como `ArrowUp`). Andar e viajar
  (`O`) são recusados com a dica enquanto o monstro estiver lá. Matar dá XP,
  anuncia level-up e joga o loot direto na mochila. Testes:
  `DesktopTests.RoadEncounter`.

## Cidades, andares e gente (`Town.cs`, `TownText.cs`, `Game.Town.cs`)

A cidade é **vertical**: um `Town` guarda um `GameMap` por andar (`Floors`,
`z = 0` rua, positivo = andares de cima, negativo = porões). Todo prédio tem a
mesma pegada em todos os andares, então a escada põe você no mesmo (x, y).
`>` desce e `<` sobe (`Game.TownStairs`); as escadas alternam entre dois cantos
(A em z par, B em z ímpar) para a que você pisa nunca ser a que continua.
Cabeçalho mostra `▲2`/`▼1`; o título do mapa vira o nome do prédio.

- **Geração** (`TownGen`): muralha com 4 portões, cruz de ruas, praça com
  fonte, lotes de 13×11 dos dois lados da rua principal. Tamanho pela
  população: `hamlet` (8 lotes) / `village` / `town` / `city` (12 lotes).
  Sempre há taverna, loja geral, ferraria e templo; o resto é sorteado.
  Layouts em coordenadas "relativas à porta" (u = largura, v = fundo), então
  o mesmo molde serve para lotes ao norte e ao sul.
- **Prédios**: Ferraria (armas; **afiar** arma até +3), Armaria (armaduras;
  **reforçar** armadura até +3), Alquimista (poções, laboratório no porão),
  Torre do Mago/Empório (varinhas e pergaminhos, **avaliar** itens, 3 andares),
  Loja geral, Taverna (cerveja, refeição, notícias, porão), Estalagem (**dormir**
  = cura tudo e avança até de manhã, 2 andares de quartos), Templo (**curar**,
  **remover males**, oferenda que dá piety; galeria em cima, cripta embaixo),
  Guilda (quadro de avisos, "pergunte sobre o Ossuary" conta a história e a
  profundidade do Amuleto), Biblioteca (livros, avaliar), Quartel (guardas, celas
  no porão), Torre de vigia, casas, barracas de mercado, cemitérios.
- **Gente** (`Monster.Townsperson`): moradores, crianças, guardas, bardo,
  mendigo, bêbado, aventureiros, eruditos, prisioneiros, cães e gatos. Falas e
  rumores em `TownText` (EN com PT ao lado via `L(en, pt)`). Andam por hash do
  turno (`TownsfolkTurn`), nunca pelo `Rng` do jogo, e nunca atacam.
- **Interação**: esbarrar numa pessoa fala; esbarrar num balcão, quadro de
  avisos ou altar chama quem trabalha lá. Quem só vende abre a loja direto;
  quem faz mais abre o painel de **serviços** (`Panel.Service`, letras a..z
  escolhem). Dentro dos muros `f z Z V k` são recusados e `Attack` em morador
  também (a Guarda enforcaria você).
- **Determinismo**: a cidade é função da seed do mundo e do lugar
  (`Rng(seed ^ hash(nome@x,y))`), é guardada em `_towns` e é a mesma em toda
  visita; entrar e sair não consome o `Rng` da simulação (`TownIsStable`).
- Loja: preço = custo × (100 + ouro_loja/60)%; vender = metade. Ouro da loja
  limita ambos os lados. Estoque vem identificado. (`ShopEconomy` testa.)
- Testes: `TownVerticality` (pares de escadas, alcance em todos os andares,
  ninguém em cima de parede ou de outro), `TownServices`, `TownIsStable`,
  `DesktopTests.TownFlow`. `headless.ps1 dump town` mostra a cidade, cada andar
  dos prédios e o painel de serviços.

## UI (`GameHud.cs`, `Ui.cs`, `UiState.cs`, `TextBuilder.cs`)

- Moldura: mapa + sidebar (Health/Energy, Depth/Turn, equipamento, AC/ouro),
  linha de status (`Dlvl HP … XP região`) e log. Minimap (`m`).
- Painéis: Inventory, Character, Help, History, Discoveries, Choice, Travel,
  Shop, Death, Win. Mira (`x`/`l`/`v`, `f` atirar, `X` trocar): cursor come
  as teclas de movimento (`NudgeTarget`, Enter confirma, Esc cancela).
