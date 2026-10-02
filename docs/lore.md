# Lore — Ossuary

Bíblia de worldbuilding. Tom: **dark fantasy**. O Ossuary é um **lugar
físico**. O protagonista é um **vagabundo**. Tudo aqui precisa bater com o
código: 5 branches, 9 regiões, Amuleto de Yendor no fundo de The Dungeons.

## Premissa (uma frase)

O mundo jogou seus mortos, seus reis e seus deuses num buraco só — o
Ossuary — e agora um ninguém desce para roubar o que sobrou.

## O que é o Ossuary

Não é metáfora. É a catacumba central do continente: séculos de criptas,
minas, esgotos, masmorras de guerra e torres empilhados uns sobre os outros
até virarem geologia. Ninguém planejou. Cada geração cavou um andar novo
por cima ou por baixo da anterior e perdeu o mapa.

Por isso o Ossuary tem 5 "branches" que não se conectam de forma limpa —
são épocas diferentes da mesma fossa:

1. **The Dungeons (10 níveis)** — a camada de cima. Prisões, quartéis
   (`Barracks`), labirintos (`Maze`) e fortes (`Fort`). Era o porão do
   reino antigo. Entrada mais fácil, saída ainda possível (`AscendPossible`).
   É onde o vagabundo nasce: `EntryText` atual diz só *"You enter the
   dungeons beneath the earth."*
2. **The Mines of Dwarfdeep (8)** — *"The air grows cold and the walls turn
   to living stone."* Mina anã abandonada. Os anões (`dwarf`, `dwarf lord`)
   cavaram fundo demais, acharam água negra e foram embora. Ferramentas
   (`pick-axe`) e veios ainda estão lá.
3. **The Warrens (9)** — *"Something has been living here a long time."*
   Toca de kobolds, ratos gigantes, aranhas, centopeias. Nível baixo da
   cadeia alimentar. Ninguém construiu: foi roído.
4. **The Sunken Vaults (12)** — *"Black water drips from a ceiling you
   cannot see."* Cofres, templos e arquivos afundados. Água subiu, os
   mortos não (`zombie`, `skeleton`, `gnome mummy`). É onde a fome e o
   veneno mais matam.
5. **The Ashen Spire (15)** — *"The stone here is warm, and it should not
   be."* Torre invertida que desce em vez de subir. Guerra final, magia
   final. No fundo: `tiamat`, `lich`, `fire giant`, `dread knight`. O lugar
   mais fundo e mais novo — cavado por quem já não era mais gente.

Regra de ouro da lore: **descer é voltar no tempo ao contrário**. Quanto
mais fundo, mais recente e pior foi o que aconteceu.

## Linha do tempo (curta, usável)

1. **Era das Cidades** — as 9 regiões do overworld eram reinos-clientes.
   Estradas (`Road`) ligavam cidades (`Town`, glifo `+`). Comércio de
   armas, poções, pergaminhos, varinhas — as 8 lojas de hoje são o fóssil
   disso.
2. **Era de Dwarfdeep** — anões abrem as Minas. Trazem `axe`, `war hammer`,
   `chain/plate mail` para a superfície. Ficam ricos, fecham os portões.
3. **O Afundamento** — água negra toma os Vaults. Arquivos, templos e
   bancos afundam com tudo dentro. Nasce o ditado: "o Ossuary não devolve".
4. **A Guerra da Spire** — alguém (o `guardian of the deep` ainda lembra
   quem) ergue a Ashen Spire para queimar os mortos. Queimou os vivos.
   Cinza cobre `Emberdown` e `Ashen Marches`.
5. **O Presente** — reinos viraram regiões perigosas (`Danger 15–100`),
   cidades viraram vilas muradas com guardas-anões e taberneiros-hobbits,
   estradas viraram trilhas com encontros. O Amuleto ficou lá embaixo e
   ninguém de importância se oferece para buscar.

## Overworld: as 9 regiões

Geradas em grade 4×3 (`DefineRegions`). Nomes fixos no código, perigos
crescentes de oeste para leste. Lore amarra cada um:

