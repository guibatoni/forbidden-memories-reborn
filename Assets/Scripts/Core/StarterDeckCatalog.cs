using System;
using System.Collections.Generic;
using System.Linq;

namespace Reborn.Core
{
    /// <summary>
    /// Seven authentic, locally imported weighted PS1 starter pools. Unlike
    /// four fixed presets, this generates four alternative deals from the same
    /// seven original distributions. RNG consumption differs from PS1's MIPS
    /// stream; no claim of byte-exact seed reproduction is made.
    /// </summary>
    public sealed class StarterDeckCatalog
    {
        public static readonly int[] DrawCounts = {16, 16, 4, 1, 1, 1, 1};
        private readonly CardDefinition[] cards;
        private readonly int[][] weights;

        public StarterDeckCatalog(IReadOnlyList<CardDefinition> source,
            IReadOnlyList<int[]> sourceWeights)
        {
            if (source == null || source.Count != 722 ||
                sourceWeights == null || sourceWeights.Count != 7)
                throw new ArgumentException("Starter Original requer 722 cartas e 7 pools");
            cards = new CardDefinition[722];
            foreach (var card in source)
            {
                if (card == null || !int.TryParse(card.Id, out int id) ||
                    id < 1 || id > 722 || cards[id - 1] != null)
                    throw new ArgumentException("Cartas devem possuir IDs originais unicos 001..722");
                cards[id-1] = card;
            }
            weights = new int[7][];
            for (int group = 0; group < 7; group++)
            {
                var row = sourceWeights[group];
                if (row == null || row.Length != 722)
                    throw new ArgumentException("Pool inicial incompleto");
                weights[group] = (int[])row.Clone();
                long sum = 0;
                int selectable = 0;
                for (int id = 0; id < 722; id++)
                {
                    if (row[id] < 0 || row[id] > 2048)
                        throw new ArgumentException("Probabilidade fora da tabela PS1");
                    if (row[id] == 0) continue;
                    if (!MatchesGroup(group, cards[id]))
                        throw new ArgumentException("Pool tem carta de categoria divergente: #" + (id+1));
                    sum += row[id];
                    selectable++;
                }
                if (sum != 2048 || selectable < (DrawCounts[group] + 2) / 3)
                    throw new ArgumentException("Pool PS1 incompleto ou impossivel de sortear: " + group);
            }
        }

        private static bool MatchesGroup(int group, CardDefinition card)
        {
            if (card == null) return false;
            if (group < 4)
            {
                if (card.Kind != CardKind.Monster) return false;
                int combined = card.Attack + card.Defense;
                if (group == 0) return combined < 1100;
                if (group == 1) return combined >= 1100 && combined < 1600;
                if (group == 2) return combined >= 1600 && combined < 2100;
                return combined >= 2100;
            }
            if (group == 4 || group == 5) return card.Kind == CardKind.Spell;
            return card.Kind == CardKind.Equip;
        }

        public IReadOnlyList<CardDefinition> Deal(Random rng)
        {
            if (rng == null) throw new ArgumentNullException(nameof(rng));
            var result = new List<CardDefinition>(40);
            var copies = new int[722];
            for (int group = 0; group < DrawCounts.Length; group++)
            {
                for (int draw = 0; draw < DrawCounts[group]; draw++)
                {
                    int sum = 0;
                    var row = weights[group];
                    for (int id = 0; id < 722; id++)
                        if (copies[id] < 3) sum += row[id];
                    if (sum <= 0)
                        throw new InvalidOperationException("Pool inicial esgotado ou invalido");
                    int roll = rng.Next(sum);
                    int chosen = -1;
                    for (int id = 0; id < 722; id++)
                    {
                        if (copies[id] >= 3) continue;
                        roll -= row[id];
                        if (roll < 0) {chosen = id; break;}
                    }
                    if (chosen < 0)
                        throw new InvalidOperationException("Falha na selecao ponderada");
                    copies[chosen]++;
                    result.Add(cards[chosen]);
                }
            }
            if (result.Count != 40)
                throw new InvalidOperationException("Baralho inicial nao totalizou 40 cartas");
            for (int i = result.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i+1);
                var temp = result[i];
                result[i] = result[j];
                result[j] = temp;
            }
            return result;
        }

        public IReadOnlyList<IReadOnlyList<CardDefinition>> FourChoices(Random rng)
        {
            if (rng == null) throw new ArgumentNullException(nameof(rng));
            var results = new List<IReadOnlyList<CardDefinition>>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (int attempt = 0; attempt < 100 && results.Count < 4; attempt++)
            {
                var deck = Deal(rng);
                var signature = string.Join(",", deck.Select(c => c.Id)
                    .OrderBy(id => id, StringComparer.Ordinal));
                if (seen.Add(signature)) results.Add(deck);
            }
            if (results.Count != 4)
                throw new InvalidOperationException(
                    "Pools nao permitem 4 decks iniciais diferentes");
            return results;
        }
    }
}
