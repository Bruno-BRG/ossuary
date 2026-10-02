using System.Collections.Generic;
using System.Text.RegularExpressions;
using Ossuary.Core.Entities;

namespace Ossuary.Core
{
    /// <summary>
    /// Everything people say and every name in town, as data. English is the source text and the key;
    /// <see cref="L"/> registers the Portuguese beside it, so a line and its translation are written once,
    /// together. <see cref="Loc"/> consults <see cref="Pt"/> and <see cref="Rx"/>.
    /// </summary>
    public static class TownText
    {
        // Must stay the first static: every L(...) below writes into it.
        public static readonly Dictionary<string, string> Pt = new Dictionary<string, string>();
        public static readonly List<(Regex, string)> Rx = new List<(Regex, string)>();

        static string L(string en, string pt) { Pt[en] = pt; return en; }
        static void R(string pattern, string replacement) => Rx.Add((new Regex("^" + pattern + "$", RegexOptions.CultureInvariant), replacement));

        // ---------------------------------------------------------------- names

        static readonly string[] Given =
        {
            "Marta", "Odo", "Hilde", "Brann", "Wren", "Tobias", "Isolde", "Garrick", "Mirela", "Joss", "Pell", "Osric",
            "Dagny", "Fenn", "Bertram", "Ilsa", "Corwin", "Nessa", "Thom", "Yara", "Edda", "Rurik", "Selka", "Alder",
            "Maude", "Kestrel", "Bram", "Lissa", "Hob", "Verna", "Cedric", "Tilda", "Gunnar", "Rosamund", "Piet", "Sunniva",
        };

        public static string GivenName(Rng rng) => Given[rng.Range(0, Given.Length)];

        static readonly Dictionary<BuildingKind, string[]> Names = new Dictionary<BuildingKind, string[]>
        {
            [BuildingKind.Smithy] = new[] { L("The Rusty Anvil", "A Bigorna Enferrujada"), L("Cinder & Steel", "Brasa & Aço"), L("The Black Hammer", "O Martelo Negro"), L("Ironmouth Forge", "Forja Boca-de-Ferro") },
            [BuildingKind.Armoury] = new[] { L("The Dented Shield", "O Escudo Amassado"), L("Mail & Mettle", "Malha & Brio"), L("The Iron Hide", "A Pele de Ferro") },
            [BuildingKind.Alchemist] = new[] { L("The Bubbling Flask", "O Frasco Borbulhante"), L("Sage & Sulphur", "Sálvia & Enxofre"), L("The Green Vial", "O Frasco Verde") },
            [BuildingKind.Emporium] = new[] { L("The Lantern Spire", "A Torre da Lanterna"), L("The Violet Tower", "A Torre Violeta"), L("Quillwind's Tower", "A Torre de Quillwind") },
            [BuildingKind.General] = new[] { L("The Hodgepodge", "O Bazar"), L("Odds & Ends", "Cacarecos & Cia"), L("The Mended Sack", "O Saco Remendado") },
            [BuildingKind.Tavern] = new[] { L("The Gilded Flagon", "O Cálice Dourado"), L("The Drowned Rat", "O Rato Afogado"), L("The Last Lantern", "A Última Lanterna"), L("The Bone & Barrel", "O Osso e o Barril") },
            [BuildingKind.Inn] = new[] { L("The Weary Pilgrim", "O Peregrino Cansado"), L("The Bed & Bell", "A Cama e o Sino"), L("The Sleeping Owl", "A Coruja Dorminhoca") },
            [BuildingKind.Temple] = new[] { L("Temple of the Pale Lamp", "Templo da Lâmpada Pálida"), L("Shrine of Mercy", "Santuário da Clemência"), L("The House of Ashes", "A Casa das Cinzas") },
            [BuildingKind.Guild] = new[] { L("The Reach League Hall", "O Salão da Liga do Reach"), L("The Delvers' Hall", "O Salão dos Escavadores"), L("The Bounty Hall", "O Salão das Recompensas") },
            [BuildingKind.Library] = new[] { L("The Quiet Archive", "O Arquivo Silencioso"), L("The Inkwell", "O Tinteiro"), L("The Dusty Folio", "O Fólio Empoeirado") },
            [BuildingKind.Barracks] = new[] { L("The Watch Barracks", "O Quartel da Guarda"), L("The Gatehouse Watch", "A Guarda do Portão") },
            [BuildingKind.Townhouse] = new[] { L("A townhouse", "Um sobrado") },
            [BuildingKind.Cottage] = new[] { L("A cottage", "Uma casa") },
            [BuildingKind.Watchtower] = new[] { L("The Watchtower", "A Torre de Vigia") },
        };