| Região | Danger/Depth | Leitura de lore |
|---|---|---|
| The Verdant Reach | baixo | Última terra que ainda finge que está tudo bem. Campos, `Ashford`-da-vez. |
| Ashen Marches | baixo-médio | Cinza da Spire. Areia, ruína (`⌂`), vento quente. |
| The Sunken Vale | médio | Borda dos Vaults. Pântano (`Whisperfen` vaza pra cá), água sobe. |
| Gallowmoor | médio | Onde enforcavam saqueadores do Ossuary. Forcas viraram `wayshrine` (`⛩`). |
| The Iron Hills | médio | Colônias anãs da superfície. `Mine` (`⇛`) e `Keep` (`♜`) por toda parte. |
| Whisperfen | médio-alto | Pântano que sussurra. `gas spore`, `mold`, `floating eye`. Ninguém bebe da água. |
| The Craglands | alto | Pedra quebrada, `Cave` (`▼`). Kobolds e orcs mandam. |
| Emberdown | alto | Cinza quente, `fire giant` na mitologia local. Vilas pagam pedágio a `ogre lord`. |
| The Hollow Wastes | máximo | Fim do mapa. `tiamat` é religião aqui. `Depth 30`, `Danger 100`. |

Cada região tem 1 entrada de dungeon (`DungeonName`: *the Sunless Vaults*,
*the Weeping Warren*… — nomes procedurais são apelidos locais, não o nome
"oficial" do branch) e cidades com nomes tipo `Ashford`, `Grimhold`
(`TownName`: Ash/Gloom/Hollow/Ember + brook/ford/haven…). Lore: cidades não
têm história própria porque **são todas a mesma cidade reconstruída** —
gente que fugiu de uma região e fundou outra com o mesmo molde: muralha,
praça, fonte, ferraria, taverna, templo, guardas. Como não havia terreno de
sobra, as cidades cresceram para cima (torres, sótãos) e para baixo (porões,
criptas): quem fundou uma vila ao lado do Ossuary aprendeu a cavar.

## Facções e povos (quem aparece de verdade no bestiário)

Sem facção com IA de facção (código tem só `Hunt/Walk/Ambush/Guard`). Lore
não promete sistema que não existe. São culturas, não times:

- **Os Que Ficaram (anões e gnomos).** `dwarf`, `dwarf lord`, `gnome`,
  `gnome lord` — `Lawful`. Não são monstros: são os últimos funcionários
  do Ossuary. Guardas de cidade usam corpo de anão, lojistas usam corpo de
  hobbit (`Town.cs`: `GuardShell`/`KeeperShell`). Lore: a cidade contrata
  os baixinhos porque os altos morreram lá embaixo.
- **Os Famintos (kobolds, orcs, jackals).** `ChaoticEvil`. Tribos dos
  Warrens e Craglands. `orc shaman` e `orc chieftain` são chefia real;
  `kobold` com `club` é criança com pau. Eles não odeiam você: têm fome.
- **O Mofo (molds, gas spore, lizard, spider).** `Neutral`, `mindless`.
  A biologia do Ossuary. `brown/yellow/gray mold` explode porque é assim
  que esporo viaja. Não é mal: é umidade.
- **Os Afogados (zombie, skeleton, mummy, wraith, lich, dread knight).**
  `Undead`. Cada um é uma camada: zumbi = camponês afogado nos Vaults,
  esqueleto = soldado das Dungeons, `gnome mummy` = sacerdote embalsamado,
  `wandering wraith` = quem viu a Spire, `lich` + `dread knight` = quem
  mandou construir. Não voltaram por maldição genérica: **ninguém os
  enterrou direito porque o cemitério virou masmorra.**
- **Os Grandes (troll, ogre, giant, tiamat).** Fome com tamanho. `troll`
  regenera porque o Ossuary não deixa nem ele morrer em paz. `tiamat`
  (nível 30, 300 HP) não é boss com fala: é o fundo da fossa respirando.
- **Os da Superfície (hobbits, wood nymph, werenothing, stalker).**
  Hobbits cuidam de loja porque são os únicos que ainda sabem contar.
  `wood nymph` e `stalker` são o mato tentando recuperar a boca do buraco.

## O Amuleto de Yendor

Quest atual: um `amulet of Yendor` (`Tier 4`, `QuestItem`) espera no nível
10 de The Dungeons; sair da masmorra com ele (`CheckVictory`) vence a run:
*"You emerge into the open air, the Amulet blazing against your chest."*

