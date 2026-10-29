using System.Collections.Generic;

namespace Ossuary.Core
{
    /// <summary>Text for named survivors (Version 24): titles, the rumour, the follow, the death and the detailed look.</summary>
    public static partial class Loc
    {
        static void AddSurvivorText()
        {
            // Titles are proper phrases: they are set after a name and translated on their own.
            TownText.L("Biter of Heroes", "Morde-Heróis");
            TownText.L("the Sly", "o Astuto");
            TownText.L("the Unbroken", "o Intacto");
            TownText.L("the Patient", "o Paciente");
            TownText.L("the Twice-Lived", "o Duas-Vezes-Vivo");
            TownText.L("the Unkillable", "o Imortal");
            TownText.L("the Name That Stays", "o Nome que Fica");

            TownText.L("lean and quick", "esguio e rápido");
            TownText.L("broad and slow", "largo e lento");
            TownText.L("scarred and wary", "cicatrizado e cauteloso");
            TownText.L("hunched and old", "curvado e velho");
            TownText.L("gaunt and hungry", "magro e faminto");
            TownText.L("wiry and young", "fibroso e jovem");
            TownText.L("heavy with sleep", "pesado de sono");
            TownText.L("ready to run", "pronto para correr");
            TownText.L("wounded and desperate", "ferido e desesperado");

            Rx.Add(R(@"^They say (.+), (.+) holds level (\d+) of (.+)\.$", "Dizem que $1, $2, domina o nível $3 de $4."));
            Rx.Add(R(@"^They say (.+) holds level (\d+) of (.+)\.$", "Dizem que $1 domina o nível $2 de $3."));
            Rx.Add(R(@"^Something follows you down: (.+), (.+)\.$", "Algo desce atrás de você: $1, $2."));
            Rx.Add(R(@"^Something follows you down: (.+)\.$", "Algo desce atrás de você: $1."));
            Rx.Add(R(@"^(.+) was a name in the deep, and is one no longer\.$", "$1 era um nome nas profundezas, e já não é."));
            Rx.Add(R(@"^(.+) looks (.+)\.$", "$1 parece $2."));
            Rx.Add(R(@"^It grips (.+)\.$", "Empunha $1."));
            Rx.Add(R(@"^A shape waits at the bend of the road, and it knows your walk\. (.+) has come up out of (.+) for you\.$",
                "Uma forma espera na curva da estrada, e conhece seu andar. $1 subiu de $2 atrás de você."));
            Rx.Add(R(@"^(.+)\. ([^.!]+) blocks your path!$", "$1. $2 bloqueia seu caminho!"));
            Rx.Add(R(@"^You run\. Something catches your shoulder as you go\. \(-(\d+)\)$", "Você corre. Algo acerta seu ombro enquanto foge. (-$1)"));

            // The six riders the hero now feels, and the lines their end says.
            Pt["Your limbs grow heavy and slow."] = "Seus membros ficam pesados e lentos.";
            Pt["Your strength drains away."] = "Sua força se esvai.";
            Pt["A cold terror takes hold of you."] = "Um terror frio toma conta de você.";
            Pt["You cannot keep your eyes open."] = "Você não consegue manter os olhos abertos.";
            Pt["The ground holds your feet fast."] = "O chão prende seus pés.";
            Pt["You wake."] = "Você acorda.";
            Pt["You wake with a start."] = "Você acorda num sobressalto.";
            Pt["The terror lets go of you."] = "O terror solta você.";
            Pt["You can move again."] = "Você pode se mover de novo.";
            Pt["The bleeding stops."] = "O sangramento para.";
            Pt["You are too afraid to raise a hand."] = "Você está com medo demais para erguer a mão.";
            Pt["You are too afraid to focus."] = "Você está com medo demais para se concentrar.";
            Pt["You are too afraid to call it up."] = "Você está com medo demais para invocá-lo.";
            Pt["bleeding"] = "sangramento";
            Pt["a survivor on the road"] = "um sobrevivente na estrada";
            Pt["It has followed you this far"] = "Ele seguiu você até aqui";
            Pt["Whatever it was, it is gone."] = "O que quer que fosse, já se foi.";
            Pt["Survivors"] = "Sobreviventes";
            Pt["(slain)"] = "(morto)";
            Pt["(still down there)"] = "(ainda lá embaixo)";
        }
    }
}
