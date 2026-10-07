using System;
using System.Collections.Generic;
using Ossuary.Core.Entities;
using Ossuary.Core.Items;
using Ossuary.Core.Magic;

namespace Ossuary.Core
{
    /// <summary>Spellcasting: validation, mana, failure and learning. Effects are in Game.Magic.Effects.cs.</summary>
    public sealed partial class Game
    {
        public void PushSpells() => UiRequests.Spells = true;

        /// <summary>The spells a worn or wielded unique item lets you cast while you hold it.</summary>
        public List<string> GrantedSpells()
        {
            var ids = new List<string>();
            void From(Item it)
            {
                var art = it != null ? Artifacts.Find(it.ArtifactId) : null;
                if (art?.Grants != null) foreach (string id in art.Grants) if (!ids.Contains(id)) ids.Add(id);
                if (it?.Imbue != null && !ids.Contains(it.Imbue)) ids.Add(it.Imbue);
            }
            From(Player.Wielded);
            foreach (var piece in Player.WornPieces()) From(piece);
            for (int i = 0; i < 2; i++) From(Player.Rings[i]);
            From(Player.Amulet);
            return ids;
        }

        /// <summary>Spells you may cast now: the ones you learned, and the ones your equipment lends you.</summary>
        public List<string> CastableSpells()
        {
            var ids = new List<string>(Player.Spells);
            foreach (string id in GrantedSpells()) if (!ids.Contains(id)) ids.Add(id);
            return ids;
        }

        public bool Knows(string id) => Player.Spells.Contains(id) || GrantedSpells().Contains(id);

        /// <summary>The spells the list panel shows: every known spell (or one school's), sorted by school, level and name.</summary>
        public List<string> SpellsShown()
        {
            var ids = new List<string>();
            foreach (string id in CastableSpells())
            {
                var sp = Spells.Find(id);
                if (sp != null && (UiState.SpellSchool < 0 || (int)sp.School == UiState.SpellSchool)) ids.Add(id);
            }
            ids.Sort((a, b) =>
            {
                var x = Spells.Find(a); var y = Spells.Find(b);
                int c = UiState.SpellSchool < 0 ? ((int)x.School).CompareTo((int)y.School) : 0;
                if (c == 0) c = x.Level.CompareTo(y.Level);
                if (c == 0) c = string.CompareOrdinal(x.Name, y.Name);
                return c;
            });
            return ids;
        }

        /// <summary>How many known spells are in a school (-1: all).</summary>
        public int SpellCount(int school)
        {
            int n = 0;
            foreach (string id in CastableSpells()) { var sp = Spells.Find(id); if (sp != null && (school < 0 || (int)sp.School == school)) n++; }
            return n;
        }

        /// <summary>
        /// Starts a cast: self spells fire at once, the rest open the targeting cursor
        /// (first on the nearest visible hostile). Costs nothing until the spell resolves.
        /// </summary>
        public bool BeginCast(string id)
        {
            var spell = Spells.Find(id);
            if (spell == null || !Knows(id)) { Say("You do not know that spell."); return false; }
            if (Map == null) { Say("There is no place to cast here."); return false; }
            if (Player.Asleep || Player.Stunned) { Say("You cannot focus."); return false; }
            if (Player.Mp < spell.Cost) { Say($"You need {spell.Cost} mana for {spell.Name} (you have {Player.Mp}).", MessageKind.Warn); return false; }
            if (spell.Target == SpellTarget.Self) return CastSpell(id, Player.X, Player.Y);

            UiState.CastSpell = id; UiState.CastItem = null;
            PushTargeting(TargetingMode.Cast);
            var near = NearestVisibleHostile(spell.Range);
            if (near != null) { UiState.TargetX = near.X; UiState.TargetY = near.Y; }
            return true;
        }

        Monster NearestVisibleHostile(int range)
        {
            Monster best = null; int bestDist = int.MaxValue;
            foreach (var m in Monsters)
            {
                if (m.IsDead || m.Ally || !Map.IsVisible(m.X, m.Y)) continue;
                int d = Pathfinder.Chebyshev(Player.X, Player.Y, m.X, m.Y);
                if (d <= range && d < bestDist) { best = m; bestDist = d; }
            }
            return best;
        }

