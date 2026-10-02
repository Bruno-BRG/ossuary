using System;
using Ossuary.Core.Entities;
using Ossuary.Core.Items;
using Ossuary.Core.Magic;

namespace Ossuary.Core
{
    /// <summary>Spellcasting: validation, mana, failure and learning. Effects are in Game.Magic.Effects.cs.</summary>
    public sealed partial class Game
    {
        public void PushSpells() => UiRequests.Spells = true;

        /// <summary>
        /// Starts a cast: self spells fire at once, the rest open the targeting cursor
        /// (first on the nearest visible hostile). Costs nothing until the spell resolves.
        /// </summary>
        public bool BeginCast(string id)
        {
            var spell = Spells.Find(id);
            if (spell == null || !Player.Spells.Contains(id)) { Say("You do not know that spell."); return false; }
            if (Map == null) { Say("There is no place to cast here."); return false; }
            if (Player.Asleep || Player.Stunned) { Say("You cannot focus."); return false; }
            if (Player.Mp < spell.Cost) { Say($"You need {spell.Cost} mana for {spell.Name} (you have {Player.Mp}).", MessageKind.Warn); return false; }
            if (spell.Target == SpellTarget.Self) return CastSpell(id, Player.X, Player.Y);

            UiState.CastSpell = id;
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
        public bool CastSpell(string id, int tx, int ty)
        {
            var spell = Spells.Find(id);
            var p = Player;
            if (spell == null || !p.Spells.Contains(id) || Map == null) return false;
            if (p.Mp < spell.Cost) { Say("You do not have the mana."); return false; }
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
                        if (tx == p.X && ty == p.Y) { Say("Pick a direction."); return false; }
                        break;
                }
            }
            if (spell.Summons > 0 && CountFreeCellsNear(spell.Summons) < spell.Summons) { Say("There is no room to call anything here."); return false; }

            int fail = Spells.FailPct(p, spell);
            if (Rng.Dice(100) <= fail)
            {
                p.Mp -= (spell.Cost + 1) / 2;
                Say($"Your {spell.Name} fizzles. ({fail}% to fail)", MessageKind.Warn);
                p.GainSkill(Skill.Magic, 1);
                EndPlayerTurn();
                return true;
            }

            p.Mp -= spell.Cost;
            p.MpTimer = 0;
            ApplySpell(spell, target, tx, ty);
            GodsOnCast(spell);
            p.GainSkill(Skill.Magic, spell.Level >= 2 ? 2 : 1);
            if (target != null && !target.IsDead && !target.Ally) { target.Alert = 1; target.Dormant = false; }
            Map.Version++;
            EndPlayerTurn();
            return true;
        }

        /// <summary>
        /// Reads a spellbook: one attempt per unknown spell, each taking a few turns. Failing a
        /// hard spell leaves you dizzy, never harmed. Not while an enemy is in view.
        /// </summary>
        public void StudyBook(Item book)
        {
            var p = Player;
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
