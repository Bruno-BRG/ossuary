using System;
using System.Collections.Generic;
using Ossuary.Core.Entities;
using Ossuary.Core.Gen;
using Ossuary.Core.Items;
using Ossuary.Core.World;

namespace Ossuary.Core
{
    /// <summary>
    /// What a level remembers of the past: old engravings that tell the chronicle, warnings cut above a boss's lair, a clue to a
    /// hidden door, the tombs of the dead the chronicle buried here (with what they were buried with), and what each room once
    /// was, read from what is left in it. A level is dressed once, when it is first built, from its own stream (never the game's
    /// Rng). The hero can carve an engraving of their own (Shift+O).
    /// </summary>
    public sealed partial class Game
    {
        public const string EngravePrompt = "Carve what?";
        readonly HashSet<string> _roomsSeen = new HashSet<string>();
        int _readCell = -1;

        static string Both2(string en, string pt) => TownText.L(en, pt);

        void Engrave(int x, int y, string text, string legend = null)
        {
            if (Map == null || !Map.InBounds(x, y)) return;
            Map.Engravings[y * Map.W + x] = text;
            if (legend != null) Map.EngravingLegends[y * Map.W + x] = legend;
        }

        /// <summary>A floor cell of a room (or anywhere) the generator left plain: no stairs, no feature, nothing standing there.</summary>
        bool PlainFloor(int x, int y) => Map.InBounds(x, y) && (Map.Get(x, y) == TileKind.Floor || Map.Get(x, y) == TileKind.FloorAlt) && !Map.Engravings.ContainsKey(y * Map.W + x) && MonsterAt(x, y) == null;

        int FindPlain(Rng r, Func<int, int, bool> where = null)
        {
            for (int i = 0; i < 400; i++)
            {
                int x = r.Range(1, Map.W - 1), y = r.Range(1, Map.H - 1);
                if (PlainFloor(x, y) && (where == null || where(x, y))) return y * Map.W + x;
            }
            return -1;
        }

        /// <summary>Called once when a level is first built: engravings, warnings, clues and tombs.</summary>
        void DressLevel()
        {
            if (Map == null) return;
            var r = new Rng(Rng.Seed ^ (ulong)(Rumours.Hash(Rng.Seed, Branch, Depth) * 2654435761UL) ^ 0xD8E55UL);
            var c = Chronicle;

            // Old engravings: the chronicle, cut into the floor by someone who wanted it remembered.
            int count = r.Chance(60) ? 1 + (r.Chance(35) ? 1 : 0) : 0;
            for (int i = 0; i < count; i++)
            {
                int cell = FindPlain(r);
                if (cell < 0) break;
                var e = c.Events[r.Range(0, c.Events.Count)];
                Engrave(cell % Map.W, cell / Map.W, Both2($"In the year {e.Year}, {e.En}.", $"No ano {e.Year}, {e.Pt}."), e.Key);
            }

            // A warning above a lair, and another at its door.
            var boss = Bosses.ForLevel(Branch, Depth + 1) ?? Bosses.ForLevel(Branch, Depth);
            if (boss != null)
            {
                int cell = FindPlain(r);
                bool below = boss.Depth == Depth + 1;
                if (cell >= 0)
                    Engrave(cell % Map.W, cell / Map.W, below
                        ? Both2($"Turn back. The {boss.Name} waits below.", $"Volte. {Loc.PtOf("The " + boss.Name)} espera lá embaixo.")
                        : Both2($"Here the {boss.Name} holds court. Nobody who read this came back up.", $"Aqui {Loc.PtOf("the " + boss.Name)} reina. Ninguém que leu isto voltou."));
            }

            // A clue: a hidden door somewhere on the level, and a line pointing at it.
            int hidden = -1;
            for (int i = 0; i < Map.W * Map.H && hidden < 0; i++) if (Map.Get(i % Map.W, i / Map.W) == TileKind.HiddenDoor) hidden = i;
            if (hidden >= 0 && r.Chance(70))
            {
                int hx = hidden % Map.W, hy = hidden / Map.W;
                int cell = FindPlain(r, (x, y) => Pathfinder.Chebyshev(x, y, hx, hy) >= 6 && Pathfinder.Chebyshev(x, y, hx, hy) <= 18);
                if (cell >= 0)
                {
                    string dir = DirWord(Math.Sign(hx - cell % Map.W), Math.Sign(hy - cell / Map.W));
                    Engrave(cell % Map.W, cell / Map.W, Both2($"Search the walls to the {dir}. Not everything there is stone.", $"Procure nas paredes, para o {Loc.PtOf(dir)}. Nem tudo ali é pedra."));
                }
            }

            // The chronicle's dead who were buried on this level: a grave, a name on it, and what they were buried with.
            foreach (var f in History.TombsOn(Rng.Seed, Branch, Depth))
            {
                int cell = FindPlain(r, (x, y) => InCrypt(x, y)) ;
                if (cell < 0) cell = FindPlain(r);
                if (cell < 0) break;
                int gx = cell % Map.W, gy = cell / Map.W;
                Map.Set(gx, gy, TileKind.Grave);
                string house = f.House != null ? $" of House {f.House}" : "", housePt = f.House != null ? $", da Casa {f.House}," : "";
                Engrave(gx, gy, Both2($"Here lies {f.Full}{house}, {f.Role} of {f.Place}, {f.Born} to {f.Died}.",
                                      $"Aqui jaz {f.FullPt}{housePt} {History.RolePt(f.Role)} de {f.Place}, {f.Born} a {f.Died}."), f.Key);
                var goods = GraveGoods(f, r);
                if (goods != null) GroundItems.Add(Map.Number, gx, gy, goods);
            }
            Map.Version++;
        }

