# Controls — Ossuary

Desktop source: `engine/Ossuary.Desktop/Input.cs` and `Session.cs`.
In the Tauri client, `F11` toggles fullscreen and the title screen accepts a seed.
vi-key movement wins over a verb on the unshifted key; shift recovers the verb.

## Keyboards (US and Portuguese)

`>` and `<` have **no key of their own** on any layout, including ABNT2: `>` is `Shift` + `.`
and `<` is `Shift` + `,`. Descending, climbing, fleeing a fight on the road and stepping into
the portal are all typed that way. The message the game shows when you stand on the stairs
says it ("Press `>` (Shift + .) to descend"), the **Commands** panel (`?`) lists it, and the
**Controls** panel prints the note under the stairs lines; the panel itself shows the bind as
`⇧.` and `⇧,`. Anything else on the keyboard is unaffected: `.` alone still waits a turn.

On ABNT2 the same is true for `?`: it is `AltGr` + `W` or the extra `IntlRo` key beside the
right `Shift`. Help is matched by the **character**, so `?` and `/` both work where the layout
can produce them (`/` sits on the `Q` key on some ABNT2 machines), and `Shift+/` is still `?`.

Numpad diagonals need Num Lock on (the keys send the digits otherwise), and vi keys never do.

## Move (also moves the aim/travel cursor)

`h j k l` / arrows / numpad — `y u b n` diagonals — `.` wait

**Holding the key** repeats the step (as in Caves of Qud) while the path is calm: it stops on its own
when it sees a hostile, steps on an item, stairs, altar, fountain or door, takes damage, or a new message appears.
Release and press again to rearm. Only movement repeats; it never queues turns.

## Auto-walk (dungeon)

`t` auto-explores the level (heads to items and the edges of the known map) — `` ` `` walks to the nearest
known stairs (down, otherwise up) — `~` (Shift+`` ` ``) walks to a remembered altar or fountain —
`Shift+S` rests until HP and mana are restored. All of them stop on seeing a
hostile, taking damage, a new message or stepping on an item/stairs; with an enemy in view they refuse without spending a turn.
Keys are remappable in Controls.

## Dungeon

`>` (`Shift` + `.`) descend — `<` (`Shift` + `,`) ascend (also building stairs in towns) — `g` or `,`
pick up — `d` drop — `s` search (traps/secret doors) —
`Shift+N` train a skill with XP (Trained mode) — `Shift+B` craft (what your trades, pack and place allow; `a` on a molotov throws it) — `Shift+J` every recipe — `Shift+I` examine an item (material, maker, quality, wear, worth) — `Shift+G` gather (butcher a carcass, or forage on the road) — `Shift+E` drink from a fountain (may corrupt) — `Shift+A` disarm a found trap (below or beside, preferably ahead) —
`k` (shift-K) kick / attack ahead (in town it strikes the person in front of you, and the Watch may notice) — `D` (shift) open door — `u` (shift-U)
use key — `a` apply tool (an instrument plays) — `f`/`Q` fire (a bow, crossbow or sling in hand or pack looses arrows, bolts or stones, which you pick up again; without one you hurl a stone) — `Shift+Y` choose which ammunition to loose first

## Equip and use

`w` wield — `W` wear — `T` take off armor — `P` put on ring —
`R` remove ring — `r` read scroll — `z` zap wand — `e` eat

## Menu

`Esc` or `F2` opens the menu (theme, CRT, size, language, volume, **Controls**, **Achievements**, **Past runs**). *Past runs* lists
finished expeditions (arrows, PgUp/PgDn, Home/End; Esc goes back). At the end of each run a morgue file is written
to `morgue/` in the game's data directory.

## Creation and modes

**Daily challenge** (button on the title): same seed and same hero for everyone on the day (UTC), starts straight into the story.
In *Past runs*, `D` toggles to the daily leaderboard.

On the last creation step, ◄► picks the mode: **Normal**, **Classic** (no hunger), **Hardcore** (a single save, written on
quit and erased on resume; no `F5`), the **Dive** (starts on level 5) and **Naked** (no equipment) challenges, or **Trained** (skills only rise by buying with XP). Challenges double the points.

## Road and town