        /// <summary>Resolves a cast at a cell. Returns true when a turn passed.</summary>
        public bool CastSpell(string id, int tx, int ty) => CastCore(id, tx, ty, null, 0);

        /// <summary>
        /// Casts a spell out of a wand, scroll or potion: no mana, no failure, at no less than <paramref name="power"/> as a caster level,
        /// and the item is spent (a charge, a scroll, a dose) only once the spell has taken effect.
        /// </summary>
        public bool CastFromItem(Item source, string id, int tx, int ty, int power)
        {
            _itemPower = power;
            try
            {
                if (!CastCore(id, tx, ty, source, power)) return false;
            }
            finally { _itemPower = 0; }
            Learn(source);
            if (source.Def.Kind == ItemKind.Wand) source.ChargesUsed++;
            else if (source.Def.Kind == ItemKind.Scroll) { source.ChargesUsed++; if (source.RemainingCharges <= 0) Player.Inventory.Remove(source); }
            else if (--source.Quantity <= 0) Player.Inventory.Remove(source);
            return true;
        }

        int _itemPower;

        /// <summary>The level a spell is cast at: yours, or the item's if that is higher.</summary>
        int CasterLevel => Math.Max(Player.Level, _itemPower);

        bool _proc;

        /// <summary>A spell an item fires by itself (no turn of its own, no mana, no charge): the spell the item carries, aimed or on you.</summary>
        void ImbueProc(Item it, int tx, int ty, string flare)
        {
            var sp = it?.Imbue != null ? Spells.Find(it.Imbue) : null;
            if (sp == null || _proc || Map == null) return;
            _proc = true; _itemPower = Math.Max(Player.Level, 5 + Depth / 2);
            try
            {
                Say($"Your {it.Name} {flare}: {sp.Name}!", MessageKind.Good);
                CastCore(sp.Id, sp.Target == SpellTarget.Self ? Player.X : tx, sp.Target == SpellTarget.Self ? Player.Y : ty, it, _itemPower, true);
            }
            finally { _proc = false; _itemPower = 0; }
        }

        /// <summary>After a blow lands, the carried spell of the wielded weapon may flare on the creature it struck.</summary>
        void ImbueStrike(Monster target)
        {
            var w = Player.Wielded;
            if (w?.Imbue == null || target == null || target.IsDead || !Rng.Chance(14)) return;
            ImbueProc(w, target.X, target.Y, "flares");
        }

        /// <summary>When you are struck, worn pieces that carry a self spell may answer with it.</summary>
        void ImbueReaction()
        {
            var list = new List<Item>(Player.WornPieces());
            for (int i = 0; i < 2; i++) if (Player.Rings[i] != null) list.Add(Player.Rings[i]);
            if (Player.Amulet != null) list.Add(Player.Amulet);
            foreach (var it in list)
            {
                if (it.Imbue == null || Player.HP <= 0) continue;
                var sp = Spells.Find(it.Imbue);
                if (sp == null || sp.Target != SpellTarget.Self || !Rng.Chance(10)) continue;
                ImbueProc(it, Player.X, Player.Y, "stirs");
            }
        }

