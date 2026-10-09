using System;
using Ossuary.Core.Entities;

namespace Ossuary.Core
{
    /// <summary>
    /// The conversations of the people the story leans on: the Elder, the Captain, the Priest, the Innkeeper and the Bard.
    /// Everyone else keeps their one-liners (TownText). Text lives here, EN with its PT beside it.
    /// </summary>
    public static class Dialogues
    {
        static string L(string en, string pt) => TownText.L(en, pt);
        static DChoice Go(string en, string pt, string to, Action<Game, Monster> doIt = null, Func<Game, Monster, bool> cond = null, int price = 0, Func<Game, Monster, bool> show = null) =>
            new DChoice { Label = L(en, pt), Goto = to, Do = doIt, If = cond, Price = price, Show = show };

        public static Dialogue For(Monster m)
        {
            if (m == null || !m.Townsperson) return null;
            if (m.Memory != null && m.Memory.Has(NpcMemory.Owes)) return Heir;
            if (m.Persona != null && m.Persona.Training.HasValue) return Apprentice;
            switch (m.Role)
            {
                case TownRole.Elder: return Elder;
                case TownRole.Captain: return Captain;
                case TownRole.Priest: return Priest;
                case TownRole.Innkeeper: return Innkeeper;
                case TownRole.Bard: return Bard;
                case TownRole.Scholar: return Reader;
                case TownRole.Beggar: return Beggar;
            }
            if (PersonalQuests.Allowed(m)) return PersonalQuests.Talk;
            return m.Role == TownRole.Pet ? null : Chat;
        }

        /// <summary>What anyone with a grudge says: nothing useful, and a way out.</summary>
        public static Dialogue Cold(Monster m) =>
            new Dialogue().Node(Dialogue.Start, (g, p) => TownText.Reaction(p, 0, 0, 0, 0, 0, false) ?? "I have nothing to say to you.");

        // ------------------------------------------------------------------ anyone else: a short chat

        static readonly Dialogue Chat = new Dialogue()
            .Node(Dialogue.Start, (g, m) => g.SmallTalkLine(m),
                Go("Ask what they have heard", "Perguntar o que ouviram", "heard"),
                Go("Ask how they are getting on", "Perguntar como vão as coisas", "life"),
                Go("Say something else", "Dizer outra coisa", Dialogue.Start))
            .Node("heard", (g, m) => g.HearRumour(), Go("Go on.", "Continue.", Dialogue.Start))
            .Node("life", (g, m) => TownText.LineFor(m, g.NextTalk()), Go("Go on.", "Continue.", Dialogue.Start));

        // ------------------------------------------------------------------ the beggar who is not (the Cult's voice)