        public static string BuildingName(Rng rng, BuildingKind kind)
        {
            if (!Names.TryGetValue(kind, out var pool)) return kind.ToString();
            return pool[rng.Range(0, pool.Length)];
        }

        static readonly string[] FoodStalls = { L("The Bread Stall", "A Barraca de Pão"), L("Fresh Provisions", "Provisões Frescas") };
        static readonly string[] JewelStalls = { L("Trinkets & Rings", "Bugigangas & Anéis"), L("The Magpie's Tray", "A Bandeja da Pega") };
        static readonly string[] GoodsStalls = { L("The Peddler's Cart", "A Carroça do Mascate"), L("Wayside Wares", "Mercadorias de Estrada") };

        public static string StallName(Rng rng, ShopKind kind)
        {
            var pool = kind == ShopKind.Food ? FoodStalls : kind == ShopKind.Jewel ? JewelStalls : GoodsStalls;
            return pool[rng.Range(0, pool.Length)];
        }

        public static string SizeBlurb(string size)
        {
            switch (size)
            {
                case "hamlet": return L("A walled hamlet: a few roofs, a forge, a tavern and a shrine.", "Um vilarejo murado: poucos telhados, uma forja, uma taverna e um santuário.");
                case "village": return L("A walled village. Some of it is built upward, and some of it down: look for stairs.", "Uma vila murada. Parte dela é construída para cima, parte para baixo: procure escadas.");
                case "town": return L("A busy walled town of towers and cellars. Climb the stairs, descend the cellars.", "Uma cidade murada e movimentada, de torres e porões. Suba as escadas, desça aos porões.");
                default: return L("A walled city stacked on itself: towers, lofts, crypts and cellars. Use < and > on the stairs.", "Uma cidade murada empilhada sobre si mesma: torres, sótãos, criptas e porões. Use < e > nas escadas.");
            }
        }

        // ---------------------------------------------------------------- roles

        public static string RoleTitle(TownRole r)
        {
            switch (r)
            {
                case TownRole.Child: return L("child", "criança");
                case TownRole.Guard: return L("town guard", "guarda da cidade");
                case TownRole.Captain: return L("captain of the watch", "capitão da guarda");
                case TownRole.Shopkeeper: return L("shopkeeper", "lojista");
                case TownRole.Smith: return L("smith", "ferreiro");
                case TownRole.Innkeeper: return L("innkeeper", "estalajadeiro");
                case TownRole.Barkeep: return L("barkeep", "taverneiro");
                case TownRole.Priest: return L("priest", "sacerdote");
                case TownRole.Elder: return L("guildmaster", "mestre da guilda");
                case TownRole.Scholar: return L("scholar", "erudito");
                case TownRole.Bard: return L("bard", "bardo");
                case TownRole.Drunk: return L("drunkard", "bêbado");
                case TownRole.Adventurer: return L("adventurer", "aventureiro");
                case TownRole.Beggar: return L("beggar", "mendigo");
                case TownRole.Prisoner: return L("prisoner", "prisioneiro");
                case TownRole.Pet: return L("animal", "animal");
                default: return L("townsperson", "morador");
            }
        }

