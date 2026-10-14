using System.Collections.Generic;
using Ossuary.Core.Entities;

namespace Ossuary.Core
{
    /// <summary>
    /// Essential people and the ones who take their posts (docs/game/main-quest.md, Essential NPCs). Every essential person has
    /// an apprentice in the same town (Town.Apprentices). When the hero removes one by an extreme act, the apprentice steps into
    /// the post: the same role, buildings and spot, a worse start with the hero, and one favour owed before the post's business
    /// goes on. With no apprentice left alive the post stands vacant, and the Guild's notice names it.
    /// </summary>
    public sealed partial class Game
    {
        /// <summary>The favour an heir asks for: one quest per post (QuestBook).</summary>
        public static string HeirQuest(TownRole role) => "heir." + role.ToString().ToLowerInvariant();

        /// <summary>The flag that the heir's favour sets when the hero reports back: its last step.</summary>
        public static string HeirReport(TownRole role) => HeirQuest(role) + ".report";

        /// <summary>The post of a removed essential person passes to their apprentice, who stands where the fallen stood.</summary>
        void Succeed(Monster fallen)
        {
            var role = fallen.Role;
            // A favour that an heir was owed dies with them: nobody is left to report to.
            var owed = QuestOf(HeirQuest(role));
            if (owed != null && owed.Status == QStatus.Active && fallen.Memory != null && fallen.Memory.Has(NpcMemory.Owes)) FailQuest(owed);
            var heir = fallen.Persona?.Successor;
            if (fallen.Persona != null) fallen.Persona.Successor = null;
            if (heir == null || heir.IsDead || heir.Persona == null || !heir.Persona.Training.HasValue)
            {
                if (!Town.Vacant.Contains(role)) Town.Vacant.Add(role);
                Say(TownText.L($"No one was trained to take the post of {TownText.RoleTitle(role)}. It stands empty.",
                    $"Ninguém foi treinado para assumir o posto de {TownText.PtOf(TownText.RoleTitle(role))}. Ele ficou vago."), MessageKind.Warn);
                return;
            }
            string title = TownText.RoleTitle(role);
            string ptTitle = TownText.PtOf(title);

            heir.Role = role;
            heir.Floor = fallen.Floor; heir.X = fallen.X; heir.Y = fallen.Y;
            heir.HomeX = fallen.HomeX; heir.HomeY = fallen.HomeY;
            heir.Home = fallen.Home; heir.Shop = fallen.Shop; heir.Leash = fallen.Leash;
            heir.IsGuard = fallen.IsGuard; heir.IsPriest = fallen.IsPriest; heir.Dormant = fallen.Dormant;
            heir.Persona.Essential = true;
            heir.Persona.Training = null;
            heir.Memory.Shift(-30);   // a worse start with the hero than the one before them
            heir.Memory.Set(NpcMemory.Owes);

            foreach (var b in Town.Buildings) if (b.Keeper == fallen) b.Keeper = heir;
            foreach (var s in Town.Shops) if (s.Keeper == fallen) s.Keeper = heir;
            if (heir.Floor == TownZ && !Monsters.Contains(heir)) Monsters.Add(heir);
            Map.Version++;
            Say(TownText.L($"{heir.Name} steps into the post of {title}. They have not forgotten who emptied it.",
                $"{heir.Name} assume o posto de {ptTitle}. Não esqueceu quem o esvaziou."), MessageKind.Warn);
        }

        /// <summary>The Guild's notice while posts stand empty: the League's councils sign for the hero in any town that keeps one.</summary>
        public string MessengerNotice()
        {
            var en = new List<string>();
            var pt = new List<string>();
            foreach (var r in Town.Vacant)
            {
                string t = TownText.RoleTitle(r);
                en.Add(t);
                pt.Add(TownText.PtOf(t));
            }
            return TownText.L(
                $"Messenger notice from the Guild: {Town.Name} has no {string.Join(", ", en)}. Any council of the League will sign for you, in the nearest town that still keeps one.",
                $"Aviso do mensageiro da Guilda: {Town.Name} está sem {string.Join(", ", pt)}. Qualquer conselho da Liga assina por você, na cidade mais próxima que ainda tenha um.");
        }

        /// <summary>The heir's favour is done: the post's business may go on with them, and the track may speak to them.</summary>
        public void SettleHeir(TownRole role)
        {
            foreach (var n in Town.Npcs)
                if (n.Role == role && !n.IsDead && n.Memory != null && n.Memory.Has(NpcMemory.Owes)) n.Memory.Unset(NpcMemory.Owes);
        }

        /// <summary>
        /// A counter whose keeper has died (a raid, the hero's blade) opens again under an heir who says so: the same trade in the
        /// same place, with a face of its own. Essential keepers are the post's business (Succeed), so they are left to it here.
        /// The heir's stream is private, so the town's layout and everyone else stay as they were.
        /// </summary>
        void InstallShopHeirs()
        {
            foreach (var b in Town.Buildings)
            {
                var old = b.Keeper;
                if (old == null || !old.IsDead || b.Burned || old.Persona == null || old.Persona.Essential) continue;
                ulong salt = (ulong)b.X * 73856093UL ^ (ulong)b.Y * 19349663UL ^ (ulong)old.Voice * 83492791UL;
                var heir = TownGen.Heir(Town, b, old, new Rng(Rng.Seed ^ (salt * 0x9E3779B97F4A7C15UL) ^ 0x4E1E7UL));
                heir.Memory.Set(NpcMemory.Inherited);
                b.FormerKeeper = old.Name;
                b.Keeper = heir;
                if (b.Shop != null) b.Shop.Keeper = heir;
                foreach (var s in Town.Shops) if (s.Keeper == old) s.Keeper = heir;
            }
        }

        /// <summary>What the heir of a counter says of the keeper before them: whether the hero's hand was in it, as the ledger remembers.</summary>
        string HeirWords(Building b)
        {
            string old = b.FormerKeeper;
            if (Ledger.Exists(d => d.Kind == Deed.Killed && d.Subject == old))
                return TownText.L($"They say you killed {old}. I keep the counter anyway, and I keep my mouth shut.",
                    $"Dizem que você matou {old}. Fico com o balcão mesmo assim, e fico de boca fechada.");
            return TownText.L($"{old} is dead, and the counter is mine now.", $"{old} morreu, e o balcão agora é meu.");
        }
    }
}