Lore mínima, sem profecia inchada:

- Não é joia de rei. É o **selo do arquivo afundado** — quem o segurava
  podia abrir qualquer porta, cofre e túmulo do reino antigo.
- Por isso todo morto do Ossuary "reconhece" ele e todo vivo o quer: lojas
  pagariam 5000 de ouro (`Cost`), mas nenhuma tem esse ouro (`ShopEconomy`).
- Yendor não é deus. É o **arquivista que carimbou o próprio túmulo** e
  desceu com o selo para ninguém mais subir. O vagabundo vai provar que ele
  errou.

Sem tela de vitória ainda (`alpha.md`): quando existir, a frase final já
está no código — *"The Ossuary remembers."* É o epitáfio padrão. Manter.

## O protagonista: o vagabundo

Sem origem heroica. Regras:

- Começa com adaga, couro, 3 rações, lock pick, 30 ouro. Isso **é** a
  backstory: ex-ladrão de cidade, ex-soldado desertor, ex-camponês — tanto
  faz, porque vendeu tudo menos isso.
- Não foi escolhido. Foi o único que aceitou descer por 30 de ouro e uma
  promessa de 5000 que ninguém pode pagar.
- F5 mostra a seed = "save". Lore: o vagabundo conta a mesma história toda
  vez que morre, só muda o número. Morte permanente (`GameOver`, qualquer
  tecla recomeça) é canon: **o Ossuary cospe outro vagabundo igual.**
- Ele sente fome (`Nutrient`), lê pergaminho sem saber (`r`), zapa
  varinha (`z`), come cadáver (`corpse`) quando precisa. Não é coragem: é
  necessidade com lanterna.

## Crenças, altares e marcos

- **Altares e fontes** (`TileKind.Altar/Fountain`): cada andar tem o deus
  da época em que foi cavado. Ninguém sabe o nome, todo mundo bebe e reza
  mesmo assim. Efeito no jogo manda; lore não explica.
- **Wayshrines** (`⛩`), **ruínas** (`⌂`), **pontes** (`=`): marcos de
  quem tentou mapear o Ossuary e desistiu no meio. Nomes genéricos
  (`FeatureName`: *"an ancient ruin"*, *"a ruined keep"*) são de propósito:
  quem nomeou morreu antes de terminar.
- **Dia/noite** (`AdvanceTime`): a superfície ainda tem sol. Lá embaixo,
  não. Viajar à noite (`IsNight +6` chance de encontro) é pedir para ser
  lembrado pelo Ossuary.

## Como a lore aparece no jogo (sem quebrar nada)

1. `Branch.EntryText` — 1 frase por branch, já existe. Não alongar: é o
   único texto garantido que o jogador lê.
2. Nomes procedurais (`TownName`, `DungeonName`, `FeatureName`) — manter
   geradores curtos e anglo-saxões. Não colocar lore em string aleatória.
3. `Say()` — todo feedback ao jogador passa por lá. Lore entra como sabor
   em mensagem existente, nunca como painel novo obrigatório.
4. Morte e vitória — `GameOver` e `Won` são os únicos "finais". Frase
   *"The Ossuary remembers."* é o lema. Repetir, não variar.
5. Futuro (quando houver UI): `History` e `Discoveries` (`UiState`) são o
   lugar natural para codex. Lore nova entra lá primeiro, não em popup.

## Países e potências (o mapa político)

As 9 regiões do código são geografia. Política são 6 potências + 3 terras
de ninguém. Nenhuma manda no Ossuary — todas pagam gente para descer.

1. **A Liga do Reach (The Verdant Reach).** Último punhado de vilas que
   ainda colhem trigo. Governo: conselho de lojistas hobbits (os mesmos
   `KeeperShell` das 8 lojas). Riqueza: comida e corda. Doutrina: "o buraco
   é problema de todo mundo, então que morra um vagabundo, não um filho
   nosso". É quem dá os 30 ouros iniciais.
2. **Os Holds de Ferro (The Iron Hills).** Anões e gnomos que selaram
   Dwarfdeep por dentro e hoje alugam machados e guardas (`dwarf` como
   `GuardShell`). Dizem que fecharam a mina por honra; fecharam porque a
   água negra subiu e o conselho votou 4 a 3 para afogar o turno da noite.