        static readonly Dialogue Beggar = new Dialogue()
            .Node(Dialogue.Start, (g, m) => g.RepOf(Houses.Cult) >= 10
                    ? L("The Drowned remember those who remember them. There is work, if you are the sort who asks.", "Os Afogados lembram de quem lembra deles. Há trabalho, se você for do tipo que pergunta.")
                    : L("A coin for the one who stayed up top?", "Uma moeda para quem ficou aqui em cima?"),
                Go("What work?", "Que trabalho?", "work", (g, m) => g.StartQuest("cult.vial"),
                    (g, m) => g.RepOf(Houses.Cult) >= 10, 0, (g, m) => g.QuestOf("cult.vial") == null),
                Go("What work?", "Que trabalho?", "errands", null,
                    (g, m) => g.RepOf(Houses.Cult) >= 10, 0, (g, m) => g.QuestDone("cult.vial") && (g.QuestOf("cult.zombies") == null || g.QuestOf("cult.bones") == null || g.QuestOf("cult.names") == null || g.QuestOf("cult.shrine") == null)),
                Go("I have the vial.", "Estou com o frasco.", "vial", (g, m) => { g.TakeItem("potion of mutation"); g.Flags.Add("cult.vial.report"); },
                    (g, m) => g.QuestActive("cult.vial") && g.QuestOf("cult.vial").Step == 1 && g.HasItem("potion of mutation")),
                Go("The zombies are dealt with.", "Os zumbis foram resolvidos.", "zombiesdone", (g, m) => g.Flags.Add("cult.zombies.report"), null, 0,
                    (g, m) => g.QuestActive("cult.zombies") && g.QuestOf("cult.zombies").Step == 1),
                Go("Here is the bone blade.", "Aqui está a lâmina de osso.", "bonesdone", (g, m) => { g.TakeItem("bone blade"); g.Flags.Add("cult.bones.report"); },
                    (g, m) => g.HasItem("bone blade"), 0, (g, m) => g.QuestActive("cult.bones") && g.QuestOf("cult.bones").Step == 1),
                Go("The acolytes are silenced.", "Os acólitos foram silenciados.", "namesdone", (g, m) => g.Flags.Add("cult.names.report"), null, 0,
                    (g, m) => g.QuestActive("cult.names") && g.QuestOf("cult.names").Step == 1),
                Go("The shrine is lit.", "O altar está aceso.", "shrinedone", (g, m) => g.Flags.Add("cult.shrine.report"), null, 0,
                    (g, m) => g.QuestActive("cult.shrine") && g.QuestOf("cult.shrine").Step == 1))
            .Node("work", L("A vial of what the black cart sells to the trusted. Bring it to me, and say nothing to the priests.", "Um frasco do que a carroça negra vende aos de confiança. Traga-me, e não diga nada aos sacerdotes."),
                Go("Understood.", "Entendido.", Dialogue.Start))
            .Node("vial", L("It is warm. Good. The Drowned will not forget this, and neither will the Temple, if it learns of it.", "Está morno. Bom. Os Afogados não vão esquecer, e o Templo também não, se descobrir."),
                Go("Good.", "Ótimo.", Dialogue.Start))
            .Node("errands", L("More jobs, for those who can keep quiet. Clear the vaults of the dead that walk, silence the acolytes below, light the shrine at the bottom of the vaults, or bring a blade of bone.", "Mais serviços, para quem sabe ficar quieto. Limpe os cofres dos mortos que andam, cale os acólitos lá embaixo, acenda o altar no fundo dos cofres, ou traga uma lâmina de osso."),
                Go("Clear the Sunken Vaults of the dead that walk", "Limpar os Cofres Afundados dos mortos que andam", Dialogue.Start, (g, m) => g.StartQuest("cult.zombies"), null, 0,
                    (g, m) => g.QuestOf("cult.zombies") == null),
                Go("Silence the acolytes in the Dungeons", "Calar os acólitos nas Masmorras", Dialogue.Start, (g, m) => g.StartQuest("cult.names"), null, 0,
                    (g, m) => g.QuestOf("cult.names") == null),
                Go("Light the shrine at the bottom of the Vaults", "Acender o altar no fundo dos Cofres", Dialogue.Start, (g, m) => g.StartQuest("cult.shrine"), null, 0,
                    (g, m) => g.QuestOf("cult.shrine") == null),
                Go("Bring a blade of bone for the Drowned", "Trazer uma lâmina de osso para os Afogados", Dialogue.Start, (g, m) => g.StartQuest("cult.bones"), null, 0,
                    (g, m) => g.QuestOf("cult.bones") == null),
                Go("Understood.", "Entendido.", Dialogue.Start))
            .Node("zombiesdone", L("Good. The vaults are quieter, and the Temple will not hear of it for a while.", "Ótimo. Os cofres estão mais calmos, e o Templo não vai saber disso por um tempo."),
                Go("Good.", "Ótimo.", Dialogue.Start))
            .Node("bonesdone", L("The blade goes into the water. The Drowned will be pleased, and the Temple will be sorry.", "A lâmina vai para a água. Os Afogados ficarão satisfeitos, e o Templo, arrependido."),
                Go("Good.", "Ótimo.", Dialogue.Start))
            .Node("namesdone", L("Good. The chanting stops, and the Temple's men will ask what became of their acolytes.", "Ótimo. O canto parou, e os homens do Templo vão perguntar o que foi feito dos acólitos."),
                Go("Good.", "Ótimo.", Dialogue.Start))
            .Node("shrinedone", L("The light comes up through the water, cold and clean. The Drowned will remember the Vaults.", "A luz sobe pela água, fria e limpa. Os Afogados vão lembrar dos Cofres."),
                Go("Good.", "Ótimo.", Dialogue.Start));

        // ------------------------------------------------------------------ the apprentice (who takes the post when it empties)

