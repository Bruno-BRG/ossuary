# Tutorials: first-time hints — design

Status: approved in conversation on 2026-10-14; written spec awaiting review. Backlog item: `docs/todo.md`, *Tutorials*.

## Goal

Each system explains itself once, the first time the hero meets it, and never again on that install.

Success means:

- A new install sees each of the four first-delivery hints exactly once.
- With Tutorials off, no hint ever appears.
- No hint changes the turn, the RNG, the replay or the saved run.

## Scope of this delivery

Four hints: entering a dungeon, being wounded, levelling up, and buying in a shop. The other six from the backlog stay in `docs/todo.md` as separate open lines.

## Decisions

- **Shape:** a non-blocking card drawn over the map, two or three lines. It is not a `Panel` and takes no keys except Esc. Every other key passes through untouched.
- **Storage:** the engine's data directory, in `tutorials.json`, next to `achievements.json`. This departs from the AGENTS.md rule that puts preferences in `DisplaySettings` + localStorage. Tutorials record per-install progress, not a display preference, and the on/off toggle lives with them. No `Request` field changes, so the three-sided protocol rule does not apply.
- **Mirrors achievements:** the Core records hints shown in this run and the host flushes them to disk, exactly as `Game.Earned` is flushed by `Session.FlushAchievements`.
- **No save-format bump:** no simulation rule changes, so `SaveData.Version` stays at 23.

## Core

### Model

- `Hint` data: `Id`, a trigger, the `Loc` key of its text, and the *done* event that closes it.
- `Game.Tutorial.cs` (partial `Game`):
  - `bool TutorialsOn` (default true), set by the host.
  - `HashSet<string> SeenHints`, set by the host at start, like `AlreadyUnlocked`.
  - `List<string> NewlySeen`, read by the host to persist.
  - `bool FirstLaunch`, true when no `tutorials.json` existed.
  - `void ShowHint(string id)`: does nothing if tutorials are off, the hint is already seen, or a card is already open. Otherwise it sets `UiState.Hint = id` and adds the id to `SeenHints` and `NewlySeen`. Marking on show, not on close, means an interrupted session never repeats a hint.
  - `void HintDone(string doneEvent)`: closes the open card if its done event matches.

### Triggers and done events

| Hint id | Trigger point | Done event (closes the card) |
|---|---|---|
| `dungeon` | `Game.cs`, after `Mode = GameMode.Dungeon` | `explore`: auto-explore (`t`) starts |
| `wounded` | `Game.Wounds.cs`, `WoundFrom`, when the defender is the `Player` | `bandage`: a bandage is applied |
| `levelup` | `Game.cs`, end of the player's turn, when `Player.PendingAdvances > 0` | `perk`: an advancement is taken |
| `shop` | `Game.Overworld.cs`, `BuyFromShop`, after a successful purchase (town and road shops both use it) | `examine`: `Shift+I` is used |

Esc closes any card. The done events fire from the code that performs the action, so they are deterministic and behave the same in a replay.

### Input

- In `Session.Key`, before the normal key handling: if a card is open and the key is Esc, close the card and return. This Esc is not logged (added to `ShouldLog`), so the replay does not see it. Esc with no card open behaves as before.
- Keys pass through an open card untouched, so the hint never blocks a turn.

## First launch

- A new `Panel.FirstRun`, handled like `Panel.Settings`: its keys are never logged.
- It opens on the first "new game" when `tutorials.json` does not exist, before the creation screen if there is one.
- Two rows: "Yes, I already know the game" turns Tutorials off; "No, show me the hints" leaves them on.
- Either answer writes `tutorials.json` immediately. The panel never opens again on that install, unless the file is deleted.

## Settings (F2)

- `MenuRow` gains `Tutorials` (on/off, changed with ←→ or Enter) and `ResetTutorials` (Enter clears `SeenHints`, so every hint can show again). Both sit right after `Language`. `Panel.Settings` is already not logged.
- The menu note for `ResetTutorials` confirms the reset in the player's language.

## Host (Desktop)

