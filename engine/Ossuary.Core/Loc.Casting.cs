namespace Ossuary.Core
{
    /// <summary>Portuguese for monsters that cast (Game.Casters.cs), the Sallow Magister, and the ice and arcing lightning (Game.Surfaces.cs).</summary>
    public static partial class Loc
    {
        static void AddCastingText()
        {
            N("dark acolyte", "acólito sombrio"); N("sorcerer", "feiticeiro"); N("Sallow Magister", "Magistrado Pálido");

            foreach (var kv in new[] {
                ("skip a running animation", "pular a animação em curso"),
                ("You shake it off.", "Você resiste ao efeito."),
                ("You stagger, confused.", "Você cambaleia, confuso."),
                ("The world goes dark for a moment.", "O mundo escurece por um instante."),
                ("You reel, stunned.", "Você cambaleia, atordoado."),
                ("The shock runs along the ice!", "O choque corre pelo gelo!"),
                // the Sallow Magister
                ("Pale candles burn in cells where no one lights them. The Sallow Magister is writing in a book that bleeds.",
                 "Velas pálidas queimam em celas onde ninguém as acende. O Magistrado Pálido escreve num livro que sangra."),
                ("The Sallow Magister tears out a page, and the page burns in the air.", "O Magistrado Pálido arranca uma página, e a página arde no ar."),
                ("The Sallow Magister folds like a bad letter, and the candles go out.", "O Magistrado Pálido se dobra como uma carta ruim, e as velas se apagam."),
            })
                P(kv.Item1, kv.Item2);

            Rx.Add(R(@"The (.+) casts (.+)!", "{a1} conjura $2!"));
            Rx.Add(R(@"(.+) you for (\d+) damage\.", "$1 te atinge: $2 de dano."));
            Rx.Add(R(@"The ice carries the shock into you for (\d+)!", "O gelo leva o choque até você: $1!"));
            Rx.Add(R(@"The lightning arcs to the (.+)\.", "O raio salta para {a1}."));
        }
    }
}
