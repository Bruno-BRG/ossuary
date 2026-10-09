# Living world — plan for NPCs, quest tracks and other people

Status: **first pass implemented (2026-10-03)**: phases 1-9 exist in code, with the gaps listed in `todo.md` (category *Living world*).

## Where we are

Towns are vertical and populated (see `systems.md`, *Towns, floors and people*), but the people are scenery
with a menu:

- A townsperson has a role (`TownRole`), a name and a `Voice` number; the line they say depends only on those
  (`TownText.LineFor`), plus a reaction to reputation, corruption, mutations and companions (`TownText.Reaction`).
- Shopkeepers and service-givers open a counter. The Guild board offers 3 jobs per town and week, of only two
  kinds (*hunt N*, *reach depth N*), and a job has no giver beyond a faction.
- Rumours are a flat list; they never reveal anything about the generated world.
- Nothing the hero does changes a person, and nobody else is out in the world: outside towns it is the hero and monsters.

## Goal

The world should contain **other people with their own business**, and the hero should be one thread in it.
Concretely: people who remember you, jobs that come from someone with a face and a stake, rumours that are true
and useful, rivals and travellers on the roads, and events that happen whether or not you are watching.

## Constraints (from `AGENTS.md`, non-negotiable)

- **Core only**: all of this is simulation + UI as data. No Tauri, DOM or platform calls.
- **Determinism**: saves are replay-based and visiting must not consume the main `Rng`. Everything generated
  uses a private `Rng` seeded from `Rng.Seed ^ hash(...)` (like `Town` and `GenerateJobs`). Any *state* (quest
  step, NPC memory) changes only as a consequence of player actions and turns, never of wall-clock or draw time.
- **Text**: English first, PT beside it through `TownText.L(en, pt)`; generated sentences are assembled from
  translated fragments so PT never falls back to English.
- **No duplicate simulation** in TypeScript or Rust. New UI is a `TextBuilder` composition.
- New glyphs go through GlyphSet and the coverage tests; colours only in `Theme.cs`.

## Pillars

### 1. Persona — a person, not a role

Each named townsperson gets a small, deterministic `Persona` generated with the town (private `Rng`):

| Field | Meaning |
|---|---|
| `Trait` | 1–2 of a short list (greedy, pious, coward, proud, kind, bitter, curious…); steers which lines they pick |
| `Want` | something they need (a lost relative, a debt, a rare item, revenge on a monster kind, passage to another town) |
| `Fear` | something they avoid (a branch, a faction, a monster kind) |
| `Secret` | optional; revealed by reputation, a favour, or a rumour (the temple priest tithes the Cult, the guard captain takes bribes…) |
| `Ties` | up to 2 links to other people in the same town (spouse, rival, debtor, employer) |

`Persona` is data on the `Monster` (Townsperson) and lives in a new `Persona.cs`; generation sits in `TownGen`.
Lines are chosen by `(Role, Trait, state)` instead of `(Role, Voice)`. The goal is not thousands of lines but
**different people saying different things that stay consistent**.

### 2. Memory — flags per person

`NpcMemory` (a small set of ids plus a `Disposition` number) per named person:
`Met`, `Helped(want)`, `Wronged`, `OwesYou`, `KnowsSecret`, `Told(rumourId)`. Memory is written by quest steps,
services, attacks, theft and reputation changes, and read by the line picker and by prices.
It is saved with the run like every other piece of state.

### 3. Dialogue — short, branching, data-driven

A `Dialogue` is a tiny graph of nodes (`Say`, `Choices`, `Effect`, `Goto`) written as data, not as code per NPC:

- Opened by bumping a person who has something to say; plain chatter stays one line.
- Choices are gated by conditions (`Rep(Guild) >= 25`, `Has(item)`, `Memory.OwesYou`, `Trait is Greedy`, `Gold >= n`).
- Effects are a closed list: give/take item or gold, change reputation or disposition, set/clear a flag,
  advance a quest step, reveal a rumour, open a service.
