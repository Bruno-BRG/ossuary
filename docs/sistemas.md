# Sistemas — Ossuary

Onde mora cada um (tudo sob `unity/Assets/Scripts/Core/`):

## Dungeon (`Dungeon.cs`, `Gen/`, `GameMap.cs`, `Tile.cs`)

- 5 branches, ~54 níveis no total (teste `AllBranchDepths` garante).
- Estilos: Rooms, Cave, Maze, Barracks, Warrens, Fort (`DungeonGen` +
  `LevelBuilder.Populate` p/ monstros/loot). Todo nível: 1 escada sobe + 1
  desce, tudo alcançável (testes por estilo, 12 seeds cada).
- Tiles: parede, chão, portas (fechada/aberta/trancada/secreta), escadas,
  altar, fonte, rubble, armadilhas (`TrapTable`: spike, hole, dart, teleport,
  alarm, web, fire). Interagir com parede resolve porta/rubble/altar/fonte.
- Vocabulário de glifos (NetHack clássico, 100% ASCII; lista canônica em
  `Core/GlyphSet.cs`, coberta pelo teste `GlyphCoverage`):
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

## Personagem (`Entities/Player.cs`, `Entities/Roles.cs`, `Entities/Progression.cs`, `Game.Rpg.cs`)

- Papéis: Adventurer (padrão, kit clássico preservado), Fighter, Rogue,
  Cleric, Wizard — viés de atributos sobre a mesma rolagem base (mesma seed,
  mesma base), skills iniciais, kit/ ouro/ rações e HP/nível próprios
  (3–6), 4 títulos por nível (ex.: Apprentice→Archmage).
- `Game(seed)` = adventurer (compatível); `Game(seed, roleId)` ou
  `NewGameWithRole` p/ os demais. Título acompanha o nível via
  `Roles.TitleFor`.
- Level-up: base (HP/XP/cura) aplica na hora; cada nível também enfileira 1
  `PendingAdvances` p/ gastar via `ApplyLevelAdvance(id)` (não custa turno):
  tough (+6 HP), mighty/agile/hale (+1 Str/Dex/Con), learned/devout
  (+1 Int/Wis + skill), focused (+Magic/Search). Painel Character (`c`)
  mostra papel, pendências e escolhas feitas. Fiação no teclado vem depois.

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
- Encontro: `k` ataca, `<` foge; loot vai direto p/ mochila.

## Cidades e lojas (`Town.cs`, `Game.Overworld.cs`)

- `TownGen`: praça + fonte, 8 lojas c/ estoque por tipo, portões, guardas,
  cidadãos (dormant). Entrar = pisar na cidade; sair = porta da borda.
- Loja: preço = custo × (100 + ouro_loja/60)%; vender = metade. Ouro da loja
  limita ambos os lados. (`ShopEconomy` testa.)

## UI (`GameHud.cs`, `Ui.cs`, `UiState.cs`, `TextBuilder.cs`)

- Moldura: mapa + sidebar (Health/Energy, Depth/Turn, equipamento, AC/ouro),
  linha de status (`Dlvl HP … XP região`) e log. Minimap (`m`).
- Painéis: Inventory, Character, Help, History, Discoveries, Choice, Travel,
  Shop, Death, Win. Mira (`x`/`l`/`v`, `f` atirar, `X` trocar): cursor come
  as teclas de movimento (`NudgeTarget`, Enter confirma, Esc cancela).
