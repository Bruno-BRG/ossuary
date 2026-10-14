# Guild jobs on the quest engine

Status: **shipped** in Version 21 (2026-10-08). Sub-project S1 of *Towns and people* in [`todo.md`](../todo.md): the notice board, the workshop commissions and the Journal read one quest system. Plan and rationale for the whole track: [`living-world.md`](living-world.md), *Quest tracks*.

## Goal

One quest system. A job from the notice board, or a workshop commission, becomes a `QuestDef` on the Guild track. Progress, reward and the one-shot rule live in the engine; the board only offers jobs, accepts them and receives reports. The two Cult errands that the todo lists under *Cult track* are built the same way.

## What exists today

- `Game.Contracts.cs`: a `Contract` (kinds `hunt`, `delve`, `make`) with its own progress (`Done`). `ContractOffers()` is a pure function of town, week and seed, but it hides only the offers the hero is carrying, so a delivered job comes back in the same week. `TurnInContract` pays, gives the giver +10 standing, records `Deed.Contract` and bumps `ContractsDone`. `ContractKill` and `ContractDepth` advance progress by hand (called from [`Game.cs:130-131`](../../engine/Ossuary.Core/Game.cs:130) and [`Game.cs:481`](../../engine/Ossuary.Core/Game.cs:481)).
- `Game.Workshops.cs`: one commission per building per week (`CommissionOffer`), delivered by the `deliver:` row, which takes the product, pays, gives Guild +5 and trade XP 5. `CommissionMade` marks it done when the product comes off the bench ([`Game.Crafting.cs:237`](../../engine/Ossuary.Core/Game.Crafting.cs:237)). A delivered commission comes back in the same week too.
- The engine (`Game.Quests.cs`, `QuestBook.cs`): steps of six kinds (`Kill`, `Reach`, `Flag`, `Item`, `Talk`, `Wait`), rewards, deadlines, one-shot ids (`StartQuest` refuses an id already present, active, done or failed) and the Journal. `QuestCheck` runs at the end of every turn ([`Game.cs:563`](../../engine/Ossuary.Core/Game.cs:563)).
- Two sources of truth: the Journal lists engine quests under *Guild* and also a separate block of contracts ([`Ui.cs:1328`](../../engine/Ossuary.Core/Ui.cs:1328), [`Ui.cs:1346`](../../engine/Ossuary.Core/Ui.cs:1346)); the Character panel lists contracts too ([`Ui.cs:1416`](../../engine/Ossuary.Core/Ui.cs:1416)).

## Approaches considered

1. **Mirror contracts into the engine.** Keep `Contract` as the store and add a `QuestState` for the Journal. Rejected: two sources of truth, the problem this sub-project removes.
2. **The board is a view over the engine (chosen).** Each job becomes a `QuestDef` when the hero accepts it. The engine owns progress, reward and the one-shot rule.
3. **Add objective kinds first** (`Deliver`, `Find`, `Craft`). Rejected for this step: every case needed here fits `Item` and `Flag`, which `cult.vial` already uses. `Escort` comes with sub-project S7.

## Decisions

- **D1 The board still receives reports.** A finished hunt or delve is handed in at a notice board (any board, as today), not completed on its own. The report is a `Flag` step that the board row sets.
- **D2 Each job has a fixed shape.**
  - hunt: `Kill(target, count, branch)`, then `Flag guild.report.<id>`.
  - delve: `Reach(branch, count)`, then `Flag guild.report.<id>`.
  - commission (`make`): `Item(product)`, then `Flag guild.deliver.<id>`, set by the workshop's `deliver` row, which takes the product.