        static readonly Dictionary<TownRole, string[]> Lines = new Dictionary<TownRole, string[]>
        {
            [TownRole.Citizen] = new[]
            {
                L("Thirty gold a head. It is not enough, but it is what the Reach pays.", "Trinta de ouro por cabeça. Não é muito, mas é o que a Liga paga."),
                L("Nobody here sleeps well since the water rose in the Vaults.", "Ninguém aqui dorme bem desde que a água subiu nos Cofres."),
                L("My brother went down in the spring. The pit kept him.", "Meu irmão desceu na primavera. O poço ficou com ele."),
                L("Mind the road at night. The dead walk further than they used to.", "Cuidado com a estrada à noite. Os mortos andam mais longe que antes."),
                L("Buy lamp oil before you go. The dark down there is not ordinary dark.", "Compre óleo de lamparina antes de ir. O escuro lá embaixo não é escuro comum."),
                L("This town has stairs everywhere. My knees have opinions about that.", "Esta cidade tem escadas por todo lado. Meus joelhos têm opinião sobre isso."),
            },
            [TownRole.Child] = new[]
            {
                L("Are you going into the hole? Can I come?", "Você vai entrar no buraco? Posso ir?"),
                L("I found a bone. Look! It is a small one.", "Achei um osso. Olha! É um pequeno."),
                L("Mama says people who go down do not come back up.", "A mamãe diz que quem desce não volta."),
                L("The cellar under the tavern has a cat. I am not allowed to say how I know.", "O porão da taverna tem um gato. Não posso dizer como sei."),
            },
            [TownRole.Guard] = new[]
            {
                L("Keep your blade sheathed inside the walls, stranger.", "Mantenha a lâmina na bainha dentro dos muros, forasteiro."),
                L("The gates close at dusk. The road does not care about hours.", "Os portões fecham ao anoitecer. A estrada não liga para horas."),
                L("Rats, ghouls, orcs: the Ossuary sends us something new every season.", "Ratos, carniçais, orcs: o Ossuary nos manda algo novo a cada estação."),
                L("The tower gives a good view of the road. Not that there is much to be glad about.", "A torre dá uma boa vista da estrada. Não que haja muito do que se alegrar."),
            },
            [TownRole.Captain] = new[]
            {
                L("Every patrol finds something worse on the road. Start in the Verdant Reach.", "Cada patrulha encontra algo pior na estrada. Comece pelo Alcance Verdejante."),
                L("Danger grows toward the east. The Hollow Wastes are no place for a first trip.", "O perigo cresce para o leste. Os Ermos Ocos não são lugar para a primeira viagem."),
                L("Below us are cells. Above us are bunks. In between we keep the town honest.", "Abaixo de nós ficam as celas. Acima, os beliches. No meio, mantemos a cidade honesta."),
            },
            [TownRole.Shopkeeper] = new[]
            {
                L("Coin first, goods second. That is the whole of my philosophy.", "Primeiro a moeda, depois a mercadoria. Essa é toda a minha filosofia."),
                L("Everything is for sale. Including, on a slow day, my opinion.", "Tudo está à venda. Inclusive, em dia fraco, a minha opinião."),
            },
            [TownRole.Smith] = new[]
            {
                L("A good edge is worth more than a good story.", "Um bom fio vale mais que uma boa história."),
                L("I can hone a blade three times. After that the steel has nothing left to give.", "Posso afiar uma lâmina três vezes. Depois disso o aço não tem mais o que dar."),
            },
            [TownRole.Innkeeper] = new[]
            {
                L("The beds are clean. Mostly. The soup is hot, which is the part that matters.", "As camas são limpas. Quase. A sopa está quente, que é o que importa."),
                L("A night here mends what a night on the road never does.", "Uma noite aqui conserta o que uma noite na estrada nunca conserta."),
            },
            [TownRole.Barkeep] = new[]
            {
                L("What will it be? The ale is bad. The company is worse. Both are cheap.", "O que vai ser? A cerveja é ruim. A companhia é pior. As duas são baratas."),
                L("The cellar is bigger than the tavern. Do not ask what lives in the barrels.", "O porão é maior que a taverna. Não pergunte o que mora nos barris."),
            },
            [TownRole.Priest] = new[]
            {
                L("Mercy is free here. The healing is not.", "Aqui a clemência é de graça. A cura, não."),
                L("The dead beneath us are quiet. Keep it that way.", "Os mortos aqui embaixo estão quietos. Que continuem assim."),
                L("Every god keeps an altar somewhere in the dark. Pray at one and it will remember you.", "Cada deus mantém um altar no escuro. Reze em um e ele vai lembrar de você."),
            },
            [TownRole.Elder] = new[]
            {
                L("Yendor sealed the vaults with his own stamp. Seals break.", "Yendor selou os cofres com o próprio carimbo. Selos quebram."),
                L("Take the notices. Whatever you do, do not take them seriously.", "Pegue os avisos. Mas, aconteça o que acontecer, não os leve a sério."),
            },
            [TownRole.Scholar] = new[]
            {
                L("The Ossuary is not a place, it is a record. Every floor remembers something.", "O Ossuary não é um lugar, é um registro. Cada andar lembra de algo."),
                L("An unknown ring is an unsafe ring. I name them for a modest fee.", "Anel desconhecido é anel perigoso. Eu os identifico por uma taxa modesta."),
                L("Read slowly. Books punish the hasty and reward the dull.", "Leia devagar. Livros punem os apressados e premiam os sem graça."),
            },
            [TownRole.Bard] = new[]
            {
                L("I sing of the nine hundred and ninety-nine who went before you.", "Eu canto os novecentos e noventa e nove que vieram antes de você."),
                L("Would you like a ballad? It ends badly. They all end badly.", "Quer uma balada? Termina mal. Todas terminam mal."),
                L("Tip the bard and the bard will remember your name. Briefly.", "Dê uma gorjeta ao bardo e ele lembrará seu nome. Por pouco tempo."),
            },
            [TownRole.Drunk] = new[]
            {
                L("I went down once. I remember a door. Then I remember this stool.", "Eu desci uma vez. Lembro de uma porta. Depois lembro deste banco."),
                L("Hic... the Amulet? It is in the... in the lowest... what was I saying?", "Hic... o Amuleto? Fica no... no mais fundo... o que eu dizia?"),
                L("Never trust a staircase that goes the wrong way. Hic.", "Nunca confie numa escada que vai para o lado errado. Hic."),
            },
            [TownRole.Adventurer] = new[]
            {
                L("Shields first. Always shields first.", "Escudo primeiro. Sempre escudo primeiro."),
                L("Do not fight on the stairs. Fight where you can still retreat.", "Não lute na escada. Lute onde ainda possa recuar."),
                L("I took thirty gold for a head and lost two fingers. Do the maths.", "Peguei trinta de ouro por uma cabeça e perdi dois dedos. Faça as contas."),
                L("Search the walls. The old builders loved a secret door.", "Procure nas paredes. Os antigos construtores adoravam portas secretas."),
                L("Eat before you are hungry. The starving die slowly, but they do die.", "Coma antes de sentir fome. Quem morre de fome morre devagar, mas morre."),
            },
            [TownRole.Beggar] = new[]
            {
                L("A coin for the one who stayed up top?", "Uma moeda para quem ficou aqui em cima?"),
                L("I was a delver once. Now I delve into pockets. Kindly.", "Fui escavador. Agora escavo bolsos. Com educação."),
            },
            [TownRole.Prisoner] = new[]
            {
                L("I stole bread. They say I stole worse. Do not believe them.", "Roubei pão. Dizem que roubei coisa pior. Não acredite neles."),
                L("The walls down here sweat. Some nights they whisper.", "As paredes aqui embaixo suam. Em algumas noites, sussurram."),
            },
        };