- Rendered as a `Panel.Dialogue` (same pattern as `Panel.Service`): NPC line on top, numbered choices, `Esc` leaves.
- Authored for key roles first (Elder, Captain, Bard, Priest, Innkeeper); generated templates for everyone else.

### 4. Quest tracks

Replace the two hard-coded contract kinds with a **quest engine**; contracts become one *template* among many.

- **QuestDef** (data): id, track, template, giver (a person or a faction), a list of **steps**, and rewards.
- **Step** = an objective + a text + an optional completion dialogue. Objective kinds:
  `Kill(monster, n, branch)`, `Reach(branch, depth)`, `Fetch(item)`, `Deliver(item, person)`,
  `Find(person | place)`, `Escort(person, to)`, `Talk(person)`, `Survive(turns)`, `Discover(altar | vault)`,
  `Wait(days)`. Progress is counted from the game's own events (the hooks `QuestKill` and `QuestDepth` in `Game.Quests.cs`).
- **QuestState** (saved): def id, current step, per-objective counters, flags. Offers stay a pure function of
  `(town, week, seed)`; only accepted quests are stored.
- **Tracks** group quests so the journal reads as stories rather than a list:

| Track | What it is | Shape |
|---|---|---|
| **Main** | The Amulet of Yendor; the Archivist's story | long, 4–6 steps; the elder, a scholar and a ghost |
| **Guild** | Reach League jobs (today's board, expanded) | short, repeatable, ranks unlocked by Guild reputation |
| **Watch** | law and order: bandits on the road, a smuggler, a prisoner to fetch | short chains, Watch reputation |
| **Temple** | cleansing, relics, tainted people | tied to corruption and altars |
| **Cult** | the dark mirror of the Temple; grave goods, rituals | opens at Cult reputation; costs Temple/Watch standing |
| **Personal** | from a person's `Want`: find my brother, repay my debt, avenge my child | 2–4 steps, one named NPC, memory afterwards |
| **Rival** | an NPC party you keep meeting (see pillar 6) | spans towns and branches |
| **Region** | a branch or road mystery: why the Warrens never ended, who flooded a vault | discoverable only by rumours |

- **Journal**: a `Panel.Journal` listing tracks → active quests → current step, with a "where/who" hint
  (town, building, branch, depth) taken from the objective so the player is never lost.
- **Consequences**: finishing, failing or ignoring a quest writes memory flags and reputation; some quests can
  fail by time (`Wait`) or by an NPC dying, and the failure is part of the story, not a silent drop.
- **Anti-grind rules**: a quest can name a one-shot id so it never re-offers; board jobs keep the weekly rotation.

### 5. Rumours with teeth

A rumour becomes a **record that points at something real**:
`Rumour { Id, Text(en/pt), Truth, Reveal }` where `Reveal` marks a thing in the generated world
(an altar location on a branch, a boss lair, a secret vault, a cursed relic, a quest hook, a faction's weak point).

- Truth is not guaranteed: `Truth` is true, partial or false, decided at generation. Persona traits and
  `Fear` bend how a person tells it (the drunk is often wrong, the scholar rarely).
- Paying for news (tavern) or good standing buys better rumours; learned rumours are listed in the journal.
- Learning a rumour with `Reveal` adds a map/marker hint (`~` travel already goes to remembered altars/fountains,
  so a revealed altar becomes a travel target without new pathing code).

### 6. Other people in the world

Make the world contain actors that are not the hero, not hostile by default, and not tied to a counter.

- **Travellers on roads and the overworld**: pedestrians, caravans (a merchant with a small stock and an escort),
  pilgrims, refugees. They move on the overworld map by a turn-hash like townsfolk do in town, never with the main `Rng`.
- **Delvers in the dungeon**: NPC adventurers met on floors (a wounded one asking for a potion, a looter
  fleeing a monster, a dead party's remains with a note). Placed at level generation like any other monster.
- **Rival party**: a persistent named group (by faction or temperament) that races you for the same objectives.
  Their progress is simulated coarsely, by days passing, not by moving them every turn. You meet them in towns
  (taverns, the board) and on floors; outcomes feed the *Rival* track.
- **Prisoners and captives** in dungeons who can be freed and walk back to a town (a free `Personal` quest).
  *As built (2026-10-08):* `Game.Folk.cs`. A captive is freed with `D` facing them; the walk home is the `Escort` objective, and the gate ends it (see `systems.md`, *Dungeon folk*). The delvers of this list are built too: the wounded one wants a potion, the looter runs, the remains are a note and a pack.
- **Companions with a voice**: your sellsword comments on places and quests (builds on `Game.Companions.cs`).
- **Ghosts of previous runs**: `Bones.cs` already stores a graveyard; ghosts and notes of earlier heroes give
  those people a place in the world's story.

### 7. Living towns — things happen

`Game.Events.cs` already exists for road events. Add **town events** that start on a deterministic schedule
(function of town and day):

- market day (extra stalls, better prices), festival (bard performs, ale discounts), funeral (priest busy, mourners),
  theft (a shop is robbed, guards search, a quest appears), fire or plague (services blocked), raid on the walls
  (townspeople flee indoors, guards fight, you may help for reputation), an executed prisoner, a stranger arrives.
- Each event changes what is on the map (extra NPCs, closed doors, notices), what people say (a pool per event),
  and may offer a one-off quest. Events end on a day count, and their outcome is remembered (a burnt building stays burnt).
- Shared routine: people already go home at night; add morning/noon/evening spots (market, tavern, temple) per `Persona`.

### 8. Overworld and branches get inhabitants too

Roads, inns on the road, camps and ruins use the same `Persona`/dialogue/quest pieces, with small variants:
a campfire with a lone traveller, a hermit offering a one-shot quest, a toll keeper, a peddler.
These are overworld/branch *encounters* (like road events) rather than full towns.

### 9. The world remembers (design principle)

**Every meaningful action leaves a mark the player can later see.** This is a rule for all the pillars above, not a feature on the side:
if the hero did something that matters (killed, spared, robbed, saved, lied, paid, burned, freed, ignored a plea), some part of the world must change
visibly: what people say, what is on the map, what things cost, who is still alive, who comes looking.

- **`WorldLedger`** (new, `Game.Ledger.cs`): an append-only record of deeds, each with *what*, *who/where*, *when* (`World.Day`, `Turn`) and *weight*:
  `Killed(who|kind)`, `Spared`, `Stole(value)`, `Saved`, `Failed(quest)`, `Freed`, `Burnt(building)`, `Bribed`, `Broke(oath)`, `Cleared(place)`.
- **Consumers**, all read-only on the ledger so they stay deterministic:
  - NPC line picker and `NpcMemory` (a saved person says "you killed my brother" or "you cleared the cellar").
  - Prices, services and access (`Haggle`, closed doors, refused healing).
  - Town state on re-entry (a burnt building stays burnt, a cleared cellar is quiet, a dead shopkeeper is replaced by an heir who says so).
  - Bounty and guards (`Crime and the Watch`), notice board and rumours (a rumour about *you* spreads to neighbouring towns after a few days).
  - Epilogue text (the ending lists the deeds that mattered), morgue and the `Discoveries` codex.
- **Memorable on purpose**: each deed type has at least one visible consequence in town and one in conversation; a test per deed type
  checks that the consequence shows up (see *Phases*, 1).
- **Scope**: only deeds the game already detects at one choke point (kill, quest step, service, event row, crime) are recorded; no new global event bus is required.
- **As built (2026-10-08)**: prices, the healer, town state, rumours and the epilogue read the ledger (`Game.Ledger.cs`). The fixed numbers: +5% on counters per killing here (cap 25%), −2% per good deed here (cap 10%); a healer refuses at two killings here; news of a killing reaches the three nearest towns three days later and stays news for forty; the epilogue names the six heaviest deeds (weight 3 or more) in the order they were done. A shopkeeper who dies is replaced by a heir who says so, and the heir's words depend on whether the ledger holds the hero's hand in it. The Guild's cellar holds three dead that are not townsfolk (`Town.Lurkers`) and stay down once the last is cleared, which writes a `cleared` deed. Each consumer has a test in `FeatureTests.cs`.

## Data and code layout

New files, all in `engine/Ossuary.Core` (partial `Game` pattern from `AGENTS.md`):

| File | Contents |
|---|---|
| `Persona.cs` | `Persona`, `Trait`, generation helpers |
| `NpcMemory.cs` | per-person flags and disposition |
| `Dialogue.cs` | node graph, conditions, effects, interpreter |
| `Quests/QuestDef.cs`, `Quests/Templates.cs` | step/objective model, templates per track |
| `Game.Quests.cs` | accept/advance/fail/turn-in, event hooks, journal data |
| `Game.Rumours.cs` | rumour records, learn/reveal |
| `Game.Travellers.cs`, `Game.Rivals.cs` | other actors on the map, rival simulation |
| `Game.TownEvents.cs` | schedule, effects, outcomes |
| `TownText.cs` | extended with fragment pools and PT for everything above |

The notice board runs on the engine: each job is a quest on the `Guild` track (`Game.Jobs.cs`, [`guild-jobs.md`](guild-jobs.md)).
Desktop protocol: **no new fields expected** (panels are `TextBuilder` data); if one is needed it must be added on
all three sides (`Program.cs`, `protocol.ts`, `main.rs`) as in `AGENTS.md`.

## Phases

Each phase ships on its own, with headless tests and a doc update.

1. **Foundation** — `Persona` + `NpcMemory` on townspeople; line picker uses trait/state; tests for determinism
   (same seed ⇒ same personas) and for not touching `Rng`.
2. **Dialogue panel** — `Dialogue` interpreter and `Panel.Dialogue`; author the Elder, Captain, Bard, Priest, Innkeeper.
3. **Quest engine + journal** — `QuestDef`/`QuestState`/objectives; move the Guild board onto it; `Panel.Journal`;
   save/load of quest state; replay test. — shipped: the Guild board and workshop commissions run on the engine (`Game.Jobs.cs`).
4. **Personal and Watch/Temple tracks** — NPC `Want`s become quests with consequences; memory feeds lines and prices.
5. **Rumours with teeth** — rumour records, reveal markers, journal list.
6. **Town events** — schedule, 5–6 events, outcomes that persist.
7. **Travellers and delvers** — overworld pedestrians/caravans, NPC adventurers in branches, captives.
8. **Rival party and Main track** — the coarse-simulated rival and the Archivist's story as a long chain.
9. **Cult track + polish** — dark mirror quests, balance of rewards, bot support (`balance`/`soak` must not get stuck on dialogue). — the two Cult errands shipped; reward balance open.

## Risks and decisions to watch

- **Scope creep in text**: favour template fragments × traits over hand-written lines for everyone; hand-author only key roles.
- **Save compatibility**: replay-based saves mean changes to generation order break old saves; every new generator
  uses its own seeded `Rng` and no new draw from the main stream.
- **Bots and soak**: `soak`/`balance` must keep working; dialogue and quests need a "no human" path (auto-skip, or ignore).
- **Performance**: personas and rival simulation are generated lazily per visited town and per elapsed day, not per turn.
- **Legibility**: the journal and the hint line are part of the feature; a quest the player cannot find is a bug.

## Decisions (2026-10-03)

- **Time failure is per quest type.** Each `QuestDef` declares whether it can expire (`Deadline` in days, or none).
  Expected: `Wait` steps, rival races, rescues and event-born quests can fail; Main, Guild boards and Personal
  chains without a clock cannot. A failure is written to memory/reputation and shown in the journal.
- **Attacked NPCs turn hostile and the Watch answers.** Hitting a townsperson makes them hostile to the hero (or flee, by
  `Trait`: coward flees, proud fights); witnesses raise an alarm and guards converge on the hero. The Watch reputation drops,
  and the hero can fight, flee the town, or surrender (fine/jail). Town-wide hostility lasts until the guards are dealt with
  or enough days pass; killing guards is a heavy reputation and bounty hit. This replaces today's "peaceful, untouchable" rule.
- **Essential NPCs are nearly immortal.** Anyone required to progress (`Persona.Essential`: Main track givers, the Elder,
  the quest-chain people) cannot be killed by normal means: at 0 HP they are *knocked out/yield* and recover after a
  few days, with the Watch/Temple punishing the attacker. Only an explicit story event or an extreme act (deliberate, repeated,
  with a heavy reputation cost) can remove one, and then the track must have a fallback route (an heir, a scroll, another giver)
  so the game never softlocks. Non-essential named NPCs can die for real, and their death is remembered.
- **Dialogue is authored in C#** (builders in Core), with the PT text next to the EN through `TownText.L`.

- **Hostility lasts while it is seen, then the crime remains** (decided 2026-10-03, see *Crime and the Watch* below).
- **Fallback routes for essential NPCs** are designed in `main-quest.md` (*Essential NPCs*): successor, two sources for every
  piece of knowledge, any League town can stand in, no gate without a bypass.

## Crime and the Watch

Modelled on the Skyrim bounty loop, adapted to this game's turns and Houses.

**Two layers**
1. **Hostility (local, short)**: a person you hit (or rob, or who sees a crime) is hostile **while they have line of sight
   to you** and for a short grace period after they lose it. When the time passes they go back to normal (a coward who fled
   returns to their routine). Persona `Trait` decides flee/fight/call the guards.
2. **Bounty (persistent, per jurisdiction)**: a witnessed crime adds to a `Bounty` kept per region (every town in a region
   shares one Watch). The bounty stays after everyone calms down, and **guards remember and come for you**.

**Sentences scale with the harm done** (decided 2026-10-03), not with a flat price per crime type:
- **Violence**: by outcome. A scuffle with no wound is a small fine; wounding scales with damage dealt; killing a civilian is the heavy
  base penalty, and killing a guard, a priest or an essential person (knocked out, see `main-quest.md`) is heavier still.
- **Theft**: by the value taken (item value in gold, with a floor), so pocketing a loaf is not the same as emptying a till.
- **Property**: by the cost of what was damaged or burnt (a broken door, a burnt building).
- **Other**: lockpicking or trespass in view, escaping jail: small fixed penalties that stack with the above.
- The fine is the bounty; the jail sentence is `bounty / k` days, built as one day per hundred gold (`k` = 100, at least one day, at most
  thirty). The balance bot may tune `k`; the rule is *penalty follows damage and loss*.

Only **witnessed** crimes count; a crime nobody saw
adds nothing (stealth and `Game.Stealth.cs` matter here). A civilian who sees it runs to the nearest guard; a guard who sees it
acts at once.

**Arrest** (a bounty > 0 and a guard in sight, or one finds you): the guard calls *"Halt!"* and a `Dialogue` offers:
- **Pay the fine** (gold = bounty; stolen items confiscated).
- **Go to jail** (see below).
- **Resist** (fight; the bounty rises, more guards come).
- **Flee** (guards chase; out of the town they stop, but the bounty stays and the next town in the region knows).
- **Persuade/bribe** gated by Persona, Watch reputation and gold.

**Jail**: you are put in the town prison (cells already exist, with a Prisoner role). A sentence is `bounty / k` days: time
passes, hunger advances, **timed quests can fail**, the Watch keeps the confiscated goods in the evidence room (also a Main-track
backup for the Captain). You can serve it, pay your way out, **pick the lock** (the starting kit has a lock pick) or let a
Guild/Temple favour get you out. Escaping adds to the bounty.

**Clearing your name**
- **Pay**: gold equal to the bounty at the Watch counter or to the Captain.
- **Favour**: a Watch quest (Captain's job, a bandit on the road) or a Guild/Temple intercession at high reputation.
- **Time in jail**: serve the sentence; the bounty clears.
- Time alone does not clear it: guards keep the crime on record until one of the three above.

**Interactions to keep consistent**
- Essential NPCs are never killed (they are knocked out); striking one is a heavy crime and Watch reputation hit.
- A hostile town changes shop and service availability (shops close doors, the Temple refuses healing), see `Game.Town.cs`.
- Allies and companions can be dragged into a fight; sellswords flee or fight by temperament.
- A town fully hostile does not soft-lock the Main track (the Messenger notice and the successor rules in `main-quest.md`).

## As built (2026-10-08)

The figures the open questions asked for, as the code keeps them (`Game.Crime.cs`, `Game.Theft.cs`, `Dialogues.cs` *Arrest*):
- **Reach.** A bounty belongs to the region where the crime was seen. The regions that touch it (within a tile, corners included) know half
  of it, rounded up, and nothing farther: the echo does not pass a second ring. The Watch in a region asks the larger of its own bounty and
  its neighbours' echoes (`BountyHere`).
- **Settling.** Paying the fine, bribing, serving the cells and calling in a favour clear the bounty here and the neighbours' bounties
  (`ClearBountyHere`). A neighbour's echo is half its bounty, so settling in a neighbour is the cheapest way out: half the price forgives the
  whole. Left to tune with the balance bot.
- **Fine.** Gold equal to the figure held here, only when the purse covers it.
- **Cells.** `SentenceDays`: one day per hundred gold of the figure, at least one, at most thirty. Serving passes the days, nutrition falls
  by 40 a day but stops at 400, and the bounty clears.
- **Bribe.** Three halves of the figure (rounded down), only under 500, at −3 standing with the Watch.
- **Escape.** The lock pick opens the cells. The figure held here doubles (an echo counts, and the doubled figure is the region's own), at
  −10 standing with the Watch.
- **Cap.** A bounty stops at 5,000 gold.

Not built, from the plan above: confiscation of stolen goods at *Pay* and the Watch's evidence room; the Watch's own favour quest (the Guild
and Temple favour is built); the persona and standing gates on the bribe (only the gold and the 500 limit gate it today).

## Technical notes from the code (2026-10-03)

Findings that shape phase 1 (paths under `engine/Ossuary.Core`):
- Townsfolk are built in `Town.cs` `MakePerson` (~579) with the town's `Plan.R`. Persona generation must **not** draw from `p.R`
  (it would shift every later NPC and layout); use a forked or hash-seeded `Rng` (`Rng.Fork()`, or a hash of town key + `Voice` + position).
- `Monster` (`Entities/Monster.cs`) has no persona field yet; add the persona and memory in the townsfolk block (~85-92).
- Towns are cached for the life of the `Game` in `_towns` (`Game.Town.cs:21`), and NPC objects persist in `Town.Npcs` (dead ones stay flagged),
  so memory can live on the NPC; off-floor NPCs do not tick, so consequences on re-entry are computed lazily.
- Saves are **replay-based** (`Ossuary.Desktop/SaveStore.cs`: seed + keys, `Version` 11). Persona, memory, ledger and bounties must be pure
  functions of seed + keys: no `DateTime`, no unseeded `Rng`, no unordered iteration. Bump `SaveStore.Version` (two places) once simulation rules change.
- Today the hero cannot hurt a townsperson: `Game.cs:429-433` refuses `Attack`, and `Commands.cs:29-33` blocks `f z Z V k` in town; there is no theft system.
  The crime system therefore needs new entry points at those spots (and in `KillMonster`, `Game.cs:463`) plus theft and property actions.
- Hooks are direct calls, not an event bus; `KillMonster` is the single kill choke point (also calls `QuestKill`), `AddRep` (`Game.Reputation.cs`) is the
  reputation choke point. Reputation is global per House today; **bounty per region** needs a new keyed store (`Region.Name`) and a neighbour lookup
  (`Overworld.RegionAt` exists, neighbours do not).
- Witness check can reuse `Fov.HasLine` and `Pathfinder.Chebyshev`; at action time use `Def.Vision` (the `NoticeRadius` noise shift only resolves at end of turn).
- The clock is `World.Day`/`World.Hour` (advanced by travel and rest, not per turn); time-based expiry and bounty decay key off `World.Day`.
- Tests: add `Test("...", Method);` lines in `Ossuary.Headless/Tests/*.cs`; `TownIsStable` (`CoreTests.cs:1115`) asserts towns do not touch `g.Rng.Calls`.
  `FeatureTests.TownRoutine` calls `TownText.Reaction` directly, so a signature change there needs the test updated.