        static readonly Dialogue Apprentice = new Dialogue()
            .Node(Dialogue.Start, (g, m) => ApprenticeLine(m),
                Go("What is the post like?", "Como é o cargo?", "post"),
                Go("Say something else", "Dizer outra coisa", Dialogue.Start))
            .Node("post", L("Long hours, and nobody lets you touch anything until they are sure you will not spoil it.", "Muitas horas, e ninguém deixa você tocar em nada até ter certeza de que você não vai estragar."),
                Go("Good luck.", "Boa sorte.", Dialogue.Start));

        static string ApprenticeLine(Monster m)
        {
            if (m.Persona.Training == TownRole.Beggar)
                return L("I am learning the old words of the Drowned. They say I am not ready yet.", "Estou aprendendo as palavras antigas dos Afogados. Dizem que ainda não estou pronto.");
            string title = TownText.RoleTitle(m.Persona.Training.Value);
            return TownText.L($"I am learning to be the {title}. They say I am not ready yet.", $"Estou aprendendo a ser o {TownText.PtOf(title)}. Dizem que ainda não estou pronto.");
        }

        // ------------------------------------------------------------------ the heir of a post that changed hands (one favour first)

        static readonly Dialogue Heir = new Dialogue()
            .Node(Dialogue.Start, L("The post is mine now, and I did not ask for it this way. Before I put my name to anything, you will do one thing for me.", "O cargo é meu agora, e não pedi que fosse assim. Antes de assinar qualquer coisa, você vai fazer uma coisa por mim."),
                Go("What do you need?", "O que você precisa?", "ask", null, null, 0, (g, m) => g.QuestOf(Game.HeirQuest(m.Role)) == null),
                Go("How is it going?", "Como vai?", "progress", null, null, 0, (g, m) => g.QuestActive(Game.HeirQuest(m.Role)) && g.QuestOf(Game.HeirQuest(m.Role)).Step == 0),
                Go("It is done.", "Está feito.", "done", (g, m) => g.Flags.Add(Game.HeirReport(m.Role)), null, 0, (g, m) => g.QuestActive(Game.HeirQuest(m.Role)) && g.QuestOf(Game.HeirQuest(m.Role)).Step == 1),
                Go("Not now.", "Agora não.", Dialogue.Start))
            .Node("ask", (g, m) => HeirAsk(m.Role),
                Go("I will do it.", "Eu faço.", Dialogue.Start, (g, m) => g.StartQuest(Game.HeirQuest(m.Role))))
            .Node("progress", L("Not yet. Come back when it is done.", "Ainda não. Volte quando estiver pronto."),
                Go("Understood.", "Entendido.", Dialogue.Start))
            .Node("done", L("Good. Then the post is yours to answer for, and I will sign what the League needs.", "Ótimo. Então o cargo é seu, e eu assino o que a Liga precisar."),
                Go("Good.", "Ótimo.", Dialogue.Start));

        /// <summary>What the heir of each post asks for: the same quest the QuestBook holds under <see cref="Game.HeirQuest"/>.</summary>
        static string HeirAsk(TownRole role)
        {
            switch (role)
            {
                case TownRole.Elder: return L("The Guild's cellar is full of human zombies, and the League does not sign for a house that cannot keep its cellar. Put down three of them.", "O porão da Guilda está cheio de zumbis humanos, e a Liga não assina para uma casa que não guarda o próprio porão. Acabe com três deles.");
                case TownRole.Captain: return L("Orcs are working the Watch's road. Put down three, and the Watch will hear your name said well.", "Orcs andam pela estrada da Guarda. Acabe com três, e a Guarda vai ouvir seu nome dito com respeito.");
                case TownRole.Priest: return L("Skeletons climb out of the crypt at night. Put down three, and the Temple will sign for you.", "Esqueletos saem da cripta à noite. Acabe com três, e o Templo assina por você.");
                case TownRole.Beggar: return L("The Drowned need the vaults cleared of the dead that walk. Put down two, and the work is yours to hear.", "Os Afogados precisam dos cofres limpos dos mortos que andam. Acabe com dois, e o trabalho é seu para ouvir.");
                default: return L("Dark acolytes have been reading in the back room of the library. Put down two, and I will see your papers signed.", "Acólitos sombrios têm lido na sala dos fundos da biblioteca. Acabe com dois, e vejo seus papéis assinados.");
            }
        }

