# Changelog

[English](CHANGELOG.md) · **Português (Brasil)**

As versões do Ossuary são numeradas só com **um número** (Versão 11, Versão 12…), como no *Project Zomboid*. Nos manifestos (`package.json`, `Cargo.toml`,
`tauri.conf.json`) e no nome do instalador a versão aparece como `0.N.0`. O **salvamento** (`SaveData.Version`) usa o mesmo número: mudou regra, sobe o número, saves antigos deixam de carregar.

## Versão 16 — Sangue e osso

### Combate e corpos
- **Golpes mirados.** Shift+F escolhe onde mirar: cabeça, braços, pernas, olhos, asas ou cauda. Alvos menores são mais difíceis de acertar, e um golpe mirado sempre deixa marca.
- **Partes decepadas.** Um fio pesado pode arrancar o braço, a perna, a asa ou a cauda de um monstro. Um braço destroçado larga a arma e bate com metade da força; uma asa quebrada derruba quem voa.
- **Monstros feridos agem como feridos.** Uma criatura aleijada com metade da vida se vira e foge; uma com os dois olhos cortados golpeia às cegas, às vezes nos próprios aliados.
- **Mais ferimentos.** Monstros ferem uns aos outros, e magias de força, gelo, raio e fogo também quebram corpos. Víboras e pítons gigantes chegam com corpo de serpente.
- **Ataduras.** Todo herói começa com duas; o templo vende e um alfaiate corta três de um pano. Elas param o sangramento e dobram o ritmo da cura. O fogo cauteriza cortes, e as horas na estrada contam para sarar.
- **Cicatrizes que contam.** Uma cicatriz no rosto custa um ponto de Carisma, cicatrizes fazem bandidos da estrada pensarem duas vezes, e o povo das cidades repara nelas.
- **Técnicas marciais.** O Guerreiro carrega um manual de golpes: Jarrete, Golpe Desarmante, Racha-Crânio e Estocada, aprendidos por nível e pagos com Vigor.

### Salvamentos
- O formato de salvamento passa para **16**: saves da Versão 15 não carregam.

## Versão 15 — Ferro e ofício

### Materiais
- **Todo equipamento é feito de algo.** Cobre, bronze, ferro, aço, prata, ferro frio, mithril, adamantina, obsidiana, osso e madeira, cada um com seu peso, preço, fio e proteção. O nome diz ("espada longa de aço"); o ferro comum não é nomeado.
- **Mais fundo, mais nobre**, e cada ramo puxa para um lado: metal bom nas Minas, osso e cobre nos Warrens, prata nos Sunken Vaults, vidro negro no Ashen Spire.
- **Material contra criatura.** A prata queima os mortos e os lobisomens, o ferro frio as fadas, a obsidiana golens e gárgulas: "A prata morde fundo!".
- **O equipamento gasta.** Armas ficam cegas e lascadas com o uso, armaduras amassam e ficam surradas, e cada estágio custa um ponto. Lâminas frágeis ou baratas podem se partir num crítico.
- **O ferreiro conserta** e refunde sua arma ou armadura no metal do minério que você trouxer. Há minério nas Minas, ele se solta quando você cava, e a ferraria vende cobre e ferro.
- **Perfuração fere fundo**: adagas, lanças, tridentes, flechas e ferrões deixam um ferimento pior num golpe forte.

### Ofícios para todos
- **Dezesseis ofícios, abertos a qualquer herói**: ferreiro, armeiro, fabricante de arcos, coureiro, alfaiate, joalheiro, alquimista, escriba, carpinteiro, ferramenteiro, luthier, cozinheiro, cervejeiro, minerador, músico, coletor. Cada um cresce com o trabalho, de Novato a Grão-mestre.
- **Cerca de cem receitas**: espadas e armaduras no metal das barras que você fundir, arcos, couro, capas, anéis que tiram uma magia da pedra, poções, pergaminhos, picaretas e varas de pesca, alaúdes, harpas e pandeiros, pão, ensopado e cerveja.
- **O lugar importa**: uma fogueira em qualquer canto, uma oficina na cidade ou a forja da ferraria. **A qualidade segue a mão**: tosca, comum, fina ou obra-prima com o nome de quem fez.
- **Shift+B** fabrica o que dá, **Shift+J** mostra o livro de receitas inteiro, **Shift+G** coleta: carneie uma carcaça, ou passe duas horas na estrada tirando madeira, linho, cevada, ervas, mel, minério ou peixe da terra.
- **Música**: toque um instrumento por moedas na cidade, ou para fazer criaturas dormirem lá embaixo.
- **Mestres e encomendas**: cada oficina ensina seus ofícios até Oficial e oferece uma encomenda por semana. As lojas vendem matéria-prima e compram seu trabalho pela pilha.
- **Você nunca precisa descer**: dá para viver de ofício desde o primeiro dia.

