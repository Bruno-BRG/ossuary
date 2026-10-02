# A fazer — Ossuary

Acompanhamento vivo do que falta no jogo. Fonte única de progresso: **toda sessão
lê este arquivo antes de começar e o atualiza ao terminar** (ver `AGENTS.md`).

## Como usar

- Estados: `[ ]` pendente · `[~]` em andamento · `[x]` feito (com data e onde mora) · `[-]` descartado (com motivo).
- Ao concluir um item: marque `[x]`, anote a data e o arquivo/sistema (`Game.<Assunto>.cs`),
  e documente o sistema em `sistemas.md` / `controles.md` como de costume.
- Ideia nova entra na categoria certa, em uma linha, com o porquê.
- Prioridade é a ordem dentro da categoria (de cima para baixo), salvo nota em contrário.
- Referências de design: Caves of Qud (QD), DCSS, Brogue, Cogmind, Sil, NetHack, Angband.

---

## Controles e QoL

- [ ] **Efeitos sonoros** curtos estilo bip de PC dos anos 80, opcional em DisplaySettings.

## Persistência e meta-progressão

- [ ] **XP gasto em skills** (opção estilo Sil), listada em `rpg.md`.

## Magia e deuses


## Geração de níveis

- [ ] **Branches opcionais de desafio** (estilo DCSS) com recompensa própria.
- [ ] Animação de água e tiles de mapa quadrados (já listados como evolução em `alpha.md`).

## Técnico e qualidade

- [ ] Cobertura de testes headless para cada item novo (padrão do projeto) e `dump panels` para layout.
- [ ] Manter `Loc.cs` (EN→PT) em dia a cada texto novo; ver `idiomas.md`.

---

## Feito

_(mova para cá, com data, o que for concluído)_

- [x] **Cofres, caches armadilhados e dois estilos novos de nível** (2026-10-02). `Gen/Vaults.cs` escava uma câmara 4×3 em rocha não usada (nunca muda o que a escada alcança):
  **cofre trancado** (porta trancada, ouro + 3 itens de loot ricos; a **chave de latão** — `Crafted.BrassKey` — está com um monstro do nível, abre qualquer porta trancada uma vez) e
  **cache escondido** (porta secreta, chão com armadilhas ~45%, loot no fundo). `LevelBuilder.PlaceVaults`. Estilos **Ruins** (salas desabadas: brechas, entulho, colunas) e
  **Catacombs** (grade de passagens com criptas e túmulos) usados nas branches (`Dungeon.BuildBranches`). Testes `VaultsAndKeys`, `LevelRuins`, `LevelCatacombs`.

- [x] **Mundo vivo: reputação, contratos, eventos de estrada e rotina** (2026-10-02). Salvamento passa à **versão 10**.
  - **Reputação** (`Game.Reputation.cs`): quatro casas — a Guarda, o Templo, a Guilda e o Culto dos Afogados — de −100 a 100 (*revered/trusted/known/distrusted/hated*).
    Efeitos: preços de loja (Guilda, 80–120%), taxas do templo (Templo), estalagem (Guarda: ≥25 mais barata; ≤−25 recusa), e o Culto vende *grave-water* (poção de mutação)
    a quem tem ≥25. Fontes: oferendas/cura/purga (Templo), serviços (Guilda/Guarda +10), enterrar/saquear corpos, sacrifícios (Culto), jurar a Nhal, vencer bandidos.
  - **Contratos** (`Game.Contracts.cs`): o quadro da Guilda mostra 3 ofertas por cidade e semana (função de nome da cidade + `World.Day/7`): *Hunt N monstros num branch* ou *Reach depth N*.
    Até 3 ativos; progresso conta sozinho (`ContractKill`, `ContractDepth`); entrega no quadro paga ouro e +10 de reputação. Conquista *Hired Hand*.
  - **Eventos de estrada** (`Game.Events.cs`): 7% por passo livre — acampamento, caravana, ruína, santuário, pedágio, corpo — com escolhas e preços, reaproveitando o painel de serviços.
  - **Rotina e reação**: à noite os moradores vão para casa e ficam lá (`GoHomeAtNight`, sem Rng); guardas, padres e comerciantes falam diferente conforme a reputação,
    padres notam corrupção e todos notam mutações (`TownText.Reaction`). Ficha Character mostra reputação e serviços ativos.
  - Testes `ReputationAndJobs`, `RoadEvents`, `TownRoutine`.

