using System;
using System.Collections.Generic;
using Ossuary.Core.Entities;

namespace Ossuary.Core
{
    public enum TownEventKind { None, Market, Festival, Funeral, Theft, Plague }

    /// <summary>
    /// Things that happen in a town whether or not the hero is watching: a deterministic schedule (town, seed, two-day bucket).
    /// Each changes what is for sale, what people say and what is open, and the theft opens a job for the Watch.
    /// </summary>
    public sealed partial class Game
    {
        /// <summary>What is going on in this town today. Pure function of the seed, the town and the day (events last two days).</summary>
        public TownEventKind TownEventToday()
        {
            if (Town == null || World == null) return TownEventKind.None;
            return TownEventOn(World.Day);
        }

        public static string EventAnnouncement(TownEventKind k)
        {
            switch (k)
            {
                case TownEventKind.Market: return TownText.L("It is market day: stalls in the square, and the prices are kinder.", "É dia de feira: barracas na praça, e os preços estão mais gentis.");
                case TownEventKind.Festival: return TownText.L("A festival: lanterns, a bard on every corner, and the ale is cheap.", "Um festival: lanternas, um bardo em cada esquina, e a cerveja está barata.");
                case TownEventKind.Funeral: return TownText.L("A funeral is under way. The temple is shut to everything but the dead.", "Há um funeral em andamento. O templo está fechado para tudo, menos os mortos.");
                case TownEventKind.Theft: return TownText.L("There has been a robbery. The guards are asking questions, and the Captain wants help.", "Houve um roubo. Os guardas fazem perguntas, e o Capitão quer ajuda.");
                case TownEventKind.Plague: return TownText.L("A fever is in the town. The inn turns travellers away, and nobody eats at the tavern.", "Há uma febre na cidade. A estalagem recusa viajantes, e ninguém come na taverna.");
            }
            return "";
        }

        public static string EventTitle(TownEventKind k)
        {
            switch (k)
            {
                case TownEventKind.Market: return TownText.L("Market day", "Dia de feira");
                case TownEventKind.Festival: return TownText.L("Festival", "Festival");
                case TownEventKind.Funeral: return TownText.L("Funeral", "Funeral");
                case TownEventKind.Theft: return TownText.L("Robbery", "Roubo");
                case TownEventKind.Plague: return TownText.L("Fever", "Febre");
            }
            return "";
        }

        /// <summary>Percent off services today (a market day or a festival).</summary>
        int EventDiscount()
        {
            if (Mode != GameMode.TownMap) return 0;
            var k = TownEventToday();
            return k == TownEventKind.Market ? 10 : k == TownEventKind.Festival ? 20 : 0;
        }

        bool EventBlocks(string service)
        {
            if (Mode != GameMode.TownMap) return false;
            var k = TownEventToday();
            if (k == TownEventKind.Funeral) return service == "heal" || service == "cure" || service == "purge";
            if (k == TownEventKind.Plague) return service == "rest" || service == "meal";
            return false;
        }

        /// <summary>The Watch's job born of a robbery: unique per town and bucket, so it can be taken once.</summary>
        public QuestDef TheftQuest()
        {
            if (Town == null) return null;
            string id = "watch.thief." + Town.Name + "." + (World.Day / 2);
            var def = new QuestDef { Id = id, Track = QuestDef.Watch, Giver = "the Captain of the Watch", Deadline = 6, RewardGold = 45,
                Title = TownText.L("Find the thief", "Ache o ladrão") };
            def.Step(ObjKind.Kill, TownText.L("Hunt down two kobolds that fenced the loot.", "Cace dois kobolds que revenderam o saque."), "kobold", 2, null, Town.Name)
               .Step(ObjKind.Flag, TownText.L("Tell the Captain the thief is dealt with.", "Diga ao Capitão que o ladrão foi resolvido."), id + ".report", 1, null, Town.Name);
            def.OnComplete = g => { g.AddRep(Houses.Watch, 8, "catching the thief"); g.RecordDeed(Deed.Helped, g.Town != null ? g.Town.Name : "", 2); };
            def.OnFail = g => g.AddRep(Houses.Watch, -3, "letting the thief go");
            return def;
        }
    }
}