        // ------------------------------------------------------------------ the Reader (who explains the documents)

        static DChoice Read(string docName, string en, string pt) =>
            new DChoice
            {
                Label = L(en, pt), Goto = "read." + Game.DocByName(docName).Truth,
                Show = (g, m) => g.HasItem(docName),
                Do = (g, m) => { g.TakeItem(docName); g.Flags.Add(Game.DocByName(docName).Truth); m.Memory.Set(NpcMemory.Helped); m.Memory.Shift(15); g.RecordDeed(Deed.Helped, docName, 3); },
            };

        static readonly Dialogue Reader = new Dialogue()
            .Node(Dialogue.Start, (g, m) => g.Flags.Contains("truth.seal")
                    ? L("You have been reading the dead. So have I. Bring me what else you find.", "Você andou lendo os mortos. Eu também. Traga-me o que mais achar.")
                    : L("Another reader of the dead. Have you found anything written down there? Anything at all?", "Mais um leitor dos mortos. Você achou algo escrito lá embaixo? Qualquer coisa?"),
                Read(Game.DocLedger, "Show the warden's ledger page", "Mostrar a página do livro do carcereiro"),
                Read(Game.DocTally, "Show the dwarf council's tally", "Mostrar a contagem do conselho dos anões"),
                Read(Game.DocEntry, "Show the archivist's last entry", "Mostrar a última anotação do arquivista"),
                Read(Game.DocOrder, "Show the Spire's order roll", "Mostrar o rol de ordens da Torre"),
                Go("Ask about the old days", "Perguntar sobre os velhos tempos", "olddays"),
                Go("What do you study?", "O que você estuda?", "study"))
            // The chronicle, one entry at a time: the old days as the Scholar tells them.
            .Node("olddays", (g, m) => g.ScholarLine(), Go("Tell me more.", "Conte mais.", "olddays"), Go("Enough.", "Chega.", Dialogue.Start))
            .Node("study", L("What is left of the old archive. Everything down there was written down by someone who hoped to be read.", "O que sobrou do velho arquivo. Tudo lá embaixo foi escrito por alguém que esperava ser lido."),
                Go("I see.", "Entendo.", Dialogue.Start))
            .Node("read.truth.seal", L("A seal, not a jewel: whoever holds the Amulet opens every door of the old archive. The warden wrote this as the water rose. Now you know what you are fetching.", "Um selo, não uma joia: quem tem o Amuleto abre toda porta do velho arquivo. O carcereiro escreveu isto enquanto a água subia. Agora você sabe o que está buscando."),
                Go("Go on.", "Continue.", Dialogue.Start))
            .Node("read.truth.vote", L("The council voted four to three to drown the night shift. They called it the Sinking. It was a ballot. The Iron Holds will not thank you for this.", "O conselho votou quatro a três para afogar o turno da noite. Chamaram de Naufrágio. Foi uma votação. Os Holds de Ferro não vão lhe agradecer."),
                Go("Go on.", "Continue.", Dialogue.Start))
            .Node("read.truth.entry", L("Yendor's last entry: \"I take the seal down so that no king climbs out. If you read this, finish the job, or put it back where I stamped it.\"", "A última anotação de Yendor: \"Levo o selo para baixo para que nenhum rei suba. Se você lê isto, termine o trabalho, ou ponha de volta onde eu carimbei.\""),
                Go("Go on.", "Continue.", Dialogue.Start))
            .Node("read.truth.order", L("The Spire was raised to burn the dead, and the founders of the League and the Holds paid for it. They forgot. The Ossuary did not. Be careful who you tell.", "A Torre foi erguida para queimar os mortos, e os fundadores da Liga e dos Holds pagaram. Eles esqueceram. O Ossuary não. Cuidado com quem você conta."),
                Go("Go on.", "Continue.", Dialogue.Start));

        // ------------------------------------------------------------------ the ending (what to do with the Amulet)

        static DChoice End(string en, string pt, string id, Func<Game, Monster, bool> cond = null) =>
            Go(en, pt, null, (g, m) => g.FinishEnding(id), cond);

