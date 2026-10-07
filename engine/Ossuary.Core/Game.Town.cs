using System;
using System.Collections.Generic;
using Ossuary.Core.Entities;
using Ossuary.Core.Items;

namespace Ossuary.Core
{
    /// <summary>One line in a service menu: what it does, what it costs, whether you can have it right now.</summary>
    public sealed class ServiceRow
    {
        public string Id, Label;
        public int Price;
        public bool Enabled = true;
    }

    /// <summary>Towns: entering and leaving, floors, people, and what they sell or do for gold.</summary>
    public sealed partial class Game
    {
        public const string AppraisePrompt = "Appraise what?";

        readonly Dictionary<string, Town> _towns = new Dictionary<string, Town>();
        int _talkCount;

        /// <summary>The floor the player is on inside a town: 0 street, positive upstairs, negative cellars.</summary>
        public int TownZ;
        public Monster Talking;
        public Building TalkBuilding;
        /// <summary>The last thing the person you are talking to said or did, shown inside the service panel.</summary>
        public string ServiceNote = "";

        public void EnterTown(string name)
        {
            string key = name + "@" + World.PlayerX + "," + World.PlayerY;
            if (!_towns.TryGetValue(key, out var town))
            {
                // A town is a pure function of the world seed and its place: same streets every visit, and the
                // simulation's own random stream is left alone.
                ulong h = 14695981039346656037UL;
                foreach (char c in key) { h ^= c; h *= 1099511628211UL; }
                int depth = Math.Max(1, World.RegionAt(World.PlayerX, World.PlayerY).Depth / 3);
                town = TownGen.Generate(name, new Rng(Rng.Seed ^ h), depth);
                _towns[key] = town;
            }
            Town = town;
            TownMap = town.Map;
            Mode = GameMode.TownMap;
            Player.InsideDungeon = false;
            ShowTownFloor(0);
            Player.X = Town.EntryX;
            Player.Y = Town.EntryY;
            UpdateFov();
            Say($"You arrive in {name}. Some {Town.Population} people live here.", MessageKind.Narrative);
            Say(TownText.SizeBlurb(town.Size), MessageKind.Info);
            var today = TownEventToday();
            if (today != TownEventKind.None) Say(EventAnnouncement(today), MessageKind.Quest);
            ArriveAtMarket();
        }

        public void LeaveTown()
        {
            Mode = GameMode.Overworld;
            Town = null;
            TownMap = null;
            Map = null;
            Talking = null; TalkBuilding = null;
            TownZ = 0;
            Monsters.Clear();
            NoteThreat();   // townsfolk are not enemies: leaving town is not a fight
            Say("You leave town and return to the road.", MessageKind.Neutral);
        }

        void ShowTownFloor(int z)
        {
            TownZ = z;
            Map = Town.FloorMap(z);
            Monsters.Clear();
            foreach (var n in Town.Npcs) if (n.Floor == z && !n.IsDead) Monsters.Add(n);
            Map.ClearVisibility();
        }

        /// <summary>The building the player is standing in, or null out on the street.</summary>
        public Building InsideBuilding() => Town?.BuildingAt(Player.X, Player.Y, TownZ, true);

        /// <summary>Takes the staircase under the player: dz = +1 climbs, -1 goes down.</summary>
        public bool TownStairs(int dz)
        {
            if (Town == null || Map == null) return false;
            var here = Map.Get(Player.X, Player.Y);
            if (here != (dz > 0 ? TileKind.StairsUp : TileKind.StairsDown))
            {
                Say(dz > 0 ? "There is no way up here." : "There is no staircase down here.");
                return false;
            }
            var next = Town.FloorMap(TownZ + dz);
            if (next == null) { Say("The stairs lead no further."); return false; }
            ShowTownFloor(TownZ + dz);
            Say(dz > 0 ? "You climb the stairs." : "You descend the staircase.", MessageKind.Narrative);
            UpdateFov();
            EndPlayerTurn();
            return true;
        }

        // --------------------------------------------------------------- people

        static uint Mix(uint a, uint b, uint c)
        {
            uint h = a * 2654435761u ^ b * 2246822519u ^ c * 3266489917u;
            h ^= h >> 15; h *= 2246822519u; h ^= h >> 13;
            return h;
        }

