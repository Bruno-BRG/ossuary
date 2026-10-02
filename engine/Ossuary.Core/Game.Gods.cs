using System;
using System.Collections.Generic;
using Ossuary.Core.Entities;
using Ossuary.Core.Items;
using Ossuary.Core.Magic;

namespace Ossuary.Core
{
    /// <summary>One line in the altar menu.</summary>
    public sealed class AltarRow
    {
        public string Id, Label;
        public bool Enabled = true;
    }

    /// <summary>Gods, piety and altars. Piety is earned by deeds the god likes and lost for deeds it dislikes.</summary>
    public sealed partial class Game
    {
        public const string OfferPrompt = "Offer what?";
        public const string SacrificePrompt = "Sacrifice what?";
        public const int TrialDeeds = 5;
        bool _deed;                                       // true while a deed the god likes is being scored
        readonly HashSet<long> _defiled = new HashSet<long>();

        // How the last kill was made; set by whoever lands the blow, read (and reset) by KillMonster.
        DamageType _killType = DamageType.Physical;
        bool _killSneak, _killByAlly;

        public GodDef God => Gods.Find(Player.God);

        // ------------------------------------------------------------- altars

        /// <summary>Opens the altar menu for the altar at (x, y).</summary>
        public void OpenAltar(int x, int y)
        {
            UiState.AltarX = x; UiState.AltarY = y; UiState.AltarConfirm = false;
            UiRequests.Altar = true;
        }

        public GodDef AltarGod() => Map == null ? null : Gods.AtAltar(Map.Number, UiState.AltarX, UiState.AltarY);

        public int SwearCost() => 150 * Player.Renounced;

        public List<AltarRow> AltarRows()
        {
            var rows = new List<AltarRow>();
            var here = AltarGod();
            if (here == null) return rows;
            var p = Player;
            if (_defiled.Contains(Map.Number * 20_000_000L + UiState.AltarY * 4096 + UiState.AltarX))
            {
                rows.Add(new AltarRow { Id = "dead", Label = "The altar is dead. Nothing answers.", Enabled = false });
                rows.Add(new AltarRow { Id = "leave", Label = "Step back" });
                return rows;
            }
            if (p.God == null)
            {
                int cost = SwearCost();
                rows.Add(new AltarRow { Id = "swear", Label = cost > 0 ? $"Swear to {here.Name} ({cost} gold tribute)" : $"Swear to {here.Name}", Enabled = p.Gold >= cost });
            }
            else if (p.God == here.Id)
            {
                rows.Add(new AltarRow { Id = "pray", Label = "Pray" });
                rows.Add(new AltarRow { Id = "offer-gold", Label = "Offer gold", Enabled = p.Gold >= 25 });
                rows.Add(new AltarRow { Id = "offer-item", Label = "Offer an item", Enabled = p.Inventory.Count > 0 });
                rows.Add(new AltarRow { Id = "sacrifice", Label = "Sacrifice a corpse", Enabled = Corpses().Count > 0 });
                if (p.TrialGoal > 0) rows.Add(new AltarRow { Id = "trial-info", Label = $"Trial: {p.TrialDone}/{p.TrialGoal} deeds {here.Name} likes", Enabled = false });
                else rows.Add(new AltarRow { Id = "trial", Label = $"Ask {here.Name} for a trial", Enabled = p.Piety >= 30 });
                rows.Add(new AltarRow { Id = "renounce", Label = UiState.AltarConfirm ? "Renounce " + here.Name + " (Enter again to confirm)" : "Renounce " + here.Name });
            }
            else
            {
                int cost = 150 * (p.Renounced + 1) * (God.Rival == here.Id ? 2 : 1);
                if (God.Rival == here.Id) rows.Add(new AltarRow { Id = "defile", Label = $"Defile the altar of {here.Name} (for {God.Name})" });
                rows.Add(new AltarRow { Id = "convert", Label = $"Forsake {God.Name}, swear to {here.Name} ({cost} gold)", Enabled = p.Gold >= cost });
            }
            rows.Add(new AltarRow { Id = "leave", Label = "Step back" });
            return rows;
        }

