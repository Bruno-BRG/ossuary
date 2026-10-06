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

Everything a player can read is translated. The only English left is **proper nouns** (people, gods,
towns, Dwarfdeep, Yendor, the League's "Reach") and the few words that are the same in both languages
(`MP`, `XP`, `CA`, `PV`, `Con`, `Int`, `Altar`, `CRT`, `Mana`, `Exp`).

Translated: the front end (title labels, the boot line, the error messages and the `aria-label` a
screen reader reads), the intro and the opening, the HUD and every panel, the message log, the altar of
a god (title, domain, gift, likes, dislikes, boon, both blessings and every row of the menu), the six
bosses (names and every line they say), the endings and the new cycle, the morgue file, region,
landmark, dungeon-entrance, branch and faction names, races, classes and the hero's titles, monster,
item, relic, set and affix names (composed from their parts), quest items and the four main-quest
documents, floor surfaces, depth moods, every spell (name, blurb **and the verb that names what hit**),
abilities, perks, skills, reputation reasons, the rival party and the hired companions.

### Names, articles and gender

- English is still the key. `Loc.Names.cs` holds the Portuguese for monsters, items, relics and the
  **tiles** (`TN`) with their **gender**, so a pattern can say `{a1}` (article + name: "a adaga", "a parede"),
  `{d1}` ("da adaga", "do rei"), `{u1}` ("uma adaga") or plain `$1`, and `Contract` turns "de o" into "do",
  "em as" into "nas".
- A capture that is a name goes through `Loc.Part`: "the jackal" becomes "o chacal", lists are split on ", ", a trailing rank or count ("Tough 2", "(x3)") is kept.
- Item names are **composed** from their parts (`blessed`, `+N`, a prefix affix, the base, an `of X` tail): add the base to `Nm`, the tail to `OfPt`, the adjective to `Adj` **in the masculine
  form** - the feminine and the plural are derived, so "sturdy boots of the bear" reads "botas robustas do urso". A name that starts with its enchantment ("+2 dagger") is composed too.
  A material in front of the base ("steel long sword") becomes an invariable "de X" right after the noun (`MatPt`: "espada longa de aço"), tried only when the
  whole name does not parse, so "iron boots" stays one entry. Wear words (`blunted`, `chipped`, `dented`, `battered`) lead the name like `blessed` and agree at the end.
- Every sentence ends in `Loc.Finish`: proper names substituted (`Loc.Names`) and the Portuguese contractions applied. Patterns kept in `TownText` go through `Loc.TranslateMatch`, which does the same over `Loc.Expand`.
- Sentences about people use `Actor.Subj` / `Actor.Obj` ("The jackal" / "you" / "Dagny"); never write `the {TheName}` by hand around a named person.
- Patterns live in `Loc.Game.cs` (play layer, HUD, sheets) and `Loc.Msgs.cs` (messages); newer patterns win. A pattern that is only a *decoration* of another string (the " : level N" tail of a
  level name) is added last on purpose, so the specific patterns see the whole string first.

### Auditing a language pass

| Command | What it does |
|---|---|
| `headless.ps1 loc [seeds] [turns]` | plays in Portuguese and prints every string that reached the screen untranslated |
| `headless.ps1 loc frames` | prints every panel in Portuguese, empty and in its rich states (a run list, an altar, a road event, the morgue, creation) |
| `headless.ps1 loc pairs` | draws the rich screens in both languages and prints every line that came out **the same**, which is a line nobody translated |
| `headless.ps1 loc msgs` | every string literal in the Core that Portuguese leaves in English (interpolations are tried with several stand-ins, so a literal only counts as translated when a pattern really carries it) |
| `headless.ps1 loc names` | lists monster, item, affix, ability, perk and control names |

The `language` test suite (`LocTests`) is the gate: names, every data table (gods, bosses,
achievements, houses, standings, difficulties, tiles, surfaces, depth moods, quest sources, the altar
menu in four states), every spell verb and the sentences the game composes at runtime, plus a check
that a screen drawn in English carries no Portuguese word (`English frames carry no Portuguese` in
`FeatureTests`). On the front end, `desktop/src/i18n.test.ts` fails when a string literal outside
`i18n.ts` has an accent.

A message built from a pattern is easy to get half right, and the tools do not catch it: when a
sentence carries a captured name (a rumour, a shop sign, "You arrive at X"), check the produced
string, not the pattern. Every capture must go through `Loc.Part`/`Loc.Names` and end in
`Loc.Finish` (names substituted, contractions applied) - a pattern that returns `re.Replace(...)`
directly leaves the name in English.

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

The story follows `docs/game/lore.md`. The save stores `Overworld` to reproduce the right start.
