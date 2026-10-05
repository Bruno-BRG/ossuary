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
            Rx.Add(R(@"The (.+) bleeds to death\.", "{a1} sangra até morrer."));
            Rx.Add(R(@"The (.+) limps\.", "{a1} manca."));
            Rx.Add(R(@"The (.+) drags itself along\.", "{a1} se arrasta."));
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