        public static string LineFor(Monster m, int n)
        {
            if (m.Role == TownRole.Pet)
                return m.Name == "cat" ? L("The cat ignores you, magnificently.", "O gato ignora você, magnificamente.") : L("The dog wags its tail and leans against your leg.", "O cão balança o rabo e se encosta na sua perna.");
            var pool = Lines.TryGetValue(m.Role, out var p) ? p : Lines[TownRole.Citizen];
            return pool[(m.Voice + n) % pool.Length];
        }

        // ------------------------------------------------------------- they remember you

        static readonly string RGuardFriend = L("Well met. The Watch has not forgotten what you did on the roads.", "Bem-vindo. A Guarda não esqueceu o que você fez nas estradas.");
        static readonly string RGuardFoe = L("I know your face. Keep your hands where I can see them.", "Conheço seu rosto. Mantenha as mãos onde eu possa ver.");
        static readonly string RPriestFriend = L("The Temple is glad of you. Few come back from the dark with their hands clean.", "O Templo se alegra com você. Poucos voltam do escuro com as mãos limpas.");
        static readonly string RPriestFoe = L("You have robbed the dead, and the dead have noticed. Do not bring that in here.", "Você roubou os mortos, e os mortos notaram. Não traga isso aqui.");
        static readonly string RPriestTainted = L("Child... what has the Ossuary done to you? Let me look at your hands.", "Criança... o que o Ossuário fez com você? Deixe-me ver suas mãos.");
        static readonly string RTradeFriend = L("For you, a fair price. The Guild speaks well of you.", "Para você, um preço justo. A Guilda fala bem de você.");
        static readonly string RTradeFoe = L("Coin first. The Guild says you do not pay what you owe.", "Dinheiro primeiro. A Guilda diz que você não paga o que deve.");
        static readonly string RMutant = L("They edge away from you, and pretend they are not.", "Eles se afastam de você e fingem que não.");
        static readonly string RSellsword = L("Nice sword-arm you have there. Does it eat much?", "Belo braço de espada você tem aí. Come muito?");