        /// <summary>Runs one altar menu action. Returns true when the menu should close.</summary>
        public bool AltarAction(string id)
        {
            var here = AltarGod();
            var p = Player;
            if (here == null) return true;
            switch (id)
            {
                case "swear":
                    {
                        int cost = SwearCost();
                        if (p.Gold < cost) { Say("You cannot pay the tribute."); return false; }
                        p.Gold -= cost;
                        SwearTo(here);
                        return true;
                    }
                case "convert":
                    {
                        int cost = 150 * (p.Renounced + 1) * (God.Rival == here.Id ? 2 : 1);
                        if (p.Gold < cost) { Say("You cannot pay the tribute."); return false; }
                        p.Gold -= cost;
                        Say($"You turn your back on {God.Name}. The old oath tears loose.", MessageKind.Warn);
                        p.Renounced++;
                        SwearTo(here);
                        return true;
                    }
                case "pray": Pray(here); return true;
                case "sacrifice":
                    {
                        var corpses = Corpses();
                        if (corpses.Count == 0) { Say("You have nothing to sacrifice."); return false; }
                        PushChoice(SacrificePrompt, corpses);
                        return true;
                    }
                case "trial":
                    if (p.TrialGoal > 0 || p.Piety < 30) return false;
                    p.TrialGoal = TrialDeeds; p.TrialDone = 0;
                    Say($"{here.Name} sets you a trial: {TrialDeeds} deeds the god likes ({here.Likes}). The reward is a gift: {here.Gift}.", MessageKind.Quest);
                    return true;
                case "defile":
                    DefileAltar(here);
                    return true;
                case "offer-gold":
                    {
                        int give = Math.Min(p.Gold, 100);
                        if (give < 25) { Say("A few coins are not an offering."); return false; }
                        p.Gold -= give;
                        AddPiety(give / 20, null);
                        Say($"You lay {give} gold on the altar. {here.Name} takes it without a word.", MessageKind.Info);
                        return true;
                    }
                case "offer-item":
                    {
                        if (p.Inventory.Count == 0) { Say("You have nothing to offer."); return false; }
                        PushChoice(OfferPrompt, new List<Item>(p.Inventory));
                        return true;
                    }
                case "renounce":
                    if (!UiState.AltarConfirm) { UiState.AltarConfirm = true; return false; }
                    Say($"You renounce {here.Name}. The silence afterwards is very large.", MessageKind.Warn);
                    p.God = null; p.Piety = 0; p.PrayerTimer = 0; p.Renounced++;
                    return true;
                default: return true;
            }
        }

        // ---------------------------------------------------------- sacrifice, trials, rivals

        List<Item> Corpses()
        {
            var list = new List<Item>();
            foreach (var it in Player.Inventory) if (it.Def.Kind == ItemKind.Corpse) list.Add(it);
            return list;
        }

        /// <summary>A corpse on the altar. Worth grows with what it was; every god weighs it by its own taste.</summary>
        public void SacrificeCorpse(Item corpse)
        {
            var god = God;
            if (corpse == null || god == null || !Player.Inventory.Remove(corpse)) return;
            string name = corpse.Def.Name.EndsWith(" corpse") ? corpse.Def.Name.Substring(0, corpse.Def.Name.Length - 7) : corpse.Def.Name;
            int level = Bestiary.TryGet(name, out var def) ? def.Level : 1;
            int gain = Math.Max(1, Math.Min(10, 1 + level / 2));
            switch (god.Id)
            {
                case "nhal": gain *= 2; break;
                case "veyra": gain = Math.Max(1, gain - 1); break;
                case "aurel": gain = -3; break;
                case "khorr": if (level < Player.Level) gain = 1; break;
            }
            AddPiety(gain, gain < 0 ? "the desecration of the dead" : null);
            AddRep(Houses.Cult, god.Id == "nhal" || god.Id == "mourne" ? 3 : 2, null);
            if (god.Id == "aurel") AddRep(Houses.Temple, -2, null);
            Say(gain < 0 ? $"You lay {corpse.Name} on the altar. {god.Name} turns away." : $"{corpse.Name} burns on the altar. {god.Name} accepts it.", gain < 0 ? MessageKind.Warn : MessageKind.Info);
            EndPlayerTurn();
        }

