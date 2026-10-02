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

- [ ] **Mais escolas/magias** e feitiços de corrupção ligados às mutações.
- [ ] **Interações magia × superfície** novas (gelo × raio, vapor, óleo × tocha/molotov).
- [ ] **Sacrifício e templos de deus** nos dungeons (ofertas, missões divinas, dádivas únicas).
- [ ] **Mais deuses** ou relações entre eles (rivalidade, conflito de piedade).

## Itens e crafting

- [ ] **Mais artefatos e conjuntos** além dos 5 (um por branch).
- [ ] **Itens de corrupção** (relíquias que dão mutação em troca de poder).

## Mundo, cidades e missões

- [ ] **Facções e reputação**: templos, guilda, Guarda, culto de Nhal. Matar/roubar muda
  preços, hostilidade e acesso a serviços.
- [ ] **Contratos e missões secundárias** no quadro da Guilda ("mate X no branch Y", "traga Z").
- [ ] **Eventos de overworld** além dos encontros de estrada (acampamentos, caravanas, ruínas com escolha).
- [ ] **NPCs com rotina e diálogos** que reagem à reputação.

## Geração de níveis

- [ ] **Vaults e salas especiais**: cofres trancados com puzzle, salas-armadilha, templos.
- [ ] **Branches opcionais de desafio** (estilo DCSS) com recompensa própria.
- [ ] **Mais estilos de level** além dos 6 atuais (Rooms, Cave, Maze, Barracks, Warrens, Fort).

## Monstros e combate

- [ ] **Bestiário expandido** com comportamentos próprios por branch (ver `lore.md`).
- [ ] **Chefes com mecânicas** (fases, invocações, arena).
- [ ] **Facções de monstros** que brigam entre si.

## Interface e visual

- [ ] Animação de água e tiles de mapa quadrados (já listados como evolução em `alpha.md`).
- [ ] Painel de **reputação** na tela Character (quando as facções existirem).

## Técnico e qualidade

- [ ] Cobertura de testes headless para cada item novo (padrão do projeto) e `dump panels` para layout.
- [ ] Manter `Loc.cs` (EN→PT) em dia a cada texto novo; ver `idiomas.md`.

---

## Feito

_(mova para cá, com data, o que for concluído)_

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
  numa fonte do dungeon (limpa, amarga ou contaminada), o Amuleto na mochila (+1/40 turnos), necromancia (+1/cast até 60) e a nova
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
