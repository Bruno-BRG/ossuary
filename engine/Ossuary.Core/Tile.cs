using System;
using System.Collections.Generic;

namespace Ossuary.Core
{
    public enum TileKind : byte
    {
        Void = 0,
        Floor,          // normal dungeon floor
        FloorAlt,       // different colouring: baked tile, blood, carpet
        Wall,           // solid stone
        WallAlt,        // brick / different masonry
        WallDark,       // granite / cave rock
        Pillar,
        ClosedDoor,
        OpenDoor,
        LockedDoor,
        HiddenDoor,
        StairsDown,
        StairsUp,
        LadderDown,
        Portal,
        Rubble,         // boulders
        Altar,
        Fountain,
        Counter,        // a shop or bar counter: bump it to trade
        Bed,
        Table,
        Barrel,
        Shelf,          // bookcase, weapon rack, potion shelf
        Forge,
        Tree,
        Board,          // notice board
        Grave,
        Hearth,
        Loom,           // a weaver's frame: tailoring
        Still,          // copper pot and coil: brewing
    }

    [Flags]
    public enum TileFlags : ushort
    {
        None = 0,
        Walkable = 1 << 0,
        Opaque = 1 << 1,      // blocks sight
        BlocksMove = 1 << 2,  // blocks walking
        Door = 1 << 3,
        Secret = 1 << 4,      // not drawn on the floor plan
        Stairs = 1 << 5,
        Dig = 1 << 6,         // can be tunnelled through
        Rubble = 1 << 7,
        Water = 1 << 8,
        Lava = 1 << 9,
        Alt = 1 << 10,
        Special = 1 << 11,
        Entrance = 1 << 12,   // a dungeon entrance on the overworld
    }

    public struct TileDef
    {
        public char Glyph;
        public TileFlags Flags;
        public string Name;
    }

    public static class Tiles
    {
        static readonly TileDef[] Defs = Build();

        static TileDef[] Build()
        {
            const TileFlags Open = TileFlags.Walkable;
            const TileFlags Wall = TileFlags.Opaque | TileFlags.BlocksMove;
            const TileFlags WallDig = TileFlags.Opaque | TileFlags.BlocksMove | TileFlags.Dig;

            // Size to the last enum member, not to Portal: Rubble/Altar/Fountain sit beyond it
            // and the Set() calls below would write past the end.
            var d = new TileDef[(int)TileKind.Still + 1];
            for (int i = 0; i < d.Length; i++) d[i] = new TileDef { Glyph = '?', Flags = 0, Name = "void" };

            void Set(TileKind k, char g, TileFlags f, string n)
            {
                d[(int)k] = new TileDef { Glyph = g, Flags = f, Name = n };
            }

            Set(TileKind.Void, ' ', 0, "the void");

            Set(TileKind.Floor, '·', Open, "floor");
            Set(TileKind.FloorAlt, '·', Open | TileFlags.Alt, "floor");
            Set(TileKind.Wall, '#', Wall | TileFlags.Dig, "wall");
            Set(TileKind.WallAlt, '#', Wall | TileFlags.Alt | TileFlags.Dig, "brick wall");
            Set(TileKind.WallDark, '#', WallDig, "rock wall");
            Set(TileKind.Pillar, 'I', Open, "pillar");

            Set(TileKind.ClosedDoor, '+', TileFlags.BlocksMove | TileFlags.Opaque | TileFlags.Door | TileFlags.Dig, "closed door");
            Set(TileKind.OpenDoor, '\'', Open | TileFlags.Door | TileFlags.Dig, "open door");
            Set(TileKind.LockedDoor, '+', TileFlags.BlocksMove | TileFlags.Opaque | TileFlags.Door, "locked door");
            // A hidden door looks like plain wall until found: drawing '+' would
            // leak every secret on the map. Glyph '#' matches the surrounding rock.
            Set(TileKind.HiddenDoor, '#', Open | TileFlags.Secret, "door");

            Set(TileKind.StairsDown, '>', Open | TileFlags.Stairs, "stairs down");
            Set(TileKind.StairsUp, '<', Open | TileFlags.Stairs, "stairs up");
            Set(TileKind.LadderDown, '>', Open | TileFlags.Stairs, "ladder down");
            Set(TileKind.Portal, '^', Open | TileFlags.Special, "magic portal");

            Set(TileKind.Rubble, '"', TileFlags.BlocksMove | TileFlags.Rubble | TileFlags.Dig, "rubble");
            Set(TileKind.Altar, '_', TileFlags.BlocksMove | TileFlags.Alt | TileFlags.Special, "altar");
            Set(TileKind.Fountain, '{', Open | TileFlags.Alt | TileFlags.Special, "fountain");

            // Furniture and town scenery. Everything but the bed and the grave blocks movement.
            const TileFlags Solid = TileFlags.BlocksMove | TileFlags.Alt;
            Set(TileKind.Counter, '═', Solid, "counter");
            Set(TileKind.Bed, '≡', Open | TileFlags.Alt, "bed");
            Set(TileKind.Table, 'π', Solid, "table");
            Set(TileKind.Barrel, '○', Solid, "barrel");
            Set(TileKind.Shelf, 'Π', Solid | TileFlags.Opaque, "shelf");
            Set(TileKind.Forge, '♦', Solid, "forge");
            Set(TileKind.Tree, '♣', Solid, "tree");
            Set(TileKind.Board, '□', Solid, "notice board");
            Set(TileKind.Grave, '†', Open | TileFlags.Alt, "grave");
            Set(TileKind.Hearth, '♦', Solid, "hearth");
            Set(TileKind.Loom, '╬', Solid, "loom");
            Set(TileKind.Still, '¤', Solid, "still");
            return d;
        }

        public static TileDef Get(TileKind k) => Defs[(int)k];

        public static bool Walkable(TileKind k) => (Defs[(int)k].Flags & TileFlags.Walkable) != 0;
        public static bool Opaque(TileKind k) => (Defs[(int)k].Flags & TileFlags.Opaque) != 0;
        public static bool BlocksMove(TileKind k) => (Defs[(int)k].Flags & TileFlags.BlocksMove) != 0;
        public static bool IsDoor(TileKind k) => (Defs[(int)k].Flags & TileFlags.Door) != 0;
        public static bool IsStairs(TileKind k) => (Defs[(int)k].Flags & TileFlags.Stairs) != 0;
        public static bool IsSecret(TileKind k) => (Defs[(int)k].Flags & TileFlags.Secret) != 0;
        public static bool IsAlt(TileKind k) => (Defs[(int)k].Flags & TileFlags.Alt) != 0;
    }
}