3. **O Khanato da Fenda (The Craglands).** Orcs, kobolds, jackals. Não é
   horda: é confederação de fome. `orc chieftain` cobra pedágio de estrada,
   `orc shaman` marca os que podem passar com cinza. Odeiam a Liga menos do
   que odeiam o Oco — o Oco come até orc.
4. **O Pacto da Brasa (Emberdown).** Vilas que trocaram imposto por
   proteção: pagam comida a um `ogre lord` e chamam de rei. Forjas acesas
   dia e noite para pagar o dízimo em `plate mail`. Cinza aqui é moeda.
5. **A Igreja da Corda (Gallowmoor).** Padres-enforcadores. Enforcavam
   saqueadores do Ossuary na estrada; quando os enforcados voltaram como
   `zombie`, declararam que a corda "prende a alma no corpo certo". Hoje
   vendem nós, mortalhas e mapas. `wayshrine` (`⛩`) é altar deles.
6. **O Trono Oco (The Hollow Wastes).** Não é país: é fila. Peregrinos,
   liches menores, `dread knight` desertores e gente que ouviu `tiamat`
   respirar e achou bonito. Não têm capital, têm direção: para baixo.

Terras de ninguém (sem trono, com história):

- **Ashen Marches** — campo de cinza entre Liga e Pacto. Nômades do véu
  (ex-soldados da Spire) guiam por água. Regra: nunca acampe no vento.
- **The Sunken Vale** — canais sobre os Vaults. Barqueiros cobram por
  remada e por silêncio. Dizem que dá para ouvir o arquivo carimbando.
- **Whisperfen** — pântano-oráculo. Ninguém governa porque ninguém volta
  igual. `floating eye` é mensageiro; matar um é declarar guerra ao pântano.

## Personagens únicos (canon jogável)

Regra: cada um usa um corpo que **já existe** no `Bestiary`. Virar "único"
custa só nome + 1 frase + 1 drop garantido. Nada de sistema novo.

**Embaixo (dungeon):**

1. **Yendor, o Arquivista (`lich`, The Dungeons:10).** Não é rei nem deus:
   era o carimbador do arquivo afundado. Desceu com o selo (`amulet of
   Yendor`) para que nenhum rei subisse com ele. Frase ao ver o amuleto no
   chão: *"Você também veio carimbar a própria cova?"* Drop: o próprio
   amuleto. Ele não guarda o amuleto — ele largou e ficou olhando.
2. **Marrow, a Guarda do Fundo (`guardian of the deep`, Dungeons:9–10).**
   Última soldada do reino antigo que ainda cumpre escala. `Guard`, não
   persegue para longe do posto. Frase: *"Posto mantido. Passe com o selo
   ou volte com os outros."* Ela deixa passar quem tem o amuleto — é o
   tutorial final que ninguém escreveu.
3. **Durek Martelo-Frio (`dwarf lord`, Mines:8).** O contramestre que selou
   Dwarfdeep. Virou `Lawful` até depois de morto: ainda confere capacete.
   Drop: `war hammer` nomeado. Frase: *"O turno da noite ficou lá dentro.
   Eu fiquei com eles."*
4. **A Contadora Afogada (`gnome mummy`, Vaults:10–12).** Tesoureira do
   banco afundado, mumificada com livro-caixa. `mindless`, mas abraça quem
   carrega ouro. Drop: `gem` + mapa mental do cofre (sabor, não item novo).
5. **O Senhor Cinza (`dread knight`, Spire:12–14).** General que mandou
   acender a Spire para "queimar os mortos". Queimou os vivos e subiu de
   posto. `Predator`, montado num cavalo que não existe mais. Frase:
   *"A ordem era cinza. Eu só obedeci até o fim."*
6. **O Verme Branco (`tiamat`, Spire:15 / Wastes).** Não fala. É o fundo da
   fossa respirando (`regen`, `flys`). O Culto do Oco o adora; os orcs o
   medem em fome ("come três aldeias por ano"). Encontrar é o ponto final
   da religião local.
