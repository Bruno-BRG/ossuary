using System.Collections.Generic;
using Ossuary.Core.Entities;
using Ossuary.Core.Items;

namespace Ossuary.Core
{
    /// <summary>
    /// RPG layer: role kits and spendable level-up advances. Kept in Core as data
    /// plus thin Game entry points so the future creation/level-up panels only
    /// need to call NewGameWithRole / ApplyLevelAdvance.
    /// </summary>
    public sealed partial class Game
    {
        /// <summary>Starts a run with the given role. Unknown ids fall back to adventurer.</summary>
        public static Game NewGameWithRole(ulong seed, string roleId) => new Game(seed, roleId);

        void StartingKit(string roleId)
        {
            var role = Roles.Find(roleId);
            Player.Gold = role.Gold;

            ItemDef weapon = KitDef(role.Weapon, Catalogue.Weapons, "dagger");
            ItemDef armor = KitDef(role.Armor, Catalogue.Armor, "leather armour");
            Player.Wielded = new Item(weapon, Rng, NextUid()) { Identified = true };
            Player.WornArmor = new Item(armor, Rng, NextUid()) { Identified = true };

            // Shields and books ride in the pack or on the arm when the role grants them.
            if (!string.IsNullOrEmpty(role.Shield))
                Player.WornShield = new Item(KitDef(role.Shield, Catalogue.Shields, null), Rng, NextUid()) { Identified = true };
            if (!string.IsNullOrEmpty(role.Book))
                Player.Inventory.Add(new Item(KitDef(role.Book, Catalogue.Books, null), Rng, NextUid()) { Identified = true });

            ItemDef ration = Find(Catalogue.Food, "food ration");
            Player.Inventory.Add(new Item(ration, Rng, NextUid()) { Identified = true, Quantity = role.Rations });
            if (!string.IsNullOrEmpty(role.Tool))
                Player.Inventory.Add(new Item(KitDef(role.Tool, Catalogue.Tools, "lock pick"), Rng, NextUid()) { Identified = true });
        }

        static ItemDef KitDef(string name, IReadOnlyList<ItemDef> list, string fallback)
        {
            if (!string.IsNullOrEmpty(name))
            {
                foreach (ItemDef d in list)
                    if (d.Name == name) return d;
            }
            if (!string.IsNullOrEmpty(fallback))
            {
                foreach (ItemDef d in list)
                    if (d.Name == fallback) return d;
            }
            // Every catalogue list is non-empty, so this never throws; it only
            // guards a typo'd role table from taking the run down with it.
            foreach (ItemDef d in list) return d;
            return default;
        }

        /// <summary>Spends one pending level-up advance. Does not cost a turn:
        /// it is a character decision, not a dungeon action.</summary>
        public bool ApplyLevelAdvance(string id)
        {
            if (Player.PendingAdvances <= 0) { Say("You have no advancements to spend. Gain more experience first."); return false; }
            var def = Progression.Find(id);
            if (def == null) { Say("Unknown advancement."); return false; }
            if (!Player.ApplyAdvance(id)) return false;
            Say($"You grow {def.Name.ToLower()}. {def.Blurb}.", MessageKind.Good);
            return true;
        }

        void AnnounceLevelUp()
        {
            Say($"You advance to level {Player.Level} ({Player.Title})! Press c to review, then spend {Player.PendingAdvances} advancement(s).", MessageKind.Good);
        }
    }
}
