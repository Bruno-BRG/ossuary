# RPG — pesquisa, catálogo e roteiro

Notas de design para expandir o lado RPG do Ossuary: personagem, magia,
habilidades, itens e progressão. Este documento é o **mapa**; cada parte
implementada vira seção em [sistemas.md](sistemas.md) e o status fica na
tabela de roteiro (§7).

## 1. Estado atual (base)

- 6 atributos NetHack (3..18, Str com fração 18/xx), HP, Energy (turnos).
- 5 papéis (`Roles.cs`): Adventurer, Fighter, Rogue, Cleric, Wizard — viés
  de atributo, kit, HP/nível, skills iniciais, 4 títulos.
- 6 skills 0..100 que sobem por uso (Combat, Dodging, Stealth, Magic,
  Survival, Search). Magic hoje **não faz nada** além de número.
- Level-up: HP/XP automático + 1 pick (`Progression`: 7 opções, todas
  incrementos planos).
- Itens: armas/armaduras/anéis/amuletos/varinhas/pergaminhos/poções/livros.
  `a spellbook` e `a book of prayers` são só decoração. Sem mana, sem magias.
- Sem **nome**, sem **raça**. Salvamento é replay (seed + teclas): qualquer
  escolha de criação precisa entrar no `SaveData`.

## 2. Pesquisa — o que cada jogo ensina

### Roguelikes

| Jogo | Ideia aproveitável | Cabe no Ossuary? |
|---|---|---|
| **DCSS** | Espécie dá *aptidões* (custo de XP por skill) e HP/MP por nível; background só define o começo (nada é proibido). Escolas de magia: magia tem até 3 escolas e falha por skill + armadura. Deuses com piedade. | **Sim**, núcleo. Aptidão por raça, falha de magia, deuses |
| **NetHack** | Papéis com ranks, alinhamento, oração, skills por *uso* com teto por classe, identificação por uso, livros com nível e chance de falha, Pw regenera por Wis/XL. | Já é a base; adotar teto de skill por classe e falha de livro |
| **Brogue** | Zero classe: o poder vem dos itens (enchant scrolls num único item). Sem XP. | Inspirar o sistema de *enchant* e itens com identidade |
| **Caves of Qud** | Mutações (70+) em vez de classe, aleatoriedade controlada, skill trees por domínio, mutações físicas vs mentais. | Parte 5: "mutações/bênçãos" raras como perks |
| **Tales of Maj'Eyal** | Talentos com cooldown, recursos distintos por classe (mana, stamina, vim, hate), árvores mistas. | Recursos por classe: Mana, Vigor, Sangue/Piedade |
| **Cogmind/Sil/Angband** | Sil: skills compradas com XP (sem nível), stealth sério. Angband: magia por livros de nível, falha por Int/armadura, resistências elementares. | Resistências, XP gasto em skills (opção) |

### Mesa

| Sistema | Ideia aproveitável |
|---|---|
| **D&D 5e** | Raça + classe + background; bônus de proficiência por nível; salvaguardas; spell slots; concentração; descanso curto/longo; vantagem/desvantagem. |
| **Pathfinder 2e** | ABC (ancestry, background, class); heritage dentro da raça; talentos de classe a cada nível par; proficiência em 4 graus (Treinado→Lendário); *focus spells* que recarregam rápido. |
| **GURPS/Savage Worlds** | Vantagens/desvantagens (perks e defeitos pontuados); skills por custo. |
| **Call of Cthulhu/Dark Souls tabletop** | Sanidade/corrupção como recurso — encaixa na lore (necromancia). |

### Videogames de magia

| Jogo | Ideia |
|---|---|
| **Baldur's Gate 3** | Slots por nível, *upcast*, concentração, ação bônus, superfícies elementais interagindo (fogo+óleo, água+raio), vantagem por terreno/altura. |
| **Skyrim** | Skills por uso e *perks* a cada nível; 5 escolas; mana + custo; encantamento ligado a gemas de alma; pergaminhos; shouts com cooldown. |
| **Diablo/PoE** | Afixos de itens (prefixo/sufixo), raridade, gemas de suporte, sockets. |
| **Dark Souls** | Atributos que destravam magia/armas, catalisadores, piromancia vs milagres. |
| **Divinity: OS2** | Superfícies, status por combinação, scrolls como parte do sistema. |