- [x] **Facções de monstros** (2026-10-02). `Game.Factions.cs`: *Greenskin* (kobold, orc…) ↔ *Deepfolk* (dwarf, gnome, hobbit) e *Dead* (mortos-vivos) ↔ *Wild* (animais)
  brigam à vista (`FightRival`, até 6 casas, só quando o herói não é o alvo mais próximo). Aliados, cidadãos, companheiros e chefes não tomam lado.
  Teste `FactionWar`.

- [x] **Chefes com mecânicas** (2026-10-02). `Game.Bosses.cs`: um chefe no fundo de cada branch (`Bosses.All`; gerado na 1ª visita com Rng privado, longe da escada,
  com a fala de entrada). **Gaoler** (Dungeons 10: corrente que puxa o herói; abaixo de 50% chama cães), **Stone Warden** (Mines 8: pancada no chão que atordoa;
  mais rápido), **Rat King** (Warrens 9: corte de ratos, até 6; fase 2 com plague rats), **Drowned King** (Vaults 12: alaga o chão e dá choque em quem está na
  água), **Ashen Regent** (Spire 15: explosão de fogo ao redor; fase 2 chama wisps). Fase 2 abaixo de 50% de vida, fala própria; ao morrer paga ouro + 2 poções;
  conquistas *Boss Slayer* e *Kingslayer*. Teste `BossFights`.

- [x] **Bestiário por branch com hábitos próprios** (2026-10-02). `MonsterDef.Branch/Trait` e `Game.Traits.cs`: 9 monstros nativos — *gaol hound* (Dungeons, caça em
  matilha: +4 de velocidade com companheiro), *cave bat* (Mines, voo errático), *ore golem* (Mines, pancada que atordoa), *plague rat* (Warrens, mordida
  apodrece/envenena), *rat swarm* (Warrens, se multiplica até 8), *drowned dead* e *tide wraith* (Vaults, se curam na água), *ember wisp* (Spire, explode em fogo)
  e *ash wraith* (Spire, incendeia). `SpawnTable(depth, rng, branch)` filtra por branch. Teste `BranchMonsters`.
  **Correção de bug antigo:** `TrapTable`/`GroundItems` são estáticos por número de mapa e vazavam entre jogos da mesma seed (o 1º bot jogava diferente dos seguintes);
  `LevelBuilder.Populate` agora os limpa ao gerar o nível.

- [x] **Mourne, rivalidades, sacrifício e provações dos deuses** (2026-10-02). Sexto deus: **Mourne, a Costura Chorosa** (carne e mudança: gosta de mutações,
  odeia purgar; dádiva = mutação sempre boa; piedade 50 → mais boas, 100 → veneno 30%). Cada deus tem um **rival** (`GodDef.Rival`: Aurel↔Nhal,
  Khorr↔Sylk, Veyra↔Mourne): trocar para o rival custa o dobro e, no altar do rival, *Defile the altar* dá +8 de piedade (40% de maldição
  e +5 corrupção) e deixa o altar morto. Novas linhas no altar: **Sacrifice a corpse** (valor pelo nível; Nhal ×2, Aurel pune) e **Trial**
  (5 feitos que o deus gosta → dádiva única: atributo, +6 PV, mutação). Teste `GodsExpanded`.

- [x] **Sete magias novas (corrupção e superfícies)** (2026-10-02). *Ice Lance*, **Steam Burst** (ferve a água: dano maior em molhados e some com a poça),
  **Create Oil** (acende com fogo), **Ossify** (CA +4, +2 corrupção), **Reshape Flesh** (uma mutação por 10 de corrupção), **Marrow Bolt** (necrótico, +2 corrupção) e
  **Purify** (−15 corrupção). Entraram nos livros de evocação, conjuração, mortos e misericórdia. Total: 39 magias. Teste `NewSpells`.
  Ideias futuras: escola nova, gelo × raio.

