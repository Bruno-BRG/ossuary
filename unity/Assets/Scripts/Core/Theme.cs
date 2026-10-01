using System;
using System.Collections.Generic;
using Ossuary.Core.Items;
using Ossuary.Core.World;

namespace Ossuary.Core
{
    /// <summary>
    /// Every colour the game draws with, in one place. Deliberately not a set of
    /// editor-set values: the palette is the game's art direction, and it needs to
    /// be diffable, testable, and consistent between the viewport and the render.
    /// </summary>
    public sealed class Theme
    {
        // Background matches TextBuilder.Clear and the renderer's ScreenBackground
        // (0x0A0A0C): three near-blacks showed seams between map, sidebar and
        // letterbox. Seen dimming uses BackgroundDim, not a third black.
        public Rgb Background = Rgb.FromHex(0x0A0A0C);
        public Rgb BackgroundDim = Rgb.FromHex(0x07080A);
        public Rgb Sidebar = Rgb.FromHex(0x111318);
        public Rgb StatusBar = Rgb.FromHex(0x14161C);
        public Rgb Panel = Rgb.FromHex(0x0E1014);
        public Rgb PanelFrame = Rgb.FromHex(0x4A5568);

        public Rgb Text = Rgb.FromHex(0xC8C2B4);
        public Rgb SidebarText = Rgb.FromHex(0xB8B2A4);
        public Rgb Dim = Rgb.FromHex(0x6E6A62);
        public Rgb Label = Rgb.FromHex(0x8A9BB4);
        public Rgb Title = Rgb.FromHex(0xE0C070);
        public Rgb Rule = Rgb.FromHex(0x3A4050);
        public Rgb StatusLocation = Rgb.FromHex(0x9AA8C0);

        public Rgb Player = Rgb.FromHex(0xF0E080);
        public Rgb PlayerDim = Rgb.FromHex(0x6A6040);
        public Rgb PlayerOver = Rgb.FromHex(0x70E0A0);
        public Rgb Cursor = Rgb.FromHex(0xFF9060);
        public Rgb Fog = Rgb.FromHex(0x14161C);
        public Rgb BarEmpty = Rgb.FromHex(0x22252C);
        public Rgb Energy = Rgb.FromHex(0x60C0F0);

        public Rgb Good = Rgb.FromHex(0x70D080);
        public Rgb Warn = Rgb.FromHex(0xE0C060);
        public Rgb Bad = Rgb.FromHex(0xE07060);
        public Rgb Danger = Rgb.FromHex(0xE05050);
        public Rgb Gold = Rgb.FromHex(0xE0C040);
        public Rgb Combat = Rgb.FromHex(0xE0A080);
        public Rgb Kill = Rgb.FromHex(0xC0E070);
        public Rgb Info = Rgb.FromHex(0x90A0B8);
        public Rgb Narrative = Rgb.FromHex(0xB0A8C8);
        public Rgb Quest = Rgb.FromHex(0x80E0D0);
        public Rgb Death = Rgb.FromHex(0xFF5050);

        // Tile colours, keyed by TileKind so the palette stays exhaustive.
        readonly Dictionary<TileKind, Rgb> _tile = new Dictionary<TileKind, Rgb>();

        public Theme()
        {
            _tile[TileKind.Void] = Rgb.FromHex(0x05060A);
            _tile[TileKind.Floor] = Rgb.FromHex(0x8A8272);
            _tile[TileKind.FloorAlt] = Rgb.FromHex(0xA89C84);
            _tile[TileKind.Wall] = Rgb.FromHex(0x6A6258);
            _tile[TileKind.WallAlt] = Rgb.FromHex(0x8A7060);
            _tile[TileKind.WallDark] = Rgb.FromHex(0x50565E);
            _tile[TileKind.Pillar] = Rgb.FromHex(0x7A7268);
            _tile[TileKind.ClosedDoor] = Rgb.FromHex(0xA08050);
            _tile[TileKind.OpenDoor] = Rgb.FromHex(0x8A7040);
            _tile[TileKind.LockedDoor] = Rgb.FromHex(0xD0A040);
            _tile[TileKind.HiddenDoor] = Rgb.FromHex(0xA08050);
            _tile[TileKind.StairsDown] = Rgb.FromHex(0xE0E0F0);
            _tile[TileKind.StairsUp] = Rgb.FromHex(0xE0C890);
            _tile[TileKind.LadderDown] = Rgb.FromHex(0xE0E0F0);
            _tile[TileKind.Portal] = Rgb.FromHex(0xC060F0);
            _tile[TileKind.Rubble] = Rgb.FromHex(0x6A5E4E);
            _tile[TileKind.Altar] = Rgb.FromHex(0xE0E0E8);
            _tile[TileKind.Fountain] = Rgb.FromHex(0x60B0E0);
        }

