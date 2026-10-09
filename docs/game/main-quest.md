# Main questline — "The Seal"

Status: **first pass implemented (2026-10-03)**: documents, Reader, truths, six endings, the Stamp as a new cycle (gaps in `todo.md`). Part of the *Living world* plan (`living-world.md`); tracked in `todo.md`.
Names of people are working names. The shape (acts, flags, endings, fallbacks) is what matters.

## What it has to do

- Keep today's rule intact: the `amulet of Yendor` waits on the last level of The Dungeons and **leaving with it still
  wins the run** (`CheckVictory`). That is the **short path** and it must stay quick and legitimate.
- Add a **long path** that uses the rest of the Ossuary (Mines, Vaults, Spire) to answer *what the Amulet is, who sank
  the world, and what to do with it*. Knowledge, not a key, is what you collect, and it decides which endings exist.
- Fit `lore.md`: the Amulet is the seal of the sunken archive; Yendor stamped his own tomb and went down holding it;
  descending is going back in time, in reverse; the Ossuary remembers.
- Use the four Houses (Watch, Temple, Guild, Cult) as the people who want the Amulet, each for their own reason.

## Short path and long path

| | Short path | Long path |
|---|---|---|
| What you do | take the Amulet from Dungeons level 10 and walk out | gather three **truths** in three branches, then choose what to do with the Amulet |
| Required NPCs | none | the Elder, the Reader, a House authority at the end |
| Ending | **Drifter's Pay** (the League cannot pay 5000, you get what is in the purse; the Amulet is taken) | one of the **House endings** below |
| Run result | victory | victory with an ending id (morgue, achievements, daily leaderboard) |

The long path is never forced. Anyone who just dives and climbs still wins; the long path changes the meaning of winning.

## Acts

### Prologue — Thirty gold (starting town)

- The **Elder** ★ of the League council hires you: 30 gold, a sealed **writ** promising 5000 on delivery.
- Teaches the board, the services and the Amulet's location (`StoryLine`, already in `TownText`).
- Objective: `Talk(Elder)`. No gate; the hero can walk into The Dungeons at once.

### Act I — The Warden's Ledger (The Dungeons)

