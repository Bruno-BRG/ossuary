using System;
using System.Collections.Generic;
using Ossuary.Core.Entities;
using Ossuary.Core.Items;

namespace Ossuary.Core
{
    /// <summary>Trained mode: skill points are bought with the experience you earn, five at a time.</summary>
    public sealed partial class Game
    {
        public const string TrainPrompt = "Train what?";
        public const int TrainStep = 5;

        static readonly Skill[] TrainableSkills = (Skill[])Enum.GetValues(typeof(Skill));

        /// <summary>What five points of this skill cost: dearer the better you already are.</summary>
        public int TrainCost(Skill s) => 20 + Player.Skills[s];

        public List<Item> TrainChoices()
        {
            var list = new List<Item>();
            var role = Roles.Find(Player.RoleId);
            for (int i = 0; i < TrainableSkills.Length; i++)
            {
                var s = TrainableSkills[i];
                int have = Player.Skills[s], cap = role.CapFor(s);
                if (have >= cap) continue;
                int to = Math.Min(cap, have + TrainStep);
                var def = new ItemDef { Name = s.ToString(), Glyph = '*', Kind = ItemKind.Tool };
                var it = new Item(def, Rng, -1 - i) { ArtifactName = $"{s} {have} -> {to}   ({TrainCost(s)} XP)", Identified = true };
                list.Add(it);
            }
            return list;
        }

        public bool Train(Item preview)
        {
            int index = (int)(-1 - preview.Uid);
            if (!Player.Trained || index < 0 || index >= TrainableSkills.Length) return false;
            var s = TrainableSkills[index];
            int cost = TrainCost(s);
            if (Player.TrainXp < cost) { Say($"You need {cost} experience to train {s}; you have {Player.TrainXp}.", MessageKind.Warn); return false; }
            int before = Player.Skills[s];
            Player.TrainXp -= cost;
            Player.GainSkill(s, TrainStep, bought: true);
            Say($"You train {s}: {before} -> {Player.Skills[s]}.", MessageKind.Good);
            if (Mode == GameMode.Dungeon || Mode == GameMode.TownMap) EndPlayerTurn();
            return true;
        }
    }
}
