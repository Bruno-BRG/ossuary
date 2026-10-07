using System;
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

        /// <summary>Starts a run with the full creation choice (name, race, role).</summary>
        public static Game NewHero(ulong seed, string name, string raceId, string roleId) => new Game(seed, roleId, raceId, name);

        void StartingKit(string roleId, string raceId)
        {
            var role = Roles.Find(roleId);
            Player.Gold = System.Math.Max(0, role.Gold + Races.Find(raceId).GoldMod);

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
            Player.Inventory.Add(new Item(KitDef("bandage", Catalogue.Tools, null), Rng, NextUid()) { Identified = true, Quantity = 2 });
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
            if (!Progression.IsAvailable(Player, def)) { Say($"{def.Name} is not available to you now."); return false; }
            if (!Player.ApplyAdvance(id)) return false;
            Say($"You grow {def.Name.ToLower()}. {def.Blurb}.", MessageKind.Good);
            return true;
        }

        /// <summary>Slow natural HP and Mp recovery. No RNG: replays stay exact. Hunger and poison stop HP regen.</summary>
        void Regenerate()
        {
            var p = Player;
            GodsTick();
            if (p.HP > 0 && p.HP < p.MaxHP && p.Nutrient > 0 && p.PoisonResist == 0 && ++p.HpTimer >= p.HpRegenInterval())
            {
                p.HpTimer = 0; p.HP++;
            }
            if (p.HP >= p.MaxHP) p.HpTimer = 0;
            if (p.MpMax > 0 && p.Mp < p.MpMax && ++p.MpTimer >= p.MpRegenInterval()) { p.MpTimer = 0; p.Mp++; }
            if (p.Mp >= p.MpMax) p.MpTimer = 0;
            if (p.Vigor < p.VigorMax && ++p.VigorTimer >= p.VigorRegenInterval()) { p.VigorTimer = 0; p.Vigor++; }
            if (p.Vigor >= p.VigorMax) p.VigorTimer = 0;
        }

        /// <summary>Wielding or wearing shows what a magical item really is.</summary>
        public void RevealGear(Item it)
        {
            if (it == null || it.Identified) return;
            it.Identified = true;
            if (it.Rarity != Items.Rarity.Common)
            {
                var lines = it.Mods.Lines();
                Say($"It is {it.Name}!" + (lines.Count > 0 ? " (" + string.Join(", ", lines.ToArray()) + ")" : ""), MessageKind.Good);
                var art = Items.Artifacts.Find(it.ArtifactId);
                if (art != null) Say(art.Lore, MessageKind.Narrative);
            }
            if (it.Imbue != null)
            {
                var sp = Magic.Spells.Find(it.Imbue);
                if (sp != null) Say($"It carries the spell {sp.Name}: you can cast it while you hold it" + (sp.Target == Magic.SpellTarget.Self && it.Def.Kind != ItemKind.Weapon ? ", and it stirs by itself when you are struck." : it.Def.Kind == ItemKind.Weapon ? ", and it flares by itself when you strike." : "."), MessageKind.Good);
            }
        }

        /// <summary>Lifesteal and the elemental die on the wielded weapon, after a melee hit.</summary>
        void MeleeProcs(Monster target, int dealt, bool alive)
        {
            ImbueStrike(target);
            // Everything on you counts: the weapon, spells, rings, the amulet, a mutation.
            var m = Player.Gear;
            if (m.LifeSteal == 0 && m.ExtraSides == 0 && Player.BuffTurns("flame") == 0 && !(Player.God == "veyra" && Player.GodTier >= 2)) return;
            if (m.LifeSteal > 0 && dealt > 0 && Player.HP < Player.MaxHP)
                Player.HP = Math.Min(Player.MaxHP, Player.HP + Math.Max(1, dealt * m.LifeSteal / 100));
            if (alive && (Player.BuffTurns("flame") > 0 || (Player.God == "veyra" && Player.GodTier >= 2)))
                Hurt(target, Rng.Roll(1, 4, 0), DamageType.Fire, "Flames lash", false);
            if (alive && !target.IsDead && m.ExtraSides > 0)
            {
                string verb;
                switch (m.ExtraType)
                {
                    case DamageType.Fire: verb = "Flames lash"; break;
                    case DamageType.Cold: verb = "Frost bites"; break;
                    case DamageType.Poison: verb = "Venom eats into"; break;
                    case DamageType.Lightning: verb = "Sparks arc into"; break;
                    case DamageType.Holy: verb = "Radiance sears"; break;
                    default: verb = "Dark power strikes"; break;
                }
                Hurt(target, Rng.Roll(1, m.ExtraSides, 0), m.ExtraType, verb, false);
            }
        }

        void AnnounceLevelUp()
        {
            RescaleCompanions();
            Cue("levelup");
            Say($"You advance to level {Player.Level} ({Player.Title})! Choose from {Player.PendingAdvances} advancement(s) (Shift+C).", MessageKind.Good);
            UiRequests.Advance = true;
        }
    }
}