        bool InCrypt(int x, int y)
        {
            if (Map.Rooms == null) return false;
            foreach (var room in Map.Rooms)
                if (room.IsSpecial && (room.Kind == SpecialRoom.Crypt || room.Kind == SpecialRoom.Temple) && x >= room.X && x < room.X + room.W && y >= room.Y && y < room.Y + room.H) return true;
            return false;
        }

        /// <summary>What a figure was buried with: a knight's sword, a warlord's axe, a king's helm, a priest's amulet. It remembers them.</summary>
        Item GraveGoods(Figure f, Rng r)
        {
            string name = f.Role == "knight" ? "long sword" : f.Role == "warlord" ? "battle axe" : f.Role == "king" ? "great helm" : "amulet of faith";
            if (!Trades.TryDef(name, out var d)) return null;
            var it = new Item(d, r, NextUid()) { Enchant = 1 + r.Range(0, 2), Rarity = Rarity.Magic };
            if (Materials.Takes(d, Materials.Find("steel"))) Materials.Set(it, Materials.Find("steel"));
            it.AddOwner(Both2($"{f.Full}, {f.Role} of {f.Place}, who was buried with it in {f.Died}", $"{f.FullPt}, {History.RolePt(f.Role)} de {f.Place}, enterrado com ele em {f.Died}"));
            return it;
        }

        // ---------------------------------------------------------------- reading what is underfoot

        /// <summary>Stepping onto an engraving or a tomb reads it once; it teaches its legend. Entering a room tells what it was.</summary>
        void ReadUnderfoot()
        {
            if (Map == null || Mode != GameMode.Dungeon) return;
            int cell = Player.Y * Map.W + Player.X;
            if (cell != _readCell && Map.Engravings.TryGetValue(cell, out string text))
            {
                Say(Map.Get(Player.X, Player.Y) == TileKind.Grave ? $"A name is cut into the gravestone: \"{text}\"" : $"Something is engraved here: \"{text}\"", MessageKind.Narrative);
                if (Map.EngravingLegends.TryGetValue(cell, out string key)) LearnLegend(key);
            }
            _readCell = cell;
            RoomFlavour();
        }

