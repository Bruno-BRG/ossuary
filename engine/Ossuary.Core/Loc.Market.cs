namespace Ossuary.Core
{
    /// <summary>Portuguese for the town markets (Game.Market.cs): goods, the order board, the hero's counter, caravans, tolls and dues.</summary>
    public static partial class Loc
    {
        static void AddMarketText()
        {
            foreach (var kv in new[] {
                ("weapons", "armas"), ("armour", "armaduras"), ("potions", "poções"), ("books and scrolls", "livros e pergaminhos"),
                ("wands", "varinhas"), ("jewellery", "joias"), ("food", "comida"), ("raw goods", "matérias-primas"), ("tools", "ferramentas"),
                ("cheap", "barato"), ("fair", "justo"), ("dear", "caro"),
                ("cheerful", "animado"), ("sour", "azedo"), ("even", "neutro"), ("Mood", "Humor"), ("Markets", "Mercados"),
                ("Rent a counter for a week", "Alugar um balcão por uma semana"),
                ("Guild dues (paid this week)", "Taxa da Guilda (paga esta semana)"),
                ("Pay Guild dues (better prices for a week)", "Pagar a taxa da Guilda (preços melhores por uma semana)"),
                ("You have no counter here.", "Você não tem balcão aqui."),
                ("That is not for sale.", "Isso não está à venda."),
                ("Your counter is full.", "Seu balcão está cheio."),
                ("Take it off first.", "Tire isso primeiro."),
                ("Set out what?", "Expor o quê?"),
                ("The counter is yours for a week. Set out your goods and come back for the coin.", "O balcão é seu por uma semana. Exponha suas mercadorias e volte pelas moedas."),
                ("Your dues are paid: the Guild's traders pay its members better this week.", "Taxa paga: os mercadores da Guilda pagam melhor aos membros esta semana."),
                ("The gate guard looks at your empty purse and waves you through.", "O guarda do portão olha sua bolsa vazia e acena para você passar."),
            })
                P(kv.Item1, kv.Item2);

            Rx.Add(R(@"A caravan is in from the road: (.+) are cheap this week\.", "Uma caravana chegou da estrada: $1 estão baratas esta semana."));
            Rx.Add(R(@"The road was raided and the carts never came: (.+) are short this week\.", "A estrada foi saqueada e as carroças não vieram: faltam $1 esta semana."));
            Rx.Add(R(@"Wanted: (.+) \(pays (\d+) gold\)", "Procura-se: {a1} (paga $2 de ouro)"));
            Rx.Add(R(@"Offered: (.+)", "Oferta: {a1}"));
            Rx.Add(R(@"Set out goods \((\d+)/8, (\d+) days left\)", "Expor mercadorias ($1/8, faltam $2 dias)"));
            Rx.Add(R(@"Asking price: (.+)", "Preço pedido: $1"));
            Rx.Add(R(@"Collect your takings \((\d+) gold\)", "Recolher os ganhos ($1 de ouro)"));
            Rx.Add(R(@"Take back your goods \((\d+)\)", "Recolher suas mercadorias ($1)"));
            Rx.Add(R(@"You hand over the (.+) and are paid (\d+) gold\.", "Você entrega {a1} e recebe $2 de ouro."));
            Rx.Add(R(@"You buy the (.+) for (\d+) gold\.", "Você compra {a1} por $2 de ouro."));
            Rx.Add(R(@"You set out (.+)\.", "Você expõe {a1}."));
            Rx.Add(R(@"You will ask a (.+) price\.", "Você vai pedir um preço $1."));
            Rx.Add(R(@"You collect (\d+) gold from your counter\.", "Você recolhe $1 de ouro do seu balcão."));
            Rx.Add(R(@"You pack up (\d+) unsold things\.", "Você recolhe $1 coisas não vendidas."));
            Rx.Add(R(@"You pay the gate toll: (\d+) gold\.", "Você paga o pedágio do portão: $1 de ouro."));
            Rx.Add(R(@"Word reaches you: your counter has (\d+) gold waiting\.", "Chega a notícia: seu balcão tem $1 de ouro esperando."));
            Rx.Add(R(@"(.+): (.+) sell at (\d+)% \(day (\d+)\)", "$1: $2 vendem a $3% (dia $4)"));
        }
    }
}
