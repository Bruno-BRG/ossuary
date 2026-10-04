# Como contribuir com o Ossuary

[English](CONTRIBUTING.md) · **Português (Brasil)**

Obrigado por querer ajudar. Este guia diz **como o projeto é organizado**, **o que não quebrar** e **como adicionar as coisas mais comuns**
(magias, itens, animações, traduções) em poucas linhas. O código e os commits ficam em inglês/português à vontade; a documentação em `docs/` é em português.

> **Versão e licença.** O jogo está na **Versão 13** (nos manifestos: `0.13.0`). O projeto usa a [licença MIT](LICENSE): ao contribuir,
> você concorda que o seu código seja distribuído sob ela. A fonte `unscii-16` tem atribuição própria ([assets/fonts/NOTICE.md](assets/fonts/NOTICE.md)).

## Sumário

1. [Antes de começar](#antes-de-começar)
2. [Preparar o ambiente](#preparar-o-ambiente)
3. [Regras de ouro](#regras-de-ouro)
4. [Fluxo de trabalho](#fluxo-de-trabalho)
5. [Receitas: adicionar coisas](#receitas-adicionar-coisas)
6. [Testes](#testes)
7. [Textos e traduções](#textos-e-traduções)
8. [Visual](#visual)
9. [Pull requests](#pull-requests)
10. [Reportar bugs e sugerir ideias](#reportar-bugs-e-sugerir-ideias)

## Antes de começar

- Leia o [README](README.pt-BR.md), [`docs/overview.md`](docs/overview.md) e [`docs/architecture.md`](docs/architecture.md).
- **Leia [`docs/todo.md`](docs/todo.md)**: é o backlog vivo (o que falta e o que já foi feito). Se a sua ideia já está lá, ótimo; se não, acrescente-a na categoria certa.
- [`AGENTS.md`](AGENTS.md) resume as regras para agentes de código; vale para pessoas também.
- Para algo grande (sistema novo, mudança de regra, mudança de protocolo), **abra uma issue antes** de escrever muito código.

## Preparar o ambiente

Alvo: **Windows x64**. Os scripts são PowerShell (`.ps1`).

| Ferramenta | Observação |
|---|---|
| Git, PowerShell | scripts com `-ExecutionPolicy Bypass` se estiverem bloqueados |
| Node.js | 22.12+ ou 24+ |
| Rust | stable, toolchain MSVC |
| Visual Studio Build Tools | "Desenvolvimento para desktop com C++" + SDK do Windows |
| .NET 10 SDK | instalado **localmente** em `.tools/` pelo `setup` |

```powershell
git clone https://github.com/Bruno-BRG/ossuary.git
cd ossuary
.\desktop.ps1 setup     # SDK .NET 10 local + pacotes npm
.\desktop.ps1 dev       # app Tauri com recarga do frontend
.\desktop.ps1 web       # o mesmo motor no navegador (loopback)
```

Detalhes e solução de problemas em [INSTALL.pt-BR.md](INSTALL.pt-BR.md) e [`docs/build-and-test.md`](docs/build-and-test.md).
O motor e os testes (`engine/`) só precisam do SDK .NET; dá para mexer nas regras do jogo sem Rust nem Tauri.

## Regras de ouro

1. **O Core só tem simulação e UI como dados.** `engine/Ossuary.Core` não pode depender de Tauri, DOM, renderização, plataforma nem janelas.
   Todos os verbos vivem em `Commands`; toda tela é composta como `TextBuilder`.
2. **Não duplique a simulação em TypeScript nem em Rust.** O frontend só desenha a grade que o motor manda e toca animações que o motor grava.
3. **A semente é o save.** Um save é semente + teclas. Tudo que afeta a simulação precisa ser determinístico: use o `Rng` do jogo, nunca `System.Random`, hora ou ordem de dicionário.
   Variações **visuais** usam `hash(x, y)` (veja `Shapes.Hash`), nunca o `Rng` da simulação, senão gravar uma animação mudaria o jogo.
4. **Mudou regra, mudou o save.** Se uma mudança altera o resultado de uma semente, suba `SaveData.Version` em `engine/Ossuary.Desktop/SaveStore.cs` (saves antigos deixam de carregar de propósito).
5. **Campo novo no protocolo JSON** exige mexer nos **três lados**: `Request` em `engine/Ossuary.Desktop/Program.cs`, `desktop/src/protocol.ts`
   e a struct `Request` em `desktop/src-tauri/src/main.rs` (usa `deny_unknown_fields`: um campo esquecido faz o app empacotado recusar toda requisição).
   Campos novos só no *frame* de resposta (como `fx`) não precisam do Rust.
6. **stdout é do protocolo.** Erros de diagnóstico vão para `stderr`.
7. **Uma ação por vez.** O frontend não avança turnos ao desenhar, redimensionar ou salvar preferências.
8. **Preserve o trabalho dos outros.** Não reverta mudanças que você não entende; pergunte.

## Fluxo de trabalho

1. Crie uma branch a partir da `main` (`feature/…`, `fix/…`, `docs/…`).
2. Rode **`.\headless.ps1 test` antes** de mexer no Core (para saber o estado) e **depois**.
3. Faça mudanças pequenas e focadas; type-check durante a edição (`.\fastcheck.ps1`).
4. Atualize a documentação quando mudar sistema, controles, recursos ou pipeline (`docs/`).
5. **Atualize [`docs/todo.md`](docs/todo.md)** ao terminar: marque `[x]` com data e onde mora, `[~]` se parcial, e anote ideias novas na categoria certa.
6. Suite completa (`.\check.ps1`) antes de abrir o PR; build (`.\desktop.ps1 build`) se mudou algo distribuível.

Convenções de código: escreva como o código vizinho (nomes, densidade de comentários, idioma). Comentários explicam o **porquê**.

- `Game` é `partial`: mecânica nova ganha `Game.<Assunto>.cs`.
- Verbo novo: um `case` em `Commands.Execute` + um método `Do*`; strings curtas.
- `Input` não guarda estado; modais e prioridade de entrada vivem em `Session.Key`.
- `Session.Draw` compõe o frame; `TerminalRenderer.draw` só consome a grade.
- Semente `uint64` viaja no JSON como **string decimal**, nunca como `Number`.

## Receitas: adicionar coisas

Tudo é dado sempre que possível. O catálogo completo e os detalhes estão em [`docs/spells-and-items.md`](docs/spells-and-items.md).

### Uma magia

1. Uma linha em `engine/Ossuary.Core/Magic/Spells.*.cs`. Exemplo:
   ```csharp
   S("cone-of-cold", "Cone of Cold", 4, E, 10, Cn, 5, "A cone of killing frost…", 5)
       .Dmg(Cold, 4, 6).Ride(Rider.Slow, 70, 8).Look(FxKind.Cone, Elem.Cold)
   ```
   `Dmg` (dano), `Ride` (efeito: paralisar, veneno, medo…), `Aura` (buff), `Call` (invocação), `Surf` (superfície), `Shove`, `Drain`, `Chain`, `Scatter`, `Taint`, `Spec` (efeito de código) e `Look` (a animação).
2. A **animação**: `.Look(FxKind, Elem, glifo)`. Veja o resultado sem abrir a janela: `.\headless.ps1 fx cone-of-cold`.
3. O id num **livro** (`Magic/Spells.Books.cs`) e a **tradução** PT em `Loc.Spells.cs`.
4. Se for um buff novo: uma linha em `Magic/SpellBuffs.cs` (+ tradução). Criatura invocada nova: `Ally(...)` no fim de `Entities/Bestiary.cs`.

O teste `ArsenalTests` varre o catálogo: toda magia está num livro, tem animação e tradução, lança sem erro e muda alguma coisa no mundo.
Se a sua magia é legitimamente silenciosa numa arena vazia (luz, achar armadilhas…), acrescente-a à lista `Quiet` do teste.

### Um item

- **Base** (arma, armadura, anel…): `Items/Catalogue.More.cs`; números de bônus em `Items/ItemEffects.cs`.
- **Afixo** (prefixo/sufixo): `Items/Affixes.cs`. **Único**: uma linha em `Items/Artifacts.More.cs` (ramo, nível, chance, magias que empresta).
- **Varinha, pergaminho ou poção** que lança uma magia: uma linha em `Items/ItemSpells.cs`.
- Magias que combinam com cada tipo de item (itens imbuídos): `Magic/SpellFit.cs`.
- Glifo novo passa por `GlyphSet` e pelos testes `GlyphCoverage` / `GlyphsInFont`.

### Uma animação

Uma peça em `FxLib` (`engine/Ossuary.Core/Fx.cs`) que escreve passos de células no mapa e devolve o passo em que termina. Use as rampas por elemento (`FxLib.Pal`),
nada de cor fixa, e a geometria compartilhada (`Shapes`) para o que você vê coincidir com o que foi atingido. Detalhes em [`docs/spells-and-items.md`](docs/spells-and-items.md#animations).

### Um monstro, chefe, deus, raça…

Veja [`docs/systems.md`](docs/systems.md) e [`docs/rpg.md`](docs/rpg.md): quase tudo é uma tabela em `engine/Ossuary.Core/Entities/`.

## Testes

```powershell
.\fastcheck.ps1                  # type-check de C# e TypeScript
.\headless.ps1 test              # suite de simulação e fluxo do desktop (C#)
.\headless.ps1 dump panels       # layout em ASCII (use [level|overworld|panels|town|create])
.\headless.ps1 fx <magia|all>    # animações em ASCII
.\headless.ps1 soak 50 500       # bot aleatório
.\headless.ps1 balance 8 2500    # bot de balanceamento por classe × raça (veja docs/balance.md)
cd desktop; npm test             # vitest do frontend
.\check.ps1                      # tudo
```

- Cada sistema novo vem com teste (padrão do projeto). Testes que dependem de sorte devem usar amostras grandes o bastante para não serem cara-ou-coroa.
- `ARSENAL_TRACE=1 .\headless.ps1 test` mostra o stack trace dos testes do arsenal.
- O teste de banda de balanceamento (`BalanceBand`) é uma trava, não uma meta: se uma classe despencar, investigue antes de mexer no limite.

## Textos e traduções

- Texto visível ao jogador **nasce em inglês** e ganha tradução PT em `Loc.cs` (ou `Loc.Spells.cs` para magias). `Say` e `TextBuilder` já traduzem; o Core nunca decide o idioma sozinho.
- Mensagens dinâmicas (com nomes e números) casam por padrões `Rx` em `Loc.cs`. Veja [`docs/languages.md`](docs/languages.md).
- Nomes de magias e buffs em PT precisam caber na lista (≤ 26 colunas); os testes conferem.

## Visual

Direção de arte **"Fósforo & Osso"** ([`docs/visual.md`](docs/visual.md)): terminal de PC dos anos 80, fundo índigo, glifos coloridos, brilho escasso.

- Cores do jogo só em `engine/Ossuary.Core/Theme.cs` (e rampas de efeito em `FxLib`); o CSS recebe tokens do frame. Não crie outra paleta.
- Fonte bitmap fixa 8×16, sem suavização, escala inteira. Fonte nova exige atribuição e licença em `docs/visual.md` e `assets/fonts/NOTICE.md`.
- Cada célula tem glifo, fg, bg e negrito; o fundo deve transmitir significado.
- Tema, CRT e escala pertencem ao usuário (`DisplaySettings` + `localStorage`).

## Pull requests

- Título claro e no imperativo; descreva **o quê** e **por quê**. Use o modelo que o GitHub carrega.
- Marque o que testou (`headless test`, `check`, `fx`, capturas de tela se mexeu no visual).
- Um PR, um assunto. Se a mudança alterar regras (e portanto saves), diga na descrição.
- Seja gentil nas revisões: o objetivo é um jogo melhor, não ter razão.

## Reportar bugs e sugerir ideias

Use as [issues](https://github.com/Bruno-BRG/ossuary/issues) (há modelos). Um bom relato de bug traz: **versão** (a do jogo, por exemplo "Versão 11"),
**semente** (aparece no início do diário e no menu), o que você fez, o que esperava e o que aconteceu. Como a semente é o save,
semente + teclas reproduzem o problema.
