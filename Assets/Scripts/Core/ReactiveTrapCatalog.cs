using System;
using System.Collections.Generic;

namespace Reborn.Core
{
    /// <summary>
    /// Only the two documented original PS1 magic-response traps.
    /// Reverse Trap and Fake Trap have different triggers; they are NOT
    /// activated by this catalog.
    /// </summary>
    public enum ReactiveTrapOperation
    {
        ReflectDirectSpellDamage,
        ReverseSpellHealing
    }

    public sealed class ReactiveTrapCatalog
    {
        private readonly Dictionary<string, ReactiveTrapOperation> rules =
            new Dictionary<string, ReactiveTrapOperation>(StringComparer.Ordinal);

        public int Count => rules.Count;

        public void Add(string id, ReactiveTrapOperation operation)
        {
            if (string.IsNullOrWhiteSpace(id) || rules.ContainsKey(id) ||
                !Enum.IsDefined(typeof(ReactiveTrapOperation), operation))
                throw new ArgumentException("Regra reativa duplicada ou invalida");
            rules.Add(id, operation);
        }

        public bool Matches(CardDefinition card, ReactiveTrapOperation operation)
        {
            return card != null && card.Kind == CardKind.Trap &&
                rules.TryGetValue(card.Id, out var actual) &&
                actual == operation;
        }

        public static ReactiveTrapCatalog OriginalPs1()
        {
            var result = new ReactiveTrapCatalog();
            result.Add("687", ReactiveTrapOperation.ReflectDirectSpellDamage);
            result.Add("688", ReactiveTrapOperation.ReverseSpellHealing);
            return result;
        }
    }
}