        public static readonly Dialogue Ending = new Dialogue()
            .Node(Dialogue.Start, (g, m) => L("The houses heard you come up. Each wants the Amulet for its own reasons. What will you do with it?", "As casas ouviram você subir. Cada uma quer o Amuleto por suas razões. O que você vai fazer com ele?"),
                End("Give it to the League and take what they can pay", "Entregar à Liga e aceitar o que podem pagar", "pay"),
                End("Have the Temple bury it, and shut the pit", "Deixar o Templo enterrá-lo, e fechar o poço", "shut",
                    (g, m) => g.RepOf(Houses.Temple) >= 25 && g.Flags.Contains("truth.seal") && g.Flags.Contains("truth.entry")),
                End("Let the Cult turn the seal, and open the archive", "Deixar o Culto girar o selo, e abrir o arquivo", "open",
                    (g, m) => g.RepOf(Houses.Cult) >= 25 && g.Flags.Contains("truth.seal")),
                End("Take the keys, and become the Warden", "Pegar as chaves, e virar o Carcereiro", "warden",
                    (g, m) => g.RepOf(Houses.Watch) >= 25 && g.Flags.Contains("truth.order")),
                End("Sell it to the highest bidder", "Vendê-lo ao maior lance", "auction", (g, m) => g.RepOf(Houses.Guild) >= 25),
                End("Carry it back down and stamp it as Yendor did", "Levá-lo de volta e carimbá-lo como Yendor fez", "stamp", (g, m) => g.AllTruths));

        // ------------------------------------------------------------------ the arrest

        public static readonly Dialogue Arrest = new Dialogue()
            .Node(Dialogue.Start, (g, m) => $"Halt! You are wanted: {g.BountyHere()} gold. Pay, serve, or be taken.",
                Go("Pay the fine", "Pagar a multa", "paid", (g, m) => { g.Player.Gold -= g.BountyHere(); g.ClearBountyHere(); },
                    (g, m) => g.Player.Gold >= g.BountyHere()),
                Go("Go quietly to the cells", "Ir em silêncio para as celas", "served", (g, m) => g.ServeSentence()),
                Go("Slip them a bribe", "Passar um suborno", "bribed",
                    (g, m) => { g.Player.Gold -= g.BountyHere() * 3 / 2; g.ClearBountyHere(); g.RecordDeed(Deed.Bribed, m.Name, 1); g.AddRep(Houses.Watch, -3, "bribing the Watch"); },
                    (g, m) => g.BountyHere() < 500 && g.Player.Gold >= g.BountyHere() * 3 / 2),
                Go("Pick the lock of the cell", "Forçar a fechadura da cela", "escaped", (g, m) => g.LockpickOut(),
                    (g, m) => g.Player.FindFirst("lock pick") != null),
                Go("Resist!", "Resistir!", null, (g, m) => g.ResistArrest()))
            .Node("escaped", L("\"The lock is old. You will not find another like it, and neither will we.\"", "\"A fechadura é velha. Você não vai achar outra igual, e nós também não.\""))
            .Node("paid", L("\"Then you are square with the Watch. Mind yourself.\"", "\"Então você está quite com a Guarda. Cuidado.\""))
            .Node("served", L("\"Your name is struck from the book. Do not make us write it again.\"", "\"Seu nome foi riscado do livro. Não nos faça escrevê-lo de novo.\""))
            .Node("bribed", L("\"I saw nothing. Nothing at all.\"", "\"Eu não vi nada. Nada mesmo.\""));

        // ------------------------------------------------------------------ the Elder (the League's council)

