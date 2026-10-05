using System;
using System.Collections.Generic;
using Ossuary.Core.Entities;
using Ossuary.Core.Items;

namespace Ossuary.Core
{
    /// <summary>A moment on the road that asks you what you will do.</summary>
    public sealed class WorldEvent
    {
        public string Id, Title, Text;
        public readonly List<ServiceRow> Rows = new List<ServiceRow>();
    }

    /// <summary>
    /// Overworld events: camps, caravans, ruins, shrines, tolls and the dead by the road. They share the town's service panel:
    /// the event is its "building", its text is the note and its choices are the rows. Each is a small decision with a price.
    /// </summary>
    public sealed partial class Game
    {
        public WorldEvent CurrentEvent;
        static readonly Building EventBuilding = new Building { Name = "The Road", Services = Service.None };

        /// <summary>Chance, per step on the road, that something other than a monster is waiting.</summary>
        const int EventChancePct = 7;

        void MaybeRaiseEvent()
        {
            if (ActiveEncounter || CurrentEvent != null || !Rng.Chance(EventChancePct)) return;
            var region = World.RegionAt(World.PlayerX, World.PlayerY);
            int roll = Rng.Range(0, 10);
            switch (roll)
            {
                case 6: OpenEvent("pilgrim", TownText.L("A pilgrim", "Um peregrino"), TownText.L("A thin figure with a staff and a cloth over the mouth, walking toward the Temple. They stop when they see you, and wait to be spoken to.", "Uma figura magra com um cajado e um pano na boca, caminhando para o Templo. Pára ao ver você e espera ser abordada.")); break;
                case 7: OpenEvent("peddler", TownText.L("A peddler", "Um mascate"), TownText.L("A man bent under a pack bigger than he is. He sells small things and large rumours, and he has opinions about both.", "Um homem curvado sob uma mochila maior que ele. Vende coisas pequenas e grandes boatos, e tem opinião sobre os dois.")); break;
                case 8: OpenEvent("refugee", TownText.L("Refugees", "Refugiados"), TownText.L("A family on the road with everything they own on a handcart. A child watches your pack, not your face.", "Uma família na estrada com tudo que tem num carrinho de mão. Uma criança olha sua mochila, não seu rosto.")); break;
                case 9: OpenEvent("delver", TownText.L("A wounded delver", "Um aventureiro ferido"), TownText.L("Someone who went down and came back worse for it, sitting against a milestone. Their sword is across their knees, and they have not decided what it is for.", "Alguém que desceu e voltou pior, sentado num marco da estrada. A espada está sobre os joelhos, e ainda não decidiu para que serve.")); break;
                case 0: OpenEvent("camp", "An abandoned camp", "A cold fire pit, a torn tent, a pack left behind. Whoever was here left in a hurry, or did not leave."); break;
                case 1: OpenEvent("caravan", "A caravan", "A line of carts has stopped on the road. The drivers watch you without hurry. They have rations and potions, and no wish to haggle long."); break;
                case 2: OpenEvent("ruin", "A ruin by the road", "A fallen wall, a stair going down into the rock, a smell of old stone and older fear."); break;
                case 3: OpenEvent("shrine", "A roadside shrine", "A little cairn and a bowl of ash. Someone still comes here to ask for things."); break;
                case 4: OpenEvent("toll", "A toll", $"Three men with a rope across the road. \"The road is not free,\" says the one with the nicest coat. (Danger here: {region.Danger}.)"); break;
                default: OpenEvent("corpse", "A body by the road", "A traveller, a few days dead. The crows have not finished. The purse is still on the belt."); break;
            }
        }

