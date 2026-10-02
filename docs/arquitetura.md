# Arquitetura — Ossuary

Ossuary é um aplicativo Tauri 2. Rust controla a janela e um processo privado
.NET 10; TypeScript desenha o terminal e entrega entrada. C# contém a simulação
completa e compõe as telas como dados, em uma única biblioteca independente.

```text
KeyboardEvent / resize / preferências
                 │
                 ▼
        desktop/src (TypeScript)
                 │ invoke game_request
                 ▼
        desktop/src-tauri (Rust)
                 │ JSON UTF-8 por stdin/stdout
                 ▼
        engine/Ossuary.Desktop
        Input → Session → Commands → Game
                               │
                         GameHud / Ui
                               │
                          TextBuilder
                               │ Frame
                 ◄─────────────┘
        Canvas 8×16 + glow + CRT
```

## Projetos

| Projeto | Responsabilidade |
|---|---|
| `engine/Ossuary.Core` | simulação, entidades, geração, RPG, combate, UI como dados |
| `engine/Ossuary.Desktop` | host .NET 10, teclado físico, modais, protocolo |
| `engine/Ossuary.Headless` | testes, dumps ASCII, bot de soak |
| `desktop/src` | renderer, fonte bitmap, título, layout e preferências |
| `desktop/src-tauri` | janela nativa, ciclo de processo, validação IPC e empacotamento |
| `assets/fonts` | fonte canônica e atribuição |

## Dependências

O Core não depende da janela, do DOM, do transporte ou do renderer. Novas
mecânicas vivem em `Game.<Assunto>.cs`; os verbos pertencem a `Commands`.
`Ui.Draw` compõe glifos, fg/bg e negrito em `TextBuilder`. O host converte a
grade em um frame; o frontend nunca reimplementa regras de gameplay.

`Session.Key` mantém a prioridade: atalhos de display, morte/vitória,
seleção de item, opções, viagem, mira, loja, outros painéis e jogo normal.
`Input` traduz `KeyboardEvent.code` com shift/ctrl sem manter estado.

## Transporte

Uma requisição e uma resposta por linha UTF-8. Operações: `new`, `key`,
`resize`, `display`, `frame`. A resposta é `{ok,frame}` ou `{ok:false,error}`.
Diagnósticos vão para stderr. Frames contêm grades em ordem linha/coluna,
dimensões, semente decimal, turno, modo, painel e tokens de display.

A semente uint64 é uma string decimal. Nenhum caminho de arquivo, comando
shell ou operação de rede pode ser solicitado por esse protocolo. Rust
valida as operações, serializa os pedidos e aplica timeout de 10 segundos.
As esperas ocorrem fora da thread gráfica. Encerrar o app recolhe o motor.

O pacote usa IPC privado e funciona localmente. O modo web de desenvolvimento
usa um endpoint Vite apenas em loopback, com o mesmo motor e uma cópia privada
dos assemblies para manter o build disponível durante a prévia.

## Turnos e display

Somente comandos da simulação avançam turnos. Redimensionar, renderizar,
persistir opções e verificar estado da janela não alteram o jogo. A interface
aceita uma ação em andamento e ignora autorepeat para evitar filas de turnos.
Tema, CRT e escala são dados do usuário, persistidos em localStorage; começar
uma run pela tela inicial ou reiniciar após morte conserva essas preferências.

## Dados principais

- `Game.Mode`: Dungeon, Overworld, TownMap, GameOver, Won.
- `Game.Map`: dungeon/cidade; mundo regional em `Game.World`.
- `Player`: HP, fome, status, equipamento, skills, ouro e inventário.
- Monstros: energia, velocidade, alerta, percepção e AI.
- `UiState`: painéis, cursores de mira/viagem/loja e seleção de itens.
- `Log` e `Transcript`: mensagens recentes e histórico.
- `Rng`: geração e mecânicas determinísticas para a mesma semente/entrada.

Referência operacional: [desktop.md](desktop.md).
