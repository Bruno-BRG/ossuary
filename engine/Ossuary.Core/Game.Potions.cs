using System;
using Ossuary.Core.Entities;
using Ossuary.Core.Items;

namespace Ossuary.Core
{
    /// <summary>Drinking potions. One potion is one dose; the item is gone afterwards.</summary>
    public sealed partial class Game
    {
        public void Quaff(Item potion)
        {
            if (potion == null || potion.Def.Kind != ItemKind.Potion) return;
            var p = Player;
            if (ItemSpells.TryGet(potion.Def.Name, out var potionSpell)) { UseSpellItem(potion, potionSpell); return; }
            potion.Identified = true;
            if (--potion.Quantity <= 0) p.Inventory.Remove(potion);
            string n = potion.Def.Name;

            if (n.Contains("healing")) { int hx = p.X, hy = p.Y; Fx((tl, s) => FxLib.Rise(tl, s, hx, hy, Elem.Nature, 3, '+')); }
            if (n.Contains("full healing")) { p.BonusMaxHP += 2; p.RecomputeMaxHP(); p.HP = p.MaxHP; p.PoisonResist = 0; Say("You feel completely whole. (+2 max HP)", MessageKind.Good); }
            else if (n.Contains("extra healing")) Heal(Rng.Roll(6, 8, 0));
            else if (n.Contains("healing")) Heal(Rng.Roll(6, 4, 0));
            else if (n.Contains("poison")) { Say("It was poison!", MessageKind.Bad); PoisonPlayer(Rng.Range(3, 8)); }
            else if (n.Contains("sleeping"))
            {
                Say("You doze off...", MessageKind.Warn);
                for (int i = Rng.Range(3, 7); i > 0 && Mode == GameMode.Dungeon; i--) EndPlayerTurn();
                if (Mode == GameMode.Dungeon) Say("You wake up.", MessageKind.Info);
                return;
            }
            else if (n.Contains("confusion")) { p.Confused = true; p.ConfusionTurns = 15; Say("Everything spins.", MessageKind.Bad); }
            else if (n.Contains("hallucination")) { p.Hallucinating = true; p.HallucinationTurns = 40; Say("Colours crawl across your eyes.", MessageKind.Warn); }
            else if (n.Contains("speed")) { p.SetBuff("haste", 25); Say("The world slows around you.", MessageKind.Good); }
            else if (n.Contains("levitation")) { p.SetBuff("levitating", 40); Say("You rise off the floor. Traps cannot reach you.", MessageKind.Good); }
            else if (n.Contains("acid"))
            {
                int d = Rng.Range(2, 11);
                p.HP -= d; HurtBy("a potion of acid");
                Say($"Acid burns your throat for {d} damage!", MessageKind.Bad);
            }
            else if (n.Contains("oil"))
            {
                for (int dy = -1; dy <= 1; dy++)
                    for (int dx = -1; dx <= 1; dx++) PutSurface(p.X + dx, p.Y + dy, SurfaceKind.Oil);
                Say("Oil slops over the floor around you.", MessageKind.Info);
            }
            else if (n.Contains("the mind")) { p.Mp = p.MpMax; Say("Your thoughts clear and the well of mana fills.", MessageKind.Good); }
            else if (n.Contains("see invisible")) Say("Your eyes tingle. Nothing new is revealed.", MessageKind.Info);
            else if (n.Contains("gain ability"))
            {
                switch (Rng.Range(0, 6))
                {
                    case 0: p.Str = Math.Min(21, p.Str + 1); Say("You feel stronger!", MessageKind.Good); break;
                    case 1: p.Dex = Math.Min(21, p.Dex + 1); Say("You feel nimbler!", MessageKind.Good); break;
                    case 2: p.Con = Math.Min(21, p.Con + 1); Say("You feel healthier!", MessageKind.Good); break;
                    case 3: p.Int = Math.Min(21, p.Int + 1); Say("You feel smarter!", MessageKind.Good); break;
                    case 4: p.Wis = Math.Min(21, p.Wis + 1); Say("You feel wiser!", MessageKind.Good); break;
                    default: p.Cha = Math.Min(21, p.Cha + 1); Say("You feel more charming!", MessageKind.Good); break;
                }
                p.RecomputeMaxHP(); p.RecomputeMaxMp();
            }
            else if (n.Contains("mutation"))
            {
                Say("It tastes of bone dust and old graves.", MessageKind.Warn);
                if (!GainMutation()) Say("Your body has nothing left to give.", MessageKind.Info);
                AddCorruption(5, null);
            }
            else if (n.Contains("gain level"))
            {
                Say("You feel more experienced!", MessageKind.Good);
                if (p.AddXp(Math.Max(1, p.XpNext - p.Xp))) AnnounceLevelUp();
            }
            else Say("You drink it. Nothing much happens.");

            CheckDeath();
            EndPlayerTurn();
        }
    }
}
