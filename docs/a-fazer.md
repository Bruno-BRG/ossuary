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

- [ ] **Segurar tecla de direção repete o movimento** (como Caves of Qud). Hoje
  `desktop/src/main.ts` descarta `event.repeat`. Plano: aceitar repeat só para teclas de
  movimento e só quando não há ação em andamento (`busy`), mantendo a regra de uma ação
  por vez, sem fila de turnos. A repetição deve parar sozinha ao surgir hostil à vista,
  dano, item/escada sob os pés, porta ou mudança de painel. Atenção ao `Session.Key`
  (modais, mira e painéis não repetem). Atualizar `arquitetura.md` (regra do autorepeat) e `controles.md`.
- [ ] **Auto-explore** (`o`/tecla a definir): usa `Pathfinder`/`FlowField` para a fronteira
  não explorada mais próxima; para ao ver hostil, item ou ao ser ferido.
- [ ] **Travel até escada/local** (`_` ou `` ` ``): ir até `<`/`>`, altar, fonte ou ponto marcado com A*.
- [ ] **Descanso até curar** (`Z`/`5`-longo estilo DCSS), interrompido por hostil ou fome.
- [ ] **Morgue file**: ao morrer ou vencer, exportar `.txt` com o resumo da run (build, deuses, kills, inventário, log final).
- [ ] **Seed diária** com placar local (a determinismo por seed já permite).
- [ ] **Modos de dificuldade**: Clássico sem fome, Hardcore, desafios.
- [ ] **Conquistas** locais (sem rede).
- [ ] **Efeitos sonoros** curtos estilo bip de PC dos anos 80, opcional em DisplaySettings.

## Persistência e meta-progressão

- [ ] **Save-and-quit** estilo Brogue/DCSS: o save some ao carregar (preserva morte permanente).
  Hoje o save é a seed + replay (`alpha.md`); avaliar se continua suficiente com overworld e cidades longas.
- [ ] **Cemitério / Bones**: tumbas de heróis anteriores; o herói morto reaparece como
  fantasma ou inimigo nomeado, com o equipamento dele (NetHack *bones*). Histórico de runs.
- [ ] **Tela de histórico de runs** no título (classe, raça, profundidade, causa da morte).

## Mecânica de personagem

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