        // Once the Stamp is done the seal is closed for good (a new cycle has no Amulet to fetch), so the council does not offer it again.
        static readonly Dialogue Elder = new Dialogue { OnOpen = (g, m) => { if (g.EndingId == null) g.StartQuest("main.seal"); } }
            .Node(Dialogue.Start, (g, m) => g.Flags.Contains("elder.warned")
                    ? L("Back from the dark, or only from the street? Either way, ask.", "Voltou do escuro, ou só da rua? De qualquer jeito, pergunte.")
                    : L("So you are the one who took the League's thirty gold. Sit a moment. There are things worth knowing before you go down.", "Então você é quem aceitou os trinta de ouro da Liga. Sente um momento. Há coisas que vale saber antes de descer."),
                Go("What is the Amulet?", "O que é o Amuleto?", "amulet", (g, m) => g.Flags.Add("elder.warned")),
                Go("Will the League really pay five thousand?", "A Liga vai mesmo pagar cinco mil?", "pay", (g, m) => g.Flags.Add("elder.warned")),
                Go("Is there work for me?", "Tem trabalho para mim?", "work"),
                Go("Ask the council for a favour", "Pedir um favor ao conselho", "favour",
                    (g, m) => { g.Player.Gold += 40; g.Flags.Add("elder.favour"); m.Memory.Set(NpcMemory.Helped); g.RecordDeed(Deed.Helped, m.Name, 1); },
                    (g, m) => g.RepOf(Houses.Guild) >= 25 && !g.Flags.Contains("elder.favour")))
            .Node("amulet", L("Not a jewel. A seal: whoever holds it opens every door of the old archive. That is why the dead know it on sight, and the living want it.", "Não é joia. É um selo: quem o tem abre toda porta do velho arquivo. Por isso os mortos o reconhecem de longe, e os vivos o querem."),
                Go("I see.", "Entendo.", Dialogue.Start))
            .Node("pay", L("The League has never held that much gold. Take the thirty and be glad. If you climb out with the Amulet, we will think of something.", "A Liga nunca teve tanto ouro. Pegue os trinta e agradeça. Se você sair com o Amuleto, a gente pensa em algo."),
                Go("I see.", "Entendo.", Dialogue.Start))
            .Node("work", L("The board in the Guild hall pays by the head, and the Watch posts its own. Take what you can finish.", "O quadro da Guilda paga por cabeça, e a Guarda publica os seus. Pegue o que você puder terminar."),
                Go("Thank you.", "Obrigado.", Dialogue.Start))
            .Node("favour", L("The council remembers who pays its debts. Forty gold from the common purse. Do not make me regret it.", "O conselho lembra de quem paga suas dívidas. Quarenta de ouro da bolsa comum. Não me faça me arrepender."));

        // ------------------------------------------------------------------ the Captain of the Watch

