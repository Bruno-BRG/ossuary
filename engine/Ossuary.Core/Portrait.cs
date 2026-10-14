using Ossuary.Core.Entities;

namespace Ossuary.Core
{
    /// <summary>
    /// The face in the conversation box: a hat from the trade, eyes from the temperament, a mouth from the mood.
    /// Four rows, five cells each, in ASCII so the bitmap font carries it. A pure function of role, persona and memory,
    /// so it never touches the Rng and is the same every time the box opens.
    /// </summary>
    public static class Portrait
    {
        public const int Width = 5, Height = 4;

        static readonly string Warm = TownText.L("mood: warm", "humor: cordial"), Neutral = TownText.L("mood: neutral", "humor: neutro"),
            Wary = TownText.L("mood: wary", "humor: desconfiado"), Cold = TownText.L("mood: cold", "humor: frio");

        /// <summary>The four rows of the face, top to bottom, each <see cref="Width"/> cells wide.</summary>
        public static string[] Rows(Monster m)
        {
            var trait = m.Persona != null ? m.Persona.Trait : Trait.Kind;
            string mood = m.Memory != null ? m.Memory.MoodKey : "neutral";
            return new[] { Hat(m.Role), "(" + Eyes(trait) + ")", "(" + Mouth(mood) + ")", " /|\\ " };
        }

        /// <summary>The mood as a tag for the box, translated: "mood: warm" / "humor: cordial".</summary>
        public static string MoodTag(Monster m)
        {
            switch (m.Memory != null ? m.Memory.MoodKey : "neutral")
            {
                case "warm": return Warm;
                case "wary": return Wary;
                case "cold": return Cold;
                default: return Neutral;
            }
        }

        /// <summary>The temperament as a tag for the box, translated: "temper: kind" / "temperamento: gentil".</summary>
        public static string TemperTag(Trait t)
        {
            switch (t)
            {
                case Trait.Kind: return TownText.L("temper: kind", "temperamento: gentil");
                case Trait.Bitter: return TownText.L("temper: bitter", "temperamento: amargo");
                case Trait.Proud: return TownText.L("temper: proud", "temperamento: orgulhoso");
                case Trait.Coward: return TownText.L("temper: timid", "temperamento: medroso");
                case Trait.Greedy: return TownText.L("temper: greedy", "temperamento: ganancioso");
                case Trait.Pious: return TownText.L("temper: pious", "temperamento: devoto");
                case Trait.Curious: return TownText.L("temper: curious", "temperamento: curioso");
                default: return TownText.L("temper: weary", "temperamento: exausto");
            }
        }

        static string Hat(TownRole role)
        {
            switch (role)
            {
                case TownRole.Guard: case TownRole.Captain: return "/^^^\\";
                case TownRole.Priest: case TownRole.Elder: return "_/^\\_";
                case TownRole.Innkeeper: case TownRole.Barkeep: return ".---.";
                case TownRole.Smith: return "_###_";
                case TownRole.Shopkeeper: return "_===_";
                case TownRole.Scholar: return "=---=";
                case TownRole.Bard: return "*~~~*";
                default: return "  _  ";
            }
        }

        static string Eyes(Trait t)
        {
            switch (t)
            {
                case Trait.Bitter: return "- -";
                case Trait.Proud: return "^ ^";
                case Trait.Coward: return "; ;";
                case Trait.Greedy: return "$ $";
                case Trait.Pious: return "~ ~";
                case Trait.Curious: return "O O";
                case Trait.Weary: return "= =";
                default: return "o o";
            }
        }

        static string Mouth(string mood)
        {
            switch (mood)
            {
                case "warm": return "\\_/";
                case "wary": return " o ";
                case "cold": return "/^\\";
                default: return " _ ";
            }
        }
    }
}