## 3. Decisões de design (o que adotamos)

1. **Dois eixos de identidade**: Raça (corpo, aptidões, traços) × Classe
   (função, kit, árvore de habilidades). Background fica de fora por ora.
2. **Poder por uso + escolha**: skills sobem usando (já existe, estilo
   NetHack/Skyrim); a cada nível, **1 perk** de uma lista curta (substitui
   os incrementos planos).
3. **Mana único** (`Mp`/`MpMax`) para magia arcana; Clérigo e Paladino
   gastam o mesmo pool (rotulado "Fé") — sem Vancian, sem recarga por
   descanso: regen por turno ligada a Int/Wis/Magic, mais poções.
4. **Magias vêm de livros** (aprender = custa turnos, pode falhar por
   Int/nível, estilo NetHack), organizadas por **escola**.
5. **Falha de magia** por armadura pesada e skill (estilo DCSS/NetHack);
   mago de armadura de placa é uma escolha, não um bug.
6. **Resistências e tipos de dano** (físico, fogo, gelo, raio, veneno,
   necrótico, sagrado) — a base para raças, itens e inimigos conversarem.
7. **Determinismo**: todo RNG novo vem de `Game.Rng`; desenho usa hash(x,y).
   Tudo novo precisa de teste headless e entrar no replay.
8. **Core puro**: mecânica em `Game.<Assunto>.cs`, telas como `TextBuilder`.

## 4. Catálogo — personagem

### Raças (traço + aptidões; HP/MP base por raça)

Todas valem a **mesma rolagem 3d6** da seed; só o viés muda (padrão atual).

| Raça | Atributos | Traço | Ligação com a lore |
|---|---|---|---|
| **Humano** | sem viés | Versátil (parte 2: +1 perk extra no nível 1) | O vagabundo padrão |
| **Anão** | Con+2, Str+1, Cha-1, Dex-1 | Resiste veneno; vê ouro/veios; infravisão curta | Mines of Dwarfdeep |
| **Elfo** | Dex+2, Int+1, Con-2 | Mana +25%; sono resistido; arcos +1 | Era das Cidades |
| **Halfling** | Dex+2, Cha+1, Str-2 | Furtividade +10; sorte (crit contra você reduzido); come menos | Taberneiros-hobbits |
| **Orc** | Str+2, Con+1, Int-1, Cha-2 | Regenera HP (lento); fúria <25% HP | Quartéis do Dungeons |
| **Gnomo** | Int+2, Con+1, Str-2 | Mana +15%; engenhocas: varinhas gastam menos carga | Gnome mummies dos Vaults |
| **Cinzento** (humano tocado pela Spire) | Wis+1, Con+1, Cha-2 | Resiste fogo; curado por necrótico em parte; calor | Ashen Spire, Emberdown |
| **Ossário** (morto-vivo, desbloqueável) | Con+2, Cha-3 | Imune a veneno/fome (!); cura só por magia/dano causado | Premissa: o buraco devolve |

Ordem de implementação: Humano, Anão, Elfo, Halfling, Orc, Gnomo, Cinzento.
Ossário depois (desbloqueio por vitória).

### Classes (função + recurso + assinatura)