        public void OpenEvent(string id, string title, string text)
        {
            var e = new WorldEvent { Id = id, Title = title, Text = text };
            void Row(string rid, string label, int price = 0, bool ok = true) => e.Rows.Add(new ServiceRow { Id = rid, Label = label, Price = price, Enabled = ok && Player.Gold >= price });
            switch (id)
            {
                case "camp":
                    Row("search", "Search the camp");
                    Row("rest", "Rest here until you are well");
                    break;
                case "caravan":
                    Row("buy-rations", "Buy rations (two)", Haggle(20, Houses.Guild));
                    Row("buy-potion", "Buy a healing potion", Haggle(60, Houses.Guild));
                    if (RepOf(Houses.Cult) >= 25) Row("buy-grave", "Ask about the vial in the black cart", Haggle(220, Houses.Cult));
                    break;
                case "ruin":
                    Row("delve", "Climb down into the ruin");
                    Row("dig", "Dig under the fallen wall");
                    break;
                case "shrine":
                    Row("offer", "Leave an offering", 25);
                    if (Player.God == null) Row("swear", "Kneel and swear to whoever listens");
                    break;
                case "toll":
                    Row("pay", "Pay the toll", Haggle(30 + Player.Level * 3, Houses.Watch, 10));
                    Row("fight", "Refuse and draw");
                    Row("bluff", "Bluff: the Watch is right behind you");
                    break;
                case "corpse":
                    Row("loot", "Take the purse");
                    Row("bury", "Bury the body");
                    break;
                case "pilgrim":
                    Row("water", TownText.L("Share your water", "Dividir sua água"));
                    Row("news", TownText.L("Ask what they have heard", "Perguntar o que ouviram"));
                    break;
                case "peddler":
                    Row("map", TownText.L("Buy a map fragment", "Comprar um fragmento de mapa"), Haggle(30, Houses.Guild));
                    Row("buy-rations", "Buy rations (two)", Haggle(20, Houses.Guild));
                    Row("news", TownText.L("Ask what he has heard", "Perguntar o que ele ouviu"), 10);
                    break;
                case "refugee":
                    Row("feed", TownText.L("Give them rations", "Dar rações a eles"), 0, HasItemNamed("food ration"));
                    Row("rob", TownText.L("Take what they have", "Tomar o que eles têm"));
                    break;
                case "delver":
                    Row("heal", TownText.L("Give them a healing potion", "Dar uma poção de cura"), 0, HasItemNamed("potion of healing"));
                    Row("rob", TownText.L("Take their pack", "Tomar a mochila"));
                    break;
            }
            Row("leave", "Move on");
            CurrentEvent = e; CurrentDialogue = null;
            Talking = null; TalkBuilding = EventBuilding;
            ServiceNote = Loc.T(title) + ". " + Loc.T(text);
            UiState.Active = Panel.Service; UiState.ServiceIndex = 0;
        }

