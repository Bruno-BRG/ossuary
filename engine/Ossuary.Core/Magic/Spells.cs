using System;
using System.Collections.Generic;
using Ossuary.Core.Entities;

namespace Ossuary.Core.Magic
{
    public enum School { Evocation, Conjuration, Alteration, Illusion, Necromancy, Sacred }

    /// <summary>
    /// What a spell aims at. Self: no cursor. Monster: a hostile creature. Cell: an empty floor
    /// cell. Area: any visible cell (the effect spreads from it). Line: a direction; the effect
    /// runs from you through the cell.
    /// </summary>
    public enum SpellTarget { Self, Monster, Cell, Area, Line }

    /// <summary>A spell as data. Effects live in Game.Magic.cs / Game.Magic.Effects.cs, keyed by <see cref="Id"/>.</summary>
    public sealed class SpellDef
    {
        public string Id, Name, Blurb;
        public int Level;          // 1..5
        public School School;
        public int Cost;           // Mp
        public SpellTarget Target;
        public int Range;          // cells, for every target but Self
        public int Radius;         // Area spells
        public int Summons;        // creatures called; the cast is refused without room for them
    }

    public static class Spells
    {
        static SpellDef S(string id, string name, int level, School school, int cost, SpellTarget target, int range, string blurb, int radius = 0, int summons = 0)
            => new SpellDef { Id = id, Name = name, Level = level, School = school, Cost = cost, Target = target, Range = range, Blurb = blurb, Radius = radius, Summons = summons };

        public static readonly SpellDef[] All = {
            // ---- Evocation
            S("magic-missile", "Magic Missile", 1, School.Evocation, 2, SpellTarget.Monster, 8, "Unerring bolts of force. Dice grow with level."),
            S("shocking-grasp", "Shocking Grasp", 1, School.Evocation, 2, SpellTarget.Monster, 1, "Lightning through your touch. Adjacent only; hits hard."),
            S("frost-ray", "Frost Ray", 2, School.Evocation, 4, SpellTarget.Monster, 8, "A beam of cold. Heavy damage, needs a clear line."),
            S("fireball", "Fireball", 3, School.Evocation, 7, SpellTarget.Area, 8, "Bursts where you aim, burning everything within 2 cells.", 2),
            S("lightning-bolt", "Lightning Bolt", 3, School.Evocation, 6, SpellTarget.Line, 8, "A bolt that pierces every creature along its line."),
            S("chain-lightning", "Chain Lightning", 4, School.Evocation, 10, SpellTarget.Monster, 8, "Strikes a target, then leaps to up to three more nearby."),
            S("wall-of-fire", "Wall of Fire", 4, School.Evocation, 9, SpellTarget.Area, 7, "A burning cross on the floor. Spreads through brush and oil; creatures in it catch fire.", 1),
            S("meteor", "Meteor", 5, School.Evocation, 15, SpellTarget.Area, 8, "A burning rock from nowhere. Devastates 3 cells around.", 3),
            // ---- Conjuration
            S("familiar", "Familiar", 1, School.Conjuration, 3, SpellTarget.Self, 0, "Calls a small beast to fight for you for a time.", 0, 1),
            S("summon-beast", "Summon Beast", 2, School.Conjuration, 5, SpellTarget.Self, 0, "Calls a stronger beast as your level grows.", 0, 1),
            S("create-water", "Create Water", 2, School.Conjuration, 4, SpellTarget.Area, 7, "Floods the floor around a spot. The wet burn less and conduct lightning; frost turns it to ice.", 2),
            S("blink", "Blink", 3, School.Conjuration, 5, SpellTarget.Cell, 6, "Step through space to a spot you can see."),
            S("teleport", "Teleport", 4, School.Conjuration, 9, SpellTarget.Self, 0, "Throws you to a random place on this level."),
            // ---- Alteration
            S("ward", "Ward", 1, School.Alteration, 2, SpellTarget.Self, 0, "A shimmering shield: AC +3 for a while."),
            S("haste", "Haste", 3, School.Alteration, 6, SpellTarget.Self, 0, "You act twice as often as everything else, briefly."),
            S("slow", "Slow", 3, School.Alteration, 5, SpellTarget.Monster, 6, "Halves a creature's speed for a time. Strong ones resist."),
            S("clairvoyance", "Clairvoyance", 3, School.Alteration, 7, SpellTarget.Self, 0, "The whole level unfolds in your mind."),
            S("stone-skin", "Stone Skin", 4, School.Alteration, 9, SpellTarget.Self, 0, "Your skin hardens: AC +6 for a long while."),
            // ---- Illusion
            S("sleep", "Sleep", 2, School.Illusion, 4, SpellTarget.Monster, 6, "Puts a living foe to sleep. Strong ones resist; damage wakes it."),
            S("confuse", "Confuse", 2, School.Illusion, 4, SpellTarget.Monster, 6, "The target staggers about at random."),
            S("invisibility", "Invisibility", 2, School.Illusion, 5, SpellTarget.Self, 0, "Foes lose track of you and strike at you worse."),
            S("charm", "Charm Monster", 4, School.Illusion, 10, SpellTarget.Monster, 6, "A living foe fights for you for a while. Hard on strong ones."),
            // ---- Necromancy
            S("drain-life", "Drain Life", 3, School.Necromancy, 5, SpellTarget.Monster, 6, "Steals life: damages the target, heals you by half. Not the dead."),
            S("raise-skeleton", "Raise Skeleton", 3, School.Necromancy, 6, SpellTarget.Self, 0, "A skeleton claws out of the floor to serve you.", 0, 1),
            S("fear", "Fear", 4, School.Necromancy, 8, SpellTarget.Monster, 6, "The target flees in terror. Mindless things do not fear."),
            S("finger-of-death", "Finger of Death", 5, School.Necromancy, 15, SpellTarget.Monster, 6, "Unmakes the living with a point of the hand."),
            S("army-of-bones", "Army of Bones", 5, School.Necromancy, 14, SpellTarget.Self, 0, "Three skeletons rise to guard you.", 0, 3),
            // ---- Sacred
            S("cure-wounds", "Cure Wounds", 1, School.Sacred, 3, SpellTarget.Self, 0, "Mends your flesh: 2d6 plus Wisdom and level."),
            S("bless", "Bless", 1, School.Sacred, 3, SpellTarget.Self, 0, "A steadier hand: +2 to hit for a long while."),
            S("smite", "Smite", 2, School.Sacred, 4, SpellTarget.Monster, 7, "Radiant wrath. The undead take double."),
            S("cleanse", "Cleanse", 2, School.Sacred, 3, SpellTarget.Self, 0, "Burns away poison, confusion, blindness and visions."),
            S("turn-undead", "Turn Undead", 2, School.Sacred, 5, SpellTarget.Self, 0, "Every undead in sight burns and flees."),
            S("greater-heal", "Greater Heal", 3, School.Sacred, 8, SpellTarget.Self, 0, "A great mending: 4d8 plus Wisdom and level."),
            S("revive", "Revive", 5, School.Sacred, 15, SpellTarget.Self, 0, "Wards your soul: the next death within 300 turns is undone."),
        };