        /// <summary>Tearing down a rival's altar: a big gift to your own god, and the rival's wrath may follow.</summary>
        void DefileAltar(GodDef rival)
        {
            var p = Player; var own = God;
            _defiled.Add(Map.Number * 20_000_000L + UiState.AltarY * 4096 + UiState.AltarX);
            AddPiety(8, null);
            Say($"You smash the altar of {rival.Name}. {own.Name} is delighted.", MessageKind.Good);
            if (Rng.Chance(40))
            {
                int dmg = Rng.Roll(3, 4, 0) + p.Level / 2;
                p.HP -= Math.Min(dmg, Math.Max(0, p.HP - 1));
                Say($"{rival.Name} curses you from beyond the stone. (-{dmg} HP)", MessageKind.Bad);
                AddCorruption(5, null);
            }
            EndPlayerTurn();
        }

        /// <summary>A deed the god likes was done: it counts toward the trial, which may now be complete.</summary>
        void ScoreDeed()
        {
            var p = Player; var god = God;
            if (god == null || p.TrialGoal <= 0) return;
            if (++p.TrialDone < p.TrialGoal) return;
            p.TrialGoal = 0; p.TrialDone = 0;
            switch (god.Id)
            {
                case "aurel": p.Wis = Math.Min(21, p.Wis + 1); p.BonusMaxHP += 6; break;
                case "khorr": p.Str = Math.Min(21, p.Str + 1); break;
                case "veyra": p.Con = Math.Min(21, p.Con + 1); break;
                case "nhal": p.Int = Math.Min(21, p.Int + 1); break;
                case "sylk": p.Dex = Math.Min(21, p.Dex + 1); break;
                case "mourne": GainMutation(true, false); break;
            }
            p.RefreshGear(); p.RecomputeMaxHP(); p.RecomputeMaxMp();
            Say($"{god.Name} is satisfied. Your trial is done. A gift: {god.Gift}.", MessageKind.Quest);
            AddPiety(20, null);
        }

        void SwearTo(GodDef god)
        {
            if (god.Id == "nhal") AddRep(Houses.Cult, 10, null);
            Player.God = god.Id;
            Player.Piety = 20;
            Player.PrayerTimer = 0;
            Player.Align = god.Align;
            Say($"You swear yourself to {god.Name}, {god.Title}. ({god.Domain})", MessageKind.Good);
        }

        /// <summary>The item goes on the altar. Worth counts: a few points for common things, more for fine ones.</summary>
        public void OfferItem(Item item)
        {
            if (item == null || !Player.Inventory.Remove(item)) return;
            int gain = Math.Max(1, Math.Min(12, item.TradeValue / 40));
            if (item.Rarity == Rarity.Artifact) gain = 25;
            var god = God;
            AddPiety(gain, null);
            Say($"{item.Name} burns on the altar. {(god != null ? god.Name : "Something")} is {(gain >= 6 ? "pleased" : "indifferent")}.", MessageKind.Info);
        }

        // -------------------------------------------------------------- prayer