        /// <summary>Runs one choice of the open event. Returns true when the panel should close.</summary>
        bool EventAction(string id)
        {
            var e = CurrentEvent;
            if (e == null) return true;
            var p = Player;
            bool done = true;
            switch (e.Id + ":" + id)
            {
                case "camp:search":
                    {
                        int roll = Rng.Range(0, 10);
                        if (roll < 4) { int g = Rng.Range(8, 30); p.Gold += g; Tell($"Under the torn tent: {g} gold.", MessageKind.Good); }
                        else if (roll < 7) { GiveItem("food ration", 2); Tell("Two rations, still wrapped.", MessageKind.Good); }
                        else if (roll < 9) Tell("Nothing but ash and a child's boot.", MessageKind.Info);
                        else { int d = Rng.Range(2, 7); p.HP = Math.Max(1, p.HP - d); HurtBy("an ambush at a camp"); Tell($"A snare in the pack bites your hand. (-{d})", MessageKind.Bad); }
                        break;
                    }
                case "camp:rest":
                    p.HP = p.MaxHP; p.Mp = p.MpMax; World.AdvanceTime(8);
                    Tell("You sleep by the dead fire, one eye open. Nothing comes. (8 hours)", MessageKind.Good);
                    break;
                case "caravan:buy-rations": if (!Pay(Haggle(20, Houses.Guild))) return false; GiveItem("food ration", 2); Tell("Two rations, wrapped in waxed cloth.", MessageKind.Good); break;
                case "caravan:buy-potion": if (!Pay(Haggle(60, Houses.Guild))) return false; GiveItem("potion of healing", 1); Tell("A little blue bottle. The driver does not meet your eye.", MessageKind.Good); break;
                case "caravan:buy-grave": if (!Pay(Haggle(220, Houses.Cult))) return false; GiveItem("potion of mutation", 1); Tell("\"Not a word,\" says the driver, and takes your coin with two fingers.", MessageKind.Warn); break;
                case "ruin:delve":
                    {
                        int roll = Rng.Range(0, 10);
                        if (roll < 5) { var loot = LevelBuilder.RollLoot(Rng, Math.Max(2, Player.MaxDepth + 1)); if (loot != null) { p.Inventory.Add(loot); Tell($"Under the stair: {loot.Name}.", MessageKind.Good); } }
                        else if (roll < 8) { var m = new Monster(Bestiary.RandomForDepth(Math.Max(1, World.RegionAt(World.PlayerX, World.PlayerY).Depth / 3), Rng, out _) ?? Bestiary.Find("jackal"), Rng); BeginRoadFight(m, "Something has been living down there"); done = true; break; }
                        else { AddCorruption(8, "The stone remembers you."); }
                        break;
                    }
                case "ruin:dig":
                    if (Rng.Chance(45)) { int g = Rng.Range(30, 90); p.Gold += g; Tell($"A buried strongbox. {g} gold.", MessageKind.Good); }
                    else { int d = Rng.Range(3, 8); p.HP = Math.Max(1, p.HP - d); HurtBy("a collapsing wall"); Tell($"The wall comes down on your arm. (-{d})", MessageKind.Bad); }
                    break;
                case "shrine:offer":
                    if (!Pay(25)) return false;
                    if (p.God != null) { AddPiety(3, null); Tell("The ash in the bowl warms for a moment.", MessageKind.Good); }
                    else { p.Piety = 0; Tell("The ash is only ash, but you feel remembered.", MessageKind.Info); AddRep(Houses.Temple, 2, null); }
                    break;
                case "shrine:swear":
                    {
                        var god = Gods.All[Rng.Range(0, Gods.All.Length)];
                        SwearTo(god);
                        break;
                    }
                case "toll:pay": if (!Pay(Haggle(30 + Player.Level * 3, Houses.Watch, 10))) return false; Tell("\"A pleasure,\" says the man in the nice coat, and means none of it.", MessageKind.Info); break;
                case "toll:fight":
                    BeginRoadFight(new Monster(Bestiary.Find(Player.Level < 4 ? "kobold" : "orc"), Rng), "The one with the nice coat steps back, and the other two do not");
                    AddRep(Houses.Watch, 5, "putting down road bandits");
                    break;
                case "toll:bluff":
                    if (Rng.Range(0, 20) + (Player.Cha - 10) / 2 + RepOf(Houses.Watch) / 10 >= 12) Tell("They look at each other, and at the empty road behind you, and step aside.", MessageKind.Good);
                    else { int fee = Math.Min(Player.Gold, 30 + Player.Level * 6); Player.Gold -= fee; Tell($"They are not fooled, and they are not gentle. (-{fee} gold)", MessageKind.Bad); }
                    break;
                case "corpse:loot":
                    {
                        int g = Rng.Range(10, 40) + Player.Level * 2; p.Gold += g;
                        AddCorruption(3, null);
                        AddRep(Houses.Temple, -3, "robbing the dead");
                        Tell($"{g} gold, and a feeling you cannot put down.", MessageKind.Warn);
                        break;
                    }
                case "pilgrim:water":
                    AddRep(Houses.Temple, 2, "sharing water with a pilgrim");
                    RecordDeed(Deed.Helped, "a pilgrim", 1);
                    Tell(TownText.L("\"May the dead be kind to you.\" They drink, and press a small stone into your hand: worn smooth, warm.", "\"Que os mortos sejam gentis com você.\" Bebem, e põem uma pedrinha na sua mão: lisa, morna."), MessageKind.Good);
                    Player.Piety = Math.Min(Player.Piety + 1, 100);
                    break;
                case "pilgrim:news": case "peddler:news":
                    if (id == "news" && e.Id == "peddler" && !Pay(10)) return false;
                    Tell(HearRumour(), MessageKind.Narrative);
                    break;
                case "peddler:map":
                    if (!Pay(Haggle(30, Houses.Guild))) return false;
                    World.Discover(9);
                    Tell(TownText.L("A scrap of vellum with roads inked on it. Most are right.", "Um pedaço de pergaminho com estradas desenhadas. A maioria está certa."), MessageKind.Good);
                    break;
                case "peddler:buy-rations": if (!Pay(Haggle(20, Houses.Guild))) return false; GiveItem("food ration", 2); Tell("Two rations, wrapped in waxed cloth.", MessageKind.Good); break;
                case "refugee:feed":
                    if (!TakeItem("food ration")) { Tell("You have nothing to give.", MessageKind.Warn); return false; }
                    AddRep(Houses.Guild, 3, "feeding refugees");
                    RecordDeed(Deed.Helped, "refugees", 2);
                    Tell(TownText.L("The child eats without looking up. The mother says nothing at all, which is its own thanks.", "A criança come sem levantar os olhos. A mãe não diz nada, o que já é um agradecimento."), MessageKind.Good);
                    break;
                case "refugee:rob":
                    p.Gold += Rng.Range(8, 25);
                    AddCorruption(3, null);
                    AddRep(Houses.Temple, -5, "robbing refugees");
                    AddRep(Houses.Watch, -3, null);
                    RecordDeed(Deed.Struck, "refugees", 3);
                    Tell(TownText.L("Nobody resists. That is the worst of it.", "Ninguém resiste. Esse é o pior."), MessageKind.Warn);
                    break;
                case "delver:heal":
                    if (!TakeItem("potion of healing")) { Tell("You have nothing to give.", MessageKind.Warn); return false; }
                    p.Gold += 40;
                    AddRep(Houses.Guild, 3, "saving a delver");
                    RecordDeed(Deed.Helped, "a delver", 2);
                    Flags.Add("delver.saved");
                    Tell(TownText.L("Colour comes back to their face. \"I owe you. Take this, and listen: ask for news in every town you pass.\" They press forty gold on you.", "A cor volta ao rosto. \"Eu lhe devo. Pegue isto, e ouça: peça notícias em toda cidade por onde passar.\" Põem quarenta de ouro na sua mão."), MessageKind.Good);
                    break;
                case "delver:rob":
                    p.Gold += Rng.Range(15, 45);
                    AddCorruption(2, null);
                    AddRep(Houses.Temple, -3, "robbing a wounded delver");
                    RecordDeed(Deed.Struck, "a delver", 3);
                    Tell(TownText.L("They watch you do it. They will remember your face, if they live.", "Eles veem você fazer isso. Vão lembrar do seu rosto, se viverem."), MessageKind.Warn);
                    break;
                case "corpse:bury":
                    World.AdvanceTime(1);
                    AddRep(Houses.Temple, 4, "burying a stranger");
                    if (p.God == "aurel") AddPiety(3, null);
                    Tell("It takes an hour and a borrowed spade. You say the words you remember.", MessageKind.Good);
                    break;
                default: break;
            }
            CurrentEvent = null;
            return done;
        }

        void GiveItem(string name, int qty)
        {
            foreach (var list in new IEnumerable<ItemDef>[] { Catalogue.Food, Catalogue.Potions })
                foreach (var d in list)
                    if (d.Name == name)
                    {
                        var existing = Player.Inventory.Find(i => i.Def.Name == name && i.Identified);
                        if (existing != null) existing.Quantity += qty;
                        else Player.Inventory.Add(new Item(d, Rng, NextUid()) { Identified = true, Quantity = qty });
                        return;
                    }
        }

        /// <summary>Starts the same road fight a roaming monster would, with a line of why.</summary>
        void BeginRoadFight(Monster m, string why)
        {
            EncounterMonster = m; EncounterX = World.PlayerX; EncounterY = World.PlayerY; ActiveEncounter = true;
            Say($"{why}. A {m.Name} blocks your path!", MessageKind.Bad);
            Say($"Fight it (Enter or K), or flee (R or <). It is {m.ThreatLabel()}.", MessageKind.Warn);
            Danger();
        }
    }
}
