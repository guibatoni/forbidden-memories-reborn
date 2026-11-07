using System;
using System.Collections.Generic;

namespace Reborn.Core
{
    /// <summary>
    /// Fisher-Yates: independent randomized decks at each new match.
    /// Inject System.Random(seed) into tests to reproduce a duel exactly.
    /// Never mutates the registered deck or its card definitions.
    /// </summary>
    public static class DeckRandomizer
    {
        public static IReadOnlyList<CardDefinition> Shuffle(
            IReadOnlyList<CardDefinition> source, Random random)
        {
            if (source == null || source.Count != DuelMatch.DeckSize)
                throw new ArgumentException("Deck must have exactly 40 cards", nameof(source));
            if (random == null) throw new ArgumentNullException(nameof(random));
            var shuffled = new List<CardDefinition>(source);
            for (int i = shuffled.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                var temp = shuffled[i];
                shuffled[i] = shuffled[j];
                shuffled[j] = temp;
            }
            return shuffled;
        }

        public static IReadOnlyList<CardDefinition> ShuffleNewMatch(
            IReadOnlyList<CardDefinition> source)
        {
            return Shuffle(source, new Random(Guid.NewGuid().GetHashCode()));
        }
    }
}
