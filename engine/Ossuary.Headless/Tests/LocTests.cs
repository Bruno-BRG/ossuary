using System;
using Ossuary.Core;
using Ossuary.Core.Entities;
using Ossuary.Core.Items;

namespace Ossuary.Tests
{
    /// <summary>Language checks: nothing the player reads should be half in one language (docs/tech/languages.md).</summary>
    public static class LocTests
    {
        static int _pass, _fail;

        public static void Run()
        {
            _pass = 0; _fail = 0;
            var old = Loc.Current;
            try
            {
                Test("English sentences have no doubled articles", EnglishGrammar);
                Test("every monster, item, affix, perk and control label has Portuguese", Names);
                Test("every table the player reads has Portuguese", Tables);
                Test("combat, look and HUD lines translate whole", Lines);
                Test("items compose with their parts", ItemParts);
            }
            finally { Loc.Current = old; }
            Console.WriteLine($"==== loc: {_pass} passed, {_fail} failed ====");
            if (_fail > 0) throw new Exception($"{_fail} language asserts failed");
        }

        static void EnglishGrammar()
        {
            Loc.Current = Lang.En;
            var jackal = new Monster(Bestiary.Find("jackal"), new Rng(1));
            var person = new Monster(Bestiary.Find("hobbit"), new Rng(1)) { Name = "Dagny", Townsperson = true };
            var g = new Game(7);
            Check(jackal.Subj == "The jackal" && jackal.Obj == "the jackal" && jackal.TheName == "jackal", "a monster takes an article: " + jackal.Subj);
            Check(person.Subj == "Dagny" && person.Obj == "Dagny", "a named person takes none: " + person.Subj);
            Check(g.Player.Obj == "you" && g.Player.Subj == "You", "the player is you");
            var rng = new Rng(5);
            for (int i = 0; i < 400; i++)
            {
                var a = Battles.MeleeAttack(jackal, g.Player, rng).Message;
                Check(!a.Contains("the the") && !a.Contains("you misses") && !a.Contains("you evades") && !a.Contains("hits the you"), a);
                var b = Battles.PlayerMelee(g.Player, person, rng, out _).Message;
                Check(!b.Contains("the Dagny") && !b.Contains("the the"), b);
                var c = Battles.PlayerMelee(g.Player, jackal, rng, out _).Message;
                Check(!c.Contains("the the"), c);
                jackal.HP = jackal.MaxHP; person.HP = 99;
            }
        }