        static readonly Dialogue Captain = new Dialogue()
            .Node(Dialogue.Start, (g, m) => g.RepOf(Houses.Watch) >= 25
                    ? L("Hold your head up. The Watch has heard good things of you.", "Erga a cabeça. A Guarda ouviu coisas boas sobre você.")
                    : g.RepOf(Houses.Watch) <= -25
                        ? L("I know your face. Say your piece and keep your hands where I can see them.", "Conheço seu rosto. Diga o que veio dizer e mantenha as mãos onde eu possa ver.")
                        : L("State your business.", "Diga o que veio fazer."),
                Go("Any trouble on the roads?", "Algum problema nas estradas?", "roads"),
                Go("What happens to those you arrest?", "O que acontece com quem vocês prendem?", "cells"),
                Go("Could the Watch use me?", "A Guarda precisaria de mim?", "job", (g, m) => g.StartQuest("watch.bandits"),
                    (g, m) => g.RepOf(Houses.Watch) >= 0 && !g.QuestActive("watch.bandits") && !g.QuestDone("watch.bandits")),
                Go("I want to clear my name.", "Quero limpar meu nome.", "clear", null, (g, m) => g.BountyHere() > 0),
                Go("I heard about the robbery.", "Soube do roubo.", "thief", (g, m) => g.StartQuest(g.TheftQuest()),
                    (g, m) => g.TownEventToday() == TownEventKind.Theft && !g.QuestActive(g.TheftQuest().Id) && !g.QuestDone(g.TheftQuest().Id)),
                Go("The thief is dealt with.", "O ladrão foi resolvido.", "thiefdone", (g, m) => g.Flags.Add(g.TheftQuest().Id + ".report"),
                    (g, m) => g.QuestActive(g.TheftQuest().Id) && g.QuestOf(g.TheftQuest().Id).Step == 1),
                Go("The toll bandits are dealt with.", "Os bandidos do pedágio foram resolvidos.", "reported", (g, m) => g.Flags.Add("watch.bandits.report"),
                    (g, m) => g.QuestActive("watch.bandits") && g.QuestOf("watch.bandits").Step == 1))
            .Node("thief", L("Somebody fenced the loot to the kobolds. Hunt two of them, and the Watch will not forget it. Be quick: the trail goes cold.", "Alguém passou o saque aos kobolds. Cace dois deles, e a Guarda não esquece. Seja rápido: a trilha esfria."),
                Go("I will find them.", "Vou achá-los.", Dialogue.Start))
            .Node("thiefdone", L("Good. The town sleeps easier tonight, and so do I.", "Bom. A cidade dorme mais tranquila esta noite, e eu também."),
                Go("Good.", "Ótimo.", Dialogue.Start))
            .Node("clear", (g, m) => $"The Watch holds {g.BountyHere()} gold against you here. There are ways to settle it.",
                Go("Pay the fine", "Pagar a multa", "cleared", (g, m) => { g.Player.Gold -= g.BountyHere(); g.ClearBountyHere(); }, (g, m) => g.Player.Gold >= g.BountyHere()),
                Go("Ask the Guild to vouch for me", "Pedir à Guilda que me recomende", "cleared",
                    (g, m) => { g.ClearBountyHere(); g.AddRep(Houses.Guild, -10, "calling in a favour"); }, (g, m) => g.RepOf(Houses.Guild) >= 25 && g.BountyHere() <= 300),
                Go("Ask the Temple to vouch for me", "Pedir ao Templo que me recomende", "cleared",
                    (g, m) => { g.ClearBountyHere(); g.AddRep(Houses.Temple, -10, "calling in a favour"); }, (g, m) => g.RepOf(Houses.Temple) >= 25 && g.BountyHere() <= 300),
                Go("Never mind.", "Deixa pra lá.", Dialogue.Start))
            .Node("cleared", L("\"Then it is settled. Keep out of trouble.\"", "\"Então está resolvido. Fique longe de confusão.\""),
                Go("Understood.", "Entendido.", Dialogue.Start))
            .Node("reported", L("Good work. The Watch pays its debts, and remembers who it owes.", "Bom trabalho. A Guarda paga o que deve, e lembra a quem deve."),
                Go("Understood.", "Entendido.", Dialogue.Start))
            .Node("roads", L("Tolls, mostly. Men with a rope and a nice coat. Pay, or draw. The Watch pays for the second, quietly.", "Pedágios, na maior parte. Homens com uma corda e um casaco bom. Pague, ou saque. A Guarda paga pelo segundo, em silêncio."),
                Go("Understood.", "Entendido.", Dialogue.Start))
            .Node("cells", L("Fines for the small crimes, cells for the rest. The wall has room, and the dead below have more.", "Multas para os crimes pequenos, celas para o resto. O muro tem espaço, e os mortos lá embaixo têm mais."),
                Go("Understood.", "Entendido.", Dialogue.Start))
            .Node("job", L("Bandits at the tolls. Put them down on the road and the Watch will remember it. That is all I have today.", "Bandidos nos pedágios. Acabe com eles na estrada e a Guarda vai lembrar. É tudo que tenho hoje."),
                Go("Understood.", "Entendido.", Dialogue.Start));

        // ------------------------------------------------------------------ the Priest