### Saves
- O formato de save vai para **15**: saves da Versão 14 não carregam.

## Versão 14 — Carne e osso

### Corpos e ferimentos
- **Todo golpe acerta algum lugar.** Quando um golpe tira uma boa parte da vida de uma criatura (15% ou mais), ele cai numa parte do corpo e deixa um ferimento além do dano. O log diz onde: "Sua perna esquerda está quebrada.", "A pata traseira esquerda do chacal está dilacerada."
- **Quatro gravidades, dois tipos.** Mordidas, garras, lâminas e flechas arranham, cortam, dilaceram e destroçam; punhos, maças e chutes machucam, contundem, quebram e esmagam. Um crítico vai mais fundo, e acertar de novo uma parte ferida piora o ferimento.
- **Ferimentos mudam a luta.** Pernas quebradas fazem qualquer um mancar ou se arrastar (asas, para quem voa), braços feridos estragam a mira, cabeça quebrada atordoa, olho cortado diminui a visão e a distância em que um monstro percebe você. Um herói mancando dá movimentos extras aos monstros.
- **Cortes sangram.** Alguns pontos por turno durante alguns turnos. Um monstro pode sangrar até morrer (conta como morte sua), e você também.
- **Toda criatura tem um corpo**: humanoides, feras de quatro patas, insetos e aranhas, morcegos e aves, dragões. Moldes, olhos flutuantes, espectros, elementais e enxames não têm o que quebrar; esqueletos, golens e mortos-vivos quebram mas não sangram.
- **Cura.** Os ferimentos saram com o tempo; um dilacerado ou quebrado deixa cicatriz. Magias e poções de cura melhoram cada ferimento em um nível, cura total e uma noite na estalagem fecham todos, e o templo estanca o sangue.
- **Dá para ver**: SANGRANDO, MANCANDO, RASTEJANDO e FERIDO na barra lateral, *Ferimentos* e *Cicatrizes* na ficha e no necrotério, e olhar um monstro lista os ferimentos dele. Tudo em português com o gênero certo.

### Documentação
- A pasta `docs/` foi organizada por assunto (`game/`, `design/`, `tech/`, `audio/`, `roadmap/`), com um índice em `docs/README.md`.
- Um roadmap novo de profundidade, a seção *Depth track* de [`docs/todo.md`](docs/todo.md), reúne as ideias inspiradas no Dwarf Fortress que ainda vêm: materiais, história gerada, biografia de relíquias, fluidos e rastros, humor, gravuras, inimigos com nome, cercos e lendas.

### Saves
- O formato do save passa para **13**: o lugar do golpe sai dos dados, então saves da Versão 13 não carregam.

## Versão 13.3 — Cada palavra, e um aviso que se ouve

Correção sobre a Versão 13 (`0.13.3` nos manifestos). Nenhuma regra mudou, então saves das Versões 12 e 13 continuam carregando.

### Português, segunda passada
- **Frases compostas traduzidas por inteiro.** Rumores, lugares e nomes dentro da frase não deixam mais metade em inglês ("Dizem que uma mina abandonada nas Colinas de Ferro..."), e a chegada ao nível lê "Você chega às Masmorras, nível 1.".
- **Nomes com gênero e número**: entradas de masmorra, marcos, casas, reis dos ramos e terrenos ("Você cava através dos escombros"); itens concordam em gênero e número ("botas robustas do urso") e encantamentos antes do nome compõem certo.
- **O resto do jogo**: relíquias, conjuntos e suas lendas, motivos de reputação, grupos rivais, títulos de herói, o arquivo do necrotério, deuses e o menu do altar, os seis chefes, os finais e o novo ciclo, companheiros, documentos da missão principal, tooltips e os 111 verbos de dano ("Uma só palavra desfaz o chacal por 5 de dano.").
- **A outra direção**: mensagens de erro do frontend, rótulos de leitor de tela e a linha de boot seguem o idioma escolhido; telas em inglês não carregam português.

