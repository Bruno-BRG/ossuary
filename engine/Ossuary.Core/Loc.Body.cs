using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Ossuary.Core
{
    /// <summary>
    /// Portuguese for bodies and wounds (Game.Wounds.cs). Part names carry their gender, and the wound adjective agrees
    /// with it: "Sua perna esquerda está quebrada", "O braço direito do kobold está cortado".
    /// </summary>
    public static partial class Loc
    {
        static readonly Dictionary<string, (string Pt, bool Fem)> BodyParts = new Dictionary<string, (string, bool)>
        {
            ["head"] = ("cabeça", true), ["torso"] = ("tronco", false), ["body"] = ("corpo", false),
            ["right arm"] = ("braço direito", false), ["left arm"] = ("braço esquerdo", false),
            ["right leg"] = ("perna direita", true), ["left leg"] = ("perna esquerda", true),
            ["right eye"] = ("olho direito", false), ["left eye"] = ("olho esquerdo", false),
            ["front left leg"] = ("pata dianteira esquerda", true), ["front right leg"] = ("pata dianteira direita", true),
            ["hind left leg"] = ("pata traseira esquerda", true), ["hind right leg"] = ("pata traseira direita", true),
            ["tail"] = ("cauda", true), ["thorax"] = ("tórax", false), ["abdomen"] = ("abdômen", false),
            ["foreleg"] = ("pata da frente", true), ["middle leg"] = ("pata do meio", true), ["hind leg"] = ("pata de trás", true),
            ["left wing"] = ("asa esquerda", true), ["right wing"] = ("asa direita", true),
        };

        /// <summary>Masculine forms; the feminine swaps the final o for a.</summary>
        static readonly Dictionary<string, string> WoundAdj = new Dictionary<string, string>
        {
            ["grazed"] = "arranhado", ["cut"] = "cortado", ["torn"] = "dilacerado", ["mangled"] = "destroçado",
            ["bruised"] = "machucado", ["battered"] = "contundido", ["broken"] = "quebrado", ["crushed"] = "esmagado",
            ["severed"] = "decepado",
        };

        static string Agree(string adj, bool fem) => fem ? adj.Substring(0, adj.Length - 1) + "a" : adj;

        static void AddBodyText()
        {
            foreach (var kv in BodyParts)
            {
                string part = kv.Value.Pt, art = kv.Value.Fem ? "A" : "O", your = kv.Value.Fem ? "Sua" : "Seu";
                Pt[kv.Key] = part;
                Pt[$"Your {kv.Key} has healed."] = $"{your} {part} sarou.";
                Pt[$"Your {kv.Key} has healed, leaving a scar."] = $"{your} {part} sarou e deixou uma cicatriz.";
                Pt[$"Eyes linger on the scar on your {kv.Key}."] = $"Os olhares se demoram na cicatriz {(kv.Value.Fem ? "da sua" : "do seu")} {part}.";
                foreach (var a in WoundAdj)
                {
                    string adj = Agree(a.Value, kv.Value.Fem);
                    Pt[$"Your {kv.Key} is {a.Key}."] = $"{your} {part} está {adj}.";
                    Pt[$"Its {kv.Key} is {a.Key}."] = $"{art} {part} está {adj}.";
                    Pt[$"{kv.Key} {a.Key}"] = $"{part} {adj}";
                }
            }
            foreach (var (en, pt) in new[] {
                ("You are bleeding.", "Você está sangrando."), ("Your bleeding stops.", "Seu sangramento para."),
                ("You reel from the blow to your head.", "Você cambaleia com o golpe na cabeça."),
                ("You can only limp now.", "Agora você só consegue mancar."), ("You can barely crawl.", "Você mal consegue se arrastar."),
                ("It is limping.", "Está mancando."), ("It is crawling.", "Está se arrastando."), ("It is bleeding.", "Está sangrando."),
                ("(bleeding)", "(sangrando)"), ("Wounds", "Ferimentos"), ("Scars", "Cicatrizes"), ("Body", "Corpo"),
                ("BLEEDING", "SANGRANDO"), ("LIMPING", "MANCANDO"), ("CRAWLING", "RASTEJANDO"), ("WOUNDED", "FERIDO"),
                ("blood loss", "perda de sangue") })
                Pt[en] = pt;
            foreach (var (en, pt) in new[] {
                ("It is blind.", "Está cego."), ("It cannot fly.", "Não consegue voar."), ("Its weapon arm is useless.", "O braço da arma está inutilizado."),
                ("You have no wounds to bind.", "Você não tem ferimentos para enfaixar."), ("Your wounds are already bound.", "Seus ferimentos já estão enfaixados."),
                ("You bind your wounds, and the bleeding stops.", "Você enfaixa os ferimentos, e o sangramento para."), ("You bind your wounds.", "Você enfaixa os ferimentos."),
                ("The flames sear your wounds shut.", "As chamas cauterizam seus ferimentos."),
                ("You stop aiming and strike wherever you can.", "Você para de mirar e golpeia onde puder."), ("Something is in the way.", "Há algo no caminho."),
                ("The drills assume years in a shield wall you never stood in.", "Os exercícios pressupõem anos numa parede de escudos onde você nunca esteve."),
                ("You already know every strike in the manual.", "Você já conhece todos os golpes do manual."),
                ("The scar on your face will draw looks. (-1 Cha)", "A cicatriz no rosto vai atrair olhares. (-1 Car)"),
                ("Aim at a body part", "Mirar numa parte do corpo"), ("AIM", "MIRA"), ("HEAD", "CABEÇA"), ("ARM", "BRAÇO"), ("LEG", "PERNA"),
                ("EYE", "OLHO"), ("WING", "ASA"), ("TAIL", "CAUDA"),
                ("Hamstring", "Jarrete"), ("Disarming Blow", "Golpe Desarmante"), ("Skull Crack", "Racha-Crânio"), ("Lunge", "Estocada"),
                ("Cut low at the legs: the wound there is a step worse.", "Corte baixo nas pernas: o ferimento ali fica um grau pior."),
                ("Go for the weapon arm, two steps worse; mangled, it drops its weapon.", "Mire no braço da arma, dois graus pior; destroçado, ele larga a arma."),
                ("A blow to the head, two steps worse: it reels, stunned.", "Um golpe na cabeça, dois graus pior: ele cambaleia, atordoado."),
                ("Close two squares in one stride and strike for double damage.", "Avance duas casas numa passada e golpeie com dano dobrado.") })
                Pt[en] = pt;
            foreach (var aim in Bodies.AimCycle)
            {
                if (aim == null) continue;
                string where = aim.Value == PartKind.Head ? "a cabeça" : aim.Value == PartKind.Arm ? "os braços" : aim.Value == PartKind.Leg ? "as pernas"
                    : aim.Value == PartKind.Eye ? "os olhos" : aim.Value == PartKind.Wing ? "as asas" : "a cauda";
                int pen = Bodies.AimPenalty(aim);
                Pt[$"You aim for {Bodies.AimName(aim)} (-{pen} to hit)."] = $"Você mira {where} (-{pen} para acertar).";
            }
            Rx.Add(R(@"The (.+) bleeds to death\.", "{a1} sangra até morrer."));
            Rx.Add(R(@"The (.+) limps\.", "{a1} manca."));
            Rx.Add(R(@"The (.+) drags itself along\.", "{a1} se arrasta."));
            Rx.Add(R(@"The (.+) crashes to the ground\.", "{a1} despenca no chão."));
            Rx.Add(R(@"The (.+) is blinded\.", "{a1} perde a visão."));
            Rx.Add(R(@"The (.+) turns to flee\.", "{a1} se vira para fugir."));
            Rx.Add(R(@"The (.+) drops its weapon\.", "{a1} larga a arma."));
            Rx.Add(R(@"The (.+) can no longer swing that arm\.", "{a1} não consegue mais usar aquele braço."));
            Rx.Add(R(@"The (.+) swings wildly at nothing\.", "{a1} golpeia o vazio às cegas."));
            Rx.Add(R(@"The blind (.+) hits the (.+) for (\d+)\.", "{a1}, às cegas, acerta {a2}: $3 de dano."));
            Rx.Add(R(@"The blind (.+) lashes out at the (.+) and misses\.", "{a1}, às cegas, ataca {a2} e erra."));
            Rx.Add(R(@"The flames seal the wounds of the (.+)\.", "As chamas fecham as feridas {d1}."));
            Rx.Add(R(@"Your grip fails, and you drop (.+)\.", "Sua mão falha, e você larga {a1}."));
            Rx.Add(R(@"You know every drill you are ready for\. The next asks for level (\d+)\.", "Você já sabe todos os exercícios do seu nível. O próximo pede o nível $1."));
        }

        static readonly Regex BodyRx = new Regex(@"^(?:The (.+)|(.+))'s (.+) is (\w+)\.$", RegexOptions.CultureInvariant);

        /// <summary>"The jackal's left leg is broken." with the part's gender carried into the adjective and the "do/da" of the owner.</summary>
        static string BodyLine(string en)
        {
            if (en.IndexOf("'s ", System.StringComparison.Ordinal) < 0) return null;
            var m = BodyRx.Match(en);
            if (!m.Success || !BodyParts.TryGetValue(m.Groups[3].Value, out var part) || !WoundAdj.TryGetValue(m.Groups[4].Value, out var adj)) return null;
            string owner;
            if (m.Groups[1].Success)
            {
                var res = Resolve(m.Groups[1].Value);
                owner = res.Known ? (res.Fem ? "da " : "do ") + res.Pt : "de " + (UCore(m.Groups[1].Value) ?? m.Groups[1].Value);
            }
            else owner = "de " + m.Groups[2].Value;
            return Finish(Capital($"{(part.Fem ? "a" : "o")} {part.Pt} {owner} está {Agree(adj, part.Fem)}."));
        }
    }
}