        static readonly Dialogue Priest = new Dialogue()
            .Node(Dialogue.Start, (g, m) => m.Memory.Has("prayed")
                    ? L("The Temple has noted your prayer. What else troubles you?", "O Templo anotou sua prece. O que mais o aflige?")
                    : L("Mercy is free here. The healing is not. What troubles you?", "Aqui a clemência é de graça. A cura, não. O que o aflige?"),
                Go("What lies beneath the town?", "O que há embaixo da cidade?", "dead"),
                Go("I feel the Ossuary in me.", "Sinto o Ossuário em mim.", "tainted", null, (g, m) => g.Player.Corruption > 0),
                Go("Pray with me.", "Reze comigo.", "prayer",
                    (g, m) => { m.Memory.Set("prayed"); m.Memory.Shift(5); g.AddRep(Houses.Temple, 2, null); },
                    (g, m) => !m.Memory.Has("prayed")),
                Go("Does the Temple need anything?", "O Templo precisa de algo?", "rest", (g, m) => g.StartQuest("temple.rest"),
                    (g, m) => g.RepOf(Houses.Temple) > -25 && !g.QuestActive("temple.rest") && !g.QuestDone("temple.rest")),
                Go("The restless dead are at peace.", "Os mortos inquietos descansam.", "rested", (g, m) => g.Flags.Add("temple.rest.report"),
                    (g, m) => g.QuestActive("temple.rest") && g.QuestOf("temple.rest").Step == 1))
            .Node("rest", L("Skeletons that will not lie down, in any branch. Destroy four, and the Temple will remember it.", "Esqueletos que não ficam deitados, em qualquer ramo. Destrua quatro, e o Templo vai lembrar."),
                Go("I will see to it.", "Vou cuidar disso.", Dialogue.Start))
            .Node("rested", L("Then the dead sleep a little deeper tonight. The Temple is in your debt.", "Então os mortos dormem um pouco mais fundo esta noite. O Templo está em dívida com você."),
                Go("Good.", "Ótimo.", Dialogue.Start))
            .Node("dead", L("The dead of every age. Most sleep. The ones who do not are why we keep the lamps lit.", "Os mortos de todas as eras. A maioria dorme. Os que não dormem são o motivo de mantermos as lamparinas acesas."),
                Go("I see.", "Entendo.", Dialogue.Start))
            .Node("tainted", L("Then you have touched what should not be touched. The purge costs, and it is worth it. Do not wait until the stain is deeper.", "Então você tocou no que não devia ser tocado. A purificação custa, e vale a pena. Não espere a mancha ficar mais funda."),
                Go("I will think on it.", "Vou pensar.", Dialogue.Start))
            .Node("prayer", L("Kneel. A short word, a long silence. The Temple notes it, and so do I.", "Ajoelhe. Uma palavra curta, um silêncio longo. O Templo anota, e eu também."),
                Go("Thank you.", "Obrigado.", Dialogue.Start));

        // ------------------------------------------------------------------ the Innkeeper

        static readonly Dialogue Innkeeper = new Dialogue()
            .Node(Dialogue.Start, L("What can I get you? Ale, a bed, or news.", "O que vai ser? Cerveja, uma cama, ou notícias."),
                Go("Any news?", "Alguma notícia?", "news"),
                Go("Who has passed through lately?", "Quem passou por aqui ultimamente?", "travellers"),
                Go("Buy a round for the house", "Pagar uma rodada para a casa", "round",
                    (g, m) => { g.AddRep(Houses.Guild, 1, null); m.Memory.Set(NpcMemory.Helped); m.Memory.Shift(10); }, null, 10))
            .Node("news", (g, m) => g.HearRumour(), Go("Anything else?", "Mais alguma coisa?", Dialogue.Start))
            .Node("travellers", (g, m) => g.RivalReport(),
                Go("They are ahead of me? I will race them.", "Estão na minha frente? Vou apostar corrida.", "race", (g, m) => g.StartQuest(g.RivalQuest()),
                    (g, m) => !g.QuestActive("rival.race") && !g.QuestDone("rival.race") && g.QuestOf("rival.race") == null && Game.RivalDepthOn(g.World.Day) < Game.RivalRaceDepth),
                Go("Thanks.", "Obrigado.", Dialogue.Start))
            .Node("race", L("Ha! Then run, hero. The tavern is betting on the other lot, but I never bet.", "Ha! Então corra, herói. A taverna está apostando no outro grupo, mas eu nunca aposto."),
                Go("Good.", "Ótimo.", Dialogue.Start))
            .Node("round", L("A round! The room will remember who paid for it.", "Uma rodada! A casa vai lembrar de quem pagou."),
                Go("Good.", "Ótimo.", Dialogue.Start));

        // ------------------------------------------------------------------ the Bard

        static readonly Dialogue Bard = new Dialogue()
            .Node(Dialogue.Start, L("A song, traveller? They all end badly, but the tune is good.", "Uma canção, viajante? Todas terminam mal, mas a melodia é boa."),
                Go("Sing of those who went before.", "Cante os que vieram antes.", "song"),
                Go("Tip the bard", "Dar uma gorjeta ao bardo", "tip",
                    (g, m) => { m.Memory.Set(NpcMemory.Helped); m.Memory.Shift(20); }, (g, m) => !m.Memory.Has(NpcMemory.Helped), 5))
            .Node("song", (g, m) => g.SongLine(),
                Go("Another?", "Outra?", Dialogue.Start))
            .Node("tip", L("Your name... yes. I will keep it. For a while.", "Seu nome... sim. Vou guardá-lo. Por um tempo."),
                Go("Good.", "Ótimo.", Dialogue.Start));
    }
}