7. **Vex, o Vesgo (`orc shaman`, Warrens/Craglands:5–8).** Xamã que marca
   viajantes com cinza para o Khanato deixar passar. No jogo: primeiro
   `shaman` que `Say()` em vez de só atacar — *"Cinza na testa ou dente no
   chão."* Drop: `club` + 1 passagem (sabor para encontro de overworld).
8. **Mãe Ferrugem (`ogre lord`, Emberdown encontros).** Cobra o pedágio do
   Pacto. Não mata quem paga; come a mula de quem não paga. Frase:
   *"O rei come primeiro. O rei sou eu."*

**Em cima (superfície e cidades):**

9. **Sorrel Pequena (`hobbit` lojista, qualquer cidade).** A rede das 8
   lojas é dela: `the Gilded Supply`, `the Rusty Depot` — nomes
   procedurais (`ShopNameFor`) são filiais. Nunca sai de trás do balcão
   (`Altar` como balcão). Frase: *"Tudo tem preço. O selo do Yendor também,
   mas você não tem onde gastar."*
10. **Capitão Sino (`dwarf` guarda, portões).** Chefe dos `town guard`.
    `Dormant` até provocarem; quando acorda, apita (sino, não item novo).
    Sabe o nome de todo vagabundo porque enterrou a versão anterior.
    Frase: *"Seed nova? Cova nova. Boa descida."*
11. **Irmã Forca (`gnome lord`, Gallowmoor).** Sacerdotisa da Corda que
    benze nós e vende mortalha como armadura (`ring mail` lore). Frase:
    *"Se voltar, traga os dentes. Os ossos o Ossuary já tem."*
12. **A Sussurrante (`wood nymph` / `floating eye`, Whisperfen).** Oráculo
    que só responde com o que o vento trouxe: nomes de mortos da sua run
    anterior (gancho para epitáfio por seed). Nunca mente, nunca ajuda
    duas vezes. Frase: *"Pergunte ao pântano. Ele lembra de você."*

## História completa em 7 eras (para usar em codex futuro)

1. **Fundação (antes da contagem).** Primeiro poço, primeira cripta. Cada
   vila enterra os seus. O Ossuary ainda é plural: ossuaries.
2. **O Arquivo (Era de Yendor).** Reino antigo centraliza selos, cofres e
   túmulos. Yendor carimba tudo. `The Dungeons` são construídos como porão
   do palácio: prisão, quartel, labirinto para despistar ladrão.
3. **O Ferro (Era de Dwarfdeep).** Anões abrem as Minas, ficam ricos,
   fecham os portões. Superfície aprende `axe`, `pick-axe`, cota de malha.
   Primeiras estradas de verdade (`Road`).
4. **O Afundamento.** Água negra — esgoto? deus? vazamento da mina? —
   toma arquivos e templos. Nasce `The Sunken Vaults`. O banco afunda com
   a Contadora dentro. Ditado: "o Ossuary não devolve".
5. **A Fome (Era dos Warrens).** Com o arquivo morto e a mina fechada,
   quem ficou comeu quem passou. Kobolds, ratos, aranhas tomam os túneis
   de serviço. `The Warrens` são roídos, não construídos.
6. **A Spire.** O Senhor Cinza ergue a torre invertida para incinerar os
   mortos de uma vez. Pedra quente (`"warm, and it should not be"`), cinza
   sobre duas regiões, `fire giant` acordado. O Verme Branco desce atraído
   pelo calor e fica.
7. **A Liga (presente).** Restam vilas, pedágios, cordas e lojistas. O
   conselho hobbit financia vagabundos a 30 ouros por cabeça porque herói
   de verdade custa mais que o amuleto vale. Você é a 1000ª tentativa.
   F5 prova: mesma seed, mesma fossa.

## Ganchos abertos (atualizado)

- [x] Quem acendeu a Spire? → O Senhor Cinza. Falta só a frase no jogo.
- [x] Por que Dwarfdeep selou? → Durek selou com o turno da noite dentro.
- [x] Yendor era pessoa? → Sim: o arquivista. Túmulo no nível 10 resolve.
- [ ] Água negra: deus, esgoto ou mina? (deixar ambíguo de propósito.)
- [ ] Epitáfio procedural por morte usando seed (A Sussurrante recita).
- [ ] 1 frase por único (`Say` ao primeiro avistamento) — barato, forte.