### Som
- **Um aviso impossível de perder**: o stinger *Danger Spotted* toca quando um inimigo aparece pela primeira vez e quando a estrada impede a passagem, sem empilhar se você insistir.
- **A introdução datilografa em voz alta**, letra por letra.

### Controles
- As teclas de escada são símbolos sem tecla própria: mensagens, o painel de Comandos e o de Controles agora dizem que "> é Shift + ." em teclados US e ABNT2.

### Para quem contribui
- `loc msgs` varre todo literal com substitutos de interpolação; `loc pairs` lista linhas idênticas nos dois idiomas; novos testes `English frames carry no Portuguese`, `i18n.test.ts` e um `LocTests` mais amplo.

## Versão 13.2 — Um idioma de cada vez

Correção sobre a Versão 13 (`0.13.2` nos manifestos). Nenhuma regra mudou, então saves das Versões 12 e 13 continuam carregando.

### Português, terminado onde estava pela metade
- **O combate lê direito nos dois idiomas.** O log em inglês dizia "You hit the the jackal", "The jackal hits the you" e "you misses the kobold"; agora diz "You hit the jackal for 3 damage.", "The jackal hits you for 3 damage.", "The jackal misses you." e "You evade the jackal's attack." Moradores com nome próprio não ganham mais "the" na frente. O português tem as mesmas frases, com gênero e contrações ("do chacal", "a aranha das cavernas").
- **"It is weak" não vaza mais para o português.** Olhar um monstro e o aviso de encontro na estrada agora dizem "Parece fraco / um pouco perigoso / perigoso / muito perigoso".
- **Nomes de monstros e itens traduzidos.** Todo o bestiário e todo item do catálogo, com as partes compostas ("blessed +1 keen dagger of the fox" vira "adaga afiada da raposa +1 abençoada"), além das pilhas "x3".
- **As telas terminam o serviço**: a barra de cima ("NÍVEL 3", "Dia 1"), a barra de atalhos, a linha de status, os rótulos PV/EN/VG/MP, a ficha do personagem (perícias, níveis, alinhamento, talentos, traços), habilidades e talentos com suas descrições, a lista de evolução, o painel de Controles inteiro, as descrições de classe e raça na criação de personagem e os nomes do terreno.
- **Cerca de 300 mensagens a mais**: magias, poções, deuses, armadilhas, eventos de estrada, chefes, itens, treino e evolução.

### Para quem contribui
- `headless.ps1 loc [sementes] [turnos]` joga em português e lista toda string que chegou à tela sem tradução; `loc frames` imprime os painéis em português; `loc msgs` confere todo `Say`/`Tell` do Core; `loc names` lista os nomes a traduzir.
- Uma suíte `language` falha quando um monstro, item, afixo, talento, habilidade ou rótulo de controle fica sem português, e fixa as principais frases de combate e HUD nos dois idiomas.

## Versão 13 — Som

### Música
- **Dezoito trilhas que acompanham o momento.** O motor agora informa o que está acontecendo (`Frame.scene`: um chefe à vista, um inimigo à vista, dentro de uma taverna ou templo, uma loja, noite ou dia, ou o ramo da masmorra) e a música muda com uma transição suave. Elas compartilham o motivo de cinco notas e as mesmas vozes: o tema do título "Phosphor & Bone", a introdução da história, a estrada de dia e de noite, as cidades de dia e de noite, a taverna, a loja, o templo, uma trilha para cada ramo de masmorra (as Masmorras, as Minas, as Warrens, as Abóbadas Afundadas, a Torre de Cinzas, o Anexo), combate, o Gaoler e o Guardião de Pedra.
- Um herói morto ou uma partida vencida não toca nada.

### Efeitos sonoros
- **Quinze stingers** gerados por código a partir de pequenos modelos de instrumentos (sinos, piano, harpa, violoncelo, órgão, tímpano, vento e raspões): subir de nível, missão aceita, atualizada e concluída, item raro, perigo, armadilha, descanso, portão, morte, vitória, os três finais e um novo ciclo. Subir de nível, missão, morte, escadas, descansar numa estalagem e emergir com o Amuleto tocam um deles.
- Sinais novos para escadas, portas destrancadas, pegar itens e descansar. Sons de menu para mover, confirmar e cancelar.
- Os volumes geral, de música e de efeitos agora funcionam.
- `npm run stingers` (em `desktop/`) grava os stingers como arquivos WAV.