- **D3 No new objective kinds in S1.** This corrects the sub-project list, which said S1 adds *Deliver* and *Find*: `Item` plus `Flag` covers both.
- **D4 One-shot per offer.** Board offers get `guild.<town>.<week>.<index>`, where `index` is the offer's place in the generator (0–2), not in the filtered list; commissions get `guild.make.<town>.<building kind>.<week>`. An offer whose id exists (active, done or failed) is not listed again that week. Offers are filtered after the generator has drawn them, so the other offers keep their identity and the RNG sequence does not change.
- **D5 Three at a time.** `MaxContracts` still caps active Guild jobs at three, commissions included.
- **D6 Rewards and standing do not change.** Hunt and delve pay `Reward` and give the giver +10. A commission pays, gives Guild +5 and trade XP 5 on delivery.
- **D7 `ContractsDone` is derived.** It becomes the count of Done quests on the Guild track. *Hired Hand* ([`Achievements.cs:36`](../../engine/Ossuary.Core/Achievements.cs:36)) and the morgue line ([`Morgue.cs:151`](../../engine/Ossuary.Core/Morgue.cs:151)) keep their meaning.
- **D8 Deeds come from the engine.** Completion records `Deed.Quest` (weight 2). Nothing reads `Deed.Contract`, so that constant goes.
- **D9 One listing.** The Journal drops its separate block and shows Guild quests through the engine's track list. The Character panel shows one line per active Guild job. The Journal footer (`Done n Failed m`) will now count finished jobs, which it does not today.
- **D10 Save format 21.** `SaveStore.Version` moves from 20 to 21 in both places ([`SaveStore.cs:13`](../../engine/Ossuary.Desktop/SaveStore.cs:13) and [`:67`](../../engine/Ossuary.Desktop/SaveStore.cs:67)). Saves from version 20 stop loading, as with every rule change.
- **D11 Four Cult errands.** All come from the Drowned, on the same engine. The Drowned offers them under *What work?* once `cult.vial` is done, at Cult standing 10 or more; each one is offered once. Two were first; `cult.names` and `cult.shrine` came with the Cult track (2026-10-08).
  - `cult.names`: `Kill(dark acolyte, 3, The Dungeons)`, then `Flag cult.names.report`, set when the hero reports to the Drowned. Pays 100 gold; Cult +8, Temple −4.
  - `cult.shrine`: `Reach(The Sunken Vaults, 6)`, then `Flag cult.shrine.report`, set by the Drowned. Pays 130 gold; Cult +8, Temple −4, and the shrine's cold light costs 3 corruption.
  - `cult.zombies`: `Kill(human zombie, 3, The Sunken Vaults)`, then `Flag cult.zombies.report`, set when the hero reports to the Drowned. Pays 110 gold; Cult +8, Temple −4.
  - `cult.bones`: `Item(bone blade)`, then `Flag cult.bones.report`, set by the Drowned, who takes the blade, as `cult.vial` does. The hero can forge the blade ([`Trades.cs:207`](../../engine/Ossuary.Core/Items/Trades.cs:207)). Pays 150 gold; Cult +8, Temple −4.
  - The rewards are set by hand, in line with the Guild's hunts (60–120 gold) and the vial (120). The balance bot does not run quests, so it cannot tune them; the quest tests check the numbers instead.

## Design

### Data

- `Contract` becomes `JobOffer`, a template with no progress: `Kind`, `Branch`, `Target`, `Count`, `Reward`, `Giver`. It has no `Done`, `Complete` or `Key`.
- `Describe()` keeps today's English strings (`Hunt N X in B`, `Reach depth N of B`, `Craft for the town: X`). [`Loc.Game.cs:167`](../../engine/Ossuary.Core/Loc.Game.cs:167) translates them by pattern, so changing them would break the Portuguese.
- Step texts: the objective of a hunt, a delve or a commission is `Describe()`. The report step reads "Report to any notice board." (hint: "Any notice board in the Ossuary's towns."); the delivery step reads "Bring it to the workshop." (hint: "A workshop that teaches the trade."). All need Portuguese.
- `Build(town, week, index)` returns the `QuestDef`: `Track = QuestDef.Guild`, `Title = Describe()`, `Giver` the faction, `RewardGold = Reward`, and the steps of D2. `OnComplete` applies the standing (and, for commissions, Guild +5 and trade XP 5).

### Board and workshop flow

- `JobOffers(town, week)` runs the generator as it does today, then drops offers whose id already exists.
- `AcceptJob(offer)` refuses when three Guild jobs are active; otherwise it calls `StartQuest` with the built `QuestDef`.
- Board rows keep the `offer:<i>` prefix, so the test that counts three rows keeps passing. A finished job gets a `report:<quest id>` row, enabled while its current step is the report flag. The row sets the flag and calls `QuestCheck()`.
- `CommissionOffer(building)` returns a `JobOffer` and is hidden while its `guild.make` id exists. The `commission` row starts the quest. Its first step, `Item(product)`, is met at the end of the turn the product is made. The step's `done` callback prints the message that `CommissionMade` prints today ("That will do for the commission. Bring it to the workshop.", [`Loc.Crafting.cs:74`](../../engine/Ossuary.Core/Loc.Crafting.cs:74)), so `CommissionMade` goes.
- `deliver:<quest id>` needs the product in the pack, takes one, sets `guild.deliver.<id>` and calls `QuestCheck()`.
- Report and deliver rows take the quest id after the first colon. Ids contain dots, so the parser must not split on them.

