using System;
using System.Collections.Generic;
using Ossuary.Core.Entities;

namespace Ossuary.Core
{
    /// <summary>
    /// Hired companions: a sellsword from the tavern who fights beside the hero, follows through every staircase, grows
    /// with the hero's level and, if they fall, stays dead. They are ordinary allies (see AllyTurn) with no summon timer.
    /// </summary>
    public sealed partial class Game
    {
        public const int MaxCompanions = 1;
        public readonly List<Monster> Companions = new List<Monster>();
        int _hired;

        static readonly string[] CompanionNames = { "Hald", "Wren", "Brask", "Ilse", "Torvin", "Maud", "Corvane", "Sedge" };
        static readonly string[] CompanionRoles = { "sellsword", "shield-bearer", "cutthroat" };

        public int HirePrice => 100 + 40 * Player.Level;

        /// <summary>Hires the next sellsword the tavern has: a fighter, a shield-bearer or a cutthroat.</summary>
        public Monster HireCompanion()
        {
            string role = CompanionRoles[_hired % CompanionRoles.Length];
            string name = CompanionNames[(_hired * 5 + Rng.Range(0, CompanionNames.Length)) % CompanionNames.Length];
            _hired++;
            var def = Bestiary.Find("dwarf");
            def.Name = role; def.Glyph = '@'; def.Color = role == "shield-bearer" ? 0x9FB8E0 : role == "cutthroat" ? 0xC49A6A : 0xD8C890;
            def.Ai = AiKind.Hunt; def.Undead = false; def.Explodes = false; def.Difficulty = 1;
            var m = new Monster(def, Rng) { Companion = true, CompanionRole = role, Ally = true, SummonTurns = 0, Alert = 0, Dormant = false };
            m.Name = role + " " + name;
            m.Unique = true;
            RescaleCompanion(m, 1f);
            Companions.Add(m);
            return m;
        }

        /// <summary>Sets a companion's stats from the hero's level. <paramref name="keepHp"/> is the share of health to keep.</summary>
        void RescaleCompanion(Monster m, float keepHp)
        {
            int lv = Math.Max(1, Player.Level);
            bool tank = m.CompanionRole == "shield-bearer", thug = m.CompanionRole == "cutthroat";
            m.Level = lv;
            m.MaxHP = (tank ? 24 : thug ? 14 : 18) + lv * (tank ? 7 : thug ? 4 : 6);
            m.HP = Math.Max(1, (int)Math.Round(m.MaxHP * keepHp));
            m.AC = Math.Max(0, (tank ? 6 : thug ? 9 : 8) - lv / 3);
            var d = m.Def;
            d.Level = lv; d.HP = m.MaxHP; d.AC = m.AC;
            d.Attacks = new[] { AttackKind.Hit };
            d.DmgDice = new[] { 1 };
            d.DmgSides = new[] { (tank ? 4 : thug ? 8 : 6) + lv / 2 };
            d.ToHit = new[] { 4 + lv / 2 };
            m.Def = d;
        }

        /// <summary>The hero levelled up: every companion keeps its health share but grows.</summary>
        public void RescaleCompanions()
        {
            foreach (var c in Companions)
                if (c.MaxHP > 0) RescaleCompanion(c, Math.Min(1f, (float)c.HP / c.MaxHP));
        }

        /// <summary>Called right after a dungeon level is entered: companions arrive beside the hero.</summary>
        void PlaceCompanions()
        {
            foreach (var c in Companions)
            {
                if (c.HP <= 0) continue;
                for (int ring = 1; ring <= 3; ring++)
                {
                    bool placed = false;
                    for (int dy = -ring; dy <= ring && !placed; dy++)
                        for (int dx = -ring; dx <= ring && !placed; dx++)
                        {
                            if (Math.Max(Math.Abs(dx), Math.Abs(dy)) != ring) continue;
                            int x = Player.X + dx, y = Player.Y + dy;
                            if (!FreeCell(x, y)) continue;
                            c.X = x; c.Y = y; c.HomeX = x; c.HomeY = y; c.Depth = Map.Depth;
                            if (!Monsters.Contains(c)) Monsters.Add(c);
                            placed = true;
                        }
                    if (placed) break;
                }
            }
        }

        /// <summary>Companions that were destroyed in the dungeon are gone for good.</summary>
        void ReapCompanions()
        {
            if (Mode != GameMode.Dungeon) return;
            for (int i = Companions.Count - 1; i >= 0; i--)
            {
                var c = Companions[i];
                if (c.HP > 0 && Monsters.Contains(c)) continue;
                Companions.RemoveAt(i);
                Say($"The {c.Name} has fallen.", MessageKind.Bad);
            }
        }

        public bool DismissCompanion()
        {
            if (Companions.Count == 0) return false;
            var c = Companions[Companions.Count - 1];
            Companions.Remove(c); Monsters.Remove(c);
            return true;
        }
    }
}
