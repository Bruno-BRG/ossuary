# Changelog

As versões do Ossuary são numeradas só com **um número** (Versão 11, Versão 12…), como no *Project Zomboid*. Nos manifestos (`package.json`, `Cargo.toml`,
`tauri.conf.json`) e no nome do instalador a versão aparece como `0.N.0`. O **salvamento** (`SaveData.Version`) usa o mesmo número: mudou regra, sobe o número, saves antigos deixam de carregar.

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
- Novos testes `ArsenalTests` e `fx.test.ts`; documentação em [`docs/magia-e-itens.md`](docs/magia-e-itens.md); [CONTRIBUTING.md](CONTRIBUTING.md) e novo README.

## Antes da versão 11

O histórico anterior está no `git log` e no registro de itens concluídos de [`docs/a-fazer.md`](docs/a-fazer.md): cidades verticais, mundo vivo (reputação, contratos,
eventos de estrada), chefes, facções, corrupção e mutações, companheiros, conquistas, desafio diário, modos de jogo, efeitos sonoros e água animada, entre outros.
