using System;
using System.Collections.Generic;
using Ossuary.Core.Entities;
using Ossuary.Core.Items;

namespace Ossuary.Core
{
    /// <summary>
    /// Light crafting: combine what you carry into something better. A recipe is data (what it needs, what it makes);
    /// the craft verb lists the ones the pack can afford and Craft builds the chosen one.
    /// </summary>
    public sealed partial class Game
    {
        public const string CraftPrompt = "Make what?";

        sealed class Recipe
        {
            public string Id, Needs;
            public Func<Player, List<Item>> Gather;     // the ingredient stacks, or null when something is missing
            public Func<Game, Item> Make;
        }

        static bool IsRemains(Item it) => it.Def.Kind == ItemKind.Corpse && (it.Def.Name == "remains" || it.Def.Name == "skeleton corpse");

        static Item Find(Player p, Func<Item, bool> match) { foreach (var it in p.Inventory) if (match(it)) return it; return null; }

        static int Count(Player p, Func<Item, bool> match) { int n = 0; foreach (var it in p.Inventory) if (match(it)) n += Math.Max(1, it.Quantity); return n; }

        static readonly Recipe[] Recipes =
        {
            new Recipe
            {
                Id = "molotov", Needs = "potion of oil + candle",
                Gather = p => { var a = Find(p, i => i.Def.Name == "potion of oil"); var b = Find(p, i => i.Def.Name == "candle"); return a != null && b != null ? new List<Item> { a, b } : null; },
                Make = g => new Item(Crafted.Molotov, g.Rng, g.NextUid()) { Identified = true },
            },
            new Recipe
            {
                Id = "bone-blade", Needs = "a blade + remains",
                Gather = p => { var a = Find(p, i => i.Def.Kind == ItemKind.Weapon && i.Def.Class == ItemClass.Blade); var b = Find(p, IsRemains); return a != null && b != null ? new List<Item> { a, b } : null; },
                Make = g => new Item(Crafted.BoneBlade, g.Rng, g.NextUid()) { Identified = true, Rarity = Rarity.Magic, Prefix = "vampiric" },
            },
            new Recipe
            {
                Id = "bone-armour", Needs = "armour + two remains",
                Gather = p =>
                {
                    var a = Find(p, i => i.Def.Kind == ItemKind.Armor && i.Def.AC <= 3);
                    var b = Find(p, IsRemains);
                    return a != null && b != null && Count(p, IsRemains) >= 2 ? new List<Item> { a, b, b } : null;
                },
                Make = g => new Item(Crafted.BoneArmour, g.Rng, g.NextUid()) { Identified = true, Enchant = 1 },
            },
            new Recipe
            {
                Id = "extra-healing", Needs = "two potions of healing",
                Gather = p =>
                {
                    var a = Find(p, i => i.Def.Name == "potion of healing");
                    return a != null && Count(p, i => i.Def.Name == "potion of healing") >= 2 ? new List<Item> { a, a } : null;
                },
                Make = g =>
                {
                    foreach (var d in Catalogue.Potions) if (d.Name == "potion of extra healing") return new Item(d, g.Rng, g.NextUid()) { Identified = true };
                    return null;
                },
            },
        };

        /// <summary>Previews of what the pack can make now. Each carries its recipe index in <see cref="Item.Uid"/>.</summary>
        public List<Item> CraftChoices()
        {
            var list = new List<Item>();
            for (int i = 0; i < Recipes.Length; i++)
            {
                if (Recipes[i].Gather(Player) == null) continue;
                var preview = Recipes[i].Make(this);
                if (preview == null) continue;
                preview.Uid = -1 - i;
                preview.ArtifactName = Loc.T(preview.Def.Name) + "   [" + Loc.T(Recipes[i].Needs) + "]";
                list.Add(preview);
            }
            return list;
        }

        /// <summary>Builds the recipe a preview stood for: the ingredients go, the product arrives. Costs a turn.</summary>
        public bool Craft(Item preview)
        {
            int index = (int)(-1 - preview.Uid);
            if (index < 0 || index >= Recipes.Length) return false;
            var r = Recipes[index];
            var parts = r.Gather(Player);
            if (parts == null) { Say("You no longer have what that needs.", MessageKind.Warn); return false; }
            var made = r.Make(this);
            if (made == null) return false;
            foreach (var part in parts)
            {
                if (--part.Quantity <= 0) Player.Inventory.Remove(part);
                if (part.Quantity < 0) part.Quantity = 0;
            }
            Player.Inventory.Add(made);
            Player.GainSkill(Skill.Survival, 2);
            Say($"You make {made.Name}.", MessageKind.Good);
            if (Mode == GameMode.Dungeon || Mode == GameMode.TownMap) EndPlayerTurn();
            return true;
        }

        // ---------------------------------------------------------------- throwing a molotov

        /// <summary>Lights the cloth and throws. Fire where it lands, and on whatever stands there.</summary>
        void ThrowAt(Item what, int x, int y)
        {
            if (what == null || !Player.Inventory.Contains(what)) { Say("You are not holding that."); return; }
            if (Map == null || !Map.InBounds(x, y)) { Say("You cannot throw it there."); return; }
            int dist = Pathfinder.Chebyshev(Player.X, Player.Y, x, y);
            if (dist > 7) { Say("That is too far to throw.", MessageKind.Info); return; }
            if (!Map.IsVisible(x, y) || !Fov.HasLine(Map, Player.X, Player.Y, x, y)) { Say("You have no clear line there.", MessageKind.Info); return; }
            if (!Tiles.Walkable(Map.Get(x, y))) { Say("It would shatter on stone. Aim at open floor.", MessageKind.Info); return; }

            if (--what.Quantity <= 0) Player.Inventory.Remove(what);
            MakeNoise(2);
            Say("The molotov bursts into flame!", MessageKind.Combat);
            foreach (var d in new[] { (0, 0), (1, 0), (-1, 0), (0, 1), (0, -1) })
            {
                int cx = x + d.Item1, cy = y + d.Item2;
                if (!Map.InBounds(cx, cy) || !Tiles.Walkable(Map.Get(cx, cy))) continue;
                if (Map.SurfaceAt(cx, cy) == SurfaceKind.Water) continue;
                PutSurface(cx, cy, SurfaceKind.Fire, 5);
            }
            var m = MonsterAt(x, y);
            if (m != null && !m.Ally)
            {
                m.Alert = 1; m.Dormant = false;
                SetAlight(m);
                if (!m.IsDead) ElementalDamage(m, Rng.Range(3, 9), DamageType.Fire);
            }
            Map.Version++;
            EndPlayerTurn();
        }
    }
}
