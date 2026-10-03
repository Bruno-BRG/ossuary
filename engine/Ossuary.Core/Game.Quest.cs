using System;
using Ossuary.Core.Items;

namespace Ossuary.Core
{
    /// <summary>
    /// The quest goal and the victory. A single amulet of Yendor waits on the
    /// deepest floor of The Dungeons; surfacing with it (LeaveToOverworld) wins
    /// the run. Pickup uses the existing "g" verb, so no command changes exist.
    /// </summary>
    public sealed partial class Game
    {
        public const string QuestBranch = "The Dungeons";
        public const string QuestAmuletName = "amulet of Yendor";

        public static ItemDef QuestAmuletDef => new ItemDef
        {
            Name = QuestAmuletName,
            Glyph = '"',
            Kind = ItemKind.Amulet,
            Cost = 5000,
            Weight = 3,
            Tier = 4,
            Flags = ItemFlags.QuestItem | ItemFlags.Uncursed,
        };

        public int QuestBottomDepth() => Dungeon.Get(QuestBranch).MaxDepth;

        public bool IsQuestBottom() => Branch == QuestBranch && Depth == QuestBottomDepth();

        /// <summary>Carried anywhere: pack or worn around the neck.</summary>
        public bool HasAmulet()
        {
            for (int i = 0; i < Player.Inventory.Count; i++)
                if (Player.Inventory[i].Def.Name == QuestAmuletName) return true;
            return Player.Amulet != null && Player.Amulet.Def.Name == QuestAmuletName;
        }

        public bool AmuletOnGround() => Map != null && AmuletOnGround(Map);

        public static bool AmuletOnGround(GameMap map)
        {
            if (map == null) return false;
            for (int y = 0; y < map.H; y++)
                for (int x = 0; x < map.W; x++)
                {
                    var stack = GroundItems.At(map.Number, x, y);
                    if (stack == null) continue;
                    for (int i = 0; i < stack.Count; i++)
                        if (stack[i].Def.Name == QuestAmuletName) return true;
                }
            return false;
        }

        /// <summary>Drops the amulet on a random open floor cell. Never duplicates.</summary>
        public static void PlaceQuestAmulet(GameMap map, Rng rng, long uid)
        {
            if (map == null || rng == null || map.W < 3 || map.H < 3) return;
            if (AmuletOnGround(map)) return;

            int bx = -1, by = -1;
            for (int attempt = 0; attempt < 80; attempt++)
            {
                int x = rng.Range(1, map.W - 1), y = rng.Range(1, map.H - 1);
                TileKind t = map.Get(x, y);
                if (t != TileKind.Floor && t != TileKind.FloorAlt) continue;
                bx = x; by = y;
                break;
            }
            if (bx < 0)
            {
                for (int y = 1; y < map.H - 1 && bx < 0; y++)
                    for (int x = 1; x < map.W - 1; x++)
                    {
                        TileKind t = map.Get(x, y);
                        if (t == TileKind.Floor || t == TileKind.FloorAlt) { bx = x; by = y; break; }
                    }
            }
            if (bx < 0) return; // no floor at all: better no amulet than a crash
            GroundItems.Add(map.Number, bx, by, new Item(QuestAmuletDef, rng, uid) { Identified = true });
        }

        /// <summary>Fallback for levels generated before the quest existed.</summary>
        public void EnsureQuestAmulet()
        {
            if (!IsQuestBottom() || Map == null) return;
            if (HasAmulet() || AmuletOnGround()) return;
            PlaceQuestAmulet(Map, Rng, NextUid());
            Say("The air here feels heavy, as if something precious waits in the dark.", MessageKind.Quest);
        }

        /// <summary>Called when leaving the dungeon for the surface. Wins the run.</summary>
        public bool CheckVictory()
        {
            if (!HasAmulet()) return false;
            if (BeginEnding()) return true;   // a hero who read the Seal chooses what to do with it first
            Mode = GameMode.Won;
            UiState.Active = Panel.Win;
            Say("You emerge into the open air, the Amulet of Yendor blazing against your chest.", MessageKind.Quest);
            CheckAchievements();
            Say($"Escaped after {Turn} turns, {Player.Kills} kills, depth {Player.MaxDepth}. The Ossuary remembers.", MessageKind.Good);
            return true;
        }
    }
}
