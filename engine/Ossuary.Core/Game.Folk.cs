using System;
using System.Collections.Generic;
using Ossuary.Core.Entities;

namespace Ossuary.Core
{
    /// <summary>What kind of other person the dungeon holds. None is an ordinary monster.</summary>
    public enum FolkKind { None, Wounded, Looter, Captive, Escort }

    /// <summary>
    /// Other people of the dungeon (docs/game/living-world.md, 6): delvers who are not the dungeon's monsters and do not hunt the
    /// hero. A wounded delver asks for a potion of healing and pays for it. A looter runs from the hero and drops a sack when
    /// killed. The remains of a party are a note and a pack on the floor. A captive in chains can be freed; then the captive
    /// walks behind the hero back to the town they wanted to reach, an escort quest on the Personal track. Placed when a level
    /// is first made, from a private stream, so the dungeon itself stays as it was.
    /// </summary>
    public sealed partial class Game
    {
        /// <summary>Freed captives: they follow the hero through the stairs, and wait out the road to their town.</summary>
        public readonly List<Monster> Escorts = new List<Monster>();
        /// <summary>The last town the hero walked into: where a captive found in the dungeon wants to be taken.</summary>
        public string LastTown;
        int _folkSerial;

        // ------------------------------------------------------------------ placement

        /// <summary>Called once, when a dungeon level is first made. A third of the levels hold a delver, and some deeper ones a captive.</summary>
        void PlaceFolk(List<LevelBuilder.SpawnPoint> spawns, int startX, int startY)
        {
            if (Map == null || spawns == null) return;
            var r = new Rng(Rng.Seed ^ (ulong)(Rumours.Hash(Rng.Seed, Branch, Depth) * 2654435761UL) ^ 0xF01CAUL);
            if (r.Chance(35))
            {
                // None is the third kind: the remains of a party, a note and a pack, with no monster at all.
                int roll = r.Range(0, 3);
                var kind = roll == 0 ? FolkKind.Wounded : roll == 1 ? FolkKind.Looter : FolkKind.None;
                int cell = FolkCell(r, startX, startY);
                if (cell >= 0) PutFolk(kind, cell % Map.W, cell / Map.W, r, spawns);
            }
            if (Depth >= 2 && LastTown != null && r.Chance(20))
            {
                int cell = FolkCell(r, startX, startY);
                if (cell >= 0) PutFolk(FolkKind.Captive, cell % Map.W, cell / Map.W, r, spawns);
            }
        }

        /// <summary>A free floor cell away from the hero's arrival, with nobody on it.</summary>
        int FolkCell(Rng r, int startX, int startY)
        {
            for (int tries = 0; tries < 200; tries++)
            {
                int x = r.Range(1, Map.W - 1), y = r.Range(1, Map.H - 1);
                var t = Map.Get(x, y);
                if (t != TileKind.Floor && t != TileKind.FloorAlt) continue;
                if (Pathfinder.Chebyshev(x, y, startX, startY) < 8 || MonsterAt(x, y) != null) continue;
                return x + y * Map.W;
            }
            return -1;
        }

        /// <summary>
        /// Puts one of the dungeon's other people on a cell. A delver or captive is a monster on the floor; the remains of a party
        /// (kind None) are a note cut into the floor and a pack beside it, and no monster at all.
        /// </summary>
        public Monster PutFolk(FolkKind kind, int x, int y, Rng r, List<LevelBuilder.SpawnPoint> spawns = null)
        {
            if (kind == FolkKind.None)
            {
                Engrave(x, y, Both2("Our company went down for the bone-key. Three of us lie here. Take what we did not need.",
                    "Nossa companhia desceu atrás da chave de osso. Três de nós estão aqui. Pegue o que não precisávamos."));
                GroundItems.Add(Map.Number, x, y, LevelBuilder.RollLoot(r, Depth, Branch));
                return null;
            }
            var def = Bestiary.Find("hobbit");
            def.Level = 1; def.Glyph = '@';
            def.Color = kind == FolkKind.Captive ? 0x8A8478 : kind == FolkKind.Looter ? 0xB0A060 : 0xC8B890;
            var m = new Monster(def, r)
            {
                Name = TownText.GivenName(r), Folk = kind, Dormant = true, Alert = 0,
                X = x, Y = y, HomeX = x, HomeY = y, Depth = Map.Depth, Uid = ++_folkSerial,
            };
            if (kind == FolkKind.Wounded) m.HP = 3;
            if (kind == FolkKind.Captive) m.HomeTown = LastTown;
            spawns?.Add(new LevelBuilder.SpawnPoint { X = x, Y = y, Monster = m });
            Monsters.Add(m);
            return m;
        }

        // ------------------------------------------------------------------ the turn and the bump

        /// <summary>A delver's own turn: the wounded and the captive keep still; a looter runs from the hero. True when it has acted.</summary>
        bool FolkTurn(Monster m)
        {
            if (m.Folk == FolkKind.Wounded || m.Folk == FolkKind.Captive) return true;
            if (m.Folk == FolkKind.Looter)
            {
                if (Pathfinder.Chebyshev(m.X, m.Y, Player.X, Player.Y) <= 6) FleeStep(m);
                return true;
            }
            return false;   // an escort acts as any ally does
        }