| Classe | Papel | Recurso | Assinatura |
|---|---|---|---|
| **Guerreiro** *(existe)* | Linha de frente | Vigor | Golpe poderoso, segundo fôlego |
| **Ladino** *(existe)* | Dano furtivo | — | Ataque pelas costas x3, desarmar armadilhas |
| **Clérigo** *(existe)* | Suporte/anti-morto | Fé (Mp) | Curar, expulsar mortos-vivos, bênção |
| **Mago** *(existe)* | Dano à distância | Mana | Magias de livros, escolas arcanas |
| **Patrulheiro** | Arco, sobrevivência | Vigor | Tiro mirado, armadilhas, rastrear |
| **Paladino** | Tanque sagrado | Fé | Aura, punição sagrada, cura pela imposição |
| **Necromante** | Invocador | Mana + Corrupção | Levantar mortos, drenar, ossos |
| **Bárbaro** | Dano bruto | Fúria | Fúria, imunidade a medo, HP alto |
| **Monge** | Desarmado | Vigor | Artes marciais, esquiva, sem armadura |
| **Bruxo/Pactário** | Dano com custo | HP/Corrupção | Pacto: poder por preço |

Ordem: Patrulheiro, Paladino, Necromante primeiro (encaixam na lore e usam o
sistema de magia), depois Bárbaro, Monge, Pactário.

### Atributos derivados (a implementar)

- **Mp máx** = base raça/classe + Int/Wis×k + nível×k. **Regen** por turno.
- **Acerto/Dano/Crítico** por Combat + Str/Dex (hoje parcialmente).
- **Salvaguardas** (Fort/Refl/Will) a partir de Con/Dex/Wis: veneno, armadilhas, controle.
- **Carga** (já há `CarryingCapacity`) afeta Evasion.

### Progressão

- XP: curva atual `×1.35+10`. Manter. Bônus de XP por primeiro encontro
  com espécie (descoberta, estilo Qud/Skyrim) — opcional.
- **Skills**: manter 0..100 por uso; adicionar **graus** (Novato,
  Treinado, Perito, Mestre, Lendário) a 0/25/50/75/100 com bônus em cada
  grau; **teto por classe** (DCSS/NetHack).
- **Perks**: 1 por nível (+1 extra nos níveis 5/10), de um pool filtrado
  por classe/raça/pré-requisito de skill. Exemplos:
  - Guerreiro: *Segundo Fôlego, Golpe Poderoso, Parede de Escudos, Fúria de Aço*
  - Ladino: *Punhalada, Passos Leves, Mãos Rápidas, Olho de Gatuno*
  - Mago: *Mente Vasta (+Mp), Foco (-falha), Conjuração Rápida*
  - Clérigo: *Fé Inabalável, Cura Maior, Aura de Proteção*
  - Gerais: *Resistente, Sortudo, Olhos Abertos, Pés no Chão*

## 5. Catálogo — magia

### Escolas (6, com a skill de cada uma)

`Evocação` (dano), `Conjuração` (invocar/portais), `Alteração` (buff/utilitário),
`Ilusão` (furtivo/confusão), `Necromancia` (drenar/mortos), `Sagrada`
(cura/proteção). *Skill por escola é fase 4; no início só `Magic`.*

### Lista inicial (30 magias, círculos 1–5)

| Círculo | Magias |
|---|---|
| 1 | Seta Arcana (dano, mira) · Luz · Detectar Magia · Curar Ferimentos · Proteger (CA) · Toque Gélido |
| 2 | Bola de Fogo Menor · Raio de Gelo · Invisibilidade · Abrir/Trancar · Sono · Esconjurar Mortos |
| 3 | Relâmpago · Teletransporte Curto · Cura em Massa · Levantar Esqueleto · Revelar Mapa · Lentidão |
| 4 | Parede de Fogo (terreno) · Pele de Pedra · Drenar Vida · Medo · Dissipar |
| 5 | Meteoro · Portal · Morte · Ressurreição (1x) · Chuva de Ossos |

Mecânicas associadas: custo de Mp, falha %, alcance/área reaproveitando
`Game.Targeting`, **duração/concentração** de buffs (um só ativo),
**superfícies** (fogo queima grama/óleo, gelo congela água, raio em poça)
como fase tardia, estilo BG3/Divinity.

### Fontes de magia