        /// <summary>
        /// A line that depends on what the hero has done, or null when this person has nothing to add. Guards answer to the
        /// Watch, priests to the Temple (and to corruption), traders to the Guild; everyone else notices the body and the company.
        /// </summary>
        public static string Reaction(Monster m, int watch, int temple, int guild, int corruption, int mutations, bool companion)
        {
            switch (m.Role)
            {
                case TownRole.Guard: case TownRole.Captain:
                    if (watch >= 25) return RGuardFriend;
                    if (watch <= -25) return RGuardFoe;
                    break;
                case TownRole.Priest:
                    if (corruption >= 40) return RPriestTainted;
                    if (temple >= 25) return RPriestFriend;
                    if (temple <= -25) return RPriestFoe;
                    break;
                case TownRole.Shopkeeper: case TownRole.Smith: case TownRole.Innkeeper: case TownRole.Barkeep:
                    if (guild >= 25) return RTradeFriend;
                    if (guild <= -25) return RTradeFoe;
                    break;
                case TownRole.Pet: return null;
            }
            if (mutations >= 2) return RMutant;
            if (companion && m.Role == TownRole.Citizen) return RSellsword;
            return null;
        }

        /// <summary>The first thing a shopkeeper or service-giver says when you step up.</summary>
        public static string Greeting(Monster m)
        {
            if (m == null) return "";
            string line = LineFor(m, 0);
            return Loc.T(line);
        }

        // -------------------------------------------------------------- rumours

