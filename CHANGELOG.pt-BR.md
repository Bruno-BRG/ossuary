# Changelog

[English](CHANGELOG.md) · **Português (Brasil)**

As versões do Ossuary são numeradas só com **um número** (Versão 11, Versão 12…), como no *Project Zomboid*. Nos manifestos (`package.json`, `Cargo.toml`,
`tauri.conf.json`) e no nome do instalador a versão aparece como `0.N.0`. O **salvamento** (`SaveData.Version`) usa o mesmo número: mudou regra, sobe o número, saves antigos deixam de carregar.

## Versão 20 — O mundo lembra

### Mundo e história
- **Um passado de verdade, vindo da semente.** Três séculos: casas nobres se erguem em suas sedes e cerca de metade cai; uma linhagem de reis se sucede pelo sangue, pela espada ou pela escolha dos senhores; ferreiros, cavaleiros, sacerdotes, ladrões, eruditos e senhores da guerra nascem, vivem e morrem — alguns ainda estão vivos no ano em que você chega. Cidades são fundadas, guerras e pestes passam, cidades queimam, e o Poço se abre.
- **Os mortos jazem lá embaixo.** Cavaleiros, senhores da guerra, reis e sacerdotes que morreram depois da abertura do Poço estão enterrados em níveis de verdade: uma sepultura, o nome talhado na pedra e o que foi enterrado com eles — equipamento que se lembra deles.
- **Painel de Lendas (F8).** Tudo o que você aprende do passado num lugar só: acontecimentos, pessoas, lugares e os heróis que vieram antes de você.
- **A história alimenta o jogo.** Canções de taverna, boatos, o *Perguntar sobre os velhos tempos* do erudito, livros tirados das estantes de bibliotecas e empórios, biografias de relíquias, gravuras e túmulos leem a crônica e ensinam suas lendas. Ruínas ganham o nome das cidades que queimaram; fortalezas, o das casas que caíram.
- **Os heróis antes de você.** Os ossos de partidas anteriores entram na história deste mundo: o bardo canta sobre eles, e o painel os lista.

### Sangue e rastros
- **Sangue por espécie.** Os vivos sangram, insetos e demônios deixam icor, mofos e gosmas deixam gosma; os mortos e os feitos não deixam nada. Golpes espirram, um golpe pesado espalha mais longe, e tudo escurece o chão onde cai.
- **Desbota.** O sangue seca e escurece ao longo de centenas de turnos; pegadas, lama trazida da água, fuligem onde o fogo apagou e as marcas de arrasto de uma criatura rastejante desbotam no seu tempo.
- **O rastro nas duas direções.** Um herói sangrando é farejado: coisas caçadoras vêm atrás de você mesmo fora de vista. E você pode seguir o rastro de uma criatura ferida com Shift+M, que aponta o caminho e o segue.

### Gravuras, salas e túmulos
- **Paredes antigas falam.** A crônica está talhada no chão lá embaixo, junto com avisos acima de um covil e uma pista apontando para uma porta escondida. Passar por cima lê e ensina a lenda.
- **As salas lembram.** Na primeira vez que você entra num quartel, templo, despensa ou tesouro, uma linha diz o que aquilo foi; olhar uma célula dentro diz o mesmo.
- **Entalhe o seu (Shift+O).** Em chão nu você pode gravar seu nome, um aviso, ou a última coisa que matou.

### Ataques a cidades
- **Algumas semanas um bando vem.** Kobolds perto das estradas da costa, orcs mais para dentro, os mortos no pior país, num dia da semana.
- **Se você estiver lá**, os saqueadores pulam o muro pelo portão e você pode lutar nas ruas. Mate todos e a cidade lembra: ouro, e a gratidão da Guarda.
- **Se você não estiver**, a Guarda aguenta ou a cidade paga: uma loja queima (estoque perdido, dono morto, chão negro para sempre) e de um a três moradores morrem. Sair da cidade com saqueadores nas ruas conta como derrota.

### A Corte Oca
- Um segundo ramo de portal, atrás de um portal no fundo das Minas (nível 5): três andares de uma corte que os anões emparedaram, com a Rainha Oca no último, seu Espinho e seu Vestido, e duas conquistas.

### O fim
- **O necrotério conta a sua lenda**: os chefes que você matou, as relíquias que carregou, as obras que fez, os ataques que rechaçou e as lendas que aprendeu.
- **O próximo mundo lembra**: seus ossos levam essa lenda para a história da partida seguinte.

### Salvamentos
- O formato de salvamento passa para **20**: saves da Versão 19 não carregam.

## Versão 19 — Todos os ofícios

### Itens e materiais
- **Identificar pelo uso.** Poções, pergaminhos e varinhas começam como o que parecem ("poção âmbar turva", "varinha retorcida de carvalho"), diferentes a cada partida. Beba, leia ou use uma, compre, mande avaliar ou leia identificação, e você conhece aquele tipo para sempre. O F6 conta o que você já conhece.
- **Serviços de loja.** Sábios em empórios e bibliotecas recarregam varinhas (a segunda vez é um risco). Sacerdotes tiram maldições.
- **Amuletos amaldiçoados.** A sanguessuga, o sono inquieto e os mortos famintos: fortes, com um preço, e não saem do pescoço até um sacerdote tirar a maldição.
- **Mais relíquias.** O Gume que Chora, a Malha de Tutano e a Coroa do Poço, todas corruptoras.
- **Relíquias com história.** O mundo agora tem um passado gerado da semente: três séculos de ferreiros, reis, cavaleiros e senhores da guerra, guerras, pestes, cidades fundadas e queimadas, e o ano em que o Poço se abriu. Cada item único tem uma biografia tirada dele (quem forjou, de quê, para quem, quem carregou, como se perdeu), e as bibliotecas leem as crônicas em voz alta.
- **Feitos gravados nas relíquias.** Mate um chefe com relíquias na mão ou no corpo e cada uma lembra disso. O necrotério imprime a história de cada relíquia.
- **Donos anteriores.** Equipamento tomado de um monstro lembra de onde veio, e o de um herói morto também. Examine (Shift+I) ou olhe (`l`) uma coisa sozinha no chão para ler tudo.