- The Elder says the last warden kept a ledger of what was sealed down there. Hook: a rumour and a board notice.
- Objective: `Fetch(Warden's ledger page)`: one page placed on a middle level of The Dungeons (barracks or fort room,
  placed with the quest's own seeded `Rng`, never the main stream).
- Objective: `Deliver(page, Reader)`. The **Reader** ★ (Scholar) reads it: *the Amulet is a seal, not a jewel; whoever holds
  it opens any door of the old archive.* Unlocks the long path and records **Truth 0: The Seal**.
- Reward: the Reader will read unknown scrolls once per town visit (a service), plus the `Discoveries` codex entry.

### Act II — The three testimonies (three branches)

Each is a short chain: a rumour points to a place; a fetch objective; a short scene (`Dialogue`) when the truth lands.

| Truth | Branch | What you find | What it says |
|---|---|---|---|
| **The Vote** | The Mines of Dwarfdeep | the dwarf council's tally, in a lord's vault | the Sinking was not an accident: the Iron Holds voted to drown the night shift |
| **The Last Entry** | The Sunken Vaults | Yendor's final ledger entry; the **Archivist's Shade** speaks | Yendor sealed the pit so the dead could not climb out and so *kings* could not either; he asks the hero to put the seal back or finish the job |
| **The Order** | The Ashen Spire | the guardian of the deep names who raised the Spire | the Spire was built to burn the dead and was paid for by the founders of the League and the Holds, who then forgot |

- Truths are flags (`Truth.Vote`, `Truth.LastEntry`, `Truth.Order`); they can be gathered in any order, and each branch
  has a rumour that hints at it from the first town.
- The Warrens and the Annex are **not** part of the main chain; they stay free for Region and Personal tracks.
- Each truth raises a house of cards in the towns: the Elder, the Captain and the Priest get new lines and new options
  once the hero has the matching truth (Persona memory, `living-world.md`).

### Act III — What to do with it (the choice)

Back on the surface with the Amulet, the houses come calling. Which options exist depends on **truths**, **reputation** and
**corruption**; an unavailable option shows greyed with the reason.

| Ending | House | Requires | What happens |
|---|---|---|---|
| **Drifter's Pay** | League | the Amulet only | the League takes it and pays what it has; the world continues unchanged |
| **The Pit Shut** | Temple | Temple ≥ 25, Truth 0 and **The Last Entry** | the priest buries the Amulet in the rite Yendor meant; the dead rest; the Ossuary closes |
| **The Archive Opens** | Cult | Cult ≥ 25, Truth 0 | the seal opens every vault; the Drowned walk the Reach; very rich, very dark |
| **The Warden** | Watch | Watch ≥ 25, **The Order** | the Watch posts a permanent guard; you become the Warden who keeps the seal, with a stipend and a duty |
| **The Auction** | Guild | Guild ≥ 25 | the Amulet is sold off to whoever bids; the Reach gets rich and the Ossuary gets a new owner |
| **The Stamp** | none | all three truths | you carry the seal back down to the Vaults and stamp it again, as Yendor did, and become the next archivist (a true ending that keeps the pit shut by taking his place) |

The ceremony needs an authority of the house that offers it (Priest, Captain, Cult speaker, Guild factor). If that person
has been removed, a successor (see *Essential NPCs*) takes the part with a harder disposition, or a rite can be done at the
matching altar. The epilogue lines are assembled from truths and the ending id.

## Essential NPCs ★

Rules from `living-world.md`: knocked out, never killed by normal means; punished by the Watch; recover after days.
Extra rules so the story can **never softlock**.

**As built (2026-10-08).** The essential set is the Elder, the Reader, the Captain, the High Priest and the Cult's voice (the beggar).
Every one has an apprentice in the same town (*Successor* in the table below). A blow at one who lies out cold is an extreme act:
the first two leave them down, each with a bounty and a loss of standing with the Watch and the Temple; the third removes them. The
apprentice takes the post where the fallen stood, keeps the building's counter, starts 30 disposition lower and **owes one favour**:
the quest `heir.<role>` (put down three of a kind, then report to the new holder). The post's talk (`Talk(role)`) counts only after
the report. An heir removed while owing takes the favour with them. With no apprentice left, the post stands vacant and the Guild
posts a Messenger notice naming the empty posts wherever its counter or board is read; the League's councils in other towns sign
for the hero, because `Talk(role)` matches any holder of the role.

1. **Every essential has a successor** generated in the same town (`Persona.Successor`): co-councillor, apprentice,
   lieutenant, acolyte, second in the Cult. If an essential is removed by a story event or an extreme act, the successor
   inherits the role and the quest flags, but starts with a worse disposition and one extra favour step.
2. **Knowledge lives in two places**: a person *and* an item or a place (a codex page, an altar, a rumour that points at
   the same fact). Losing the person costs time or a favour, never the information.
3. **All the League towns are the same town** (`lore.md`): a councillor can stand in for the Elder in any town. If a whole
   town loses its essentials, the Guild board posts a **Messenger notice** that re-anchors the track in the nearest town.
4. **No gate without a bypass**: the only hard gates are physical items you must carry; every person-gate has the
   successor or the item route.
5. **Non-essential named NPCs can die for real**, and their death is remembered; the Main track never depends on them.

| Essential | Role | Successor | Backup source of the same knowledge |
|---|---|---|---|
| Elder | gives the writ, opens the track | a co-councillor, in any League town | the writ itself, plus the board notice |
| Reader | reads the ledger, anchors the truths | the apprentice (same library) | a codex found in the Vaults, plus a Temple scribe who can read funerary script |
| Captain | Watch ending, crime and pardon | the lieutenant (a guard promoted) | the Watch ledger in the evidence room |
| High Priest | The Pit Shut | the acolyte | the rite written in the Reader's codex, doable at any shrine you have already prayed at |
| Cult speaker | The Archive Opens | the second voice | the Drowned altars in the Vaults |

The Archivist's Shade and the guardian of the deep are not NPCs in this sense: they are encounters that can be failed
and retried. As built (2026-10-08) both are bosses that stay until they fall: the Shade does not return after a few days (no timer), and the guardian's retry is the existing bones system.

**As built (2026-10-08).** Both are `BossDef`s (`Game.Bosses.cs`): the Shade on The Sunken Vaults at level 10, the Guardian of the Deep on The Ashen Spire at level 12. A boss stays on its level until it falls, so a hero who flees or dies finds it where it stood; the bones a death leaves are the cross-run retry that already exists. There is no timer that brings the Shade back after a few days: the spec's timer is not built. The ceremony's speaker is the living holder of the house's post in a town the hero has walked into this run (`Game.Main.cs`, `HouseSpeaker`); with the post empty, the rite is done at the altar. The morgue records the ending (`Morgue.cs`, `EndingName`).

## Quest-engine mapping (`living-world.md`)

All of it uses the planned objectives; nothing here needs a special system:

| Beat | Objective |
|---|---|
| Hire | `Talk(Elder)` |
| Ledger | `Fetch(item)` then `Deliver(item, Reader)` |
| Testimonies | `Discover(place)` + `Fetch(item)` per branch, then a `Dialogue` scene |
| Shade and guardian | `Talk` inside a boss/encounter, with `Survive(turns)` as the failure path |
| Choice | `Talk(House authority)` with gated choices from reputation, corruption and truths |
| Ending | an `Effect` that sets `EndingId`, records it in the morgue and ends the run as a win |

Deadlines: **none** on the Main track. The only time-based failures are Rival races and event quests (`living-world.md`).

## Data and tests

- `Quests/Main.cs`: the track as C# builders with the PT text next to the EN through `TownText.L`.
- Placement of the page and the three testimonies uses private `Rng` seeded from `Rng.Seed ^ hash(questId, branch, depth)`.
- Headless tests: determinism of placement; each ending reachable with a script bot; each essential NPC removable
  (knocked out and successor takes over) without blocking an ending; the short path still wins with no quest taken;
  save/load mid-chain.
- Morgue and the daily leaderboard record the ending id; achievements per ending and for all six.

## Decisions (2026-10-03)

- **New game plus is in.** The Stamp (and, where it fits, other endings) can continue play as a new cycle instead of ending the run.
  Settled 2026-10-08 (as built in `StartNewCycle`, `Game.Main.cs`): the hero keeps gold, pack, gear (all but the Amulet), level, skills,
  companions and House standing; the truths, the ledger (the legend), the towns' memory and the bosses that fell stay. What resets: every
  bounty, the Amulet (no second one is placed, so the hunt does not start over), health (full), and the seal errand (an open one leaves the
  journal; the Elder does not offer it again). The hero comes up on the overworld. The candidate reset of gold, most items and local
  reputation was not taken: the Houses remember what the hero did, and the new cycle is the reward for the ending.
- **The truths change the world.** Each truth becomes public history with visible effects (e.g. after *The Vote*, Iron Holds guards and shopkeepers
  are cold or hostile to the hero; after *The Order*, the League council's lines and prices change). This follows the world-memory principle in `living-world.md` (pillar 9):
  important actions must be felt and remembered by the people around the hero.
- Order of the Spire/Vault truths if The Order's guardian is already strong at the depth the hero reaches: keep it a boss or a conversation first?