        public static readonly Theme Default = new Theme();

        public Rgb TileColor(TileKind k) => _tile.TryGetValue(k, out var c) ? c : Text;

        public Rgb ItemColor(Item it)
        {
            switch (it.Def.Kind)
            {
                case ItemKind.Weapon: return Rgb.FromHex(0xD0C0A0);
                case ItemKind.Armor: return Rgb.FromHex(0xA0B0C0);
                case ItemKind.Shield: return Rgb.FromHex(0xA0B0C0);
                case ItemKind.Ring: return Rgb.FromHex(0xE0E070);
                case ItemKind.Amulet: return Rgb.FromHex(0xE0E070);
                case ItemKind.Wand: return Rgb.FromHex(0xC0A0F0);
                case ItemKind.Scroll: return Rgb.FromHex(0xF0F0E0);
                case ItemKind.Potion: return Rgb.FromHex(0x60E0A0);
                case ItemKind.Food: return Rgb.FromHex(0xC0C080);
                case ItemKind.Gold: return Rgb.FromHex(0xE8C850);
                case ItemKind.Gem: return Rgb.FromHex(0x80E0F0);
                case ItemKind.Tool: return Rgb.FromHex(0xB0B0C0);
                case ItemKind.Book: return Rgb.FromHex(0xD0C090);
                case ItemKind.Ornament: return Rgb.FromHex(0xF0C0E0);
                default: return Text;
            }
        }

        public Rgb MessageColor(MessageKind k)
        {
            switch (k)
            {
                case MessageKind.Good: return Good;
                case MessageKind.Bad: return Bad;
                case MessageKind.Combat: return Combat;
                case MessageKind.Kill: return Kill;
                case MessageKind.Info: return Info;
                case MessageKind.Warn: return Warn;
                case MessageKind.Death: return Death;
                case MessageKind.Narrative: return Narrative;
                case MessageKind.Quest: return Quest;
                default: return Text;
            }
        }

        public Rgb OverworldColor(OverworldTerrain terrain, OverworldFeature feature)
        {
            if (feature != OverworldFeature.None)
            {
                switch (feature)
                {
                    case OverworldFeature.Town: return Rgb.FromHex(0xF0E080);
                    case OverworldFeature.Dungeon: return Rgb.FromHex(0xE07050);
                    case OverworldFeature.Ruin: return Rgb.FromHex(0x9A9488);
                    case OverworldFeature.Cave: return Rgb.FromHex(0xB08050);
                    case OverworldFeature.Mine: return Rgb.FromHex(0xC09060);
                    case OverworldFeature.Keep: return Rgb.FromHex(0xC0B0A0);
                    case OverworldFeature.Shrine: return Rgb.FromHex(0x80E0D0);
                    case OverworldFeature.Bridge: return Rgb.FromHex(0xA0A0B0);
                    default: return Info;
                }
            }

            switch (terrain)
            {
                case OverworldTerrain.DeepWater: return Rgb.FromHex(0x204070);
                case OverworldTerrain.Water: return Rgb.FromHex(0x305898);
                case OverworldTerrain.Shallow: return Rgb.FromHex(0x4078A8);
                case OverworldTerrain.Sand: return Rgb.FromHex(0xC0B080);
                case OverworldTerrain.Grass: return Rgb.FromHex(0x5A8A48);
                case OverworldTerrain.Forest: return Rgb.FromHex(0x2E6A34);
                case OverworldTerrain.Hills: return Rgb.FromHex(0x7A8A50);
                case OverworldTerrain.Mountain: return Rgb.FromHex(0x8A8A92);
                case OverworldTerrain.Swamp: return Rgb.FromHex(0x4A6048);
                case OverworldTerrain.Snow: return Rgb.FromHex(0xD8E0E8);
                case OverworldTerrain.Ash: return Rgb.FromHex(0x6A6A6A);
                case OverworldTerrain.Ruins: return Rgb.FromHex(0x6A6A72);
                case OverworldTerrain.Road: return Rgb.FromHex(0x9A8A68);
                default: return Text;
            }
        }
    }
}