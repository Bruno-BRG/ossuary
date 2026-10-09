# Magic, round two

Design and plan for the four open items of the *Magic* topic in [`docs/todo.md`](../todo.md), shipping as **Version 22 — Spells that answer back**.
Requested by the maintainer on 2026-10-08 with the instruction to plan, then implement without stopping for review.
Once shipped, the system is described in [`design/spells-and-items.md`](../design/spells-and-items.md) and this file becomes history.

## The four items

| Todo item | Decision |
|---|---|
| Monsters that cast | Casters pick a real recipe spell in view and fire it at the hero, animated. Three existing-or-new monsters and one new boss. |
| Ice × lightning | Lightning on ice runs along the ice. Lightning on a wet creature arcs to the next wet creature. |
| Damage with the projectile | The hit (damage, death, log, HP) lands when the first projectile arrives, not when the animation starts. |
| Per-element sound and a skip key | Each element has its own cast cue. Escape skips a running animation. |

## 1. Monsters that cast

### Who casts

| Monster | Where | Casts (recipe spell ids) |
|---|---|---|
| `orc shaman` (existing) | depths 5–20 | `ember-dart`, `frostbite`, `stone-shard` |
| `dark acolyte` (new, Level 6, 22 HP) | depths 4–14 | `bone-shard`, `wither`, `soul-bolt`, `rotting-burst` |
| `sorcerer` (new, Level 11, 26 HP) | depths 9–20 | `arc-flash`, `searing-orb`, `thunderstrike`, `static-field` |
| **Sallow Magister** (new boss, Level 10, 100 HP, 2d6 blows) | The Dungeons, depth 7 | phase 1: `searing-orb`, `thunderstrike`, `arc-flash`; phase 2 adds `ice-comet`, `static-field` |

The boss is a new `BossDef` with base body `dwarf lord`, glyph `M`, and its own intro, phase and fall lines (English, Portuguese in `Loc`).
Its numbers were first Level 11 with 130 HP and 2d8 blows; the dive balance bot then blamed it for about 60 of its deaths, so it was lowered.
Its music falls back to the generic boss track (`audio.ts`: `boss-<id>` falls back to `boss`).

### The pool (`Magic/MonsterSpells.cs`)

- A pool holds **recipe spells only**: a spell whose damage is data (`Dice > 0`). The hand-coded spells in `ApplySpell` assume the hero
  is the caster, so they are not usable by monsters.
- Shapes allowed: `Single`, `Ball`, `Cone`, `Line`, `Nova`. `Chain` and `Scatter` are not used by monsters.
- Riders that reach the hero: **Burn, Confuse, Blind, Stun, Poison**. Other riders (Slow, Weaken, Bleed, Fear, Sleep, Root…) are damage only,
  because the hero has no status for them. Documented as a limit.

### When a caster acts (`Game.Casters.cs`)

- In `MonsterTurn`, right after the ranged-shot check. Conditions: the monster is alert, the hero is in its sight line, and the spell is in reach
  (`Range` for most shapes, `Radius` for `Nova`). A caster on cooldown does not cast.
- The chance per eligible turn is 40% for monsters and 50% for bosses. The caster picks one eligible spell uniformly (simulation RNG, like every
  monster decision). A cast spends the turn and sets a cooldown of 2 turns on `Monster.CastCooldown`. The cooldown is not saved: a save is a replay,
  so it rebuilds itself.
- The boss runs the same path inside `BossTurn` (new `case` for `sallow-magister`), so its phases and clock still apply.

### What a cast does

- No mana, no failure roll. Damage: `Rng.Roll(ceil(dice / 2), sides, flat + level / 4)`, then `Player.ResistDamage`, so the hero's resistances count.
  Halving the dice keeps a creature that casts every other turn below the hero's own version of the spell.
