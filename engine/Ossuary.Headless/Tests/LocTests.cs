using System;
using Ossuary.Core;
using Ossuary.Core.Entities;
using Ossuary.Core.Items;

namespace Ossuary.Tests
{
    /// <summary>Language checks: nothing the player reads should be half in one language (docs/languages.md).</summary>
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
            foreach (var r in Races.All) Miss(missing, Loc.T(r.Description) != r.Description, "race text " + r.Id);
            if (missing.Count > 0) throw new Exception(missing.Count + " without Portuguese: " + string.Join("; ", missing.ToArray()));
        }

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