        /// <summary>Spells taught by each book, by item name. Other books carry only lore.</summary>
        static readonly Dictionary<string, string[]> Books = new Dictionary<string, string[]>
        {
            { "a spellbook", new[] { "magic-missile", "shocking-grasp", "ward", "frost-ray", "familiar" } },
            { "a tome of evocation", new[] { "magic-missile", "frost-ray", "fireball", "lightning-bolt", "wall-of-fire" } },
            { "a codex of storms", new[] { "lightning-bolt", "fireball", "wall-of-fire", "chain-lightning", "meteor" } },
            { "a tome of conjuration", new[] { "familiar", "summon-beast", "create-water", "blink", "teleport" } },
            { "a book of wards", new[] { "ward", "haste", "slow", "clairvoyance", "stone-skin" } },
            { "a book of illusions", new[] { "sleep", "confuse", "invisibility", "charm" } },
            { "a book of shadows", new[] { "sleep", "blink", "drain-life", "raise-skeleton", "fear" } },
            { "a grimoire of the dead", new[] { "raise-skeleton", "drain-life", "fear", "finger-of-death", "army-of-bones" } },
            { "a book of prayers", new[] { "cure-wounds", "ward", "bless", "cleanse" } },
            { "a book of mercy", new[] { "smite", "turn-undead", "greater-heal", "revive" } },
        };

        public static SpellDef Find(string id)
        {
            for (int i = 0; i < All.Length; i++) if (All[i].Id == id) return All[i];
            return null;
        }

        public static string[] InBook(string bookName) =>
            bookName != null && Books.TryGetValue(bookName, out var ids) ? ids : new string[0];

        /// <summary>Buff ids a spell leaves on the player, for the status bar and expiry messages.</summary>
        public static string BuffLabel(string id)
        {
            switch (id)
            {
                case "ward": return "Ward";
                case "stone-skin": return "Stone Skin";
                case "haste": return "Haste";
                case "bless": return "Bless";
                case "invisibility": return "Invisible";
                case "revive": return "Revive";
                case "flame": return "Flame";
                case "levitating": return "Levitate";
                default: return id;
            }
        }

        static int Stat(Player p) => Roles.Find(p.RoleId).MpStat == 'W' ? p.Wis : p.Int;

        /// <summary>Percent chance a cast fizzles: harder spells fail more; the casting stat, Magic skill and light armour help.</summary>
        public static int FailPct(Player p, SpellDef s)
        {
            int pct = 20 + 10 * s.Level - 3 * (Stat(p) - 10) - p.Skills[Skill.Magic] / 4;
            pct -= 5 * p.PerkRank("focus");
            if (p.WornArmor != null) pct += p.WornArmor.Def.AC * 2;
            if (p.WornShield != null) pct += p.WornShield.Def.AC * 3;
            return Math.Max(0, Math.Min(95, pct));
        }

        /// <summary>Percent chance to learn a spell from its book in one attempt.</summary>
        public static int LearnPct(Player p, SpellDef s)
        {
            int pct = 70 + 4 * (Stat(p) - 10) + p.Skills[Skill.Magic] / 4 - 18 * (s.Level - 1);
            return Math.Max(5, Math.Min(95, pct));
        }
    }
}
