using System.Collections.Generic;

namespace Ossuary.Core
{
    /// <summary>Portuguese for crafting, gathering, music and the workshops (Items/Trades.cs, Game.Crafting.cs, Game.Gathering.cs, Game.Workshops.cs).</summary>
    public static partial class Loc
    {
        static void AddCraftText()
        {
            // ---- goods, meals, instruments, tools
            foreach (var n in new[] {
                ("copper bar", "barra de cobre", true), ("bronze bar", "barra de bronze", true), ("iron bar", "barra de ferro", true),
                ("steel bar", "barra de aço", true), ("silver bar", "barra de prata", true), ("cold iron bar", "barra de ferro frio", true),
                ("mithril bar", "barra de mithril", true), ("adamantine bar", "barra de adamantina", true), ("metal bar", "barra de metal", true),
                ("log", "tora", true), ("plank", "tábua", true), ("raw hide", "couro cru", false), ("leather", "couro curtido", false),
                ("flax", "linho", false), ("thread", "linha", true), ("cloth", "tecido", false), ("healing herb", "erva curativa", true),
                ("swiftroot", "raiz-ligeira", true), ("nightshade", "beladona", true), ("glass flask", "frasco de vidro", false),
                ("parchment", "pergaminho em branco", false), ("ink", "tinta", true), ("tallow", "sebo", false), ("barley", "cevada", true),
                ("honey", "mel", false), ("raw meat", "carne crua", true), ("raw fish", "peixe cru", false),
                ("roast meat", "carne assada", true), ("grilled fish", "peixe grelhado", false), ("hearty stew", "ensopado farto", false),
                ("loaf of bread", "pão", false), ("honey cake", "bolo de mel", false), ("mug of ale", "caneca de cerveja", true),
                ("bottle of mead", "garrafa de hidromel", true),
                ("flute", "flauta", true), ("drum", "tambor", false), ("tambourine", "pandeiro", false), ("lute", "alaúde", false),
                ("horn", "trompa", true), ("fiddle", "rabeca", true), ("harp", "harpa", true), ("fishing rod", "vara de pesca", true) })
                N(n.Item1, n.Item2, n.Item3);

            // ---- trades: lowercase inside sentences, a title on the sheet; ranks
            foreach (var t in new[] {
                ("blacksmith", "ferreiro"), ("armourer", "armeiro"), ("bowyer", "fabricante de arcos"), ("leatherworker", "coureiro"),
                ("tailor", "alfaiate"), ("jeweller", "joalheiro"), ("alchemist", "alquimista"), ("scribe", "escriba"), ("carpenter", "carpinteiro"),
                ("toolmaker", "ferramenteiro"), ("luthier", "luthier"), ("cook", "cozinheiro"), ("brewer", "cervejeiro"), ("miner", "minerador"),
                ("musician", "músico"), ("forager", "coletor") })
            {
                P(t.Item1, t.Item2);
                P(char.ToUpperInvariant(t.Item1[0]) + t.Item1.Substring(1), char.ToUpperInvariant(t.Item2[0]) + t.Item2.Substring(1));
            }
            foreach (var kv in new[] {
                ("Apprentice", "Aprendiz"), ("Journeyman", "Oficial"), ("Grandmaster", "Grão-mestre"), ("Novice", "Novato"), ("Master", "Mestre"),
                ("Trades", "Ofícios"), ("Every recipe", "Todas as receitas"), ("Gather / butcher", "Coletar / carnear"),
                ("town workshop", "oficina na cidade"), ("anywhere", "qualquer lugar"), ("forge", "forja"),
                ("a blade", "uma lâmina"), ("light armour", "armadura leve"), ("a gem", "uma gema"),
                ("craft: make what your trades, pack and place allow", "fabricar: o que seus ofícios, a mochila e o lugar permitem"),
                ("every recipe, by trade and rank", "todas as receitas, por ofício e nível"),
                ("gather: butcher a carcass, or forage on the road", "coletar: carnear uma carcaça, ou buscar recursos na estrada"),
                ("You have nothing you can make here. Shift+J lists every recipe.", "Você não tem nada que possa fazer aqui. Shift+J lista todas as receitas."),
                ("You botch the work. The materials are ruined.", "Você estraga o trabalho. Os materiais se perdem."),
                ("You butcher the carcass.", "Você carneia a carcaça."),
                ("Only bones are left here. They serve as they are.", "Só restam ossos aqui. Servem do jeito que estão."),
                ("There is nothing to gather in the streets. The shops sell raw goods.", "Não há nada a coletar nas ruas. As lojas vendem matéria-prima."),
                ("There is nothing here to gather. Stand on a carcass, or gather on the road.", "Não há nada para coletar aqui. Fique sobre uma carcaça, ou colete na estrada."),
                ("Not with a foe in front of you.", "Não com um inimigo à sua frente."),
                ("You need an axe to fell trees. You take what lies on the ground.", "Você precisa de um machado para derrubar árvores. Leva o que está no chão."),
                ("You need a pick-axe to break rock.", "Você precisa de uma picareta para quebrar rocha."),
                ("You need a fishing rod to fish.", "Você precisa de uma vara de pesca para pescar."),
                ("Little grows here.", "Pouca coisa cresce aqui."),
                ("You spend two hours searching and find nothing of use.", "Você passa duas horas procurando e não acha nada útil."),
                ("You spend two hours gathering.", "Você passa duas horas coletando."),
                ("A vein of ore breaks loose.", "Um veio de minério se solta."),
                ("You play for a while. The street has given what it will today.", "Você toca por um tempo. A rua já deu o que tinha para dar hoje."),
                ("The master shows you the tricks of the trade.", "O mestre mostra os segredos do ofício."),
                ("You do not have it with you.", "Você não tem isso com você."),
                ("That will do for the commission. Bring it to the workshop.", "Isso serve para a encomenda. Leve à oficina."),
            }) P(kv.Item1, kv.Item2);

            Rx.InsertRange(0, new List<(System.Text.RegularExpressions.Regex, string)>
            {
                R(@"Your craft grows: (.+) is now (.+)\.", "Seu ofício cresce: $1 agora é $2."),
                R(@"You make (\d+) x (.+)\.", "Você faz $1 x $2."),
                R(@"You gather (.+) \(x(\d+)\)\.", "Você coleta $1 (x$2)."),
                R(@"You gather (.+)\.", "Você coleta $1."),
                R(@"You play the (.+)\. Passers-by drop (\d+) gold\.", "Você toca {a1}. Quem passa deixa $2 de ouro."),
                R(@"You play the (.+)\. (\d+) creature\(s\) sink into sleep\.", "Você toca {a1}. $2 criatura(s) adormece(m)."),
                R(@"You play the (.+)\. Nothing listens\.", "Você toca {a1}. Nada escuta."),
                R(@"You play the (.+) by the road\.", "Você toca {a1} à beira da estrada."),
                R(@"Learn the (.+)'s craft \((.+)\)", "Aprender o ofício de $1 ($2)"),
                R(@"Commission: make (.+) \((\d+) gold\)", "Encomenda: fazer {u1} ($2 de ouro)"),
                R(@"Deliver: (.+) \((\d+) gold\)", "Entregar: {u1} ($2 de ouro)"),
                R(@"Craft for the town: (.+)", "Fazer para a cidade: {u1}"),
            });
        }
    }
}