A monster blocking the road: `Enter`, `Space`, `K` or `F` attack; `R` or `<` (`Shift` + `,`)
flee. In town, bumping into people talks to them and bumping into a counter, notice
board or altar opens the shop or service menu (letters choose, Esc leaves). At a counter, `b` buys, `s` sells and `o` haggles over the thing under the cursor: offer 90, 75 or 60 percent, and a refusal sours the trader for the day.

## Look and travel

`x` inspect — `v` or `L` look — `X` swap places with a monster —
`O` travel mode (overworld) — Enter confirms, Esc cancels

## Panels

`i` inventory — `c` character sheet — `H` history — `F6` discoveries — `F7` quest journal —
`?` or `/` help — `m` minimap — `F5` saves the run — `F2` or `Esc` menu — `F3` CRT — `F4` theme —
`Ctrl-Q` twice quits (abandons the run) — `Esc`/`Enter` closes a panel

## Death

Any key restarts the run (new seed). `Esc` on death leaves play.

## Menu, saves and keys

`F2` (or `Esc` with nothing open) opens the menu: Resume, Save game, Display (theme, CRT,
text size), Audio (master, music, effects volumes, 0-10, applied live),
Controls, Main menu and Quit game.

**Controls** lists every action by group. `Enter` captures the next key,
`Del` clears, `R` restores the line and `Shift+R` restores everything. A key belongs to one
action only: when reassigned, it leaves the one that had it. Arrows, `Enter`, `Esc`, `Space`, `Tab` and
`F11` are reserved and always work, so the menu is never unreachable.
Keys and volumes live in `%APPDATA%\Ossuary\settings.json`.

**Saves.** The engine is deterministic, so a save is the seed plus the canonical keys
applied in the run (`%APPDATA%\Ossuary\save.json`); loading replays the run. Menu and
option keys are not logged, and since the log stores the canonical key, changing binds does not
invalidate a save. There is a single save; "Main menu" and "Quit game" save automatically, `F5` saves
immediately. Dying, winning or abandoning erases that run's save (permadeath). On the title,
**Continue** resumes the run in progress or the save. `OSSUARY_DATA` changes the folder.

**Title.** `Esc` (or the Options button) opens the same menu over the title, before any
run; Save game and Main menu appear dimmed. `?` works by character, so it also works on
keyboards where it is not on the `/` key (ABNT2: `IntlRo` key or AltGr+W).

## Altars

Walking into an altar (`_`) opens the god's menu (arrows or letter, `Enter`, `Esc`); it costs no turn.

## Potions

`Shift+Q` drinks a potion (asks which if there are several).

## Equipment

`w` wields, `W` wears (helm, gloves, boots, cloak, body, shield), `T` takes off (asks which if there are several).
Magic items show only "magical ..." until wielded/worn or identified.

## Abilities and advances

`Shift+V` opens abilities (they spend Vigor; adjacent ones hit the single hostile neighbor, otherwise open the aim).
`Shift+C` opens the advances panel (opens by itself on level-up): arrows or letter choose, `Enter` takes.
`Shift+F` picks the body part to aim for (anywhere, head, arms, legs, eyes, wings, tail); it costs no turn and shows as AIM in the sidebar.
`a` on a bandage binds your wounds.

## Magic

`Shift+Z` opens the spell list: `←`/`→` switch **school** (All, Evo, Con, Alt, Ilu, Nec, Sag, Nat, Som), `↑`/`↓` move, `PageUp`/`PageDown`/`Home`/`End`
scroll, a letter (a–z, within the tab) or `Enter` casts, `Esc` closes. A spell marked `◆` is lent by an item you hold.
Targeted spells open the aim (nearest monster already marked); `Enter` confirms. `r` on a spellbook tries to learn its spells.
Wands (`z`), scrolls (`r`) and potions (`q`) that are spells also ask for the aim; `Esc` cancels without spending anything.
`P` puts on a ring **or amulet**; `R` removes a ring, then an amulet.
Any key cuts a running spell animation short.

## Character creation

When starting an expedition: type the name (Backspace erases), `Enter` advances; `↑`/`↓` choose race and class; `Enter` confirms; `Esc` goes back a step (on the name, back to the title).
