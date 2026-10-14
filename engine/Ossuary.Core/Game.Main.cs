using System;
using System.Collections.Generic;
using Ossuary.Core.Entities;
using Ossuary.Core.Items;

namespace Ossuary.Core
{
    /// <summary>
    /// The main questline "The Seal" (docs/game/main-quest.md): four documents hidden in four branches, a Reader who explains them,
    /// the truths they unlock, and the choice of what to do with the Amulet once the hero walks out with it. The short path
    /// (take the Amulet, leave) still wins as it always did.
    /// </summary>
    public sealed partial class Game
    {
        public const string DocLedger = "warden's ledger page", DocTally = "dwarf council's tally", DocEntry = "archivist's last entry", DocOrder = "spire order roll";

        /// <summary>Where each document waits, the flag it unlocks when read, and what it says.</summary>
        public sealed class MainDoc
        {
            public string Name, Branch, Truth; public int Depth;
        }

        public static readonly MainDoc[] MainDocs =
        {
            new MainDoc { Name = DocLedger, Branch = "The Dungeons", Depth = 5, Truth = "truth.seal" },
            new MainDoc { Name = DocTally, Branch = "The Mines of Dwarfdeep", Depth = 6, Truth = "truth.vote" },
            new MainDoc { Name = DocEntry, Branch = "The Sunken Vaults", Depth = 8, Truth = "truth.entry" },
            new MainDoc { Name = DocOrder, Branch = "The Ashen Spire", Depth = 12, Truth = "truth.order" },
        };

        public static MainDoc DocByName(string name) { foreach (var d in MainDocs) if (d.Name == name) return d; return null; }

        static ItemDef DocDef(string name) => new ItemDef { Name = name, Glyph = ':', Kind = ItemKind.Ornament, Cost = 0, Weight = 1, Tier = 3, Flags = ItemFlags.QuestItem | ItemFlags.Uncursed };

        readonly HashSet<string> _docsPlaced = new HashSet<string>();

        /// <summary>When a level that holds a document is entered for the first time, the document is laid on open floor (own Rng, never the main stream).</summary>
        void PlaceMainDocs()
        {
            if (Map == null) return;
            foreach (var d in MainDocs)
            {
                if (d.Branch != Branch || d.Depth != Depth || !_docsPlaced.Add(d.Name)) continue;
                var rng = new Rng(Rng.Seed ^ (Rumours.Hash(0xD0C5UL, d.Name, d.Depth) * 0x9E3779B97F4A7C15UL));
                for (int attempt = 0; attempt < 120; attempt++)
                {
                    int x = rng.Range(1, Map.W - 1), y = rng.Range(1, Map.H - 1);
                    var t = Map.Get(x, y);
                    if (t != TileKind.Floor && t != TileKind.FloorAlt) continue;
                    GroundItems.Add(Map.Number, x, y, new Item(DocDef(d.Name), rng, NextUid()) { Identified = true });
                    break;
                }
            }
        }

        /// <summary>People in the regions a truth hurts say so (the world reacts to what the hero learned and told).</summary>
        string TruthLine(Monster m)
        {
            if (World == null || m.Role == TownRole.Pet || (m.Voice + _talkCount) % 3 != 2) return null;
            string region = RegionHere();
            if (Flags.Contains("truth.vote") && region == "The Iron Hills") return TownText.VoteLine;
            if (Flags.Contains("truth.order") && region == "The Verdant Reach") return TownText.OrderLine;
            return null;
        }

        public bool AllTruths => Flags.Contains("truth.vote") && Flags.Contains("truth.entry") && Flags.Contains("truth.order");

        // ------------------------------------------------------------------ the ending

        public string EndingId;
        public int Cycle;
        Monster _voice;

        Monster Voice()
        {
            if (_voice == null)
            {
                _voice = new Monster(Bestiary.Find("hobbit"), new Rng(7)) { Name = "The houses", Townsperson = true, Role = TownRole.Elder };
                _voice.Persona = new Persona { Essential = true }; _voice.Memory = new NpcMemory();
            }
            return _voice;
        }

        /// <summary>Walking out with the Amulet: a hero who read the Seal is asked what to do with it; anyone else simply wins.</summary>
        public bool BeginEnding()
        {
            if (EndingId != null || !HasAmulet() || !Flags.Contains("truth.seal")) return false;
            Talking = Voice(); TalkBuilding = null;
            OpenDialogue(Dialogues.Ending, Voice());
            return true;
        }

        public void FinishEnding(string id)
        {
            EndingId = id;
            RecordDeed("ending", id, 5);
            var speaker = SpeakerRole(id);
            if (speaker != null) Say(CeremonyLine(id, HouseSpeaker(speaker.Value)), MessageKind.Quest);
            if (id == "stamp") { StartNewCycle(); return; }
            Mode = GameMode.Won;
            UiState.Active = Panel.Win;
            Say(Loc.T("You emerge into the open air, the Amulet of Yendor blazing against your chest."), MessageKind.Quest);
            Say(Loc.T(EndingEpilogue(id)), MessageKind.Quest);
            var kept = MatteringDeeds();
            if (kept.Count > 0) Say(TownText.L("The Ossuary remembers:", "O Ossuário se lembra:"), MessageKind.Narrative);
            foreach (var line in kept) Say(line, MessageKind.Narrative);
            CheckAchievements();
            Say($"Escaped after {Turn} turns, {Player.Kills} kills, depth {Player.MaxDepth}. The Ossuary remembers.", MessageKind.Good);
        }

