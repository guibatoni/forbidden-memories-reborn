using System;
using System.Collections.Generic;

namespace Reborn.Core
{
    /// <summary>
    /// Original PS1 ritual definition: one ritual card, three exact monster
    /// IDs already face-up on the field, one monster result. This is NOT the
    /// modern OCG/TCG level-sum or ritual-monster-in-hand mechanic.
    /// </summary>
    public sealed class RitualRecipe
    {
        public string RitualCardId { get; }
        public string[] TributeIds { get; }
        public CardDefinition Result { get; }

        public RitualRecipe(string ritualId, string[] tributeIds,
            CardDefinition result)
        {
            if (string.IsNullOrWhiteSpace(ritualId) ||
                tributeIds == null || tributeIds.Length != 3 ||
                result == null || result.Kind != CardKind.Monster)
                throw new ArgumentException("Original ritual data must have 3 monsters and one result");
            // Some retail recipes request three COPIES of the same card ID.
            // The duel engine requires distinct field slots, not distinct IDs.
            foreach (var tribute in tributeIds)
            {
                if (string.IsNullOrWhiteSpace(tribute))
                    throw new ArgumentException("Ritual material ID cannot be empty");
            }
            RitualCardId = ritualId;
            TributeIds = (string[])tributeIds.Clone();
            Result = result;
        }
    }

    /// <summary>Immutable-usage catalog populated only by the user's local export.</summary>
    public sealed class RitualCatalog
    {
        private readonly Dictionary<string, RitualRecipe> recipes =
            new Dictionary<string, RitualRecipe>(StringComparer.Ordinal);

        public void Add(string ritualId, string[] tributeIds, CardDefinition result)
        {
            if (recipes.ContainsKey(ritualId))
                throw new ArgumentException("Duplicate ritual card: " + ritualId);
            recipes.Add(ritualId, new RitualRecipe(ritualId, tributeIds, result));
        }

        public bool TryGet(string ritualId, out RitualRecipe recipe)
        {
            recipe = null;
            return !string.IsNullOrWhiteSpace(ritualId) &&
                recipes.TryGetValue(ritualId, out recipe);
        }

        public int Count => recipes.Count;
    }
}
