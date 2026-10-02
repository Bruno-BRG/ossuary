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

- [ ] **Desafios** (modos extras além de Normal/Classic/Hardcore): pacifista, sem equipamento, mergulho (começa fundo). `Difficulty` já é dado salvo e passa pelo replay.
- [ ] **Travel até altar/fonte/ponto marcado**: generalizar `Game.AutoStep` (já aceita qualquer objetivo) com um seletor.
- [ ] **Conquistas** locais (sem rede).
- [ ] **Efeitos sonoros** curtos estilo bip de PC dos anos 80, opcional em DisplaySettings.

## Persistência e meta-progressão

- [ ] **Mutações e Corrupção do Ossuário** (QD): fontes de corrupção (locais, itens, o Amuleto)
  dão mutações permanentes, boas e ruins (ossos extras → AC, olhos múltiplos → FOV; fragilidade, fome).
  Trade-off e builds estranhos. Compatível com Ashen/Necromancer.
- [ ] **Furtividade real** (Sil/Thief): raio de ruído do jogador; correr, lutar e abrir portas
  fazem barulho; armadura pesada piora. Dá peso ao Rogue e à skill Stealth.
- [ ] **Companheiros permanentes**: mercenário contratável na taverna, sobe de nível,
  carrega itens, comandos simples. Hoje só há aliados invocados/temporários.
- [ ] **XP gasto em skills** (opção estilo Sil), listada em `rpg.md`.
- [ ] **Desarmar armadilhas** (Ladino), prometido em `rpg.md`.

## Magia e deuses

- [ ] **Mais escolas/magias** e feitiços de corrupção ligados às mutações.
- [ ] **Interações magia × superfície** novas (gelo × raio, vapor, óleo × tocha/molotov).
- [ ] **Sacrifício e templos de deus** nos dungeons (ofertas, missões divinas, dádivas únicas).
- [ ] **Mais deuses** ou relações entre eles (rivalidade, conflito de piedade).

## Itens e crafting

- [ ] **Crafting leve**: óleo + garrafa/tocha = molotov; ossos + ferro = armas; ligado ao tema.
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
- [ ] Painel de **mutações/corrupção** e de **reputação** na tela Character.

## Técnico e qualidade

- [ ] Cobertura de testes headless para cada item novo (padrão do projeto) e `dump panels` para layout.
- [ ] Manter `Loc.cs` (EN→PT) em dia a cada texto novo; ver `idiomas.md`.

---

## Feito

_(mova para cá, com data, o que for concluído)_

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
