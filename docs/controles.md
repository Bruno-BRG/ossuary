# Controles — Ossuary

Fonte desktop: `engine/Ossuary.Desktop/Input.cs` e `Session.cs`.
No cliente Tauri, `F11` alterna tela cheia e a tela inicial aceita uma semente.
Movimento vi-keys vence verbo na tecla sem shift; shift recupera o verbo.

## Mover (também move cursor de mira/viagem)

`h j k l` / setas / numpad — `y u b n` diagonais — `.` esperar

**Segurar a tecla** repete o passo (como em Caves of Qud) enquanto o caminho está calmo: para sozinho
ao ver um hostil, pisar em item, escada, altar, fonte ou porta, levar dano ou aparecer mensagem nova.
Soltar e apertar de novo rearma. Só movimento repete; nunca enfileira turnos.

## Dungeon

`>` descer — `<` subir (também escadas de prédios nas cidades) — `g` ou `,`
pegar — `d` largar — `s` procurar (armadilhas/portas secretas) —
`k` (shift-K) chutar / atacar à frente — `D` (shift) abrir porta — `u` (shift-U)
usar chave — `a` aplicar ferramenta — `f`/`Q` atirar

## Equipar e usar

`w` empunhar — `W` vestir — `T` tirar armadura — `P` pôr anel —
`R` tirar anel — `r` ler pergaminho — `z` zapar varinha — `e` comer

## Estrada e cidade

Monstro bloqueando a estrada: `Enter`, `Espaço`, `K` ou `F` atacam; `R` ou `<`
fogem. Na cidade, esbarrar em gente fala com ela e esbarrar em balcão, quadro de
avisos ou altar abre a loja ou o menu de serviços (letras escolhem, Esc sai).

## Olhar e viajar

`x` inspecionar — `v` ou `L` olhar — `X` trocar de lugar c/ monstro —
`O` modo viagem (overworld) — Enter confirma, Esc cancela

## Painéis

`i` inventário — `c` ficha — `H` histórico — `F6` descobertas —
`?` ou `/` ajuda — `m` minimapa — `F5` salva a run — `F2` ou `Esc` menu — `F3` CRT — `F4` tema —
`Ctrl-Q` duas vezes sai (abandona a run) — `Esc`/`Enter` fecha painel

## Morte

Qualquer tecla recomeça a run (seed nova). `Esc` na morte sai do play.


## Menu, saves e teclas

`F2` (ou `Esc` sem nada aberto) abre o menu: Resume, Save game, Display (tema, CRT,
tamanho do texto), Audio (master, música, efeitos; guardados para quando houver som),
Controls, Main menu e Quit game.

**Controls** lista todas as ações por grupo. `Enter` captura a próxima tecla,
`Del` limpa, `R` restaura a linha e `Shift+R` restaura tudo. Uma tecla pertence a uma
ação só: ao reatribuir, ela sai da que a tinha. Setas, `Enter`, `Esc`, `Espaço`, `Tab` e
`F11` são reservadas e sempre funcionam, para o menu nunca ficar inacessível.
As teclas e os volumes ficam em `%APPDATA%\Ossuary\settings.json`.

**Saves.** O motor é determinístico, então o save é a semente mais as teclas canônicas
aplicadas na run (`%APPDATA%\Ossuary\save.json`); carregar refaz a run. Teclas de menu e
opções não entram no log, e como o log guarda a tecla canônica, trocar binds não
invalida um save. Há um save só; "Main menu" e "Quit game" salvam sozinhos, `F5` salva
na hora. Morrer, vencer ou abandonar apaga o save daquela run (permadeath). No título,
**Continuar** retoma a run em andamento ou o save. `OSSUARY_DATA` muda a pasta.

**Título.** `Esc` (ou o botão Opções) abre o mesmo menu por cima do título, antes de
qualquer run; Save game e Main menu aparecem apagados. `?` funciona por caractere, então
vale também em teclados em que ele não fica na tecla `/` (ABNT2: tecla `IntlRo` ou AltGr+W).

## Altares

Andar contra um altar (`_`) abre o menu do deus (setas ou letra, `Enter`, `Esc`); não gasta turno.

## Poções

`Shift+Q` bebe uma poção (pergunta qual se houver várias).

## Equipamento

`w` empunha, `W` veste (elmo, luvas, botas, capa, corpo, escudo), `T` tira (pergunta qual se houver várias).
Itens mágicos mostram só "magical ..." até serem empunhados/vestidos ou identificados.

## Habilidades e avanços

`Shift+V` abre as habilidades (gastam Vigor; adjacentes atacam o único vizinho hostil, senão abre a mira).
`Shift+C` abre o painel de avanços (abre sozinho ao subir de nível): setas ou letra escolhem, `Enter` leva.

## Magia

`Shift+Z` abre a lista de magias (setas ou letra para escolher, `Enter` conjura, `Esc` fecha).
Magias com alvo abrem a mira; `Enter` confirma. `r` num livro de magias tenta aprendê-las.

## Criação de personagem

Ao começar uma expedição: digite o nome (Backspace apaga), `Enter` avança; `↑`/`↓` escolhem raça e classe; `Enter` confirma; `Esc` volta um passo (no nome, volta ao título).
