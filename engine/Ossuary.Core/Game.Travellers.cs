using System;
using System.Collections.Generic;
using Ossuary.Core.Entities;

namespace Ossuary.Core
{
    /// <summary>
    /// Other people on the road and in the dark: road events with a pilgrim, a peddler, refugees and a wounded delver (Game.Events.cs),
    /// and the hired sword who comments on where you take them. Nothing here touches the simulation's random stream in town or on levels.
    /// </summary>
    public sealed partial class Game
    {
        static readonly Dictionary<string, string[]> CompanionLines = new Dictionary<string, string[]>
        {
            ["sellsword"] = new[]
            {
                TownText.L("\"Deeper? You are the one paying.\"", "\"Mais fundo? Quem paga é você.\""),
                TownText.L("\"My last employer went down this way. I still have his boots.\"", "\"Meu último patrão desceu por aqui. Ainda tenho as botas dele.\""),
                TownText.L("\"Keep the torch high. I like to see what is about to kill me.\"", "\"Mantenha a tocha alta. Gosto de ver o que vai me matar.\""),
            },
            ["shield-bearer"] = new[]
            {
                TownText.L("\"Stay behind me. That is not a request.\"", "\"Fique atrás de mim. Não é um pedido.\""),
                TownText.L("\"I have buried better people than the thing waiting down here.\"", "\"Enterrei gente melhor do que o que espera aqui embaixo.\""),
                TownText.L("\"Another stair. Another reason to hold the line.\"", "\"Outra escada. Outra razão para segurar a linha.\""),
            },
            ["cutthroat"] = new[]
            {
                TownText.L("\"Quiet now. The best fights are the ones that never start.\"", "\"Silêncio. As melhores lutas são as que nunca começam.\""),
                TownText.L("\"Whatever is down there owns something worth taking. I can smell it.\"", "\"O que tem lá embaixo guarda algo que vale a pena levar. Sinto o cheiro.\""),
                TownText.L("\"If this goes badly, I was never here.\"", "\"Se der errado, eu nunca estive aqui.\""),
            },
        };

        /// <summary>A hired follower says something about the new depth (every other level, by their role).</summary>
        void CompanionRemark()
        {
            if (Companions.Count == 0 || Depth <= 0 || Depth % 2 != 0) return;
            var c = Companions[0];
            if (c == null || c.IsDead || c.CompanionRole == null || !CompanionLines.TryGetValue(c.CompanionRole, out var pool)) return;
            Say($"{c.Name}: " + Loc.T(pool[(Depth / 2) % pool.Length]), MessageKind.Narrative);
        }
    }
}