        /// <summary>
        /// Townsfolk potter about inside their leash and never start anything. They draw on a hash of the
        /// turn, not on the simulation's random stream, so time spent in town does not change what the dungeon holds.
        /// </summary>
        void TownsfolkTurn(Monster m)
        {
            if (m.DownUntilDay > Today) return;
            if (CurrentDialogue != null && Talking == m) return;   // someone you are talking to waits for you
            if (m.HostileUntil > Turn && HostileTurn(m)) return;
            if (m.IsGuard && GuardWatch(m)) return;
            if (m.Leash <= 0) return;
            if (GoHomeAtNight(m)) return;
            uint h = Mix((uint)(m.Voice * 31 + m.X), (uint)(m.Y * 7 + Turn), (uint)m.Floor + 11u);
            if (h % 100 >= 24) return;
            int k = (int)((h >> 8) % 8);
            int nx = m.X + Pathfinder.Dx8[k], ny = m.Y + Pathfinder.Dy8[k];
            if (!Map.InBounds(nx, ny)) return;
            var t = Map.Get(nx, ny);
            if (t != TileKind.Floor && t != TileKind.FloorAlt) return;
            if (Pathfinder.Chebyshev(nx, ny, m.HomeX, m.HomeY) > m.Leash) return;
            if ((nx == Player.X && ny == Player.Y) || MonsterAt(nx, ny) != null) return;
            if (!Map.CanStep(m.X, m.Y, nx, ny, true)) return;
            m.X = nx; m.Y = ny;
            Map.Version++;
        }

        /// <summary>
        /// Townsfolk keep hours. After dark the ones with a home make for its door and stay in until morning (guards, keepers,
        /// priests, the drunk and the beggar stay out). A routine of the world clock, not of the Rng.
        /// </summary>
        bool GoHomeAtNight(Monster m)
        {
            if (World == null || !World.IsNight || m.Home == null || m.Floor != TownZ || TownZ != 0) return false;
            if (m.IsGuard || m.IsPriest || m.Home.Keeper == m) return false;
            if (m.Role != TownRole.Citizen && m.Role != TownRole.Child && m.Role != TownRole.Elder && m.Role != TownRole.Scholar) return false;
            var b = m.Home;
            int cx = b.X + b.W / 2, cy = b.Y + b.H / 2;
            if (b.Contains(m.X, m.Y) && Pathfinder.Chebyshev(m.X, m.Y, cx, cy) <= 1) return true;   // in for the night
            uint h = Mix((uint)(m.Voice * 17 + m.X), (uint)(m.Y * 13 + Turn), 77u);
            if (h % 100 >= 70) return true;                                                           // walking, not running
            StepToward(m, b.Contains(m.X, m.Y) ? cx : b.DoorX, b.Contains(m.X, m.Y) ? cy : b.DoorY);
            return true;
        }

        /// <summary>Bumping a person: shopkeepers and service-givers open their counter, everyone else chats.</summary>
        public void TalkTo(Monster m)
        {
            if (m == null || !m.Townsperson) return;
            if (m.DownUntilDay > Today) { Say($"{m.Name} is out cold.", MessageKind.Neutral); return; }
            MetPerson(m);
            if (m.Home != null && m.Home.Keeper == m) { OpenCounter(m.Home, m); return; }
            if (m.Role == TownRole.Pet) { Say(TownText.LineFor(m, _talkCount++), MessageKind.Neutral); EndPlayerTurn(); return; }
            // Everyone else opens a conversation box and stays put until it ends (no turn passes while you talk).
            Talking = m; TalkBuilding = null; StartDialogue(m);
        }

        /// <summary>What this person says when you simply greet them: what they remember of you, what the town is going through, or their own line.</summary>
        public string SmallTalkLine(Monster m)
        {
            return TownText.Reaction(m, RepOf(Houses.Watch), RepOf(Houses.Temple), RepOf(Houses.Guild), Player.Corruption, Player.Mutated.Count, Companions.Count > 0)
                          ?? (TownEventToday() != TownEventKind.None && m.Role != TownRole.Pet && (m.Voice + _talkCount) % 4 == 1 ? TownText.EventLine(TownEventToday(), m.Voice + _talkCount) : null)
                          ?? TruthLine(m)
                          ?? (Town != null && m.Role != TownRole.Pet && MurdersIn(Town.Name) > 0 && (m.Voice + _talkCount) % 3 == 0 ? TownText.Grief : null)
                          ?? TownText.LineFor(m, _talkCount++);
        }

        /// <summary>Bumping a counter, notice board or altar: whoever works there answers.</summary>
        public void UseCounter(int x, int y)
        {
            var b = Town?.BuildingAt(x, y, TownZ);
            if (b == null) return;
            if (b.Keeper == null || b.Keeper.Floor != TownZ || b.Keeper.IsDead) { Say("There is no one here."); return; }
            if (b.Keeper.DownUntilDay > Today) { Say("They are out cold; no one is serving."); return; }
            if (b.Keeper.HostileUntil > Turn) { Say("They will not serve you now.", MessageKind.Warn); return; }
            OpenCounter(b, b.Keeper);
        }