        static void Names()
        {
            var missing = new System.Collections.Generic.List<string>();
            Loc.Current = Lang.Pt;
            foreach (var m in Bestiary.All) Miss(missing, Loc.KnowsName(m.Name), "monster " + m.Name);
            foreach (var list in new[] { Catalogue.Weapons, Catalogue.Armor, Catalogue.Shields, Catalogue.Helms, Catalogue.Gloves, Catalogue.Boots, Catalogue.Cloaks, Catalogue.Rings, Catalogue.Amulets,
                                         Catalogue.Wands, Catalogue.Scrolls, Catalogue.Potions, Catalogue.Food, Catalogue.Tools, Catalogue.Misc, Catalogue.Books, Catalogue.Ornaments })
                foreach (var d in list) Miss(missing, Loc.KnowsName(d.Name), "item " + d.Name);
            foreach (var a in Affixes.All) Miss(missing, Loc.T("blessed " + a.Name + " dagger").Contains("adaga") || a.Name.StartsWith("of ") || Loc.U(a.Name) != a.Name || Loc.T(a.Name + " dagger") != a.Name + " dagger", "affix " + a.Name);
            foreach (var a in Abilities.All) { Miss(missing, Loc.U(a.Name) != a.Name, "ability " + a.Name); Miss(missing, Loc.U(a.Blurb) != a.Blurb, "ability text " + a.Id); }
            foreach (var p in Progression.All) { Miss(missing, Loc.U(p.Name) != p.Name, "perk " + p.Name); Miss(missing, Loc.U(p.Blurb) != p.Blurb, "perk text " + p.Id); }
            foreach (var k in KeyBindings.Actions) { Miss(missing, Loc.U(k.Label) != k.Label, "control " + k.Label); Miss(missing, Loc.U(k.Group) != k.Group, "control group " + k.Group); }
            foreach (var r in Roles.All) Miss(missing, Loc.T(r.Description) != r.Description, "class text " + r.Id);
            foreach (var r in Roles.All) foreach (var t in r.Titles) Miss(missing, Loc.T(t) != t || Loc.U(t) != t, "hero title " + t);
            foreach (var r in Races.All) Miss(missing, Loc.T(r.Description) != r.Description, "race text " + r.Id);
            // Artifacts: every relic the branches can give you is named to the player, and its lore is read out when it is identified.
            foreach (var a in Artifacts.All) Miss(missing, Loc.T(a.Name) != a.Name, "artifact " + a.Name + " (" + a.Branch + " " + a.Depth + ")");
            foreach (var a in Artifacts.All) Miss(missing, string.IsNullOrEmpty(a.Lore) || Loc.T(a.Lore) != a.Lore, "artifact lore " + a.Id);
            foreach (var s in ArtifactSets.All) Miss(missing, Loc.T(s.Name) != s.Name, "artifact set " + s.Name);
            // The parties that race the hero down the Dungeons, and the reasons a house changes its mind.
            foreach (string party in new[] { "the Ash Company", "Maren's Blades", "the Grey Lanterns", "the Penny Knives", "Brother Voss and his five", "the Hollow Crows" })
                Miss(missing, Loc.T(party) != party, "rival party " + party);
            foreach (string reason in new[] { "bribing the Watch", "calling in a favour", "killing a priest", "killing a guard", "putting down road bandits",
                                              "robbing the dead", "sharing water with a pilgrim", "feeding refugees", "robbing refugees", "saving a delver",
                                              "robbing a wounded delver", "burying a stranger", "beating a rival party to the bottom", "losing the race",
                                              "catching the thief", "letting the thief go", "putting down the toll bandits", "leaving the toll bandits to the roads",
                                              "laying the restless to rest", "carrying the Drowned's vial", "putting down a king of the dark",
                                              "clearing the vaults", "bringing a blade of bone to the Drowned" })
                Miss(missing, Loc.T(reason) != reason, "reputation reason " + reason);
            if (missing.Count > 0) throw new Exception(missing.Count + " without Portuguese: " + string.Join("; ", missing.ToArray()));
        }

