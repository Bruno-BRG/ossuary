using System;
using System.Collections.Generic;
using Ossuary.Core.Entities;

namespace Ossuary.Core
{
    /// <summary>
    /// Errands that come from a person's <see cref="Want"/>: avenge a loss, pay a debt, fetch a cure, find lost kin. They are built
    /// from the persona at the moment the hero says yes (so they are a pure function of seed and keys) and end in what the world remembers:
    /// the person's disposition, a Helped flag, a deed in the ledger and a little standing with the Guild.
    /// </summary>
    public static class PersonalQuests
    {
        static string L(string en, string pt) => TownText.L(en, pt);
        const int DebtPrice = 40;

        public static string Id(Game g, Monster m) => "personal." + (g.Town != null ? g.Town.Name : "road") + "." + m.Name + "." + m.Voice;

        public static bool Allowed(Monster m) => m.Persona != null && m.Persona.Troubled && Persona.IsPersonalRole(m.Role);

        /// <summary>The kin a lost-kin errand points at: another ordinary person of the same town.</summary>
        static Monster Kin(Game g, Monster m)
        {
            var pool = new List<Monster>();
            if (g.Town != null)
                foreach (var n in g.Town.Npcs)
                    if (n != m && !n.IsDead && n.Persona != null && Persona.IsPersonalRole(n.Role)) pool.Add(n);
            return pool.Count == 0 ? null : pool[m.Voice % pool.Count];
        }

        public static bool Start(Game g, Monster m)
        {
            string id = Id(g, m);
            var def = new QuestDef { Id = id, Track = QuestDef.Personal, Giver = m.Name, RewardGold = 30 + (m.Voice % 5) * 10 };
            string where = (g.Town != null ? g.Town.Name : "") + ", " + m.Name;
            switch (m.Persona.Want)
            {
                case Want.Revenge:
                    def.Title = L("Settle a score", "Acertar uma conta");
                    def.Step(ObjKind.Kill, L("Kill three kobolds for them.", "Mate três kobolds por eles."), "kobold", 3, null, where)
                       .Step(ObjKind.Flag, L("Tell them it is done.", "Diga a eles que está feito."), "told." + id, 1, null, where);
                    break;
                case Want.Debt:
                    def.Title = L("A debt to clear", "Uma dívida a quitar");
                    def.Step(ObjKind.Flag, L("Pay their debt (forty gold).", "Pague a dívida deles (quarenta de ouro)."), "told." + id, 1, null, where);
                    def.RewardGold = 0;   // the hero pays; the reward is what the person gives back in standing
                    break;
                case Want.RareItem:
                    def.Title = L("A cure for the sick", "Um remédio para o doente");
                    def.Step(ObjKind.Item, L("Find a potion of healing.", "Encontre uma poção de cura."), "potion of healing", 1, null, where)
                       .Step(ObjKind.Flag, L("Bring it to them.", "Leve até eles."), "told." + id, 1, null, where);
                    break;
                case Want.KinLost:
                    var kin = Kin(g, m);
                    if (kin == null) return false;
                    kin.Memory.Set("kin.of." + id);
                    def.Title = L("Someone who did not come home", "Alguém que não voltou para casa");
                    def.Step(ObjKind.Flag, L("Find their kin somewhere in this town.", "Encontre o parente deles em algum lugar desta cidade."), "found." + id, 1, null, where)
                       .Step(ObjKind.Flag, L("Tell them their kin is well.", "Conte a eles que o parente está bem."), "told." + id, 1, null, where);
                    break;
                default: return false;
            }
            def.OnComplete = gm =>
            {
                m.Memory.Set(NpcMemory.Helped); m.Memory.Shift(40);
                gm.RecordDeed(Deed.Helped, m.Name, 2);
                gm.AddRep(Houses.Guild, 2, null);
            };
            return g.StartQuest(def);
        }

        // ------------------------------------------------------------------ the conversation

        static bool Active(Game g, Monster m) => g.QuestActive(Id(g, m));
        static int StepOf(Game g, Monster m) => g.QuestOf(Id(g, m))?.Step ?? -1;
        static Want WantOf(Monster m) => m.Persona.Want;