### Journal and panels

- [`Ui.cs:1328`](../../engine/Ossuary.Core/Ui.cs:1328) and [`Ui.cs:1346-1348`](../../engine/Ossuary.Core/Ui.cs:1346): the separate jobs block goes. Guild quests use the existing track loop, which already shows the step text and its hint.
- [`Ui.cs:1416-1417`](../../engine/Ossuary.Core/Ui.cs:1416): one line per active Guild job, `Job: <title> (<progress>)`. Progress is for the current objective: `Progress/Count` for a kill, the best depth over `Count` for a delve, `ready` at a report step.

### Record and compatibility

- `ContractsDone` becomes a computed property over `Quests`.
- Saves are replay-based, so nothing migrates. Version 21 refuses version 20 saves (D10).

### Tests

Rewrite [`FeatureTests.cs`](../../engine/Ossuary.Headless/Tests/FeatureTests.cs) lines 871–883 (commission) and 2059–2095 (offers, accepting, turn-in, three at a time). Keep the three-row check at 2065–2066. New tests:

1. Offers match the old generator for the same town, week and seed, and listing them does not move the main `Rng` (`Rng.Calls` unchanged, as `TownIsStable` checks).
2. A delivered job's offer is not listed again that week, and is listed the next week.
3. A hunt advances on kills in its branch; `report:` completes it at any board; it pays, gives the giver +10 and records one quest deed.
4. A delve completes when the branch's best depth reaches its count.
5. A commission completes its `Item` step at the end of the turn; `deliver:` takes the product, pays, gives Guild +5 and trade XP 5.
6. At most three active Guild jobs, commissions included.
7. The Journal lists Guild quests and has no separate block (`headless.ps1 dump panels`).
8. *Hired Hand* unlocks after three finished Guild jobs, and the morgue counts them.
9. Every job string still translates to Portuguese (`LocTests`).
10. `cult.zombies` and `cult.bones` open only after `cult.vial`, complete on their objectives, and move standing as stated.
11. The `human zombie` target can spawn in The Sunken Vaults, so the zombie errand can always be finished.

### Docs and text

- Every new English string gets Portuguese through `TownText.L` or `Loc`: step texts, hints, the two Cult errands. `Craft for the town: X` has no pattern in `Loc.Game.cs` yet, so add one beside the job patterns there.
- This spec is listed in [`docs/README.md`](../README.md).
- At shipping: `living-world.md` (phase 3 status), `systems.md` (*Living world*: the board and commissions), `todo.md` (*Quest engine* and *Cult track* move to *Done*), `CHANGELOG.md` at release.

## Out of scope

- Job givers as people. They are factions today; the living-world plan wants faces, later.
- Ranks by Guild standing (in the living-world plan, not in the todo).
- Find-person objectives (S7). The Escort objective exists since the dungeon's captives (`ObjKind.Escort`, `Game.Folk.cs`), so an escort can be a quest of the Personal track; a Guild job that asks for one is still S7.
- New quality rules for commissions.

## Risks

- **Turn timing.** `Item` completes at the end of the turn. Crafting always ends a turn ([`Game.Crafting.cs:239`](../../engine/Ossuary.Core/Game.Crafting.cs:239)), so a commission never waits a turn.
- **Bots.** `soak` and `balance` do not use the board (no references outside the tests), but both must still run after the change; the plan checks it.
- **Behaviour change.** A delivered job or commission now stays closed for the week. That is the anti-grind rule from [`living-world.md`](living-world.md) (*Quest tracks*), and it is the intended change.

## Defaults I chose (say if you want another)

- Reports go to any notice board, as today.
- The Cult rewards and standing are proposals, tuned by the bot.
- The release number is yours; the save format is 21.

## As shipped

- A board does not offer a delve to a depth the hero has already reached, because the job would be ready on acceptance (`JobOffers`).
- A commission is delivered at any workshop that teaches its trade, not only the one that offered it.
- The code is in `Game.Jobs.cs`; `Game.Contracts.cs` is gone.
