namespace Ossuary.Core
{
    /// <summary>Helpers for text the Core composes in both languages at once (relic owners, history lines).</summary>
    public static partial class Loc
    {
        /// <summary>The Portuguese of a text whatever language is on: for lines stored in both languages when they happen.</summary>
        public static string PtOf(string en)
        {
            var saved = Current;
            Current = Lang.Pt;
            try { return T(en); }
            finally { Current = saved; }
        }

        /// <summary>"um kobold", "uma aranha": the Portuguese indefinite with the noun's gender.</summary>
        public static string Indefinite(string en)
        {
            var r = Resolve(en);
            return r.Known ? (r.Fem ? "uma " : "um ") + r.Pt : en;
        }

        /// <summary>A material in Portuguese, bare ("aço").</summary>
        public static string MaterialPt(string en) => MatPt.TryGetValue(en ?? "", out var v) && v.StartsWith("de ") ? v.Substring(3) : en;

        static readonly System.Collections.Generic.Dictionary<string, string> LookPt = new System.Collections.Generic.Dictionary<string, string>
        {
            // potion moods and colours (after "poção", feminine)
            ["murky"] = "turva", ["bubbling"] = "borbulhante", ["smoky"] = "esfumaçada", ["milky"] = "leitosa", ["fizzing"] = "efervescente",
            ["viscous"] = "viscosa", ["cloudy"] = "nublada", ["glowing"] = "brilhante", ["oily"] = "oleosa", ["brackish"] = "salobra",
            ["clear"] = "límpida", ["swirling"] = "rodopiante", ["thick"] = "espessa", ["foaming"] = "espumante", ["sparkling"] = "cintilante", ["dull"] = "opaca",
            ["amber"] = "âmbar", ["crimson"] = "carmim", ["inky"] = "negra como tinta", ["golden"] = "dourada", ["pearl"] = "perolada", ["rust"] = "cor de ferrugem",
            ["bone"] = "cor de osso", ["violet"] = "violeta", ["green"] = "verde", ["black"] = "preta", ["blue"] = "azul", ["silver"] = "prateada",
            ["pink"] = "rosa", ["grey"] = "cinza", ["orange"] = "laranja", ["white"] = "branca",
            // wand shapes (after "varinha", feminine) and what they are made of
            ["twisted"] = "retorcida", ["straight"] = "reta", ["knotted"] = "nodosa", ["carved"] = "entalhada", ["slender"] = "delgada",
            ["heavy"] = "pesada", ["runed"] = "rúnica", ["forked"] = "bifurcada",
            ["w:oak"] = "carvalho", ["w:bone"] = "osso", ["w:iron"] = "ferro", ["w:glass"] = "vidro", ["w:ebony"] = "ébano", ["w:copper"] = "cobre",
            ["w:crystal"] = "cristal", ["w:ivory"] = "marfim", ["w:pine"] = "pinho", ["w:jade"] = "jade", ["w:brass"] = "latão", ["w:marble"] = "mármore",
            ["w:yew"] = "teixo", ["w:ash"] = "freixo", ["w:tin"] = "estanho", ["w:horn"] = "chifre",
        };

        /// <summary>Portuguese for what an unknown potion, wand or scroll looks like (Items/Appearances.cs), or null.</summary>
        static (string Pt, bool Fem)? AppearancePt(string en)
        {
            if (en.StartsWith("scroll labelled ", System.StringComparison.Ordinal)) return ("pergaminho com a inscrição " + en.Substring(16), false);
            var w = en.Split(' ');
            if (w.Length != 3) return null;
            if (w[2] == "potion" && LookPt.TryGetValue(w[0], out var mood) && LookPt.TryGetValue(w[1], out var colour)) return ("poção " + colour + " " + mood, true);
            if (w[2] == "wand" && LookPt.TryGetValue(w[0], out var shape) && LookPt.TryGetValue("w:" + w[1], out var stuff)) return ("varinha " + shape + " de " + stuff, true);
            return null;
        }
    }
}
