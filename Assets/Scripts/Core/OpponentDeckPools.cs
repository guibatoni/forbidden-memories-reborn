using System;
using System.Collections.Generic;
using System.Globalization;

namespace Reborn.Core
{
    /// <summary>
    /// Weighted deck SOURCE extracted by the user from their own disc.
    /// This prototype reproduces weighted draws with a three-copy limit,
    /// not the retail game's exact PRNG or initial deck algorithm.
    /// </summary>
    public sealed class WeightedOpponentPool
    {
        private readonly int[] weights;
        public int Id { get; }
        public string Name { get; }
        public string DeckKey { get; }

        public WeightedOpponentPool(int id, string name, string deckKey, IReadOnlyList<int> weightsByCardId)
        {
            if (id < 0) throw new ArgumentOutOfRangeException(nameof(id));
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Opponent name required", nameof(name));
            if (string.IsNullOrWhiteSpace(deckKey)) throw new ArgumentException("Deck key required", nameof(deckKey));
            if (weightsByCardId == null || weightsByCardId.Count != 722)
                throw new ArgumentException("Deck weights must contain 722 ID-indexed values", nameof(weightsByCardId));
            weights = new int[722];
            int nonzero = 0;
            for (int i = 0; i < 722; i++)
            {
                if (weightsByCardId[i] < 0) throw new ArgumentException("Negative weight at card ID " + (i + 1));
                weights[i] = weightsByCardId[i];
                if (weights[i] > 0) nonzero++;
            }
            if (nonzero * 3 < DuelMatch.DeckSize)
                throw new ArgumentException("Pool cannot deal 40 cards with a three-copy cap");
            Id = id; Name = name; DeckKey = deckKey;
        }

        public int WeightForCardId(int cardId)
        {
            if (cardId < 1 || cardId > 722) throw new ArgumentOutOfRangeException(nameof(cardId));
            return weights[cardId - 1];
        }

        public IReadOnlyList<CardDefinition> Deal(IReadOnlyList<CardDefinition> catalog, Random random)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            if (random == null) throw new ArgumentNullException(nameof(random));
            var byId = new Dictionary<int, CardDefinition>();
            foreach (var card in catalog)
            {
                if (card == null) throw new ArgumentException("Null catalog card");
                if (!int.TryParse(card.Id, NumberStyles.None, CultureInfo.InvariantCulture, out int id) ||
                    id < 1 || id > 722 || byId.ContainsKey(id))
                    throw new ArgumentException("Invalid or duplicated catalog card ID: " + card.Id);
                byId.Add(id, card);
            }
            var cards = new CardDefinition[722];
            var copies = new int[722];
            for (int id = 1; id <= 722; id++)
            {
                if (weights[id - 1] == 0) continue;
                if (!byId.TryGetValue(id, out var card))
                    throw new ArgumentException("Pool references missing card ID " + id);
                cards[id - 1] = card;
            }
            var result = new List<CardDefinition>(DuelMatch.DeckSize);
            for (int draw = 0; draw < DuelMatch.DeckSize; draw++)
            {
                long total = 0;
                for (int i = 0; i < 722; i++)
                    if (copies[i] < 3) total += weights[i];
                if (total <= 0) throw new InvalidOperationException("Pool exhausted before 40 cards");
                double choice = random.NextDouble() * total;
                long cumulative = 0;
                int selected = -1;
                for (int i = 0; i < 722; i++)
                {
                    if (copies[i] >= 3 || weights[i] == 0) continue;
                    cumulative += weights[i];
                    if (choice < cumulative) { selected = i; break; }
                }
                if (selected < 0 || cards[selected] == null)
                    throw new InvalidOperationException("Invalid weighted selection");
                result.Add(cards[selected]);
                copies[selected]++;
            }
            return result.AsReadOnly();
        }
    }

    public sealed class OpponentDeckCatalog
    {
        private readonly List<WeightedOpponentPool> pools = new List<WeightedOpponentPool>();
        public string SourceSha256 { get; }
        public IReadOnlyList<WeightedOpponentPool> Pools => pools.AsReadOnly();

        public OpponentDeckCatalog(string sourceSha256, IEnumerable<WeightedOpponentPool> sourcePools)
        {
            if (string.IsNullOrWhiteSpace(sourceSha256) || sourceSha256.Length != 64)
                throw new ArgumentException("A disc SHA-256 is required");
            if (sourcePools == null) throw new ArgumentNullException(nameof(sourcePools));
            var ids = new HashSet<int>();
            var keys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var pool in sourcePools)
            {
                if (pool == null || !ids.Add(pool.Id) || !keys.Add(pool.DeckKey))
                    throw new ArgumentException("Missing or duplicate opponent deck pool");
                pools.Add(pool);
            }
            if (pools.Count == 0) throw new ArgumentException("No opponent deck pools");
            SourceSha256 = sourceSha256;
        }
    }
}