        /// <summary>
        /// Everything the player can read is data somewhere, and no pattern covers data: the altar of a god, the lines a
        /// boss says, the achievements, the standings. Proper nouns (people, towns, gods) are the only exception, and
        /// are checked as names. A missing entry here is a sentence a Brazilian player reads in English.
        /// </summary>
        static void Tables()
        {
            Loc.Current = Lang.Pt;
            var missing = new System.Collections.Generic.List<string>();
            foreach (var g in Gods.All)
            {
                Miss(missing, Has(g.Title), "god title " + g.Title);
                Miss(missing, Has(g.Domain), "god domain " + g.Domain);
                Miss(missing, Has(g.Gift), "god gift " + g.Gift);
                Miss(missing, Has(g.Likes), "god likes " + g.Likes);
                Miss(missing, Has(g.Dislikes), "god dislikes " + g.Dislikes);
                Miss(missing, Has(g.Boon), "god boon " + g.Boon);
                Miss(missing, Has(g.Tier1), "god blessing " + g.Tier1);
                Miss(missing, Has(g.Tier2), "god blessing " + g.Tier2);
            }
            foreach (var a in Achievements.All)
            {
                Miss(missing, Has(a.Name), "achievement " + a.Name);
                Miss(missing, Has(a.Blurb), "achievement text " + a.Id);
            }
            foreach (var b in Bosses.All)
            {
                Miss(missing, Loc.KnowsName(b.Name), "boss name " + b.Name);
                Miss(missing, Has(b.Intro), "boss intro " + b.Id);
                Miss(missing, Has(b.Phase2), "boss phase " + b.Id);
                Miss(missing, Has(b.Fall), "boss fall " + b.Id);
            }
            foreach (var house in Houses.All) Miss(missing, Has(Houses.Name(house)), "house " + house);
            // The mode and the panel names travel in the frame for a screen reader (Session.Frame), so they are text too.
            foreach (Panel p in Enum.GetValues(typeof(Panel))) Miss(missing, Has(p.ToString()) || SameInBoth.Contains(p.ToString()), "panel label " + p);
            foreach (GameMode m in Enum.GetValues(typeof(GameMode))) Miss(missing, Has(m.ToString()) || SameInBoth.Contains(m.ToString()), "mode label " + m);
            foreach (string standing in new[] { "revered", "trusted", "known", "distrusted", "hated" }) Miss(missing, Has(standing), "standing " + standing);
            foreach (Difficulty d in Enum.GetValues(typeof(Difficulty))) Miss(missing, Has(Difficulties.Blurb(d)), "mode text " + d);
            foreach (TileKind k in Enum.GetValues(typeof(TileKind)))
            {
                string tile = Tiles.Get(k).Name;
                Miss(missing, Has(tile) || SameInBoth.Contains(tile), "tile " + tile);
            }
            // What lies on the floor, what a depth feels like, and the people a quest can come from: all read on screen.
            foreach (SurfaceKind k in Enum.GetValues(typeof(SurfaceKind)))
                Miss(missing, k == SurfaceKind.None || Has(SurfaceInfo.Name(k)), "surface " + SurfaceInfo.Name(k));
            for (int depth = 0; depth <= 6; depth++) Miss(missing, Has(Theme.DepthMood(depth)), "depth mood " + Theme.DepthMood(depth));
            foreach (string actor in new[] { "a rumour", "the tavern", "the Captain of the Watch", "the gravekeeper", "stray dog", "The houses",
                                             "sellsword", "shield-bearer", "cutthroat", "cat" })
                Miss(missing, Has(actor), "actor " + actor);
            Miss(missing, Has("shade of Morgana"), "shade name");
            Miss(missing, Has("A new cycle begins. What you did is remembered."), "new cycle line");
            // Quest items and the things the hero can pick up outside the catalogues: they are named like any other item.
            foreach (string item in new[] { "brass key", "skeleton corpse", "amulet of Yendor", "warden's ledger page",
                                            "dwarf council's tally", "archivist's last entry", "spire order roll" })
                Miss(missing, Loc.KnowsName(item), "quest item " + item);
            Miss(missing, Has("Day 2  20:00 (night)"), "night clock");
            // Every damaging effect names what did it: the verb is data on the spell, and the line is composed around it.
            foreach (var sp in Ossuary.Core.Magic.Spells.All)
            {
                if (string.IsNullOrEmpty(sp.Verb)) continue;
                Miss(missing, Has(sp.Verb + " the jackal for 3 damage."), "spell verb " + sp.Id + " (" + sp.Verb + ")");
                Miss(missing, Has(sp.Verb + " the jackal for 3, and it dies."), "spell kill verb " + sp.Id);
            }
            foreach (string verb in new[] { "Bolts of force strike", "Lightning arcs into", "A ray of frost hits", "A lance of ice pierces",
                                            "Life tears out of", "Radiance scours", "Holy light sears", "Lightning tears through",
                                            "Lightning strikes", "The lightning leaps to", "Flames lash", "Light sears", "You stab", "You cut",
                                            "You finish", "Magic strikes" })
                Miss(missing, Has(verb + " the jackal for 3 damage."), "effect verb " + verb);

            // Every row of a god's altar, in every state the menu can be in.
            var hero = Game.NewHero(31337, "Altar", "human", "fighter");
            int ax = -1, ay = -1;
            for (int y = 1; y < hero.Map.H && ax < 0; y++)
                for (int x = 1; x < hero.Map.W && ax < 0; x++)
                    if (Gods.AtAltar(hero.Map.Number, x, y) != null) { ax = x; ay = y; }
            Miss(missing, ax > 0, "a level has an altar to check");
            if (ax > 0)
            {
                hero.OpenAltar(ax, ay);
                var here = hero.AltarGod();
                AltarRows(missing, hero, "unsworn");
                hero.Player.God = here.Id;
                hero.Player.Piety = 60;
                hero.Player.TrialGoal = Game.TrialDeeds; hero.Player.TrialDone = 1;
                AltarRows(missing, hero, "own god, trial running");
                hero.Player.TrialGoal = 0; hero.Player.Gold = 500;
                AltarRows(missing, hero, "own god, with gold");
                var rival = Gods.Find(here.Rival);
                if (rival != null) { hero.Player.God = rival.Id; AltarRows(missing, hero, "rival god"); }
                Miss(missing, Has("The altar is dead. Nothing answers."), "dead altar");
                Miss(missing, Has(Game.SacrificePrompt), "sacrifice prompt");
            }

            // Lines the game composes at runtime: no single entry can cover them, so a pattern must - and it must carry
            // the whole sentence, not the first half of it.
            foreach (string line in new[]
            {
                "Hunt 5 giant rats in The Dungeons", "Reach depth 3 of The Mines of Dwarfdeep",
                "(2/5) Hunt 5 giant rats in The Dungeons",
                "Take a job: Hunt 5 giant rats in The Dungeons (pays 120g)",
                "Report: Hunt 5 giant rats in The Dungeons (2/5)",
                "Craft for the town: iron bar", "Job: Hunt 3 jackal in The Dungeons (1/3)", "Job: Hunt 3 jackal in The Dungeons (ready)",
                "A skeleton answers the Gaoler.", "3 kobolds answer the Gaoler.",
                "Your staff flares: magic missile!", "Your staff stirs: magic missile!",
                // The altar of a god, and the screens that show a hero's standing.
                "God of war and stone.", "Likes:    killing the undead; sacred magic", "Dislikes: necromancy; killing the harmless",
                "Boon (60 piety): a +1 enchantment on your weapon", "Piety 50: wounds close faster",
                "Your piety 63/200", "The last prayer is still fresh (212 turns).", "You may pray.", "You follow Aurel.",
                "Begin as Morgana the Elf Paladin?  (Enter)", "Level 6   HP 87/87   MP 13/13   AC -16   XP 20",
                "Dlvl 3   12 kills",
                // The character sheet, the panels that list things, and the prompts a choice opens with.
                "Gear: +2 Dex, fire 30%, 1d4 fire", "+2 Dex", "+1d6 fire", "Faith: Khorr, the Hammer Beneath - piety 63/200 (tier 2)",
                "Level 2+", "Nothing known in this school.", "Wanted: 150 gold", "Done 2   Failed 1", "+4 more", "...and 4 more",
                "Drink what?", "Apply what?", "Eat what?", "Wield what?", "Wear what?", "Take off what?", "Put on which ring or amulet?",
                "Read what?", "Zap what?", "Sell what?", "Offer what?", "The Road",
                // Spells, abilities, item procs, locking, digging and the rebind notes.
                "Your roar scatters 3 foe(s)!", "Your roar echoes, and nothing flees.", "Your empty hands tingle.",
                "Your skin prickles, but you wear nothing to enchant.", "Nothing living stirs near you.",
                "The spell lands on empty ground.", "The spell takes hold.", "The land shows you its shape.", "No traps nearby.",
                "Nothing here is locked.", "There is nothing there to dig.", "There is no trap to disarm here.",
                "Nothing you carry is cursed.", "No beasts answer.", "The water boils away in a scream of steam.",
                "Steam hisses up from the stone, and thins to nothing.", "A frost ray", "A bolt of lightning", "A gout of flame",
                "A silver bolt", "Frost bites the jackal.", "Venom eats into the jackal.", "Sparks arc into the jackal.",
                "Radiance sears the jackal.", "Dark power strikes the jackal.", "The jackal is cooked by the current.",
                "which item?", "Slash is reserved for menus.", "Taken from \"Look\".", "Bound to ⇧.",
                "Something has been living down there", "The stone remembers you.", "The one with the nice coat steps back, and the other two do not",
                // Doors, and the six endings of the main quest.
                "The door slams shut.", "The door swings open.",
                "The priest buries the Amulet in the rite Yendor meant. The dead settle, the lamps burn lower, and the pit closes.",
                "The seal turns, and every vault of the old kingdom opens. The Reach is rich by morning, and it is not the living who collect.",
                "The Watch posts a standing guard and gives you the keys. You are the Warden now, and the seal is yours to keep from everyone.",
                "The Amulet goes to the highest bidder. The Reach eats well for a year, and the Ossuary has a new owner.",
                "The League cannot pay five thousand. It pays what it has, takes the Amulet, and thanks you. The world goes on as it was.",
                "You stamp the seal where Yendor stamped it, and the stone takes it. You are the Archivist now. The Ossuary will not forget you.",
                "The house watches. You are one of theirs now, and everyone can see it.",
            })
            {
                string pt = Loc.T(line);
                Miss(missing, pt != line, "composed line " + line);
                foreach (string leftover in Leftovers)
                    if (pt.Contains(leftover))
                    {
                        Miss(missing, false, "half translated '" + line + "' -> '" + pt + "' (kept '" + leftover + "')");
                        break;
                    }
            }

            // The job steps and hints the engine builds, and every quest the book holds: each must translate.
            var sample = new Game(31337);
            var board = new JobOffer { Id = "guild.check.0.0", Kind = "hunt", Branch = "The Dungeons", Target = "jackal", Count = 3, Reward = 60, Giver = Houses.Guild };
            var commission = new JobOffer { Id = "guild.make.check.Smithy.0", Kind = "make", Branch = "blacksmith", Target = "bone blade", Count = 1, Reward = 40, Giver = Houses.Guild };
            var delve = new JobOffer { Id = "guild.check.0.1", Kind = "delve", Branch = "The Warrens", Count = 4, Reward = 220, Giver = Houses.Watch };
            foreach (var def in new[] { sample.BuildJob(board), sample.BuildJob(commission), sample.BuildJob(delve) })
            {
                Miss(missing, Loc.T(def.Title) != def.Title, "job title " + def.Title);
                foreach (var s in def.Steps)
                {
                    Miss(missing, Loc.T(s.Text) != s.Text, "job step " + s.Text);
                    if (s.Hint != null) Miss(missing, Loc.T(s.Hint) != s.Hint, "job hint " + s.Hint);
                }
            }
            foreach (var q in QuestBook.All.Values)
            {
                Miss(missing, Loc.T(q.Title) != q.Title, "quest title " + q.Title);
                foreach (var s in q.Steps)
                {
                    Miss(missing, Loc.T(s.Text) != s.Text, "quest step " + s.Text);
                    if (s.Hint != null) Miss(missing, Loc.T(s.Hint) != s.Hint, "quest hint " + s.Hint);
                }
            }

            if (missing.Count > 0)
            {
                // Enough of the list to work from, without burying the console: the rest is in the same shape.
                int show = Math.Min(missing.Count, 30);
                throw new Exception(missing.Count + " without Portuguese: " + string.Join("; ", missing.GetRange(0, show).ToArray())
                    + (missing.Count > show ? "; ..." : ""));
            }
        }