- Shape around the hero: `Single`, `Ball` (centred on the hero's cell, so it always lands when in reach), `Cone` and `Line` hit the hero when aimed at him.
  `Nova` hits when the hero is within its radius of the caster.
- Riders on the hero: Burn sets him alight (`SetAlight`); Confuse and Blind set `ConfusionTurns` and `BlindTurns`; Stun sets `StunTurns`; Poison calls
  `PoisonPlayer`. Each rider is resisted by the hero's `ResistPct` like the environment is.
- Death: `CheckDeath`, with `HurtBy(Article(m))` so the morgue names the caster.
- Animation: `PlaySpellFx` gains an origin, so a cast from a monster starts at the monster. Everything else is the existing recipe animation.
- Text: `The {caster} casts {spell}!`, then `{verb} you for {n} damage.`, with Portuguese patterns in `Loc.Msgs.cs`.

## 2. Ice × lightning

Lightning reaching a creature (in `Aftermath`, the lightning case) picks the first rule that fits:

1. **Standing on ice**: `ConductIce`. Flood-fill the connected ice cells within 4 cells of the target (60 cells at most, as `Conduct` does).
   Every hostile on those cells takes half the damage; the hero too if he stands on ice (through `ResistDamage`). The log says the shock runs along the ice.
2. **Standing in water**: the existing `Conduct`, unchanged.
3. **Wet** (`WetTurns > 0`, not in water): **arc**. Jump to the nearest other wet hostile within 3 cells of the last one, up to three jumps,
   each jump at half the previous damage. Drawn with `FxLib.Chain`, which already exists.

"Frozen" means standing on an ice cell. No status is added, so nothing changes in the save format beyond the version bump.
Test: the same lightning bolt does more damage through a wet creature than a dry one, and reaches the frozen neighbour on the ice.

## 3. Damage with the projectile

Today the spell resolves at once and the animation plays over the finished frame, so a dead enemy vanishes before the bolt arrives.

- **Core**: `FxLib.Bolt` already returns the step at which the projectile arrives. It now also records it on the timeline (`FxTimeline.Arrive`), which keeps the
  first arrival of the frame (`Impact`, -1 when there is none). `Game.FxImpact` exposes it, read before `DrainFx`.
- **Protocol**: `Frame.FxHit` (outbound only; the `Request` struct and `deny_unknown_fields` are unaffected). -1 when there is no projectile.
- **Front end** (`fx.ts`, `main.ts`): a frame with `fxHit >= 0` plays its script over the **previous frame** (what the player saw) for steps before
  `fxHit`, and over the new frame from there on. The log, HP, mana and map all change at arrival, together, because they are all in the frame.
- **Limits**: effects without a projectile (cones, beams, novas, chains, scatters, traps) still resolve at once. Several projectiles in one frame share
  the first arrival. Summons appear before their own animation, as before.
- Rule kept: the frontend does not advance turns on draw; it only chooses which of the two frames to draw.

## 4. Per-element sound and the skip key

- **Cast cue by element**: `Game.SpellCue(Elem)` returns one of `arcane, fire, frost, shock, rot, holy, venom, nature, shadow, earth, blood, water, mind, wind`.
  It replaces the single `magic` cue. Each has a short pattern in `audio.ts`; `CuePriority` lists them in the old `magic` slot.
- **Launch and impact**: a cast cue plays when the animation starts. The cues that describe the outcome (`hit`, `hurt`, `kill`, `death`, `warn`, …) are
  held and played at `fxHit`, so a kill is heard when the bolt lands. Without a projectile everything plays at once, as before.
- **Skip**: while an animation plays, **Escape** jumps to its last frame and plays the held cues. The key is swallowed (it is not sent to the engine and does not open the
  pause menu). Outside an animation Escape is unchanged.

## Versions and documents

- Save format **22** (`SaveStore.cs`), with the rule change listed in the `SaveData.Version` comment.
- Manifests to **0.22.0**: `package.json` and its lock, `tauri.conf.json`, `Cargo.toml` and `Cargo.lock`.
- `CHANGELOG.md`, `CHANGELOG.pt-BR.md`, `README.md`, `README.pt-BR.md`: a Version 22 entry and badge.
- `docs/todo.md`: the four Magic items move to *Done*; the *At a glance* row and the *Next up* line change.
- `docs/design/spells-and-items.md`: the animation section (the "known limits" paragraph is rewritten) and the monster-casting rules.
- `docs/tech/languages.md` is not changed; every new string gets its Portuguese in `Loc`.

## Testing

- **Core** (`engine/Ossuary.Headless/Tests/MagicTests.cs`, wired from `CoreTests` like the other feature suites):
  every pool id exists, is a recipe spell with an allowed shape, and its caster exists; a sorcerer's spell hurts the hero through resistance;
  a cast sets the cooldown and the cooldown blocks the next one; the boss is found on The Dungeons depth 7; ice conducts to a frozen neighbour;
  wet creatures arc; `FxImpact` equals the arrival step of a bolt and is -1 for a nova; every element has a cast cue that is in `CuePriority`;
  the save format is 22.
- **Front end** (vitest): `fx.test.ts` covers the before/after switch, `onHit` firing once at the arrival step and on skip, and `skip()`;
  `audio.test.ts` covers that every element cue has a pattern.
- **Validation**: `fastcheck.ps1`, `headless.ps1 test`, `headless.ps1 dump panels` and `dump town`, `headless.ps1 fx fireball` (arrival visible in ASCII),
  `headless.ps1 soak 20 500` for the new casters, `headless.ps1 loc msgs` for untranslated text, `check.ps1`, and `desktop.ps1 build`.

## Out of scope

- Monster summons, buffs, charm, sleep and fear from spells (the hero has no state for most of them yet).
- A status for the hero that slows him.
- The balance bot knowing every spell (its own todo item).
- Sound design beyond the fourteen element cues.

## Risk and mitigation

- **Monster damage tuning.** Casters are capped by a 2-turn cooldown and a 40% chance, and their dice are below the hero's equivalent spells.
  A soak run checks the dungeon depths they live in; numbers are adjusted there, not in the spec.
- **Protocol drift.** `Frame.FxHit` is outbound only, so the three-way rule for requests does not apply; `protocol.ts` still gets the field.
- **Bolt callers.** Every bolt (ammunition, thrown molotovs, abilities) now lands at arrival. That is the intended behaviour; their tests stay as they are.

## Result of the balance check

`headless.ps1 balance 12 1500 dive` (the dive bot, 12 seeds, every class and race), before and after the tuning above:

| Run | Deaths by the Sallow Magister | Other top killers |
|---|---|---|
| Boss at 130 HP, 2d8 blows, 60% chance, full dice | 60 | fire 25, unknown 21 |
| Halved dice, still 130 HP and 2d8 blows | 43 | unknown 9, fire 8 |
| Shipped: 100 HP, 2d6 blows, 50% chance, halved dice | 18 | fire 12, unknown 8 |

Against the first run, survival rose in every class (adventurer 67% → 73%, rogue 71% → 93%, wizard 95% → 99%). The bot is not a player; the number is a watch item, not a verdict.

## Plan

Implementation order, each step checked before the next:

1. Core timing: `FxTimeline.Arrive`, `Game.FxImpact`, `Frame.FxHit` (C# and `protocol.ts`), `PlaySpellFx` origin parameter.
2. Core casting: `Magic/MonsterSpells.cs`, `Game.Casters.cs`, `Monster.CastCooldown`, the `MonsterTurn` hook, the Magister case in `BossTurn`, the bestiary entries, the boss def.
3. Ice and arc: `ConductIce` and the arc in `Game.Surfaces.cs`.
4. Element cues: `Game.SpellCue`, `CuePriority`, the `audio.ts` patterns.
5. Front end: `fx.ts` (before/after and `onHit`, `skip`), `main.ts` (paint, the Escape skip, deferred cues).
6. Loc: Portuguese for every new message, monster, boss and boss line; `headless.ps1 loc msgs` run until clean.
7. Save format 22, tests (Core and vitest), `fastcheck`, the full headless suite, dumps and soak.
8. Docs: `todo.md`, `design/spells-and-items.md`, CHANGELOG and README in both languages.
9. Versions 0.22.0 in every manifest, `check.ps1`, `desktop.ps1 build`.