        void OpenCounter(Building b, Monster keeper)
        {
            CurrentEvent = null; CurrentDialogue = null;
            Talking = keeper; TalkBuilding = b;
            ServiceNote = TownText.Reaction(keeper, RepOf(Houses.Watch), RepOf(Houses.Temple), RepOf(Houses.Guild), Player.Corruption, Player.Mutated.Count, false) is string said
                ? Loc.T(said) : TownText.Greeting(keeper);
            if (b.Services == Service.None && b.Shop != null)
            {
                OpenShop(b.Shop);
                UiState.Active = Panel.Shop;
                UiState.ShopIndex = 0;
                return;
            }
            UiState.Active = Panel.Service;
            UiState.ServiceIndex = 0;
        }

        /// <summary>True when closing the shop should drop back into the service menu it was opened from.</summary>
        public bool ShopReturnsToServices => TalkBuilding != null && TalkBuilding.Services != Service.None && Mode == GameMode.TownMap;

        // ------------------------------------------------------------- services

        public int RestPrice => Haggle(6 + Player.Level * 2, Houses.Watch, 10);
        public int HealPrice => Haggle(4 + Player.Level * 2 + Math.Max(0, Player.MaxHP - Player.HP) / 3, Houses.Temple);
        public const int MealPrice = 5, AlePrice = 3, BaseCurePrice = 12, DonatePrice = 25, AppraisePrice = 30;
        public int CurePrice => Haggle(BaseCurePrice, Houses.Temple);

        bool NeedsCure() =>
            Player.PoisonResist > 0 || Player.Blinded || Player.Confused || Player.Hallucinating || Player.StunTurns > 0 || Player.BlindTurns > 0;

        Item HonableWeapon() => Player.Wielded != null && Player.Wielded.Enchant < 3 ? Player.Wielded : null;
        Item HonableArmour() => Player.WornArmor != null && Player.WornArmor.Enchant < 3 ? Player.WornArmor : null;

        public int HonePrice(Item it) => 60 * (Math.Max(0, it.Enchant) + 1);

        public List<ServiceRow> ServiceRows()
        {
            var rows = new List<ServiceRow>();
            var b = TalkBuilding;
            if (b == null) return rows;
            if (CurrentEvent != null) return CurrentEvent.Rows;
            if (CurrentDialogue != null) return DialogueRows();
            var s = b.Services;
            int gold = Player.Gold;
            void Add(string id, string label, int price, bool ok = true) =>
                rows.Add(new ServiceRow { Id = id, Label = label, Price = price, Enabled = ok && gold >= price && !EventBlocks(id) });

            if (b.Shop != null) Add("browse", "Browse the wares", 0);
            if ((s & Service.Rest) != 0) Add("rest", RepOf(Houses.Watch) <= -25 ? "Rest until morning (they know your face)" : "Rest until morning", RestPrice, RepOf(Houses.Watch) > -25);
            if ((s & Service.Meal) != 0) Add("meal", "A hot meal", MealPrice);
            if ((s & Service.Ale) != 0) Add("ale", "A mug of ale", AlePrice);
            if ((s & Service.Ale) != 0 && Companions.Count < MaxCompanions) Add("hire", "Hire a sellsword", HirePrice);
            if ((s & Service.Ale) != 0 && Companions.Count > 0) Add("dismiss", "Send my sellsword home", 0);
            if ((s & Service.Heal) != 0) Add("heal", "Heal my wounds", HealPrice, Player.HP < Player.MaxHP);
            if ((s & Service.Cure) != 0) Add("cure", "Cure my ailments", CurePrice, NeedsCure());
            if ((s & Service.Cure) != 0) Add("purge", "Purge the Ossuary from me", Haggle(PurgePrice, Houses.Temple), Player.Corruption > 0);
            if ((s & Service.Cure) != 0 && RepOf(Houses.Cult) >= 25) Add("grave", "A vial from the back room (the Cult sells)", Haggle(220, Houses.Cult));
            if ((s & Service.Donate) != 0) Add("donate", "Make an offering", DonatePrice);
            if ((s & Service.Appraise) != 0) Add("appraise", "Appraise an item", AppraisePrice, UnidentifiedItems().Count > 0);
            if ((s & Service.Hone) != 0)
            {
                var w = HonableWeapon(); var a = HonableArmour();
                Add("hone", "Hone my weapon", w != null ? HonePrice(w) : 0, w != null);
                Add("reinforce", "Reinforce my armour", a != null ? HonePrice(a) + 20 : 0, a != null);
                AddSmithyRows(Add);
            }
            if ((s & Service.Quest) != 0) Add("dues", DuesUntil > MarketDay ? "Guild dues (paid this week)" : "Pay Guild dues (better prices for a week)", DuesPrice, DuesUntil <= MarketDay);
            if ((s & Service.Quest) != 0) Add("story", "Ask about the Ossuary", 0);
            if ((s & Service.Quest) != 0) Add("board", "Read the notice board", 0);
            if ((s & Service.Quest) != 0)
            {
                var offers = ContractOffers();
                for (int i = 0; i < offers.Count; i++)
                    Add("offer:" + i, $"Take a job: {offers[i].Describe()} (pays {offers[i].Reward}g)", 0, Contracts.Count < MaxContracts);
                for (int i = 0; i < Contracts.Count; i++)
                    if (Contracts[i].Kind != "make")
                        Add("turnin:" + i, $"Report: {Contracts[i].Describe()} ({Math.Min(Contracts[i].Done, Contracts[i].Count)}/{Contracts[i].Count})", 0, Contracts[i].Complete);
            }
            if ((s & Service.Rumor) != 0 && (s & Service.Quest) == 0) Add("rumor", "Ask for news", (s & Service.Ale) != 0 ? 4 : 0);
            AddWorkshopRows(Add);
            AddMarketRows(Add);
            if (Talking != null && CanTalk(Talking)) Add("talk", "Talk", 0);
            Add("leave", "Take my leave", 0);
            return rows;
        }

