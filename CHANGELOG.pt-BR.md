# Changelog

[English](CHANGELOG.md) · **Português (Brasil)**

As versões do Ossuary são numeradas só com **um número** (Versão 11, Versão 12…), como no *Project Zomboid*. Nos manifestos (`package.json`, `Cargo.toml`,
`tauri.conf.json`) e no nome do instalador a versão aparece como `0.N.0`. O **salvamento** (`SaveData.Version`) usa o mesmo número: mudou regra, sobe o número, saves antigos deixam de carregar.

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
- Novos testes `ArsenalTests` e `fx.test.ts`; documentação em [`docs/spells-and-items.md`](docs/spells-and-items.md); [CONTRIBUTING.pt-BR.md](CONTRIBUTING.pt-BR.md) e novo README.

## Antes da versão 11

O histórico anterior está no `git log` e no registro de itens concluídos de [`docs/todo.md`](docs/todo.md): cidades verticais, mundo vivo (reputação, contratos,
eventos de estrada), chefes, facções, corrupção e mutações, companheiros, conquistas, desafio diário, modos de jogo, efeitos sonoros e água animada, entre outros.
