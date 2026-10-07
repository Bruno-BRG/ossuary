using System.Collections.Generic;

namespace Ossuary.Core
{
    /// <summary>Portuguese for Version 20: history in play, legends, stains and trails, engravings and rooms, raids, the morgue's legend and the Hollow Court.</summary>
    public static partial class Loc
    {
        static void AddWorldText()
        {
            foreach (var n in new[] {
                ("The Hollow Court", "A Corte Oca", true), ("Hollow Queen", "Rainha Oca", true),
                ("Queen's Thorn", "Espinho da Rainha", false), ("Gown of the Last Dance", "Vestido da Última Dança", false) })
                N(n.Item1, n.Item2, n.Item3);

            foreach (var kv in new[] {
                // the Hollow Court
                ("Velvet gone to rot, a long hall of empty thrones, and someone humming a dance.", "Veludo apodrecido, um longo salão de tronos vazios, e alguém cantarolando uma dança."),
                ("At the end of the hall a crowned thing sits on the only throne that is not empty, and asks you to dance.", "No fim do salão, uma coisa coroada senta no único trono que não está vazio, e convida você para dançar."),
                ("The Hollow Queen rises, and her courtiers rise with her.", "A Rainha Oca se levanta, e seus cortesãos se levantam com ela."),
                ("The Hollow Queen comes apart like a dress left too long in a chest.", "A Rainha Oca se desfaz como um vestido esquecido tempo demais num baú."),
                ("A hatpin long enough to be a blade. The Queen wore it, and used it, and the court learned not to lean close.", "Um alfinete de chapéu longo o bastante para ser lâmina. A Rainha o usava, e a corte aprendeu a não chegar perto."),
                ("Moth-eaten velvet that still moves as if a waltz were playing.", "Veludo roído de traça que ainda se move como se tocasse uma valsa."),
                ("The Last Dance", "A Última Dança"), ("Step through the portal in the Mines into the Hollow Court.", "Atravesse o portal das Minas até a Corte Oca."),
                ("The Throne Is Empty", "O Trono Está Vazio"), ("Kill the Hollow Queen.", "Mate a Rainha Oca."),
                // legends
                ("Legends", "Lendas"), ("up/down scroll, any key closes", "cima/baixo rola, qualquer tecla fecha"),
                ("Events", "Acontecimentos"), ("People", "Pessoas"), ("Places", "Lugares"), ("Heroes before you", "Heróis antes de você"),
                ("You know nothing of the past yet. Libraries, scholars, bards and old walls do.", "Você ainda não sabe nada do passado. Bibliotecas, eruditos, bardos e paredes antigas sabem."),
                ("legends: the past you have learned", "lendas: o passado que você aprendeu"),
                ("follow a wounded creature's trail", "seguir o rastro de uma criatura ferida"), ("carve something into the floor", "entalhar algo no chão"),
                ("Follow a trail", "Seguir um rastro"), ("Carve into the floor", "Entalhar no chão"),
                ("Ask about the old days", "Perguntar sobre os velhos tempos"),
                // stains and trails
                ("Somewhere, something has caught the scent of your blood.", "Em algum lugar, algo sentiu o cheiro do seu sangue."),
                ("There is no trail to follow here.", "Não há rastro para seguir aqui."), ("You find no trail to follow.", "Você não acha rastro para seguir."),
                ("fresh", "fresco"), ("drying", "secando"), ("old", "velho"), ("blood", "sangue"), ("ichor", "icor"), ("slime", "gosma"), ("mud", "lama"),
                ("soot", "fuligem"), ("footprints", "pegadas"), ("drag marks", "marcas de arrasto"), ("you", "você"),
                ("north", "norte"), ("south", "sul"), ("east", "leste"), ("west", "oeste"), ("north-east", "nordeste"), ("north-west", "noroeste"),
                ("south-east", "sudeste"), ("south-west", "sudoeste"),
                // rooms and carving
                ("a barracks", "um quartel"), ("a temple", "um templo"), ("a shrine", "um santuário"), ("a garden", "um jardim"), ("a mine working", "uma frente de mina"),
                ("a forge", "uma forja"), ("a crypt", "uma cripta"), ("a strongpoint", "um posto fortificado"), ("a treasury", "um tesouro"),
                ("a place of sacrifice", "um lugar de sacrifício"), ("a larder", "uma despensa"), ("a dormitory", "um dormitório"), ("a feasting hall", "um salão de banquetes"),
                ("a well-room", "uma sala de poço"), ("something else", "outra coisa"),
                ("Carve what?", "Entalhar o quê?"), ("There is nothing here to carve into.", "Não há onde entalhar aqui."),
                ("You need bare floor to carve into.", "Você precisa de chão nu para entalhar."), ("Something is already carved here.", "Já há algo entalhado aqui."),
                ("Turn back.", "Volte."), ("Beware what waits below.", "Cuidado com o que espera lá embaixo."),
                // raids
                ("Only ash and a blackened counter are left. Raiders burned it.", "Só restam cinzas e um balcão enegrecido. Saqueadores queimaram tudo."),
                ("beating off a raid", "rechaçar um ataque"), ("leaving a town to its raiders", "abandonar uma cidade aos saqueadores"),
                // the morgue's legend
                ("Legend", "Lenda"), ("Nothing the world will sing about.", "Nada que o mundo vá cantar."), ("Enemies brought down", "Inimigos derrubados"),
                ("Brought down by", "Derrubado por"), ("Legends learned", "Lendas aprendidas"),
            }) P(kv.Item1, kv.Item2);

            Rx.InsertRange(0, new List<(System.Text.RegularExpressions.Regex, string)>
            {
                R(@"The present year is (\d+)\. The Pit opened in (\d+)\.", "O ano presente é $1. O Poço se abriu em $2."),
                R(@"(\s*)(fresh|drying|old) (blood|ichor|slime|mud|soot|footprints|drag marks), left by (.+)", "$1$3 ($2), deixado por $4"),
                R(@"You follow the trail of (.+) (north|south|east|west|north-east|north-west|south-east|south-west): an? (.+), wounded\.", "Você segue o rastro de $1 para o $2: {u3}, ferido."),
                R(@"(\s*)an engraving: ""(.+)""", "$1uma gravura: \"$2\""),
                R(@"(\s*)in what was (.+)", "$1no que já foi $2"),
                R(@"Something is engraved here: ""(.+)""", "Há algo gravado aqui: \"$1\""),
                R(@"A name is cut into the gravestone: ""(.+)""", "Um nome está talhado na lápide: \"$1\""),
                R(@"You carve into the floor: ""(.+)""", "Você entalha no chão: \"$1\""),
                R(@"Raiders at the gate! (\d+) (.+)s come over the wall\.", "Saqueadores no portão! $1 {u2}(s) pulam o muro."),
                R(@"The last raider falls\. The elder of (.+) presses (\d+) gold on you, and the street cheers your name\.", "O último saqueador cai. O ancião de $1 põe $2 de ouro na sua mão, e a rua grita seu nome."),
                R(@"On day (\d+) a band of (.+)s raided (.+), and the Watch beat them off\.", "No dia $1, um bando de {u2}(s) atacou $3, e a Guarda o rechaçou."),
                R(@"On day (\d+) (.+)s raided (.+): (.+) burned, and (\d+) died\.", "No dia $1, {u2}(s) atacaram $3: {u4} queimou, e $5 morreram."),
                R(@"On day (\d+) (.+)s raided (.+), and (\d+) died\.", "No dia $1, {u2}(s) atacaram $3, e $4 morreram."),
                R(@"The Hollow Queen blows you a kiss across the hall\. It lands cold\. \(-(\d+)\)", "A Rainha Oca manda um beijo do outro lado do salão. Chega gelado. (-$1)"),
                R(@"slew the (.+)", "matou {a1}"), R(@"killed (?!by )(.+)", "matou $1"), R(@"made (?!by )(.+)", "fez $1"), R(@"carried (.+)", "carregou $1"),
            });
        }
    }
}