- Livros (aprender permanente, nível/falha), pergaminhos (uso único, sem
  mana, já existem), varinhas (cargas), cajados (cargas que recarregam),
  altares/deuses (preces), poções.

## 6. Catálogo — itens e encantamentos

- **Raridade**: Comum, Incomum (+1..+2), Raro (afixo), Épico/Artefato (nome
  fixo + lore), Amaldiçoado (já existe `Cursed`).
- **Afixos** (Diablo/PoE): prefixos (`Afiada +1`, `Flamejante` +1d4 fogo,
  `Venenosa`, `Vampírica`, `Gélida`) e sufixos (`da Raposa` +Dex, `do
  Urso` +Str, `da Coruja` +Int/Mp, `do Mago` +Mp regen, `da Ruína` +crit).
- **Encantar** (Brogue/Skyrim): pergaminho *Enchant* +1 em item; **gema
  de alma**/osso-alma recarrega cajado ou encanta arma (tema Ossuary).
- **Identificação** por uso (já parcial) + pergaminho de identificar.
- **Conjuntos/artefatos**: *Coroa do Rei Afogado*, *Machado de Dwarfdeep*,
  *Cinzas da Spire*, ligados às branches.
- **Slots** novos: elmo, botas, luvas, capa, amuleto, 2 anéis — hoje há
  Armor/Shield/2 anéis/amuleto; separar slots é a parte 6.
- **Resistências** em itens e inimigos (listadas em §3.6).

## 7. Roteiro de implementação

Cada parte fecha com: testes headless novos, `fastcheck`, `docs/sistemas.md`
atualizado e suite completa. Uma parte por vez.

| # | Parte | Entrega | Status |
|---|---|---|---|
| 1 | **Identidade** | Nome, raça (7), classe (+Patrulheiro, Paladino, Necromante), tela de criação, `SaveData` com personagem, título/painel Character | **feito** (traços ativos das raças ficam p/ a parte 2) |
| 2 | **Atributos derivados** | Mp/regen, traços raciais ativos, graus e teto de skill, resistências base | **feito** (salvaguardas ficam p/ quando houver o que salvar) |
| 3 | **Núcleo da magia** | Mp na HUD, verbo `Z` conjurar, livros, aprender, falha, 6 magias de partida, mira reaproveitada | **feito** |
| 4 | **Catálogo de magias** | 32 magias em 6 escolas, buffs com duração, invocados/aliados | **feito** |
| 5 | **Perks e habilidades de classe** | Substitui `Progression`; habilidades ativas pagas em Vigor | **feito** (Fúria/cooldowns ficam para as classes Bárbaro/Monge/Pactário) |
| 6 | **Itens** | Raridade, afixos, enchant, slots novos, artefatos, gemas de alma | **feito** (gemas de alma ficam para depois do sistema de cajados) |
| 7 | **Deuses e piedade** | Altares, preces, favor (clérigo/paladino) | **feito** (templos com sacerdote ficam para depois) |
| 8 | **Superfícies e status** | Fogo/gelo/raio no terreno, resistências ativas | **feito** (sangramento/veneno como condições ficam para a revisão de combate) |
| 9 | **Balanceamento** | Soak por classe×raça, curvas de dano/Mp, docs finais | **feito** ([balance.md](balance.md)) |

## 8. Fontes

- [DCSS — espécies, backgrounds, escolas](https://crawl.develz.org/wordpress/0-31-the-alchemy-of-forms) · [manual](https://mitjafelicijan.com/assets/notes/dcss_manual.pdf)
- [Caves of Qud (RogueBasin)](https://www.roguebasin.com/index.php/Caves_of_Qud) · [Tales of Maj'Eyal](https://en.wikipedia.org/wiki/Tales_of_Maj%27Eyal)
- [D&D 5e vs Pathfinder 2e](https://www.dnddiceroller.com/blog/pathfinder-2e-vs-dnd-5e/)
- NetHack, Brogue, Angband, Sil, Baldur's Gate 3, Skyrim, Divinity: OS2, Diablo: conhecimento de domínio.