        /// <summary>The first time the hero walks into a special room, what it was, read from what is in it.</summary>
        void RoomFlavour()
        {
            if (Map.Rooms == null) return;
            for (int i = 0; i < Map.Rooms.Count; i++)
            {
                var room = Map.Rooms[i];
                if (!room.IsSpecial || Player.X < room.X || Player.X >= room.X + room.W || Player.Y < room.Y || Player.Y >= room.Y + room.H) continue;
                if (!_roomsSeen.Add(Map.Number + ":" + i)) return;
                var line = RoomLine(room.Kind);
                if (line.En != null) Say(Both2(line.En, line.Pt), MessageKind.Narrative);
                return;
            }
        }

        /// <summary>What a room was, as a phrase for looking at a cell inside it.</summary>
        public string RoomAt(int x, int y)
        {
            if (Map?.Rooms == null) return null;
            foreach (var room in Map.Rooms)
                if (room.IsSpecial && x >= room.X && x < room.X + room.W && y >= room.Y && y < room.Y + room.H)
                    return RoomWord(room.Kind);
            return null;
        }

        static string RoomWord(SpecialRoom k)
        {
            switch (k)
            {
                case SpecialRoom.Barracks: case SpecialRoom.BarracksOrc: return "a barracks";
                case SpecialRoom.Temple: return "a temple";
                case SpecialRoom.Shrine: return "a shrine";
                case SpecialRoom.Garden: case SpecialRoom.GardenFountain: return "a garden";
                case SpecialRoom.Mine: return "a mine working";
                case SpecialRoom.Forge: return "a forge";
                case SpecialRoom.Crypt: return "a crypt";
                case SpecialRoom.Keep: case SpecialRoom.Fort: return "a strongpoint";
                case SpecialRoom.Vault: case SpecialRoom.Treasure: return "a treasury";
                case SpecialRoom.Ziggurat: return "a place of sacrifice";
                case SpecialRoom.Slaughterhouse: return "a larder";
                case SpecialRoom.Dormitory: return "a dormitory";
                case SpecialRoom.Hall: return "a feasting hall";
                case SpecialRoom.Fountain: return "a well-room";
                default: return "something else";
            }
        }

        static (string En, string Pt) RoomLine(SpecialRoom k)
        {
            switch (k)
            {
                case SpecialRoom.Barracks: return ("This was a barracks: rotten bunks, a rack where spears stood, a dice cup nobody came back for.", "Isto foi um quartel: beliches podres, um suporte onde ficavam lanças, um copo de dados que ninguém voltou para buscar.");
                case SpecialRoom.BarracksOrc: return ("Orcs have made this a barracks: straw beds, gnawed bones, the smell of too many of them.", "Orcs fizeram disto um quartel: camas de palha, ossos roídos, o cheiro de gente demais.");
                case SpecialRoom.Temple: return ("This was a temple. The altar is still here; the god may not be.", "Isto foi um templo. O altar ainda está aqui; o deus talvez não.");
                case SpecialRoom.Shrine: return ("A shrine: a small altar, old wax, and scratched prayers on the stones.", "Um santuário: um altar pequeno, cera velha e preces riscadas nas pedras.");
                case SpecialRoom.Garden: case SpecialRoom.GardenFountain: return ("Somebody kept a garden down here once. The moss remembers the rows.", "Alguém manteve um jardim aqui embaixo, uma vez. O musgo lembra das fileiras.");
                case SpecialRoom.Mine: return ("A mine working: picks worn to stubs, a cart on its side, a seam that ran out.", "Uma frente de mina: picaretas gastas até o cabo, um carrinho tombado, um veio que acabou.");
                case SpecialRoom.Forge: return ("This was a forge. The anvil is cold, and the slag on the floor is very old.", "Isto foi uma forja. A bigorna está fria, e a escória no chão é muito antiga.");
                case SpecialRoom.Crypt: return ("A crypt. Niches in the walls, most of them full.", "Uma cripta. Nichos nas paredes, quase todos ocupados.");
                case SpecialRoom.Keep: case SpecialRoom.Fort: return ("A strongpoint: arrow slits, a barred inner door, scorch marks where it was taken.", "Um posto fortificado: seteiras, uma porta interna trancada, marcas de fogo de quando foi tomado.");
                case SpecialRoom.Vault: case SpecialRoom.Treasure: return ("A treasury, by the thickness of the walls. Whoever counted the coin here was afraid of thieves.", "Um tesouro, pela grossura das paredes. Quem contava moedas aqui tinha medo de ladrões.");
                case SpecialRoom.Ziggurat: return ("Steps rising to nothing, and a stain at the top that never dried. A place of sacrifice.", "Degraus que sobem até o nada, e uma mancha no topo que nunca secou. Um lugar de sacrifício.");
                case SpecialRoom.Slaughterhouse: return ("A larder: hooks in the ceiling, and some of them are not empty.", "Uma despensa: ganchos no teto, e alguns não estão vazios.");
                case SpecialRoom.Dormitory: return ("A dormitory: rows of cots, a boot under each, as if they all left at once.", "Um dormitório: fileiras de catres, uma bota debaixo de cada, como se todos tivessem saído de uma vez.");
                case SpecialRoom.Hall: return ("A feasting hall: a long table, broken benches, cups still waiting.", "Um salão de banquetes: uma mesa comprida, bancos quebrados, copos ainda esperando.");
                case SpecialRoom.Fountain: return ("A well-room: a basin cut from the rock, and the floor worn smooth by those who came to drink.", "Uma sala de poço: uma bacia talhada na rocha, e o chão polido por quem vinha beber.");
                default: return (null, null);
            }
        }

