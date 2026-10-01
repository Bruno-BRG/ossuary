# Arquitetura — Ossuary

```
┌─────────────┐   comandos    ┌──────────────┐   estado    ┌──────────────┐
│  GameApp     │  "move-n"     │  Commands    │  muta       │  Game        │
│  +InputRouter├──────────────►│  (verbos)    ├────────────►│  (simulação) │
└──────┬──────┘               └──────────────┘             └──────┬───────┘
       │ Present() apenas aqui                                   │ lido por
       ▼                                                         ▼
┌──────────────┐   TextBuilder   ┌──────────────┐   mesh+atlas  ┌──────────────┐
│  GameHud/Ui  ├────────────────►│ TextBuilder  ├──────────────►│Terminal-     │
│  (compõe)    │                 │ (grade char) │               │Renderer      │
└──────────────┘                 └──────────────┘               └──────────────┘
```

## Camadas

- **Core** (`Scripts/Core`, sem Unity): `Game` (partial em 5 arquivos) é toda
  a simulação; `Commands` são os verbos; `UiState`/`UiRequests`/`ChoiceRequest`
  são a UI como dados; `GameHud`/`Ui` compõem a tela num `TextBuilder`.
- **Game** (`Scripts/Game`): `GameApp` (MonoBehaviour, bootstrap via
  `AutoSpawn`, 1 tecla/frame, `Present()` é o único toque no renderer),
  `InputRouter` (tecla → string de comando, sem estado).
- **Render** (`Scripts/Render`): `TerminalRenderer` (mesh + atlas de glifos,
  CRT scanline/vignette), `GlyphAtlas` (fonte CascadiaMono/Consolas).
- **Editor** (`Scripts/Editor/OssuaryCli.cs`): builds, `DumpFrame`,
  `CaptureFrames` (PNG offscreen), `RenderDiagnostics`.

## Regras de dependência

- Core → nada (só System + Collections). **Nunca UnityEngine.**
- Game → Core + Unity. Render → Core (tipos de tela) + Unity.
- Editor → tudo (só roda no editor).
- `Hud.Ui.Cmd = Hud.Cmd`: Commands resolve escolhas e fala com a UI por
  essa referência (fechada em `GameApp.BuildGame`).

## Dados importantes

- `Game.Mode`: Dungeon | Overworld | TownMap | GameOver | Won.
- `Game.Map` é nulo fora de dungeon/cidade; `Town`/`TownMap` só em cidade.
- `Log` (200 msgs) + `Transcript` (2000). `Map.Version` invalida o renderer.
- `Player`: HP, fome (`Nutrient`), status (cego, confuso, veneno…), anéis,
  skills, ouro, inventário. Monstros: energia/velocidade, alerta, AI
  (Ambush/Guard/Territorial), explodem/regeneram/dormem.