        static DChoice Go(string en, string pt, string to, Action<Game, Monster> doIt = null, Func<Game, Monster, bool> show = null, Func<Game, Monster, bool> cond = null, int price = 0) =>
            new DChoice { Label = L(en, pt), Goto = to, Do = doIt, Show = show, If = cond, Price = price };

        public static readonly Dialogue Talk = new Dialogue()
            .Node(Dialogue.Start, (g, m) =>
                {
                    var q = g.QuestOf(Id(g, m));
                    if (q == null) return TownText.LineFor(m, g.NextTalk());
                    if (q.Status == QStatus.Done) return L("Thank you again. I do not forget a debt.", "Obrigado de novo. Eu não esqueço uma dívida.");
                    if (q.Status == QStatus.Failed) return L("I will not ask you again.", "Não vou pedir de novo.");
                    return L("Any news about what I asked of you?", "Alguma notícia sobre o que eu pedi?");
                },
                Go("Is something troubling you?", "Algo está lhe incomodando?", "trouble", null, (g, m) => g.QuestOf(Id(g, m)) == null),
                Go("About that favour...", "Sobre aquele favor...", "report", null, (g, m) => Active(g, m)))
            .Node("trouble", (g, m) =>
                {
                    switch (WantOf(m))
                    {
                        case Want.Revenge: return L("Something took what I loved, out there. I want it dead. Three of the kobolds would do.", "Algo levou o que eu amava, lá fora. Quero morto. Três dos kobolds bastariam.");
                        case Want.Debt: return L("I owe a debt I cannot pay, and the collectors are patient men. Forty gold would end it.", "Devo uma dívida que não posso pagar, e os cobradores são homens pacientes. Quarenta de ouro acabariam com isso.");
                        case Want.RareItem: return L("Someone I love is ill. A healing potion would save them, and I cannot go down to find one.", "Alguém que eu amo está doente. Uma poção de cura o salvaria, e eu não posso descer para achar uma.");
                        default: return L("Someone of mine went to the other end of town and never came back. Find them, and tell them I am looking.", "Alguém meu foi para o outro lado da cidade e nunca voltou. Encontre-o e diga que estou procurando.");
                    }
                },
                Go("I will help.", "Eu ajudo.", "thanks", (g, m) => Start(g, m)),
                Go("Not now.", "Agora não.", Dialogue.Start))
            .Node("thanks", L("Thank you. I will remember this, whatever comes of it.", "Obrigado. Vou lembrar disto, aconteça o que acontecer."),
                Go("Of course.", "Claro.", Dialogue.Start))
            .Node("report", L("Tell me, then.", "Diga, então."),
                Go("Pay the debt", "Pagar a dívida", "settled", (g, m) => g.Flags.Add("told." + Id(g, m)),
                    (g, m) => Active(g, m) && WantOf(m) == Want.Debt, null, DebtPrice),
                Go("Give the healing potion", "Dar a poção de cura", "settled", (g, m) => { if (g.TakeItem("potion of healing")) g.Flags.Add("told." + Id(g, m)); },
                    (g, m) => Active(g, m) && WantOf(m) == Want.RareItem, (g, m) => g.HasItem("potion of healing")),
                Go("It is done.", "Está feito.", "settled", (g, m) => g.Flags.Add("told." + Id(g, m)),
                    (g, m) => Active(g, m) && WantOf(m) == Want.Revenge && StepOf(g, m) == 1),
                Go("I found them. They are well.", "Eu os encontrei. Estão bem.", "settled", (g, m) => g.Flags.Add("told." + Id(g, m)),
                    (g, m) => Active(g, m) && WantOf(m) == Want.KinLost && StepOf(g, m) == 1),
                Go("Not yet.", "Ainda não.", Dialogue.Start))
            .Node("settled", L("Then it is settled. You have done more for me than you know.", "Então está resolvido. Você fez mais por mim do que imagina."),
                Go("Take care.", "Cuide-se.", Dialogue.Start));
    }
}