        // ---------------------------------------------------------------- carving

        /// <summary>The lines a hero may carve: their name, a warning, or the last thing they killed.</summary>
        public List<Item> CarveChoices()
        {
            var list = new List<Item>();
            string hero = Player.CharName;
            var lines = new List<(string, string)>
            {
                ($"{hero} was here.", $"{hero} esteve aqui."),
                ("Turn back.", "Volte."),
                ("Beware what waits below.", "Cuidado com o que espera lá embaixo."),
            };
            var last = Ledger.FindLast(d => d.Kind == Deed.Killed);
            if (last != null) lines.Add(($"Here {hero} killed the {last.Subject}.", $"Aqui {hero} matou {Loc.PtOf("the " + last.Subject)}."));
            if (!Trades.TryDef("parchment", out var def)) return list;
            for (int i = 0; i < lines.Count; i++)
                list.Add(new Item(def, Rng, -1 - i) { Identified = true, ArtifactName = Both2(lines[i].Item1, lines[i].Item2) });
            return list;
        }

        public bool BeginCarve()
        {
            if (Map == null || Mode != GameMode.Dungeon) { Say("There is nothing here to carve into.", MessageKind.Info); return false; }
            if (!PlainFloor(Player.X, Player.Y) && !(Map.Get(Player.X, Player.Y) == TileKind.Floor || Map.Get(Player.X, Player.Y) == TileKind.FloorAlt))
            { Say("You need bare floor to carve into.", MessageKind.Info); return false; }
            if (Map.Engravings.ContainsKey(Player.Y * Map.W + Player.X)) { Say("Something is already carved here.", MessageKind.Info); return false; }
            PushChoice(EngravePrompt, CarveChoices());
            UiState.ChoiceIndex = 0;
            return true;
        }

        public void Carve(Item row)
        {
            if (row?.ArtifactName == null || Map == null) return;
            Engrave(Player.X, Player.Y, row.ArtifactName);
            _readCell = Player.Y * Map.W + Player.X;
            RecordDeed("carved", row.ArtifactName);
            Say($"You carve into the floor: \"{row.ArtifactName}\"", MessageKind.Good);
            Map.Version++;
            EndPlayerTurn();
        }
    }
}
