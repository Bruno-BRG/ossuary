# Build e teste — Ossuary

Unity **6000.3.13f1** em
`C:\Program Files\Unity\Hub\Editor\6000.3.13f1\Editor\Unity.exe`,
projeto em `C:\dev\ossuary\unity`. Logs em `%LOCALAPPDATA%\Temp\opencode\`.

## Iteração (sem lock do Unity)

- `pwsh -File C:\dev\ossuary\fastcheck.ps1` — Roslyn direto, segundos.
- `pwsh -File C:\dev\ossuary\headless.ps1 test` — suite 29 testes, sem Unity.
- `pwsh -File C:\dev\ossuary\headless.ps1 dump [level|overworld|panels]` —
  ASCII dos frames p/ revisar UI.
- `pwsh -File C:\dev\ossuary\headless.ps1 soak 50 500` — bot aleatório.

## Verdade final (com lock — um por vez)

- `check.ps1` — "compila?" (só erros CS importam).
- `test.ps1` / `run-tests.ps1` — `TestRunner.RunAll` dentro do Unity.
- `unity-run.ps1 -Method Ossuary.EditorTools.OssuaryCli.<M> [-Graphics]` —
  `BuildWindows64` | `BuildLinux64` | `CreateScene` | `DumpFrame` |
  `DumpOverworld` | `DumpLevel` | `RenderDiagnostics` | `CaptureFrames`
  (só `CaptureFrames` pede `-Graphics`, precisa de GPU).

## Suite (`Assets/Tests/CoreTests.cs`)

RNG, tiles, 6 estilos de nível, todos os branches, FOV, pathfinding,
flow field, overworld, ida-e-volta de escada, combate, itens, loot,
bestiário, soak (25 seeds × 200 turnos), UI de todos os painéis, viagem,
cidades, economia, papéis/level-ups, vitória. **29/29 PASS.**

## Artefatos

- Jogável: `unity/Builds/StandaloneWindows64/Ossuary.exe` (+ `Ossuary_Data/`).
- Screens: `unity/Builds/shots/` (`01-dungeon`, `02-dungeon-explored`,
  `03-inventory`, `04-character`, `05-overworld`, `atlas.png`).
- Cena: `unity/Assets/Scenes/Main.unity` (recriar via `CreateScene` se sumir).
