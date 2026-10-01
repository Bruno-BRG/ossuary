# AGENTS.md — Ossuary

Roguelike terminal (ASCII/glyphs) em Unity **6000.3.13f1**. Projeto em
`C:\dev\ossuary`, projeto Unity em `C:\dev\ossuary\unity`.
Unity em `C:\Program Files\Unity\Hub\Editor\6000.3.13f1\Editor\Unity.exe`.

## Regra de ouro

**`Assets/Scripts/Core` NÃO pode referenciar `UnityEngine`.**
Toda a simulação (mapa, combate, itens, overworld, UI como dados) roda
headless via `tools/HeadlessMain.cs`. Se precisar de Unity, o código vai em
`Assets/Scripts/Game` (runtime), `Assets/Scripts/Render` (renderer) ou
`Assets/Editor` (CLI). Quebrou essa regra = quebrou `headless.ps1 test`.

## Layout

```
unity/Assets/Scripts/Core/      simulação + UI como dados (sem Unity)
  Game.cs                       loop de turnos, movimento, FOV, monstros, fome
  Game.Api.cs                   flags e entry points finos p/ UI
  Game.Items.cs                 pergaminhos, varinhas, comida, anéis
  Game.Targeting.cs             olhar/atirar/cavar/fechar alvo
  Game.Overworld.cs             viagem, encontros, cidades, lojas
  Commands.cs                   TODOS os verbos do jogador (testável headless)
  GameMap.cs / Tile.cs          mapa + tabela de tiles
  GameHud.cs / Ui.cs / UiState.cs  HUD, composição de tela, estado de UI
  Dungeon.cs / Gen/             branches + geradores (Rooms, Cave, Maze,
                                Barracks, Warrens, Fort)
  World/                        overworld (regiões, estradas, dia/noite)
  Entities/                     Player, Monster, Bestiary (~25+ monstros)
  Items/                        Catalogue, loot, GroundItems
  Combat/ / Fov.cs / Pathfinder.cs / Rng.cs / TextBuilder.cs / Theme.cs
unity/Assets/Scripts/Game/      GameApp.cs (bootstrap, input, Present),
                                InputRouter.cs (tecla -> comando)
unity/Assets/Scripts/Render/    TerminalRenderer.cs, GlyphAtlas.cs
unity/Assets/Editor/            OssuaryCli.cs (build, DumpFrame, CaptureFrames)
unity/Assets/Tests/             CoreTests.cs (TestRunner.RunAll)
tools/HeadlessMain.cs           main console: test | dump | soak
docs/                           documentação do sistema (PT-BR)
unity/Builds/StandaloneWindows64/Ossuary.exe  build jogável
unity/Builds/shots/             screenshots do renderer
```

## Comandos (fonte da verdade)

| Script | O que faz | Quando usar |
|---|---|---|
| `fastcheck.ps1` | type-check via Roslyn, segundos, sem lock | iteração rápida de C# |
| `headless.ps1 test` | suite headless sem Unity, sem lock | lógica de jogo |
| `headless.ps1 dump [level\|overworld\|panels]` | frames ASCII no console | revisar UI sem display |
| `headless.ps1 soak <seeds> <turns>` | bot aleatório, ex. `soak 50 500` | balanceamento/crash |
| `check.ps1` | compile-check via Unity CLI (verdade de "compila?") | antes de commit/build |
| `test.ps1` / `run-tests.ps1` | `TestRunner.RunAll` via Unity CLI (precisa do lock) | validação final |
| `unity-run.ps1 -Method <M>` | método de editor via CLI (precisa do lock) | build, scenes, dumps |

Unity recusa abrir o projeto 2x: os scripts `*-run` esperam o lock
(`unity/Temp/UnityLockfile`). Não delete o lock com editor aberto.

CLI úteis (`-Method Ossuary.EditorTools.OssuaryCli.<M>`):
`BuildWindows64`, `BuildLinux64`, `CreateScene`, `DumpFrame`,
`DumpOverworld`, `DumpLevel`, `CaptureFrames` (esse exige GPU, sem
`-nographics` — usar `-Graphics`), `RenderDiagnostics`.

## Convenções de código

- `Game` é `partial`: mecânicas novas ganham `Game.<Assunto>.cs`, não incham `Game.cs`.
- Verbo novo de jogador = 1 `case` em `Commands.Execute` + método `Do*`.
  Strings de comando são literais (`"g"`, `"s"`, `"move-n"`…); `InputRouter`
  traduz tecla -> essas strings. Não invente nomes longos.
- `InputRouter` é burro e sem estado; estado de painel/mira vive em
  `GameApp`. Cursor/grade usa `TryStep`.
- Renderer só é tocado em `GameApp.Present()`. HUD desenha num `TextBuilder`.
- **Atlas de glifos = textura da fonte do Unity, sem cópia própria.** Copiar
  pixels para um atlas privado quebra: o Alpha8 é reconstruído quando esgota
  espaço e invalida os UVs capturados → texto com letras erradas. Filtro
  `Point` é obrigatório. Detalhes e como verificar: `docs/renderer.md`.
- `UiState` é dado puro; painéis em `Panel`. Teste de UI = `hud.Draw().ToAscii()`
  com `ui.Resize(c, r)` — ver `UiComposes` em CoreTests.
- RNG determinístico por seed (`Rng`); seed 0 = aleatória (`TickCount`).
  `Game(seed)` + `Commands.Execute` = partida reproduzível. "Save" = seed (F5).
- Sem TODOs/FIXMEs no código (verificado). Estilo: comentários explicam o
  *porquê*, métodos pequenos, `Say()` p/ todo feedback ao jogador.
- Teste novo em `CoreTests.cs` via `Test(nome, fn)` + `Assert(cond, msg)`;
  segue o padrão (seed fixa, sem Unity). Suite atual: **30 testes, PASS** (núcleo + loja + papéis/level-ups + vitória).

## Sessões em paralelo (02 sessões ativas em 01/10/2026)

- Esta sessão: renderer/visual (`Render/`, shaders), `docs/`, `site/`, suite.
- Outra sessão: camada RPG (`Roles`, `Progression`, `Game.Rpg.cs`, `Player.cs`).
- Compartilhados (só mexer com suite verde antes E depois):
  `Game.cs`, `Ui.cs`, `Commands.cs`, `CoreTests.cs`, `Fov.cs`.
- Sem `git commit` ainda (outra sessão no meio de edição); `.gitignore` já criado.

## Fluxo de trabalho esperado

1. `fastcheck.ps1` enquanto edita.
2. `headless.ps1 test` p/ lógica; `dump panels` p/ ver a tela.
3. `check.ps1` antes de fechar (verdade de compilação).
4. `unity-run.ps1 -Method ...BuildWindows64` p/ o alpha jogável.
5. Atualize `docs/` se mudou sistema, controles ou pipeline.
