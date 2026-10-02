# AGENTS.md — Ossuary

Roguelike ASCII de fantasia sombria. Aplicativo **Tauri 2 + Rust + TypeScript/Canvas**
em `desktop/`. Simulação C# independente em `engine/Ossuary.Core`, host local
.NET 10 autocontido em `engine/Ossuary.Desktop`, testes em `engine/Ossuary.Headless`.

## Regra de ouro

O Core contém apenas simulação e UI como dados. Não pode depender de Tauri,
DOM, renderização, plataforma ou serviços de janela. Todos os verbos vivem em
`Commands`, e toda a composição de tela produz `TextBuilder`.

## Layout

- `engine/Ossuary.Core/`: Game partial, Commands, Ui, mapas, combate, itens,
  entidades, RPG, missão, geração, FOV, pathfinding, RNG e temas.
- `engine/Ossuary.Desktop/`: Input, Session, protocolo JSON por stdin/stdout.
- `engine/Ossuary.Headless/`: console test/dump/soak e Tests.
- `desktop/src/`: frontend, terminal bitmap, título, ajuste de grade e preferências.
- `desktop/src-tauri/`: janela Rust, transporte local, ciclo do processo e pacote.
- `assets/fonts/unscii-16.hex`: fonte canônica 8×16; atribuição em NOTICE.md.
- `docs/`: documentação PT-BR; capturas reais em `docs/shots/`.
- `docs/a-fazer.md`: backlog e acompanhamento de progresso, organizado por categoria.

## Comandos

| Script | Função |
|---|---|
| `desktop.ps1 setup` | prepara SDK .NET 10 local e dependências npm |
| `desktop.ps1 dev` | abre aplicativo Tauri com recarga do frontend |
| `desktop.ps1 web` | navegador em loopback com o mesmo motor real |
| `fastcheck.ps1` | type-check C# e TypeScript |
| `headless.ps1 test` | suite de simulação e fluxo desktop |
| `headless.ps1 dump [level\|overworld\|panels\|town]` | frames ASCII |
| `headless.ps1 soak <seeds> <turns>` | bot aleatório |
| `headless.ps1 balance <seeds> <turns> [dive]` | bot de balanceamento por classe×raça (ver docs/balance.md) |
| `check.ps1` | suite completa, frontend, IPC empacotado e Rust |
| `test.ps1` / `run-tests.ps1` | aliases de teste headless |
| `desktop.ps1 build` | executável Windows x64 + instalador NSIS |

MSBuild é incremental e detecta alterações de fontes. O preview web roda uma
cópia temporária privada dos assemblies para não bloquear a compilação no Windows.
O pacote distribui `ossuary.exe` e o motor `ossuary-engine.exe` lado a lado.

## Convenções

- `Game` é partial: novas mecânicas ganham `Game.<Assunto>.cs`.
- Verbo novo: case em `Commands.Execute` + método `Do*`; manter strings curtas.
- `Input` é stateless; modais e prioridade de entrada vivem em `Session.Key`.
- `Session.Draw` compõe o frame; `TerminalRenderer.draw` só consome a grade.
- O frontend não avança turnos ao desenhar, redimensionar ou persistir preferências.
- Uma ação em andamento por janela; não acumular autorepeat do teclado.
- Semente uint64 passa no JSON como string decimal, sem conversão para Number.
- Texto visível ao jogador nasce em inglês e ganha tradução PT em `Loc.cs`; `Say` e
  `TextBuilder` já traduzem (ver docs/idiomas.md). O Core nunca decide o idioma sozinho.
- Erros de diagnóstico vão para stderr; stdout é exclusivo do protocolo.
- Não duplicar a simulação em TypeScript ou Rust.

## Visual: Fósforo & Osso

- Terminal de PC dos anos 80, fundo índigo, glifos coloridos, brilho escasso.
- Cores do jogo só em `engine/Ossuary.Core/Theme.cs`, exceto cores de conteúdo
  do bestiário. O CSS recebe tokens do frame; não criar outra paleta hardcoded.
- Cada célula contém glifo, fg, bg e negrito. Fundo deve transmitir significado.
- Fonte bitmap fixa 8×16, sem suavização, escala inteira em pixels físicos.
- Novo glifo passa por GlyphSet e pelos testes GlyphCoverage/GlyphsInFont.
- Variantes visuais são hash(x,y) no desenho, nunca consumo de RNG da simulação.
- Tema, CRT e escala pertencem ao usuário: DisplaySettings + localStorage.
- Fonte nova exige atribuição e licença em docs/visual.md e assets/fonts/NOTICE.md.

## Trabalho e validação

0. Antes de começar, leia `docs/a-fazer.md`; ao terminar, atualize-o (marque `[x]` com data,
   `[~]` se parcial, e registre ideias novas na categoria certa). É o nosso acompanhamento de progresso.
1. Type-check durante a edição.
2. Suite headless antes e depois de alterações no Core; dump panels para layout.
3. Suite completa antes de fechar e build depois de mudanças distribuíveis.
4. Atualizar documentação quando mudar sistema, controles, recursos ou pipeline.
5. Preservar alterações locais de outras sessões; não fazer commit sem solicitação.
