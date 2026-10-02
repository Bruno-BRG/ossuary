namespace Ossuary.Core
{
    /// <summary>
    /// Opening story as data: pages in the player's language, one era per page, told in order from the
    /// first pit to the player's own arrival. The first line of every page is its heading (era and how long
    /// ago); the rest is the text. The frontend plays them as an animated cross-section of the world, one
    /// scene per page; nothing here touches the simulation. Facts follow docs/lore.md.
    /// </summary>
    public static class Story
    {
        public static string[] Epitaph() => new[] { Loc.Current == Lang.Pt ? "O Ossuary lembra." : "The Ossuary remembers." };

        public static string[][] Intro()
        {
            if (Loc.Current == Lang.Pt) return new[]
            {
                new[] { "I · O POÇO · há mil e duzentos anos",
                        "Antes dos reinos, havia só um buraco no meio do continente.",
                        "Cada vila enterrava seus mortos nele, e ninguém o chamava de nada." },
                new[] { "II · OS REIS E OS ARQUIVOS · há novecentos anos",
                        "Vieram os reis, e enterraram ali tesouros, soldados e segredos.",
                        "Vieram os Arquivistas, com seus livros e seus cofres.",
                        "Cada era cavou um andar novo, por cima ou por baixo do anterior,",
                        "e perdeu o mapa." },
                new[] { "III · O OSSUARY · há seiscentos anos",
                        "Os andares se empilharam até virarem geologia.",
                        "O buraco virou pedra. A pedra virou o Ossuary:",
                        "cinco camadas de mortos, cada uma de uma época diferente,",
                        "e quanto mais fundo, mais recente e pior o que aconteceu." },
                new[] { "IV · O SELO DE YENDOR · há trezentos anos",
                        "Quando os mortos começaram a andar, Yendor, o Arquivista,",
                        "selou os cofres afundados com o próprio carimbo",
                        "e desceu com o selo nas mãos, para que nenhum rei subisse de volta.",
                        "Ele nunca voltou. A água negra subiu. Os mortos não ficaram enterrados." },
                new[] { "V · A GUERRA DA TORRE · há cento e vinte anos",
                        "Alguém ergueu a Torre de Cinza, de ponta-cabeça, para queimar o que subia.",
                        "Queimou os mortos. Depois queimou os vivos.",
                        "Os reinos caíram. Sobraram vilas muradas, estradas perigosas",
                        "e a Liga do Reach." },
                new[] { "VI · HOJE",
                        "A Liga paga trinta moedas por cabeça a quem aceitar descer.",
                        "Promete cinco mil pelo Amuleto de Yendor, o selo que ele levou.",
                        "Quem o trouxer à luz do dia fecha o Ossuary para sempre.",
                        "Ninguém tem tanto ouro. Ninguém espera que você volte." },
                new[] { "VII · VOCÊ",
                        "Você é a milésima tentativa.",
                        "Ninguém o escolheu. Você foi o único que disse sim.",
                        "As vilas dão abrigo, comida e um ferreiro. O resto está lá embaixo.",
                        "Objetivo: desça, pegue o Amuleto e volte à superfície.",
                        "",
                        "Desça. O Ossuary lembra." },
            };
            return new[]
            {
                new[] { "I · THE PIT · twelve hundred years ago",
                        "Before the kingdoms, there was only a hole in the middle of the continent.",
                        "Every village buried its dead in it, and no one called it anything." },
                new[] { "II · KINGS AND ARCHIVES · nine hundred years ago",
                        "Then came the kings, and buried treasure, soldiers and secrets there.",
                        "Then came the Archivists, with their books and their vaults.",
                        "Each age dug a new floor above or below the last,",
                        "and lost the map." },
                new[] { "III · THE OSSUARY · six hundred years ago",
                        "The floors piled up until they became geology.",
                        "The pit became stone. The stone became the Ossuary:",
                        "five layers of dead, each from a different age,",
                        "and the deeper you go, the newer and worse the thing that happened." },
                new[] { "IV · YENDOR'S SEAL · three hundred years ago",
                        "When the dead began to walk, Yendor the Archivist",
                        "sealed the sunken vaults with his own stamp",
                        "and went down holding the seal, so no king could climb back out.",
                        "He never returned. Black water rose. The dead did not stay buried." },
                new[] { "V · THE WAR OF THE SPIRE · a hundred and twenty years ago",
                        "Someone raised the Ashen Spire, upside down, to burn whatever climbed out.",
                        "It burned the dead. Then it burned the living.",
                        "The kingdoms fell. What was left: walled villages, dangerous roads,",
                        "and the Reach League." },
                new[] { "VI · TODAY",
                        "The League pays thirty gold a head to anyone who will go down.",
                        "It promises five thousand for the Amulet of Yendor, the seal he carried.",
                        "Whoever brings it into daylight closes the Ossuary for good.",
                        "No one has that much gold. No one expects you back." },
                new[] { "VII · YOU",
                        "You are the thousandth to try.",
                        "No one chose you. You were the only one who said yes.",
                        "The villages offer shelter, food and a smith. The rest is down there.",
                        "Goal: descend, take the Amulet, and return to the surface.",
                        "",
                        "Descend. The Ossuary remembers." },
            };
        }

        /// <summary>Messages logged when the hero first stands on the overworld.</summary>
        public static string[] Opening(string region) => Loc.Current == Lang.Pt
            ? new[] { $"A estrada termina na boca do Ossuary. Você está em {Loc.U(region)}.", "Explore o mundo com as setas; entre na masmorra quando estiver pronto." }
            : new[] { $"The road ends at the mouth of the Ossuary. You are in {region}.", "Explore with the arrow keys; enter the dungeon when you are ready." };
    }
}