        static readonly string[] Gossip =
        {
            L("They say the Amulet of Yendor rests on the deepest floor of the Dungeons.", "Dizem que o Amuleto de Yendor repousa no andar mais fundo das Masmorras."),
            L("The Sunken Vaults drowned their dead. The dead did not mind.", "Os Cofres Afundados afogaram seus mortos. Os mortos não ligaram."),
            L("Dwarves dug the Mines too deep and found black water. They left their picks behind.", "Os anões cavaram as Minas fundo demais e acharam água negra. Deixaram as picaretas."),
            L("The Warrens were never built. Something chewed them out of the rock.", "As Tocas nunca foram construídas. Algo as roeu na pedra."),
            L("The Ashen Spire burned the dead, and then the living. Ask anyone from Emberdown.", "A Torre de Cinza queimou os mortos, e depois os vivos. Pergunte a qualquer um de Brasa Baixa."),
            L("Every god keeps an altar somewhere in the dark. Pray at one and it will remember you.", "Cada deus mantém um altar no escuro. Reze em um e ele vai lembrar de você."),
            L("Search the walls. The old builders loved a secret door.", "Procure nas paredes. Os antigos construtores adoravam portas secretas."),
            L("Never fight with your back to the stairs, and never fight without a way up.", "Nunca lute de costas para a escada, e nunca lute sem uma saída para cima."),
            L("The smith can hone a blade three times. After that the steel has no more to give.", "O ferreiro afia uma lâmina até três vezes. Depois disso o aço não tem mais o que dar."),
            L("The temple heals what a night's rest cannot.", "O templo cura o que uma noite de sono não cura."),
            L("A thing in the road? Fight it or run from it. It will not let you walk past.", "Algo na estrada? Lute ou fuja. Não vai deixar você passar."),
            L("The harder the country, the heavier the purse. The east is rich and deadly.", "Quanto mais dura a terra, mais pesada a bolsa. O leste é rico e mortal."),
            L("Stairs here go up as well as down. Try the tower, if you have the legs for it.", "As escadas aqui sobem e descem. Experimente a torre, se tiver pernas para isso."),
            L("They say the cellar of the tavern is bigger than the tavern.", "Dizem que o porão da taverna é maior que a taverna."),
            L("Yendor went down holding the seal so no king could climb back out. Nobody asked what he would do after.", "Yendor desceu segurando o selo para que nenhum rei subisse de volta. Ninguém perguntou o que ele faria depois."),
            L("The Ossuary does not give back. But it does pay, now and then.", "O Ossuary não devolve. Mas paga, de vez em quando."),
            L("A potion you cannot name is a coin you cannot spend. Have it appraised first.", "Poção sem nome é moeda que não se gasta. Mande avaliar primeiro."),
            L("The Craglands are kobold country. They are small. They are many.", "As Terras Fendidas são terra de kobolds. São pequenos. São muitos."),
        };

        public static string Rumor(int n) => Gossip[((n % Gossip.Length) + Gossip.Length) % Gossip.Length];

        static readonly string[] Story =
        {
            L("The Ossuary is the old burial pit of the whole continent. Every age dug a new floor above or below the last.", "O Ossuary é o antigo poço de sepultamento do continente inteiro. Cada era cavou um andar novo acima ou abaixo do anterior."),
            L("Yendor the Archivist sealed the sunken vaults with his own stamp and went down holding the seal. Black water rose. The dead did not stay buried.", "Yendor, o Arquivista, selou os cofres afundados com o próprio carimbo e desceu com o selo. A água negra subiu. Os mortos não ficaram enterrados."),
            L("The Reach League pays thirty gold a head for what climbs out, and five thousand for the Amulet of Yendor. Nobody has that much gold.", "A Liga do Reach paga trinta de ouro por cabeça do que sobe, e cinco mil pelo Amuleto de Yendor. Ninguém tem tanto ouro."),
            L("Carry the Amulet up into daylight and the pit is shut for good. That is the whole job.", "Leve o Amuleto à luz do dia e o poço se fecha para sempre. Esse é o trabalho inteiro."),
        };

        public static string StoryLine(int n, int bottomDepth)
        {
            int i = ((n % (Story.Length + 1)) + Story.Length + 1) % (Story.Length + 1);
            if (i == Story.Length) return $"The Amulet lies {bottomDepth} levels down, at the bottom of the Dungeons.";
            return Story[i];
        }

        // -------------------------------------------------------- translations

