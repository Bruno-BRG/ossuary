# Languages and the opening story

The game speaks **English** and **Portuguese (Brazil)**. The language switches in
`F2` → Display → **Language** (also from the title, under Options) and applies immediately.
The choice lives in `DisplaySettings.Language` and is stored in `localStorage`
alongside theme, CRT and scale; the frontend resends it to the engine on every `new`/`load`/`display`.
The Core defaults to English; the frontend's stored default is set in `desktop/src/protocol.ts`.

## How it works (`Loc.cs`)

- **English is the key.** `Loc.T("text")` returns Portuguese when the language is PT and,
  if there is no translation, the English itself: nothing breaks for a missing entry.
- `Game.Say` already passes everything through `Loc.T`: static messages match by dictionary and
  dynamic ones (names, numbers) by `Rx` patterns (`"You buy (.+) for (\d+) gold."`).
- `TextBuilder.Write/WriteClipped` go through `Loc.U`, with whole-string matching only.
  UI labels, titles and hints translate without editing each screen.
- `Names` translates proper names (regions, branches) even inside dynamic messages.
- The Core is born in **English** (`Loc.Current`); headless tests stay stable. The host sets the language.
- New text: write it in English, add the PT entry in `Loc.cs`. Column-aligned strings
  need a translation of similar width.

## Translation status

Translated: title, menu/options, HUD, panel labels, commands, overworld messages,
town, shop and victory, region/branch names, races and classes (names and descriptions), intro and opening,
**monster and item names**, combat/look/magic/potion/god/trap messages, skills, abilities, perks and the Controls panel.
Still English: altar texts, artifact lore, some rarely seen messages (run `headless.ps1 loc msgs` for the current list).

### Names, articles and gender

- English is still the key. `Loc.Names.cs` holds the Portuguese for monsters and items with their **gender**, so a pattern can say
  `{a1}` (article + name: "a adaga"), `{d1}` ("da adaga"), `{u1}` ("uma adaga") or plain `$1`, and `Contract` turns "de o" into "do".
- A capture that is a name goes through `Loc.Part`: "the jackal" becomes "o chacal", lists are split on ", ", a trailing rank or count ("Tough 2", "(x3)") is kept.
- Item names are **composed** from their parts (`blessed`, `+N`, a prefix affix, the base, an `of X` tail): add the base to `Nm`, the tail to `OfPt`, the adjective to `Adj`.
- Sentences about people use `Actor.Subj` / `Actor.Obj` ("The jackal" / "you" / "Dagny"); never write `the {TheName}` by hand around a named person.
- Patterns live in `Loc.Game.cs` (play layer, HUD, sheets) and `Loc.Msgs.cs` (messages); newer patterns win.

### Auditing a language pass

| Command | What it does |
|---|---|
| `headless.ps1 loc [seeds] [turns]` | plays in Portuguese and prints every string that reached the screen untranslated |
| `headless.ps1 loc frames` | prints every panel in Portuguese |
| `headless.ps1 loc msgs` | lists every `Say`/`Tell` in the Core that Portuguese leaves unchanged |
| `headless.ps1 loc names` | lists monster, item, affix, ability, perk and control names |

The `language` test suite fails when a name has no Portuguese.

## Opening

1. Title → **New expedition** → character creation.
2. **Animated intro** (`desktop/src/intro.ts`): the whole story, from the first grave to the
   player's arrival, in **seven ages** (The Pit, The Kings and the Archives, The Ossuary, Yendor's
   Seal, The Tower War, Today, You). Each page is a **cutaway side view of the
   world**: the pit where villages bury their dead; the floors dug by kings and
   archivists; the five layers with the branch names; Yendor descending with the seal and the
   black water rising; the Ash Tower burning whatever climbs; the walled villages and the
   bounty board (30 ◆ per head, ★ 5000 ◆ for the Amulet); and the road to the mouth of the pit.
   At the top, a timeline and the age's title ("three hundred years ago"). The **first line
   of each page** from the engine (`Story.Intro()`) is the title; the others are the text, typed out.
   Each page finishes and **advances by itself** after ~4.5 s; `Enter` completes the text and then
   advances, `Esc` skips everything. The last page states the goal (descend, take the Amulet, return)
   and waits for `Enter`. With "reduce motion" on, each page shows already complete.
3. The expedition starts **in the overworld**, in the calmest region, next to the dungeon
   entrance (`Game.BeginAtOverworld`), with the first text in the journal. The dungeon only opens
   when the player enters it.

The story follows `docs/lore.md`. The save stores `Overworld` to reproduce the right start.