        void Pray(GodDef god)
        {
            var p = Player;
            bool trouble = p.HP * 3 < p.MaxHP || p.PoisonResist > 0 && p.HP * 2 < p.MaxHP;
            Say($"You kneel and pray to {god.Name}.", MessageKind.Narrative);

            if (p.PrayerTimer > (trouble ? 150 : 0))
            {
                // Too soon: the god is not a vending machine.
                int loss = Math.Min(p.Piety, 20);
                p.Piety -= loss;
                p.PrayerTimer += 100;
                int dmg = Math.Max(1, Rng.Range(1, 6) + p.Level / 2);
                p.HP -= Math.Min(dmg, Math.Max(0, p.HP - 1));
                Say($"{god.Name} is displeased by your haste. A cold hand shoves you down. (-{loss} piety)", MessageKind.Bad);
                return;
            }
            if (trouble)
            {
                if (p.Piety < 10) { Say($"{god.Name} does not answer.", MessageKind.Warn); p.PrayerTimer = 100; EndPlayerTurn(); return; }
                p.HP = p.MaxHP; p.PoisonResist = 0;
                p.Piety = Math.Max(0, p.Piety - 20);
                p.PrayerTimer = 300;
                Say($"{god.Name} hears you. Your wounds close. (-20 piety)", MessageKind.Good);
                EndPlayerTurn();
                return;
            }
            if (p.Piety < god.BoonCost)
            {
                Say($"{god.Name} is silent. (a boon costs {god.BoonCost} piety; you have {p.Piety})", MessageKind.Info);
                EndPlayerTurn();
                return;
            }

            p.Piety -= god.BoonCost;
            p.PrayerTimer = Gods.PrayerCooldown;
            switch (god.Id)
            {
                case "aurel":
                    p.HP = p.MaxHP; p.Mp = p.MpMax; p.PoisonResist = 0;
                    p.Confused = false; p.ConfusionTurns = 0; p.Blinded = false; p.BlindTurns = 0;
                    Say("Warm light pours through you. Aurel makes you whole.", MessageKind.Good);
                    break;
                case "khorr":
                    {
                        var w = p.Wielded;
                        if (w == null) { Say("Khorr finds your hands empty and gives nothing.", MessageKind.Warn); p.Piety += god.BoonCost; p.PrayerTimer = 0; break; }
                        if (w.Enchant >= 5) { Say("Your weapon can hold no more of Khorr's regard.", MessageKind.Info); p.Piety += god.BoonCost; p.PrayerTimer = 0; break; }
                        w.Enchant++; p.RefreshGear();
                        Say($"The stone hums. Your {w.Name} is made stronger.", MessageKind.Good);
                        break;
                    }
                case "veyra":
                    p.SetBuff("flame", 300);
                    Say("Veyra breathes on your hands. Your blows will burn.", MessageKind.Good);
                    break;
                case "nhal":
                    if (CountFreeCellsNear(2) < 2) { Say("There is no room for Nhal's servants.", MessageKind.Warn); p.Piety += god.BoonCost; p.PrayerTimer = 0; break; }
                    SummonAllies("skeleton", 2, 200);
                    break;
                case "mourne":
                    if (!GainMutation(true, false)) { Say("There is nothing left in you for Mourne to give.", MessageKind.Warn); p.Piety += god.BoonCost; p.PrayerTimer = 0; }
                    break;
                case "sylk":
                    p.SetBuff("invisibility", 100); p.Invisible = true;
                    p.Vigor = p.VigorMax;
                    Say("The shadows lean in and keep your secret.", MessageKind.Good);
                    break;
            }
            EndPlayerTurn();
        }

        // ------------------------------------------------------------- favour

        /// <summary>Changes piety, clamped, and says so when a tier is crossed or a god frowns.</summary>
        public void AddPiety(int amount, string reason)
        {
            var god = God;
            if (god == null || amount == 0) return;
            var p = Player;
            int before = p.Piety;
            if (amount > 0 && _deed) ScoreDeed();
            p.Piety = Math.Max(0, Math.Min(Gods.MaxPiety, p.Piety + amount));
            if (before < Gods.Tier1At && p.Piety >= Gods.Tier1At) Say($"{god.Name} favours you. ({god.Tier1})", MessageKind.Good);
            if (before < Gods.Tier2At && p.Piety >= Gods.Tier2At) Say($"{god.Name} holds you dear. ({god.Tier2})", MessageKind.Good);
            if (before >= Gods.Tier1At && p.Piety < Gods.Tier1At) Say($"{god.Name}'s favour slips away.", MessageKind.Warn);
            if (amount <= -2 && reason != null) Say($"{god.Name} frowns at {reason}.", MessageKind.Warn);
        }