        /// <summary>
        /// What the world does once the Amulet is carried out. The text is English here (the source language) and the
        /// Portuguese lives in <see cref="Loc"/> with every other line, so there is only one place to keep it.
        /// </summary>
        public static string EndingEpilogue(string id)
        {
            switch (id)
            {
                case "shut": return "The priest buries the Amulet in the rite Yendor meant. The dead settle, the lamps burn lower, and the pit closes.";
                case "open": return "The seal turns, and every vault of the old kingdom opens. The Reach is rich by morning, and it is not the living who collect.";
                case "warden": return "The Watch posts a standing guard and gives you the keys. You are the Warden now, and the seal is yours to keep from everyone.";
                case "auction": return "The Amulet goes to the highest bidder. The Reach eats well for a year, and the Ossuary has a new owner.";
                default: return "The League cannot pay five thousand. It pays what it has, takes the Amulet, and thanks you. The world goes on as it was.";
            }
        }

        /// <summary>The post whose holder speaks the words of each ending: the house that offers it (main-quest.md, the ceremony).</summary>
        static TownRole? SpeakerRole(string id)
        {
            switch (id)
            {
                case "shut": return TownRole.Priest;
                case "open": return TownRole.Beggar;
                case "warden": return TownRole.Captain;
                case "auction": case "pay": return TownRole.Elder;
                case "stamp": return TownRole.Scholar;
                default: return null;
            }
        }

        /// <summary>The living holder of a post in a town the hero has walked into this run; null when the post is empty or no one of it was met.</summary>
        Monster HouseSpeaker(TownRole role)
        {
            foreach (var t in _towns.Values)
                foreach (var n in t.Npcs)
                    if (n.Role == role && n.Persona != null && n.Persona.Essential && !n.IsDead) return n;
            return null;
        }

        /// <summary>The words the house's speaker says at the ceremony. With no one of that house left, the rite is done at the altar.</summary>
        string CeremonyLine(string id, Monster who)
        {
            if (who == null)
                return TownText.L("No one of that house is left to speak. The rite is done at the altar, by whoever remembers the words.",
                    "Ninguém dessa casa sobrou para falar. O rito é feito no altar, por quem lembra as palavras.");
            string name = who.Name, title = TownText.RoleTitle(who.Role), pt = Loc.PtOf(title);
            switch (id)
            {
                case "shut": return TownText.L($"{name}, the {title}, lays the Amulet in the earth: the rite Yendor meant.", $"{name}, {pt}, deita o Amuleto na terra: o rito que Yendor queria.");
                case "open": return TownText.L($"{name} of the Drowned turns the seal in the black water, and the archive opens.", $"{name}, dos Afogados, gira o selo na água negra, e o arquivo se abre.");
                case "warden": return TownText.L($"{name}, the {title}, hangs the keys at the gate. The Warden keeps the seal.", $"{name}, {pt}, pendura as chaves no portão. O Carcereiro guarda o selo.");
                case "auction": return TownText.L($"{name}, the {title}, opens the bidding.", $"{name}, {pt}, abre o leilão.");
                case "pay": return TownText.L($"{name}, for the League, counts out the gold.", $"{name}, pela Liga, conta o ouro.");
                default: return TownText.L($"{name}, the {title}, reads Yendor's words back to you, and the stone takes the seal.", $"{name}, {pt}, lê de volta as palavras de Yendor, e a pedra recebe o selo.");
            }
        }

        /// <summary>The Stamp: the hero carries the seal back down and takes Yendor's place. The run goes on as a new cycle, remembered.</summary>
        void StartNewCycle()
        {
            Cycle++;
            for (int i = Player.Inventory.Count - 1; i >= 0; i--) if (Player.Inventory[i].Def.Name == QuestAmuletName) Player.Inventory.RemoveAt(i);
            if (Player.Amulet != null && Player.Amulet.Def.Name == QuestAmuletName) Player.Amulet = null;
            Bounties.Clear();
            Player.HP = Player.MaxHP;
            Flags.Add("legend");
            // The truths and the ledger stay. The seal is stamped and the Amulet is gone for good: no second one is placed, so the hunt
            // does not start over. An open seal errand leaves the journal; a finished one stays on record.
            Quests.RemoveAll(q => q.Def.Id == "main.seal" && q.Status == QStatus.Active);
            Say(Loc.T("You stamp the seal where Yendor stamped it, and the stone takes it. You are the Archivist now. The Ossuary will not forget you."), MessageKind.Quest);
            Say(Loc.T("A new cycle begins. What you did is remembered."), MessageKind.Good);
            LeaveToOverworld();
        }
    }
}