### Salvamento
- O formato do save não mudou (continua 12): saves da Versão 12 continuam carregando.

## Versão 12 — Um mundo que lembra

### Pessoas
- **Moradores têm personalidade e memória.** Cada um tem uma persona e lembra o que você fez com ele; um registro de feitos acompanha o herói de cidade em cidade. Eles ficam parados enquanto você conversa, numa caixa de diálogo própria.
- **Missões com prazo**, um diário em `F7`, e trilhas pessoais, da Guarda, do Templo e do Culto.
- **Rumores** que apontam para chefes e lugares reais.
- **Eventos na cidade** e **viajantes na estrada**: um peregrino, um mascate com um fragmento de mapa, refugiados e um explorador ferido, com escolhas que o mundo lembra. A espada de aluguel comenta pelo caminho, e um **grupo rival** disputa com você a descida das Masmorras.

### Crime e a Guarda
- Testemunhas, recompensa por região, prisão, celas e assassinato. NPCs essenciais são nocauteados em vez de mortos.

### A trama principal
- **"O Selo"**: documentos, o Leitor, verdades, seis finais e um novo ciclo.

### Correções
- **Entrar numa masmorra pelo mapa-múndi no nível 1 não deixava caminho de volta.** A escada de subida era removida do nível 1 de toda ramificação; agora só as ramificações laterais (o Anexo, que se sai pelo portal) a perdem.

### Salvamento
- **Formato de save 12.** Saves da Versão 11 deixam de carregar.
## Versão 11 — Magia que se vê

### Magias e animações
- **322 magias em 8 escolas** (eram 39 em 6). Escolas novas: **Natureza** (patrulheiro) e **Sombra** (ladino). Magias novas são receitas de dados: dano, efeitos (paralisar, veneno, sangramento, empurrão…), buffs, invocações, superfícies, cadeias, chuvas de golpes, cones, execuções.
- **Animações de magia**: o motor grava o que acontece, o frontend toca por cima. Projéteis com rastro, raios denteados, cadeias, cones, explosões, ondas, chuvas, pilares, meteoros. Também animam varinhas, tiro de arco, molotov, armadilhas e ataques de chefes. `headless.ps1 fx <magia>` mostra em ASCII.
- **55 livros** em cinco níveis de profundidade; painel de magias com **abas por escola**; todas as magias, buffs e efeitos traduzidos para PT.
- Patrulheiro e Ladino agora começam com livro e magias.

### Itens
- **Itens mágicos imbuídos com uma magia que combina com o tipo** (espada: ataque; armadura: proteção; botas: movimento…). A magia é emprestada enquanto o item está em uso; armas disparam sozinhas ao acertar e armaduras respondem ao golpe.
- **43 itens únicos** em todos os ramos (38 emprestam magias) e 4 conjuntos novos.
- Varinhas, pergaminhos e poções agora **lançam magias de verdade** (38 / 34 / 18), com a mesma animação e sem mana.
- Dezenas de armas, armaduras, elmos, luvas, botas, capas, escudos, anéis e amuletos novos; 55 afixos novos; o loot respeita a profundidade.
- Anéis e amuletos passaram a **valer** (e amuletos podem ser vestidos: `P`/`R`).

### Correções
- Os amuletos antigos eram *anéis* no catálogo; o de salvar vidas nunca funcionou. Agora são amuletos de verdade.
- O jogo quebrava se um monstro morresse envenenado no próprio turno.

### Para quem desenvolve
- Salvamento passa à **versão 11** (saves antigos não carregam).
- Novos testes `ArsenalTests` e `fx.test.ts`; documentação em [`docs/spells-and-items.md`](docs/design/spells-and-items.md); [CONTRIBUTING.pt-BR.md](CONTRIBUTING.pt-BR.md) e novo README.

## Antes da versão 11

O histórico anterior está no `git log` e no registro de itens concluídos de [`docs/todo.md`](docs/todo.md): cidades verticais, mundo vivo (reputação, contratos,
eventos de estrada), chefes, facções, corrupção e mutações, companheiros, conquistas, desafio diário, modos de jogo, efeitos sonoros e água animada, entre outros.
