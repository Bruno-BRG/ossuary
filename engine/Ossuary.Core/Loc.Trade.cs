using System.Collections.Generic;

namespace Ossuary.Core
{
    /// <summary>Portuguese for Version 19: identify by use, item services, relics and history, stations, crafters, archers, the quiver, caravans, haggling and traders' memory.</summary>
    public static partial class Loc
    {
        static void AddTradeText()
        {
            // ---- things
            foreach (var n in new[] {
                ("amulet of the leech", "amuleto da sanguessuga", false), ("amulet of restless sleep", "amuleto do sono inquieto", false),
                ("amulet of the hungry dead", "amuleto dos mortos famintos", false),
                ("The Weeping Edge", "O Gume que Chora", false), ("Marrow Mail", "Malha de Tutano", true), ("Crown of the Pit", "Coroa do Poço", true),
                ("loom", "tear", false), ("still", "alambique", false) })
                N(n.Item1, n.Item2, n.Item3);

            foreach (var kv in new[] {
                // relic lore
                ("It is always wet, and it is never water. Whoever held it last is still crying somewhere.",
                 "Está sempre molhado, e nunca é água. Quem o segurou por último ainda chora em algum lugar."),
                ("Every ring is a knuckle-bone. It fits like it was measured for you, which is the worrying part.",
                 "Cada elo é um nó de dedo. Serve como se tivesse sido medida para você, e é isso que preocupa."),
                ("Pressed from the skulls of three kings. It whispers which of them it liked best.",
                 "Prensada dos crânios de três reis. Sussurra de qual deles gostava mais."),
                // panels, rows, prompts
                ("Potions known", "Poções conhecidas"), ("Scrolls known", "Pergaminhos conhecidos"), ("Wands known", "Varinhas conhecidas"),
                ("Recharge which wand?", "Recarregar qual varinha?"), ("Recharge a wand", "Recarregar uma varinha"), ("Lift a curse", "Tirar uma maldição"),
                ("Read the chronicles", "Ler as crônicas"), ("Relics", "Relíquias"), ("Loose which first?", "Disparar qual primeiro?"),
                ("Choose ammunition", "Escolher munição"), ("choose which ammunition to loose first", "escolher qual munição disparar primeiro"),
                ("Offer how much?", "Oferecer quanto?"), ("Rob the caravan", "Roubar a caravana"),
                ("Enter/b buy   o haggle   s sell   hjkl move   Esc leave", "Enter/b compra   o pechincha   s vende   hjkl move   Esc sai"),
                // messages
                ("The priest says the old words, and something lets go of you.", "O sacerdote diz as palavras antigas, e algo solta você."),
                ("There is no curse on you.", "Não há maldição sobre você."),
                ("The amulet will not come off. It is cursed: a priest can lift it.", "O amuleto não sai. Está amaldiçoado: um sacerdote pode tirar a maldição."),
                ("The amulet drinks a little of you.", "O amuleto bebe um pouco de você."), ("Your eyes close by themselves.", "Seus olhos se fecham sozinhos."),
                ("There is nothing here to haggle over.", "Não há nada aqui para pechinchar."), ("Gold is not haggled over.", "Ouro não se pechincha."),
                ("You cannot afford even that.", "Você não pode pagar nem isso."), ("You robbed a caravan.", "Você roubou uma caravana."),
                ("guarding a caravan", "guardar uma caravana"), ("robbing a caravan", "roubar uma caravana"),
                ("Bandits come out of the ditch at dusk", "Bandidos saem da vala ao anoitecer"),
                ("You loose a shot into empty air.", "Você dispara no ar vazio."),
            }) P(kv.Item1, kv.Item2);

            Rx.InsertRange(0, new List<(System.Text.RegularExpressions.Regex, string)>
            {
                R(@"You learn what the (.+) is: (.+)\.", "Você descobre o que é {a1}: {u2}."),
                R(@"The (.+) cannot hold any more\. It splits with a crack and a flash\. \(-(\d+)\)", "{a1} não aguenta mais. Racha com um estalo e um clarão. (-$2)"),
                R(@"The sage hums over the (.+) until it is warm\. \((\d+) charges\)", "O sábio murmura sobre {a1} até ela esquentar. ($2 cargas)"),
                R(@"Once carried by (.+)\.", "Antes foi de $1."),
                R(@"Slew the (.+) on (.+) (\d+), day (\d+), in the hand of (.+)\.", "Matou {a1} em $2 $3, no dia $4, na mão de $5."),
                R(@"Was worn by (.+) when the (.+) fell on (.+) (\d+), day (\d+)\.", "Vestido por $1 quando {a2} caiu em $3 $4, no dia $5."),
                R(@"Your relics will remember the (.+)\.", "Suas relíquias vão se lembrar {d1}."),
                R(@"(.+) has set out new work this week\.", "$1 pôs à venda trabalho novo esta semana."),
                R(@"The (.+) shoots: the (.+) hits you for (\d+) damage\.", "{a1} atira: {a2} acerta você, $3 de dano."),
                R(@"The (.+) shoots: the (.+) misses you\.", "{a1} atira: {a2} erra você."),
                R(@"You will loose (.+) first\.", "Você vai disparar {a1} primeiro."),
                R(@"Ride with them to (.+) as a guard \((\d+) gold\)", "Ir com eles até $1 como guarda ($2 de ouro)"),
                R(@"Six hours at the tailboard with a spear across your knees\. The carts reach (.+), and the drivers pay (\d+) gold\.",
                  "Seis horas na traseira da carroça com uma lança nos joelhos. As carroças chegam a $1, e os carroceiros pagam $2 de ouro."),
                R(@"The drivers run\. You take (\d+) gold and what you can carry\. (.+) will go short this week\.",
                  "Os carroceiros fogem. Você leva $1 de ouro e o que conseguir carregar. $2 vai passar falta esta semana."),
                R(@"(.+) carries (.+), made by (.+)\.", "$1 carrega $2, feito por $3."),
                R(@"Offer (\d+)%: (\d+) gold for (.+)", "Oferecer $1%: $2 de ouro por {a3}"),
                R(@"(.+) folds their arms\. ""You again\. Buy, or go\.""", "$1 cruza os braços. \"Você de novo. Compre, ou vá embora.\""),
                R(@"(.+) looks at you hard\. ""The last thing you sold me was rotten\. I have not forgotten\.""", "$1 encara você. \"A última coisa que você me vendeu era podre. Eu não esqueci.\""),
                R(@"(.+) sighs\. ""Not more of the same, I hope\. I am still selling the last lot\.""", "$1 suspira. \"Mais do mesmo não, espero. Ainda estou vendendo o último lote.\""),
                R(@"(.+) grins\. ""My favourite haggler\. Go easy on me today\.""", "$1 sorri. \"Meu pechinchador favorito. Pegue leve hoje.\""),
                R(@"(.+) takes (\d+) gold for (.+)\. ""Robbery\. Go on\.""", "$1 aceita $2 de ouro por {a3}. \"Um roubo. Vá.\""),
                R(@"(.+) goes red\. ""Is that what you think my work is worth\?"" They will not bargain with you again today\.",
                  "$1 fica vermelho. \"É isso que você acha que meu trabalho vale?\" Não vai negociar com você de novo hoje."),
            });
        }
    }
}