        /// <summary>
        /// Bumping a delver talks instead of striking. A wounded one with a potion is helped and goes; without one, or a captive
        /// in chains, they shuffle aside and the hero steps through: nobody who sits in a passage shuts it for good. True when
        /// the bump was used up; a looter is simply fought.
        /// </summary>
        bool FolkBump(Monster m)
        {
            if (m.Folk == FolkKind.Wounded && TakeItem("potion of healing"))
            {
                int pay = 30 + 5 * Depth;
                Player.Gold += pay;
                RecordDeed(Deed.Helped, m.Name, 2);
                Say(TownText.L($"You pour the potion into {m.Name}. They are on their feet at once and gone up the stairs before you can thank them. \"Bless you. For the road.\" ({pay} gold)",
                    $"Você derrama a poção em {m.Name}. A pessoa se levanta na hora e some escada acima antes que você possa agradecer. \"Que você seja abençoado. Para a estrada.\" ({pay} ouro)"), MessageKind.Good);
                Monsters.Remove(m);
                m.Folk = FolkKind.None;
                m.HP = 0;
                EndPlayerTurn();
                return true;
            }
            if (m.Folk != FolkKind.Wounded && m.Folk != FolkKind.Captive) return false;

            // Too weak to rise, or in chains: they shuffle aside and the hero steps into the gap.
            Say(m.Folk == FolkKind.Wounded
                ? TownText.L($"{m.Name} is too weak to rise. They shuffle aside, and need a potion of healing.",
                    $"{m.Name} está fraco demais para se levantar. Sai do caminho, e precisa de uma poção de cura.")
                : TownText.L($"{m.Name} shuffles aside, the chains rattling. Face them and press D to break the chains.",
                    $"{m.Name} sai do caminho, as correntes tilintando. De frente para a pessoa, aperte D para quebrar as correntes."), MessageKind.Neutral);
            int hx = Player.X, hy = Player.Y;
            Player.X = m.X; Player.Y = m.Y;
            m.X = hx; m.Y = hy; m.HomeX = hx; m.HomeY = hy;
            Map.Version++;
            UpdateFov();
            EndPlayerTurn();
            return true;
        }

        // ------------------------------------------------------------------ captives and their road home

        /// <summary>Breaks a captive's chains. They walk behind the hero now, and the walk back to their town is a quest.</summary>
        public void FreeCaptive(Monster m)
        {
            if (m == null || m.Folk != FolkKind.Captive || m.HomeTown == null) return;
            m.Folk = FolkKind.Escort;
            m.Ally = true;
            m.Dormant = false;
            Escorts.Add(m);
            string home = m.HomeTown;
            var walk = new QuestDef
            {
                Id = "escort." + m.Uid, Track = QuestDef.Personal, Giver = m.Name, RewardGold = 60,
                Title = TownText.L($"Walk {m.Name} back to {home}", $"Leve {m.Name} de volta a {home}"),
                OnComplete = g => g.EscortArrived(m, home),
            }.Step(ObjKind.Escort, TownText.L($"Walk {m.Name} back to {home}.", $"Leve {m.Name} de volta a {home}."), home, 1, null,
                TownText.L("They keep close, and they are slow on the stairs.", "Eles andam perto, e são lentos nas escadas."));
            StartQuest(walk);
            Say(TownText.L($"The chains give. {m.Name} stands, shaky, and says they will walk with you to {home}.",
                $"As correntes cedem. {m.Name} se levanta, trêmulo, e diz que vai caminhar com você até {home}."), MessageKind.Good);
            EndPlayerTurn();
        }

        /// <summary>The escort is brought through the gate of their town: they go home, and the town remembers who brought them.</summary>
        void EscortArrived(Monster m, string home)
        {
            Escorts.Remove(m);
            AddRep(Houses.Guild, 4, TownText.L($"You brought {m.Name} home to {home}.", $"Você levou {m.Name} para casa, em {home}."));
            RecordDeed(Deed.Helped, m.Name, 4);
            Say(TownText.L($"{m.Name} walks into {home} and does not look back.", $"{m.Name} entra em {home} e não olha para trás."), MessageKind.Good);
        }

        /// <summary>An escort who falls on the road is gone, and the walk with them.</summary>
        void ReapEscorts()
        {
            for (int i = Escorts.Count - 1; i >= 0; i--)
            {
                var e = Escorts[i];
                if (e.HP > 0) continue;
                Escorts.RemoveAt(i);
                var walk = QuestOf("escort." + e.Uid);
                if (walk != null && walk.Status == QStatus.Active) FailQuest(walk);
                Say(TownText.L($"{e.Name} has died on the road.", $"{e.Name} morreu no caminho."), MessageKind.Bad);
            }
        }

        /// <summary>A looter who dies drops what they had taken: a pack on the cell where they fell, from a stream of its own.</summary>
        void DropSack(Monster m)
        {
            if (Map == null) return;
            var r = new Rng(Rng.Seed ^ ((ulong)m.Uid * 0x9E3779B97F4A7C15UL) ^ 0x5AC4UL);
            GroundItems.Add(Map.Number, m.X, m.Y, LevelBuilder.RollLoot(r, Depth, Branch));
        }
    }
}