- `SaveStore.ReadTutorials()` and `WriteTutorials()`, with a record `{ "enabled": bool, "seen": [string] }` in `Dir/tutorials.json`, written atomically like the achievements file.
- A missing file means first launch. An unreadable file logs to stderr and is treated as first launch.
- `Session.Start` loads the record into `Game.TutorialsOn`, `Game.SeenHints` and `Game.FirstLaunch`.
- `Session.FlushTutorials()` sits beside `FlushAchievements`, is called from `Draw`, and writes only when `NewlySeen` is non-empty or the toggle changed. It does nothing during a replay.
- stdout stays protocol-only; diagnostics go to stderr.

## Text and Loc

- `Loc.Tutorial.cs` registers every string through `P(english, portuguese)`, following `Loc.Market.cs`.
- Every player-visible string has both languages.

| Key | English | Português |
|---|---|---|
| `dungeon` | Underground. `t` explores on its own. | Subterrâneo. `t` explora sozinho. |
| `wounded` | Wounded. A bandage (`a`) stops the bleeding. | Ferido. Uma bandagem (`a`) para o sangramento. |
| `levelup` | A new level: pick a perk. Arrows and Enter take one. | Novo nível: escolha um aperfeiçoamento. Setas e Enter escolhem. |
| `shop` | Bought something? `Shift+I` shows its numbers. | Comprou algo? `Shift+I` mostra os números. |
| card footer | [Esc] close | [Esc] fechar |
| F2 row | Tutorials | Tutoriais |
| F2 row | Reset tutorials | Reiniciar tutoriais |
| first launch | Have you played before? | Você já jogou antes? |
| first launch, yes | Yes, I already know the game | Sim, já conheço o jogo |
| first launch, no | No, show me the hints | Não, me mostre as dicas |

## Visuals

- The card is drawn over the bottom rows of the map area, in the existing panel colours from `Theme.Current` and the frame glyphs `─│╭╮╰╯`. No new palette.
- It wraps inside the map width (84 columns minimum). If the hero stands inside the card's rectangle, the card moves to the top rows.

## Tests

FeatureTests (headless):

1. Each hint fires once and is not shown again in the same run.
2. With Tutorials off, no trigger shows a card and `SeenHints` does not change.
3. A hint already in `SeenHints` stays quiet.
4. The same key sequence with Tutorials on and off ends with the same turn, player state and RNG position.
5. Esc closes an open card and is not logged.
6. Each done event closes its card.

DesktopTests:

7. `tutorials.json` round-trips through `SaveStore`.
8. First launch asks once: no file opens `Panel.FirstRun`, and the answer writes the file so the next start does not ask.
9. Reset tutorials clears `SeenHints` and leaves the toggle as it was.

LocTests:

10. Every hint and panel key has a Portuguese string. The existing `Tables` gate covers this once the strings are registered with `P`.

Checks: `headless.ps1 test` before and after the Core change; `headless.ps1 dump panels` and `dump town` to check where the card lands.

## Documentation to update

- `docs/design/systems.md`: a new *Tutorials* section.
- The controls documentation under `docs/game/`: the two F2 rows.
- `docs/todo.md`: *Tutorials* becomes `[~]` with *has / left* after these four hints, and a finished item moves to *Done* with its date.

## As built

- The two F2 rows sit in the Game section after *Past runs*, which keeps the menu's navigation order and its drawing order the same.
- Card text names keys without backticks (`t`, `a`, `Shift+I`), matching the rest of the game's prose.
- The dungeon hint is raised by the hero's turn (`EndPlayerTurn`), not by level generation. A new game builds its first level before the overworld opening, so generation-time triggers would show "underground" on the overworld.
- The level-up hint needs `Level > 1`: a human starts with one perk pending at level 1.
- The card moves to the top only when it would cover the hero on a dungeon or town map; on the overworld it stays at the bottom.
- Test sessions run in a scratch data folder on an install with tutorials off (`CoreTests.RunAll`, `DesktopTests.Run`), so existing key flows are unchanged.

## Out of scope

- The other six hints in the backlog.
- Any text beyond English and Portuguese.
- Changes to the save format.
- Committing. The work stays uncommitted until asked.
