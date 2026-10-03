## What and why

<!-- What changes and what problem it solves. Link the issue, if any. -->

## Type

- [ ] Bug fix
- [ ] Mechanics / content (spell, item, monster…)
- [ ] Visuals / animation
- [ ] Documentation / translation
- [ ] Tooling / tests

## How I tested

- [ ] `.\headless.ps1 test`
- [ ] `.\check.ps1`
- [ ] `.\headless.ps1 fx <spell>` / `dump panels` / screenshot (if visuals changed)

## Checklist

- [ ] The Core still has no UI/platform dependency (golden rule)
- [ ] Nothing non-deterministic in the simulation (used the game's `Rng`; visual effects use a hash)
- [ ] If this changes what a seed produces, I bumped `SaveData.Version` and say so here
- [ ] A new JSON protocol field is updated on all three sides (if any)
- [ ] New text has a Portuguese translation (`Loc.cs` / `Loc.Spells.cs`)
- [ ] Updated `docs/` and `docs/todo.md`