        bool CastCore(string id, int tx, int ty, Item source, int power, bool proc = false)
        {
            var spell = Spells.Find(id);
            var p = Player;
            bool free = source != null;
            if (spell == null || (!free && !Knows(id)) || Map == null) return false;
            if (!free && p.Mp < spell.Cost) { Say("You do not have the mana."); return false; }
            MakeNoise(2);

            Monster target = null;
            if (spell.Target != SpellTarget.Self)
            {
                int dist = Pathfinder.Chebyshev(p.X, p.Y, tx, ty);
                if (dist > spell.Range) { Say("That is out of range."); return false; }
                if (!Map.InBounds(tx, ty) || !Map.IsVisible(tx, ty) || !Fov.HasLine(Map, p.X, p.Y, tx, ty)) { Say("You have no clear line there."); return false; }
                switch (spell.Target)
                {
                    case SpellTarget.Monster:
                        target = MonsterAt(tx, ty);
                        if (target == null || target.IsDead) { Say($"{spell.Name} needs a creature to aim at."); return false; }
                        if (target.Ally) { Say("Not on your own ally."); return false; }
                        break;
                    case SpellTarget.Cell:
                        if (!Tiles.Walkable(Map.Get(tx, ty)) || MonsterAt(tx, ty) != null || (tx == p.X && ty == p.Y)) { Say("You cannot go there."); return false; }
                        break;
                    case SpellTarget.Line:
                    case SpellTarget.Cone:
                        if (tx == p.X && ty == p.Y) { Say("Pick a direction."); return false; }
                        break;
                }
            }
            if (spell.Summons > 0 && CountFreeCellsNear(spell.Summons) < spell.Summons) { Say("There is no room to call anything here."); return false; }

            int fail = free ? 0 : Spells.FailPct(p, spell);
            if (!free && Rng.Dice(100) <= fail)
            {
                p.Mp -= (spell.Cost + 1) / 2;
                Say($"Your {spell.Name} fizzles. ({fail}% to fail)", MessageKind.Warn);
                p.GainSkill(Skill.Magic, 1);
                Fx((tl, s) => FxLib.Flash(tl, s, p.X, p.Y, Elem.Shadow));
                EndPlayerTurn();
                return true;
            }

            if (!free) { p.Mp -= spell.Cost; p.MpTimer = 0; }
            Cue("magic");
            PlaySpellFx(spell, tx, ty);
            ApplySpell(spell, target, tx, ty);
            if (!free) GodsOnCast(spell);
            if (!free && spell.School == School.Necromancy && spell.Level >= 4 && p.Corruption < 40) AddCorruption(1, null);   // the greater rites leave a mark, but only so far
            p.GainSkill(Skill.Magic, free ? 1 : spell.Level >= 2 ? 2 : 1);
            if (target != null && !target.IsDead && !target.Ally) { target.Alert = 1; target.Dormant = false; }
            Map.Version++;
            if (!proc) EndPlayerTurn();
            return true;
        }

        /// <summary>
        /// Reads a spellbook: one attempt per unknown spell, each taking a few turns. Failing a
        /// hard spell leaves you dizzy, never harmed. Not while an enemy is in view.
        /// </summary>
        public void StudyBook(Item book)
        {
            var p = Player;
            if (book.Def.Name == Abilities.StrikesBook) { StudyStrikes(); return; }
            var ids = Spells.InBook(book.Def.Name);
            if (ids.Length == 0) { Say($"You skim {book.Name}. Nothing in it you can use.", MessageKind.Info); return; }
            if (p.MpMax <= 0) { Say("The symbols mean nothing to you; you have no gift for magic.", MessageKind.Info); return; }
            if (p.Blinded || p.Confused) { Say("You cannot read like this."); return; }
            if (Map != null)
                foreach (var m in Monsters)
                    if (!m.IsDead && !m.Ally && !m.Dormant && Map.IsVisible(m.X, m.Y)) { Say("You cannot study with an enemy in view.", MessageKind.Warn); return; }

            bool any = false;
            foreach (string id in ids)
            {
                if (p.Spells.Contains(id)) continue;
                var spell = Spells.Find(id);
                if (spell == null) continue;
                any = true;
                for (int t = 0; t < spell.Level * 2 && Mode != GameMode.GameOver; t++) EndPlayerTurn();
                if (Mode == GameMode.GameOver) return;
                if (Rng.Dice(100) <= Spells.LearnPct(p, spell))
                {
                    p.Spells.Add(id);
                    p.GainSkill(Skill.Magic, 2 * spell.Level);
                    Say($"You learn {spell.Name}!", MessageKind.Good);
                }
                else
                {
                    p.Confused = true; p.ConfusionTurns = Rng.Range(4, 9);
                    Say($"The words of {spell.Name} writhe and slip away. You feel dizzy.", MessageKind.Bad);
                    break;
                }
            }
            if (!any) Say("You already know everything in this book.", MessageKind.Info);
            else p.RecomputeMaxMp();
        }
    }
}
