using System;
using System.Collections.Generic;

namespace Ossuary.Core
{
    /// <summary>
    /// Every quest of the game, as C# data with the PT beside the EN. The Main track grows act by act
    /// (docs/game/main-quest.md); here is where each new chain is registered.
    /// </summary>
    public static class QuestBook
    {
        static string L(string en, string pt) => TownText.L(en, pt);

        public static readonly Dictionary<string, QuestDef> All = Build();

        static Dictionary<string, QuestDef> Build()
        {
            var d = new Dictionary<string, QuestDef>();
            void Add(QuestDef q) => d[q.Id] = q;

            Add(new QuestDef
            {
                Id = "main.seal", Track = QuestDef.Main, Giver = "the Elder",
                Title = L("The Seal", "O Selo"),
                OnComplete = g => g.Say("The Elder was right: the Amulet is a seal, and you carry it.", MessageKind.Quest),
            }
            .Step(ObjKind.Flag, L("Ask the Elder what the Amulet is.", "Pergunte ao Ancião o que é o Amuleto."), "elder.warned", 1, null,
                  L("The Elder, in the Guild hall.", "O Ancião, no salão da Guilda."))
            .Step(ObjKind.Item, L("Find the warden's ledger page in The Dungeons.", "Encontre a página do livro do carcereiro nas Masmorras."), Game.DocLedger, 1, "The Dungeons",
                  L("The Dungeons, around level 5.", "As Masmorras, por volta do nível 5."))
            .Step(ObjKind.Flag, L("Have the Reader read the page.", "Peça ao Leitor que leia a página."), "truth.seal", 1, null,
                  L("The Reader (a scholar), in a library or a tower.", "O Leitor (um estudioso), numa biblioteca ou torre."))
            .Step(ObjKind.Flag, L("Learn what the deeper branches know.", "Descubra o que os ramos mais fundos sabem."), "truth.all", 1, null,
                  L("Dwarfdeep (6), the Sunken Vaults (8), the Ashen Spire (12): bring what you find to the Reader.", "Dwarfdeep (6), Cofres Afundados (8), Torre de Cinza (12): leve o que achar ao Leitor."))
            .Step(ObjKind.Item, L("Take the Amulet of Yendor from the bottom of The Dungeons.", "Pegue o Amuleto de Yendor no fundo das Masmorras."), Game.QuestAmuletName, 1, "The Dungeons",
                  L("The deepest level of The Dungeons. Then decide what it is for.", "O nível mais fundo das Masmorras. Depois decida para que serve.")));

            Add(new QuestDef
            {
                Id = "watch.bandits", Track = QuestDef.Watch, Giver = "the Captain of the Watch", Deadline = 14, RewardGold = 60,
                Title = L("Toll bandits", "Bandidos do pedágio"),
                OnComplete = g => g.AddRep(Houses.Watch, 10, "putting down the toll bandits"),
                OnFail = g => g.AddRep(Houses.Watch, -5, "leaving the toll bandits to the roads"),
            }
            .Step(ObjKind.Kill, L("Put down three orcs for the Watch.", "Acabe com três orcs para a Guarda."), "orc", 3, null,
                  L("Anywhere in the Ossuary; report to the Captain.", "Em qualquer lugar do Ossuary; avise o Capitão."))
            .Step(ObjKind.Flag, L("Report to the Captain of the Watch.", "Avise o Capitão da Guarda."), "watch.bandits.report", 1, null,
                  L("The Captain, in the Watch house.", "O Capitão, no posto da Guarda.")));

            Add(new QuestDef
            {
                Id = "temple.rest", Track = QuestDef.Temple, Giver = "the High Priest", RewardGold = 50,
                Title = L("Let the dead rest", "Deixe os mortos descansarem"),
                OnComplete = g => g.AddRep(Houses.Temple, 10, "laying the restless to rest"),
            }
            .Step(ObjKind.Kill, L("Destroy four skeletons that will not lie down.", "Destrua quatro esqueletos que não ficam deitados."), "skeleton", 4, null,
                  L("Anywhere in the Ossuary; report to the High Priest.", "Em qualquer lugar do Ossuary; avise o Sumo Sacerdote."))
            .Step(ObjKind.Flag, L("Tell the High Priest it is done.", "Diga ao Sumo Sacerdote que está feito."), "temple.rest.report", 1, null,
                  L("The High Priest, in the Temple.", "O Sumo Sacerdote, no Templo.")));

            Add(new QuestDef
            {
                Id = "cult.vial", Track = QuestDef.Cult, Giver = "the Drowned", RewardGold = 120,
                Title = L("A vial for the Drowned", "Um frasco para os Afogados"),
                OnComplete = g => { g.AddRep(Houses.Cult, 10, "carrying the Drowned's vial"); g.AddRep(Houses.Temple, -8, null); g.AddRep(Houses.Watch, -4, null); g.AddCorruption(10, "The vial's black glass is warm in your hand."); },
            }
            .Step(ObjKind.Item, L("Get hold of a potion of mutation.", "Consiga uma poção de mutação."), "potion of mutation", 1, null,
                  L("The black cart on the roads sells it to the trusted.", "A carroça negra das estradas vende aos de confiança."))
            .Step(ObjKind.Flag, L("Bring it to the one who asked.", "Leve a quem pediu."), "cult.vial.report", 1, null,
                  L("The beggar by the square, who is not a beggar.", "O mendigo da praça, que não é mendigo.")));

            // The Drowned's two errands, after the vial: each is offered once (docs/game/guild-jobs.md, D11).
            Add(new QuestDef
            {
                Id = "cult.zombies", Track = QuestDef.Cult, Giver = "the Drowned", RewardGold = 110,
                Title = L("Clear the vaults for the Drowned", "Limpe os cofres para os Afogados"),
                OnComplete = g => { g.AddRep(Houses.Cult, 8, "clearing the vaults"); g.AddRep(Houses.Temple, -4, null); },
            }
            .Step(ObjKind.Kill, L("Destroy three human zombies in The Sunken Vaults.", "Destrua três zumbis humanos nos Cofres Afundados."), "human zombie", 3, "The Sunken Vaults",
                  L("Only the vaults count. Report to the one who asked.", "Só os cofres contam. Avise a quem pediu."))
            .Step(ObjKind.Flag, L("Tell the one who asked that it is done.", "Diga a quem pediu que está feito."), "cult.zombies.report", 1, null,
                  L("The beggar by the square, who is not a beggar.", "O mendigo da praça, que não é mendigo.")));

            Add(new QuestDef
            {
                Id = "cult.bones", Track = QuestDef.Cult, Giver = "the Drowned", RewardGold = 150,
                Title = L("A blade of bone for the Drowned", "Uma lâmina de osso para os Afogados"),
                OnComplete = g => { g.AddRep(Houses.Cult, 8, "bringing a blade of bone to the Drowned"); g.AddRep(Houses.Temple, -4, null); },
            }
            .Step(ObjKind.Item, L("Get hold of a bone blade.", "Consiga uma lâmina de osso."), "bone blade", 1, null,
                  L("A smith can forge one from a blade and remains.", "Um ferreiro pode forjar uma a partir de uma lâmina e restos."))
            .Step(ObjKind.Flag, L("Give the blade to the one who asked.", "Dê a lâmina a quem pediu."), "cult.bones.report", 1, null,
                  L("The beggar by the square, who is not a beggar.", "O mendigo da praça, que não é mendigo.")));

            // Two more of the Drowned's errands: the acolytes below the Dungeons, and a shrine at the bottom of the vaults.
            Add(new QuestDef
            {
                Id = "cult.names", Track = QuestDef.Cult, Giver = "the Drowned", RewardGold = 100,
                Title = L("Silence the acolytes for the Drowned", "Cale os acólitos para os Afogados"),
                OnComplete = g => { g.AddRep(Houses.Cult, 8, "silencing the acolytes"); g.AddRep(Houses.Temple, -4, null); },
            }
            .Step(ObjKind.Kill, L("Destroy three dark acolytes in The Dungeons.", "Destrua três acólitos sombrios nas Masmorras."), "dark acolyte", 3, "The Dungeons",
                  L("They chant under the shrines of the Dungeons.", "Eles cantam sob os altares das Masmorras."))
            .Step(ObjKind.Flag, L("Tell the one who asked that it is done.", "Diga a quem pediu que está feito."), "cult.names.report", 1, null,
                  L("The beggar by the square, who is not a beggar.", "O mendigo da praça, que não é mendigo.")));

            Add(new QuestDef
            {
                Id = "cult.shrine", Track = QuestDef.Cult, Giver = "the Drowned", RewardGold = 130,
                Title = L("Light the shrine for the Drowned", "Acenda o altar para os Afogados"),
                OnComplete = g => { g.AddRep(Houses.Cult, 8, "lighting the shrine of the Drowned"); g.AddRep(Houses.Temple, -4, null); g.AddCorruption(3, "The shrine's light is cold, and it gets under the skin."); },
            }
            .Step(ObjKind.Reach, L("Go down the Sunken Vaults to the sixth level.", "Desça aos Cofres Afundados até o sexto nível."), "The Sunken Vaults", 6, "The Sunken Vaults",
                  L("The shrine is at the bottom, under the water.", "O altar fica no fundo, sob a água."))
            .Step(ObjKind.Flag, L("Tell the one who asked that the shrine is lit.", "Diga a quem pediu que o altar está aceso."), "cult.shrine.report", 1, null,
                  L("The beggar by the square, who is not a beggar.", "O mendigo da praça, que não é mendigo.")));

            // The favours of the heirs of the essential posts: one before the post's business goes on (main-quest.md, Essential NPCs).
            Add(Favour(TownRole.Elder, "the new guildmaster", L("A favour for the new guildmaster", "Um favor para o novo mestre da guilda"),
                L("Put down three human zombies that wander the Guild's cellar.", "Acabe com três zumbis humanos que vagam pelo porão da Guilda."), "human zombie", 3,
                L("They walk the cellar stairs at night.", "Andam pela escada do porão à noite.")));
            Add(Favour(TownRole.Captain, "the new captain of the watch", L("A favour for the new captain", "Um favor para o novo capitão da guarda"),
                L("Put down three orcs on the Watch's road.", "Acabe com três orcs na estrada da Guarda."), "orc", 3,
                L("They come in from the hills by the gate.", "Eles vêm das colinas, pelo portão.")));
            Add(Favour(TownRole.Priest, "the new priest", L("A favour for the new priest", "Um favor para o novo sacerdote"),
                L("Put down three skeletons out of the Temple's crypt.", "Acabe com três esqueletos da cripta do Templo."), "skeleton", 3,
                L("The crypt door sticks, and they come up the stair.", "A porta da cripta emperra, e eles sobem pela escada.")));
            Add(Favour(TownRole.Scholar, "the new scholar of the library", L("A favour for the new scholar", "Um favor para o novo erudito"),
                L("Put down two dark acolytes in the library's back room.", "Acabe com dois acólitos sombrios na sala dos fundos da biblioteca."), "dark acolyte", 2,
                L("They whisper over the old books.", "Eles sussurram sobre os livros antigos.")));
            Add(Favour(TownRole.Beggar, "the new voice of the Drowned", L("A favour for the Drowned", "Um favor para os Afogados"),
                L("Put down two of the dead that walk in the Sunken Vaults.", "Acabe com dois mortos que andam nos Cofres Afundados."), "human zombie", 2,
                L("They walk where the water came in.", "Andam onde a água entrou.")));

            return d;
        }

        /// <summary>
        /// The favour an heir asks for before the post's business goes on: put down a few of a kind, then report back to the new
        /// holder of the post. The post's business reopens when the report is in (<see cref="Game.SettleHeir"/>).
        /// </summary>
        static QuestDef Favour(TownRole role, string giver, string title, string kill, string target, int count, string killHint) =>
            new QuestDef { Id = Game.HeirQuest(role), Track = QuestDef.Main, Giver = giver, Title = title, OnComplete = g => g.SettleHeir(role) }
                .Step(ObjKind.Kill, kill, target, count, null, killHint)
                .Step(ObjKind.Flag, L("Report back to the new holder of the post.", "Avise o novo ocupante do cargo."), Game.HeirReport(role), 1, null,
                      L("Talk to them at their post.", "Fale com eles em seu posto."));
    }
}