### Ofícios
- **Tear e alambique.** Tecido e capas se fazem no tear do armazém; cerveja e hidromel no alambique da taverna ou do alquimista. Toda ferraria agora tem uma forja ao seu alcance.
- **Artesãos pelo mundo.** Toda semana o ferreiro, o armeiro, o alfaiate e o alquimista põem à venda trabalho novo assinado, e nos dias de feira o grupo rival vende flechas e o que trouxe lá de baixo.
- **Monstros arqueiros.** Kobolds, gnomos e orcs com fundas, arcos e bestas atiram de longe com munição de verdade, que cai aos seus pés.
- **Escolha sua munição** (Shift+Y): prata para os mortos, comum para o resto.
- **Suas obras com nome pelo mundo.** Venda uma e, dias depois, alguém da cidade compra e passa a carregá-la; as tavernas falam dela e de você.

### Economia e mercado
- **Caravanas na estrada.** A caravana que você encontra é a da cidade mais próxima. Escolte e as mercadorias chegam e você recebe; roube e a cidade passa falta e a Guarda quer você.
- **Pechincha** (`o` no balcão): ofereça 90, 75 ou 60 por cento. Carisma, o humor do comerciante e a Guilda decidem; uma recusa azeda o comerciante pelo resto do dia.
- **Comerciantes lembram.** Inunde um com mercadoria, venda algo amaldiçoado ou estragado, ou pechinche bem, e ele recebe você e cobra de acordo.

### Salvamentos
- O formato de salvamento passa para **19**: saves da Versão 18 não carregam.

## Versão 18 — Da forja ao mercado

### Ofícios, itens e mercado
- **Munição.** Arcos disparam flechas, bestas disparam virotes, fundas disparam pedras. Cada disparo gasta uma; ela cai onde você mirou e pode ser recolhida, a menos que tenha quebrado. Uma ponta de prata ou de ferro frio é letal como uma lâmina desse metal. Sem lançador, ou sem o que disparar, você arremessa uma pedra.
- **Flechas feitas à mão.** O fabricante de arcos faz flechas com uma tora e uma linha em qualquer lugar, flechas com ponta de metal e virotes na forja, e pedras de funda a partir de rochas. As flechas com ponta de um oficial saem +1.
- **Patrulheiros e ladinos começam prontos para atirar**: um arco curto e 30 flechas, uma funda e 15 pedras.
- **Obras-primas com nome.** A obra-prima de um mestre às vezes ganha nome ("Ashtooth, espada longa de aço de mestre +3") e vira relíquia de quem a fez: vale muito mais e aparece no necrotério.
- **Examinar um item** (Shift+I): o que é, seus números, material, autor e qualidade, gravação, desgaste e valor.
- **O trabalho do ferreiro da cidade.** Ferrarias e armarias vendem uma peça assinada pelo próprio ferreiro, e as ferrarias vendem munição em feixes.
- Munição, matérias-primas e comida se juntam numa pilha só ao pegar ou fabricar; munição é negociada como arma e vendida por pilha. As matérias-primas agora aparecem no inventário.
- Um mendigo ou estudioso preocupado não recebe mais um pedido que a própria conversa escondia.

### Salvamentos
- O formato de salvamento passa para **18**: saves da Versão 17 não carregam.

## Versão 17 — Moeda e caravana

### Economia e mercado
- **Oferta e procura.** Cada cidade tem seus gostos por armas, armaduras, poções, livros, varinhas, joias, comida, matérias-primas e ferramentas. Venda dez espadas a um ferreiro e o que a cidade paga por lâminas cai; o preço se recupera em poucos dias.
- **Caravanas e estradas saqueadas.** Toda semana chega uma caravana (um tipo de mercadoria fica barato e o excedente da cidade é comprado) ou a estrada é saqueada (esse tipo fica caro). Regiões perigosas são mais saqueadas. A notícia te recebe no portão.
- **A praça do mercado.** As barracas abrem um menu. Em dia de feira, mercadores de fora expõem suas mercadorias e as barracas pagam mais.
- **O quadro de encomendas.** Toda semana a cidade pede duas coisas, pagando bem (muitas vezes algo que você pode fabricar), e oferece uma barata.
- **Seu próprio balcão.** Alugue um balcão por uma semana, exponha até oito coisas, peça preço barato, justo ou caro e volte pelas moedas: ele vende enquanto você está fora.
- **Barganha.** Carisma, o humor do comerciante (mostrado no balcão), a reputação na Guilda e a taxa da Guilda mexem nos preços. Seu trabalho assinado vende por um quarto a mais quando você é mestre.
- **Gastos.** Pedágio no portão de vilas e cidades (amigos da Guarda entram de graça), aluguel do balcão e taxa da Guilda.
- **Histórico de preços.** O diário (F7) lista os últimos preços que você viu, cidade a cidade.
- Matérias-primas e comida vendem por pilha, e a loja agora paga do próprio bolso.

### Salvamentos
- O formato de save passa para **17**: saves da Versão 16 não carregam.

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