        /// <summary>Called from KillMonster with the circumstances of the kill.</summary>
        void GodsOnKill(Monster m)
        {
            var god = God;
            var type = _killType; bool sneak = _killSneak, byAlly = _killByAlly;
            _killType = DamageType.Physical; _killSneak = false; _killByAlly = false;
            if (god == null) return;
            var p = Player;
            _deed = true;
            try { GodsOnKillRules(god, m, type, sneak, byAlly); }
            finally { _deed = false; }
        }

        void GodsOnKillRules(GodDef god, Monster m, DamageType type, bool sneak, bool byAlly)
        {
            var p = Player;
            switch (god.Id)
            {
                case "aurel":
                    if (m.Def.Undead) AddPiety(2, null);
                    if (m.Def.Level == 0 && !byAlly) AddPiety(-4, "the killing of the harmless");
                    break;
                case "khorr":
                    if (!byAlly) AddPiety(m.Def.Level >= p.Level + 2 ? 3 : 1, null);
                    break;
                case "veyra":
                    if (type == DamageType.Fire) AddPiety(2, null);
                    else if (type == DamageType.Cold) AddPiety(-1, null);
                    break;
                case "nhal":
                    if (type == DamageType.Necrotic) AddPiety(2, null);
                    if (byAlly) AddPiety(1, null);
                    if (p.Piety >= Gods.Tier2At) p.HP = Math.Min(p.MaxHP, p.HP + 2);
                    break;
                case "sylk":
                    if (sneak) AddPiety(2, null);
                    break;
            }
        }

        /// <summary>Called when a spell lands.</summary>
        void GodsOnCast(SpellDef spell)
        {
            var god = God;
            if (god == null) return;
            _deed = true;
            try { GodsOnCastRules(god, spell); }
            finally { _deed = false; }
        }

        void GodsOnCastRules(GodDef god, SpellDef spell)
        {
            switch (god.Id)
            {
                case "mourne":
                    if (spell.Id == "reshape-flesh" || spell.Id == "ossify" || spell.Id == "marrow-bolt") AddPiety(2, null);
                    if (spell.Id == "purify") AddPiety(-3, "your purging");
                    break;
                case "aurel":
                    if (spell.School == School.Sacred) AddPiety(1, null);
                    if (spell.School == School.Necromancy) AddPiety(-3, "your necromancy");
                    break;
                case "khorr":
                    if (spell.School == School.Illusion) AddPiety(-1, null);
                    break;
                case "veyra":
                    if (spell.Id == "fireball" || spell.Id == "meteor" || spell.Id == "wall-of-fire") AddPiety(1, null);
                    break;
                case "nhal":
                    if (spell.School == School.Necromancy) AddPiety(1, null);
                    if (spell.School == School.Sacred) AddPiety(-2, "your prayers to another light");
                    break;
                case "sylk":
                    if (spell.School == School.Illusion) AddPiety(1, null);
                    break;
            }
        }

        /// <summary>Called when an ability is used.</summary>
        void GodsOnAbility(string id)
        {
            var god = God;
            if (god == null) return;
            if (god.Id == "sylk" && id == "war-cry") AddPiety(-2, "your roar");
            if (god.Id == "khorr" && id == "vanish") AddPiety(-2, "your skulking");
        }

        /// <summary>Piety wears off slowly: a god wants to be remembered.</summary>
        void GodsTick()
        {
            var p = Player;
            if (p.PrayerTimer > 0) p.PrayerTimer--;
            if (p.God != null && Turn % 250 == 0 && p.Piety > 0) p.Piety--;
        }
    }
}