        bool Pay(int price)
        {
            if (Player.Gold < price) { Tell("You cannot afford that.", MessageKind.Warn); return false; }
            Player.Gold -= price;
            if (TalkBuilding?.Shop != null) TalkBuilding.Shop.Gold += price;
            return true;
        }

        public void Tell(string text, MessageKind kind = MessageKind.Neutral)
        {
            Say(text, kind);
            ServiceNote = Loc.T(text);
        }

        /// <summary>Runs a service row. Returns true when the panel should close afterwards.</summary>
        public bool ServiceAction(string id)
        {
            var b = TalkBuilding;
            if (b == null) return true;
            if (CurrentEvent != null) return EventAction(id);
            if (CurrentDialogue != null) return DialogueAction(id);
            if (id == "talk") { StartDialogue(Talking); return false; }
            if (id.StartsWith("offer:") || id.StartsWith("turnin:")) return ContractAction(id);
            if (SmithyAction(id)) return false;
            if (WorkshopAction(id)) return false;
            if (MarketAction(id)) return false;
            switch (id)
            {
                case "leave": return true;
                case "browse":
                    OpenShop(b.Shop);
                    UiState.Active = Panel.Shop;
                    UiState.ShopIndex = 0;
                    return false;
                case "rest":
                    if (!Pay(RestPrice)) return false;
                    Rest();
                    return true;
                case "meal":
                    if (!Pay(MealPrice)) return false;
                    Player.Nutrient = Math.Min(2000, Player.Nutrient + 400);
                    Player.Hunger = 0;
                    Tell("You eat a hot meal. It is plain and it is good.", MessageKind.Good);
                    return false;
                case "ale":
                    if (!Pay(AlePrice)) return false;
                    Player.HP = Math.Min(Player.MaxHP, Player.HP + Math.Max(2, Player.MaxHP / 10));
                    Tell("You drink a mug of ale. It is bad, and it warms you.", MessageKind.Good);
                    return false;
                case "hire":
                    {
                        if (Companions.Count >= MaxCompanions || !Pay(HirePrice)) return false;
                        var hired = HireCompanion();
                        Tell($"{hired.Name} takes your coin and your word. They will follow you down.", MessageKind.Good);
                        return false;
                    }
                case "dismiss":
                    DismissCompanion();
                    Tell("Your sellsword shakes your hand and goes back to the bar.", MessageKind.Info);
                    return false;
                case "heal":
                    if (!Pay(HealPrice)) return false;
                    Player.HP = Player.MaxHP;
                    AddRep(Houses.Temple, 1, null);
                    Tell("A cold hand, a quiet word. Your wounds close.", MessageKind.Good);
                    return false;
                case "cure":
                    if (!Pay(CurePrice)) return false;
                    CureAilments();
                    Tell("A prayer, and the sickness leaves you.", MessageKind.Good);
                    return false;
                case "purge":
                    if (Player.Corruption <= 0 || !Pay(Haggle(PurgePrice, Houses.Temple))) return false;
                    PurgeCorruption();
                    AddRep(Houses.Temple, 3, null); AddRep(Houses.Cult, -3, null);
                    return false;
                case "grave":
                    if (RepOf(Houses.Cult) < 25 || !Pay(Haggle(220, Houses.Cult))) return false;
                    GiveItem("potion of mutation", 1);
                    Tell("A vial of black water, handed over without a word.", MessageKind.Warn);
                    return false;
                case "donate":
                    if (!Pay(DonatePrice)) return false;
                    AddRep(Houses.Temple, 2, null);
                    if (Player.God != null)
                    {
                        Player.Piety = Math.Min(Gods.MaxPiety, Player.Piety + 3);
                        Tell("Your offering is noted. Your god is pleased.", MessageKind.Good);
                    }
                    else Tell("The priest blesses you, and asks which god you follow.", MessageKind.Neutral);
                    return false;
                case "appraise":
                    PushChoice(AppraisePrompt, UnidentifiedItems());
                    UiState.ChoiceIndex = 0;
                    return false;
                case "hone":
                    {
                        var w = HonableWeapon();
                        if (w == null || !Pay(HonePrice(w))) return false;
                        w.Enchant++;
                        Player.RefreshGear();
                        Tell($"The edge is true again. {w.Name} is better than it was.", MessageKind.Good);
                        return false;
                    }
                case "reinforce":
                    {
                        var a = HonableArmour();
                        if (a == null || !Pay(HonePrice(a) + 20)) return false;
                        a.Enchant++;
                        Player.RefreshGear();
                        Tell($"Fresh rivets and a new lining. {a.Name} will turn a blow better now.", MessageKind.Good);
                        return false;
                    }
                case "story":
                    Tell(TownText.StoryLine(_talkCount++, QuestBottomDepth()), MessageKind.Narrative);
                    return false;
                case "board":
                case "rumor":
                    if (id == "rumor" && !Pay((b.Services & Service.Ale) != 0 ? 4 : 0)) return false;
                    Tell(HearRumour(), MessageKind.Narrative);
                    return false;
            }
            return false;
        }