        static TownText()
        {
            // Service labels and messages (see Game.Town.cs).
            foreach (var kv in new Dictionary<string, string>
            {
                ["Browse the wares"] = "Ver as mercadorias", ["Rest until morning"] = "Dormir até amanhecer", ["A hot meal"] = "Uma refeição quente",
                ["A mug of ale"] = "Uma caneca de cerveja", ["Heal my wounds"] = "Curar meus ferimentos", ["Cure my ailments"] = "Curar meus males",
                ["Make an offering"] = "Fazer uma oferenda", ["Appraise an item"] = "Avaliar um item", ["Hone my weapon"] = "Afiar minha arma",
                ["Reinforce my armour"] = "Reforçar minha armadura", ["Ask about the Ossuary"] = "Perguntar sobre o Ossuary",
                ["Read the notice board"] = "Ler o quadro de avisos", ["Ask for news"] = "Pedir notícias", ["Take my leave"] = "Despedir-me",
                ["Appraise what?"] = "Avaliar o quê?",
                ["The stairs lead no further."] = "A escada não vai além.", ["There is no one here."] = "Não há ninguém aqui.",
                ["You eat a hot meal. It is plain and it is good."] = "Você come uma refeição quente. É simples e é boa.",
                ["You drink a mug of ale. It is bad, and it warms you."] = "Você bebe uma caneca de cerveja. É ruim, e esquenta.",
                ["A cold hand, a quiet word. Your wounds close."] = "Uma mão fria, uma palavra baixa. Seus ferimentos se fecham.",
                ["A prayer, and the sickness leaves you."] = "Uma prece, e a doença deixa você.",
                ["Your offering is noted. Your god is pleased."] = "Sua oferenda foi notada. Seu deus está satisfeito.",
                ["The priest blesses you, and asks which god you follow."] = "O sacerdote o abençoa e pergunta a qual deus você segue.",
                ["You sleep soundly, and wake to morning light and the smell of bread."] = "Você dorme profundamente e acorda com a luz da manhã e o cheiro de pão.",
                ["Not inside the walls. The Watch frowns on that."] = "Não dentro dos muros. A Guarda não aprova.",
                ["Not here. The Watch would hang you, and the dead would laugh."] = "Aqui não. A Guarda o enforcaria, e os mortos iriam rir.",
                ["There is a staircase down here. Press > to descend."] = "Há uma escada para baixo aqui. Aperte > para descer.",
                ["There is a staircase up here. Press < to climb."] = "Há uma escada para cima aqui. Aperte < para subir.",
                ["It blocks the way. Fight it (Enter or K) or flee (R or <)."] = "Ela bloqueia o caminho. Lute (Enter ou K) ou fuja (R ou <).",
                ["You cannot travel with a foe in your path. Fight it (Enter or K) or flee (R or <)."] = "Você não pode viajar com um inimigo no caminho. Lute (Enter ou K) ou fuja (R ou <).",
                ["You step back from the counter."] = "Você se afasta do balcão.",
                ["descend / climb (stairs in towns too)"] = "descer / subir (escadas nas cidades também)",
                ["talk to a person, trade at a counter (town)"] = "falar com alguém, negociar no balcão (cidade)",
                ["road: fight / flee a monster in your way"] = "estrada: lutar / fugir de um monstro no caminho",
                ["bump"] = "esbarrar", ["Enter K  R"] = "Enter K  R",
                ["attack"] = "atacar", ["flee"] = "fugir", ["talk"] = "falar", ["Up/Down  Enter chooses  Esc leaves"] = "↑↓  Enter escolhe  Esc sai",
                ["Wealth"] = "Riqueza", ["Gold"] = "Ouro", ["Floor"] = "Andar", ["street"] = "rua", ["cellar"] = "porão", ["upstairs"] = "andar de cima",
                ["It costs"] = "Custa", ["Leave"] = "Sair", ["Service"] = "Serviço", ["Talk"] = "Conversa",
            }) Pt[kv.Key] = kv.Value;

            R(@"You step up to (.+)\.", "Você se aproxima de $1.");
            R(@"The edge is true again\. (.+) is better than it was\.", "O fio está firme de novo. $1 está melhor do que antes.");
            R(@"Fresh rivets and a new lining\. (.+) will turn a blow better now\.", "Rebites novos e forro novo. $1 vai aparar melhor os golpes agora.");
            R(@"You learn that it is (.+)\.", "Você descobre que é $1.");
            R(@"The Amulet lies (\d+) levels down, at the bottom of the Dungeons\.", "O Amuleto está $1 níveis abaixo, no fundo das Masmorras.");
            R(@"Fight it \(Enter or K\), or flee \(R or <\)\. It is (.+)\.", "Lute (Enter ou K) ou fuja (R ou <). Parece $1.");
        }

        /// <summary>Portuguese for a message of the town layer, or null when it is none of ours.</summary>
        public static string Translate(string en)
        {
            if (Pt.TryGetValue(en, out var pt)) return pt;
            foreach (var (re, rep) in Rx)
                if (re.IsMatch(en)) return re.Replace(en, rep, 1);
            return null;
        }
    }
}