        /// <summary>Words that must not survive into a translated sentence: an English half is worse than a missing entry.</summary>
        static readonly string[] Leftovers = { "Hunt ", "Reach depth", "answers the", "flares", "stirs", "pays ", "gold", "the job", "It pays",
                                               "You are paid", "Report to the board", "Take a job", "Report: ", "The Dungeons", "deeds ",
                                               "God of", "Likes:", "Dislikes:", "Boon", "piety", "Piety ", "prayer", "You may", "You follow",
                                               "Begin as", "Level ", "HP ", "kills", "Dlvl", "Dex", "what?", "Nothing known", "tier ", "The Road",
                                               "Wanted", "Done ", "Failed", " more", "Gear:", "foe(s)", "reserved for menus", "Taken from", "Bound to",
                                               "which item", "The spell", "The land", "The water", "Steam ", "A frost ray", "A bolt of", "A gout of",
                                               "A silver bolt", "Frost bites", "Venom eats", "Sparks arc", "Radiance sears", "Dark power",
                                               "Nothing living", "Nothing here", "Nothing you", "No beasts", "Your empty", "Your skin", "Your roar",
                                               "The stone remembers", "Something has been", "The one with the nice coat",
                                               "The door slams", "The door swings", "The priest buries", "The seal turns", "The Watch posts",
                                               "The Amulet goes", "The League cannot", "You stamp the seal", "The house watches" };