        bool ContractAction(string id)
        {
            int i = int.Parse(id.Substring(id.IndexOf(':') + 1));
            if (id.StartsWith("offer:"))
            {
                var offers = ContractOffers();
                if (i < 0 || i >= offers.Count) return false;
                AcceptContract(offers[i]);
                return false;
            }
            if (i < 0 || i >= Contracts.Count) return false;
            TurnInContract(Contracts[i]);
            return false;
        }

        List<Item> UnidentifiedItems()
        {
            var list = new List<Item>();
            foreach (var it in Player.Inventory) if (!it.Identified) list.Add(it);
            if (Player.Wielded != null && !Player.Wielded.Identified) list.Add(Player.Wielded);
            foreach (var it in Player.WornPieces()) if (!it.Identified) list.Add(it);
            for (int i = 0; i < 2; i++) if (Player.Rings[i] != null && !Player.Rings[i].Identified) list.Add(Player.Rings[i]);
            return list;
        }

        /// <summary>Pays for and applies an appraisal chosen from the picker.</summary>
        public bool AppraiseItem(Item item)
        {
            if (item == null || item.Identified) return false;
            if (!Pay(AppraisePrice)) return false;
            item.Identified = true;
            Tell($"You learn that it is {item.Name}.", MessageKind.Good);
            return true;
        }

        void CureAilments()
        {
            Player.PoisonResist = 0;
            Player.Blinded = false; Player.BlindTurns = 0;
            Player.Confused = false; Player.ConfusionTurns = 0;
            Player.Hallucinating = false; Player.HallucinationTurns = 0;
            Player.StunTurns = 0;
            StopBleeding();
        }

        /// <summary>A night at the inn: everything mended, hungry mouths fed, the world a few hours older.</summary>
        void Rest()
        {
            Player.HP = Player.MaxHP;
            Player.Mp = Player.MpMax;
            Player.Vigor = Player.VigorMax;
            CureAilments();
            MendWounds(4);
            Player.Nutrient = Math.Max(Player.Nutrient - 40, 300);
            Player.Hunger = 0;
            int hours = (7 - World.Hour + 24) % 24;
            if (hours < 5) hours += 8;
            World.AdvanceTime(hours);
            Tell("You sleep soundly, and wake to morning light and the smell of bread.", MessageKind.Good);
            Cue("rest");
        }

        /// <summary>"▲2" upstairs, "▼1" in a cellar, empty on the street.</summary>
        public string TownFloorLabel() => TownZ == 0 ? "" : TownZ > 0 ? "▲" + TownZ : "▼" + (-TownZ);
    }
}