- [x] **Mais artefatos, conjuntos e relíquias de corrupção** (2026-10-02). `Artifacts.All` foi de 5 para 13 (um por nível em cada branch). **Conjuntos**
  (`ArtifactSets`): *The Drowned Court* (Crown, Tidecaller's Gauntlets, Brinewalkers) e *The Ashen Regalia* (Ashfall, Mantle of Ash, Cinder Plate); 2 peças =
  1º bônus, 3 = 2º (somados em `Player.Gear`). **Relíquias** (`Corrupts`): *Hollow Ribs*, *Gravedigger's Spade*, *Gnawed Cowl*: fortes, mas cada
  uma vestida dá +1 corrupção a cada 25 turnos (`Game.WornRelics`). Mostrados na ficha Character. Teste `SetsAndRelics`.

- [x] **Crafting leve** (2026-10-02). `Shift+B` (`Game.Crafting.cs`, `Items/Crafted.cs`): receitas como dados — **molotov** (poção de óleo + vela),
  **bone blade** (lâmina + restos; vem *vampiric*), **bone-studded armour** (armadura leve + 2 restos, +1) e **extra healing** (2 curas).
  O molotov é aplicado com `a` e jogado (novo `TargetingMode.Throw`, alcance 7): fogo no alvo e nas 4 vizinhas. Defs fora das tabelas de loot.
  Teste `CraftingFlow`.

- [x] **Desafios: Dive e Naked** (2026-10-02). Novos valores de `Difficulty` escolhidos na criação: **Dive** (começa no nível 5, nível 4, 2 poções, `ApplyChallenge`)
  e **Naked** (sem arma/armadura/escudo, +1 avanço). Pontos ×2 (`Difficulties.ScoreFactor`). Teste `Challenges`. Ideia futura: Pacifista.
- [x] **Travel até altar/fonte** (2026-10-02). `Shift+Backquote` (`~`): `Game.FeatureStep`/`IsFeatureSpot` (fonte ou chão ao lado de altar lembrado), reaproveitando `AutoStep`.
  Teste `FeatureTravel`. Ainda sem pontos marcados pelo jogador.

- [x] **Companheiros permanentes** (2026-10-02). `Game.Companions.cs`: a taverna contrata um mercenário (*sellsword / shield-bearer / cutthroat*,
  `100 + 40×nível` de ouro); é um aliado sem timer que atravessa toda escada, sobe de nível com o herói (mantendo a fração de vida) e,
  se cair, acabou (`ReapCompanions`). Aparece na ficha Character. Teste `Companions`. Pendente: inventário/ordens (ficar/seguir), mais de um
  companheiro, arqueiros.

- [x] **Mutações e Corrupção do Ossuário** (2026-10-02). `Entities/Mutations.cs` (16 mutações: boas, mistas e más; números em `ItemMods` somados
  em `Player.Gear` + visão/fome/ruído) e `Game.Corruption.cs`: `Player.Corruption` 0–100, **uma mutação a cada 20 pontos**. Fontes: **Shift+E**
  numa fonte do dungeon (limpa, amarga ou contaminada), o Amuleto na mochila (+1/40 turnos), necromancia de nível 4+ (+1/cast até 40) e a nova
  *potion of mutation*. O templo vende *Purge the Ossuary from me* (−30, remove a pior mutação mais nova). Aparece na ficha Character, no
  morgue e na conquista *Mutant*. Teste `CorruptionMutations`. Pendente: relíquias que dão mutação em troca de poder (Itens).

- [x] **Furtividade e ruído** (2026-10-02). `Game.Stealth.cs`: o raio em que um monstro percebe você é `Vision − Stealth/25 − light-feet×2 − Sylk + ruído`.
  Ruído da ação: luta +3, magia/varinha/tiro/porta +2, armadura pesada ao andar +1/+2 (chain/splint, plate), parado ou buscando −2.
  Stealth sobe ao passar despercebido. Sem Rng. Salvamento passou à **versão 9** (saves antigos deixam de carregar). Teste `StealthNoise`.

- [x] **Armadilhas achadas e desarmar** (2026-10-02). `Game.Traps.cs` + `TrapTable.Reveal`: antes uma armadilha "achada" não ficava marcada.
  Agora `s` e a percepção passiva (`SenseTraps`: Search/2 + 25 do Ladino, por hash da posição, sem Rng) revelam; achadas aparecem como `^`,
  o auto-explore as evita e `Shift+A` as desarma (35 + Dex×2 + Search/2, +30 Ladino; falha pode dispará-la). Teste `TrapsFlow`.

- [x] **Conquistas locais** (2026-10-02). `Core/Achievements.cs`: 18 conquistas como funções puras do estado (`Game.CheckAchievements`,
  a cada turno e na vitória); o anúncio vai direto ao log sem mexer em `Game.Said` (não altera auto-walk/replay). O host guarda
  `achievements.json` (id → data) e o menu tem o painel *Achievements*. Testes `AchievementsEarned` e `AchievementsFlow`.
  Ideia futura: mais conquistas (por classe, por deus, por branch).

- [x] **Seed diária com placar local** (2026-10-02). `Core/Daily.cs` (seed FNV-1a da data UTC + herói fixo pela seed), botão
  *Desafio diário* no título (`op:new, daily:true`; campo novo nos três lados do protocolo), `Session.NewDaily`. A run grava
  `RunRecord.Daily`; em *Past runs* a tecla `D` mostra o placar diário (melhor pontuação primeiro). Teste `DailyFlow`.

- [x] **Modos de dificuldade + save-and-quit** (2026-10-02). `Core/Difficulty.cs`: Normal, **Classic** (sem fome) e **Hardcore**
  (um save só, gravado ao sair pelo menu e apagado ao retomar; sem quicksave). Escolhido na tela de criação (◄► no passo
  de confirmação), vai em `SaveData.Difficulty` e no registro da run (pontos ×1,5 / ×0,67). Teste `DifficultyFlow`.

- [x] **Cemitério / Bones** (2026-10-02). `Core/Bones.cs`: quem morre a partir do nível 2 de um dungeon deixa `Bones` (nome, classe,
  causa, equipamento) em `bones.json` (um por nível, máx. 100). Em runs futuras, ao gerar esse nível pela primeira vez,
  60% de chance de um *shade of <nome>* (Unique, morto-vivo, escala com o nível do herói, guarda o tumulo com o equipamento).
  Rng próprio: a fase e o Rng do mundo não mudam. O save carrega o cemitério do início da run (`SaveData.Bones`) para o replay.
  Matar a sombra apaga o bones. Testes `BonesShades` e `BonesFlow`.

- [x] **Histórico de runs** (2026-10-02). Menu (`Esc`/`F2`) → *Past runs* (`Panel.Runs`, `Ui.DrawRunsPanel`, `Session.RunsKey`):
  lista as runs terminadas, mais novas primeiro, com causa, classe, profundidade e pontos. Dados lidos de `history.json`.

- [x] **Morgue file** (2026-10-02). Ao morrer, vencer ou abandonar: `Core/Morgue.cs` (`RunRecord`, `Summarize`, `Text`) e
  `Game.Death.cs` (`DeathCause`, `HurtBy`); o host grava `morgue/<data>-<nome>.txt` e acrescenta ao `history.json`
  (`SaveStore.WriteRun`, máx. 200). A tela de morte mostra a causa. Base do histórico de runs e do Cemitério.

- [x] **Auto-explore** (`t`), **travel até a escada** (`` ` ``) e **descanso até curar** (`Shift+S`) (2026-10-02).
  `Game.Explore.cs` (BFS sobre células vistas, `ExploreGoal`, `StairsStep`) e `Commands.DoAutoWalk`: laços de
  turnos comuns, então o replay do log continua exato. Param ao ver hostil, levar dano, qualquer mensagem, itens
  ou escada sob os pés; recusas não gastam turno. Testes em `Tests/FeatureTests.cs` e `DesktopTests`.
  Pendente: travel até altar/fonte/ponto marcado (ficou em Controles e QoL).

- [x] **Segurar tecla de direção repete o movimento** (2026-10-02). `Game.Repeat.cs` (`CanKeepWalking`,
  `HostileInView`), `Session.KeyRepeat`, flag `repeat` no protocolo/`main.ts`. Para sozinho com hostil à vista,
  item/escada/altar/fonte/porta sob os pés, dano, mensagem nova ou parede. Teste `HeldKeyWalking`.