        /// <summary>Every label of the altar menu right now has to be Portuguese (called once per menu state).</summary>
        static void AltarRows(System.Collections.Generic.List<string> missing, Game g, string what)
        {
            foreach (var row in g.AltarRows()) Miss(missing, Has(row.Label), "altar row (" + what + ") " + row.Label);
        }

        /// <summary>True when the string reaches the player in Portuguese (whole entry or pattern).</summary>
        static bool Has(string s) => !string.IsNullOrEmpty(s) && (Loc.T(s) != s || Loc.U(s) != s);

        /// <summary>Words that are the same in both languages on purpose: loanwords and units, never a missing entry.</summary>
        static readonly System.Collections.Generic.HashSet<string> SameInBoth = new System.Collections.Generic.HashSet<string>
        {
            "altar", "CRT", "Mana", "Exp", "Con", "Int", "Normal", "Hardcore", "Halfling", "Orc", "Menu", "Auto", "Rival", "molotov", "Altar",
        };

        static void Lines()
        {
            Loc.Current = Lang.Pt;
            Eq("You hit the jackal for 3 damage.", "Você acerta o chacal por 3 de dano.");
            Eq("You kill the giant rat with a critical hit for 6 damage.", "Você mata o rato gigante com um golpe crítico por 6 de dano.");
            Eq("You miss the cave spider.", "Você erra a aranha das cavernas.");
            Eq("The jackal hits you for 2 damage.", "O chacal acerta você por 2 de dano.");
            Eq("The jackal misses you.", "O chacal erra você.");
            Eq("You evade the kobold's attack.", "Você esquiva do ataque do kobold.");
            Eq("It is weak.", "Parece fraco.");
            Eq("Fight it (Enter or K), or flee (R or <). It is somewhat dangerous.", "Lute (Enter ou K) ou fuja (R ou <). Parece um pouco perigoso.");
            Eq("You have killed the newt. (3 experience)", "Você matou o tritão. (3 de experiência)");
            Eq("DEPTH 3", "NÍVEL 3");
            Eq("Day 2  08:00", "Dia 2  08:00");
            Eq("Lv 4", "Nv 4");
            Eq("Level 3   HP 20/30   AC 5   XP 40", "Nível 3   PV 20/30   CA 5   XP 40");
            Eq(" IN VIEW ", " À VISTA ");
            Eq("Here ", "Aqui ");
            Eq("Combat      0  Novice  ", "Combat      0  Novice  ".Replace("Combat", "Combat"));   // skill rows are translated before padding (Ui.cs)
            Eq("Combat", "Combate");
            Eq("1 pick(s) to spend", "1 escolha(s) por gastar");
            Eq("Perks: Power Strike, Tough 2", "Talentos: Golpe Poderoso, Resistente 2");
            Eq("Wanderer, the Human Adventurer", "Wanderer, Aventureiro Humano");
            // A message of the play layer that carries a reason: the house and the reason both have to come out in Portuguese.
            // A house opens the sentence now (Houses.Subject), so its name is a proper noun either way.
            Eq("The Guild thinks the better of you for putting down a king of the dark.", "A Guilda pensa melhor de você por derrubar um rei do escuro.");
            Eq("The Watch thinks the worse of you for robbing the dead.", "A Guarda pensa pior de você por roubar os mortos.");
            Eq("The Watch now holds you known.", "A Guarda agora tem você como conhecido.");
            Eq("Reach level 7 of The Dungeons before the rival party does.", "Chegue ao nível 7 das Masmorras antes do grupo rival.");
            Eq("The rival party reached level 7 first, and they are not shy about it.", "O grupo rival chegou ao nível 7 primeiro, e não faz segredo disso.");
        }

        static void ItemParts()
        {
            Loc.Current = Lang.Pt;
            Eq("You pick up dagger.", "Você pega a adaga.");
            Eq("You pick up blessed +2 keen dagger of the fox.", "Você pega a adaga afiada de da raposa +2 abençoada.".Replace("de da", "da"));
            Eq("You eat food ration. That was good (+800 nourishment).", "Você come a ração de comida. Estava bom (+800 de nutrição).");
            Eq("You are now wielding long sword.", "Você empunha a espada longa.");
            Eq("potion of healing", "poção de cura");
            Eq("scroll of the tempest", "pergaminho da tempestade");
            // Materials and wear (Items/Materials.cs): "de X" after the noun, wear with the blessing at the end.
            Eq("You pick up steel long sword.", "Você pega a espada longa de aço.");
            Eq("You pick up chipped +1 keen cold iron dagger of the fox.", "Você pega a adaga de ferro frio afiada da raposa +1 lascada.");
            Eq("iron boots", "botas de ferro");
            Eq("Your steel dagger shatters!", "A adaga de aço se despedaça!");
            Eq("iron ore", "minério de ferro");
        }

        static void Eq(string en, string pt)
        {
            string got = Loc.U(en);
            if (got == en) got = Loc.T(en);
            Check(got == pt, $"'{en}' -> '{got}' (wanted '{pt}')");
        }

        static void Miss(System.Collections.Generic.List<string> list, bool ok, string what) { if (!ok) list.Add(what); }

        static void Check(bool ok, string what) { if (!ok) throw new Exception(what); }

        static void Test(string name, Action fn)
        {
            try { fn(); _pass++; Console.WriteLine("  ok   " + name); }
            catch (Exception e) { _fail++; Console.WriteLine("  FAIL " + name + ": " + e.Message); }
        }
    }
}
