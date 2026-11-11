using System;
using System.Collections.Generic;

namespace Reborn.Core
{
    /// <summary>
    /// PS1 six ATK-threshold trap cards. Thresholds originate from the
    /// executable's ascending 6-byte table (units of 100 ATK).
    /// Other trap types are NOT silently treated as attack traps.
    /// </summary>
    public sealed class AttackTrapCatalog
    {
        private readonly Dictionary<string, int> thresholdById =
            new Dictionary<string, int>(StringComparer.Ordinal);

        public int Count => thresholdById.Count;
        public void Add(string id, int maximumAttack)
        {
            if (string.IsNullOrWhiteSpace(id) || maximumAttack < 0 ||
                maximumAttack > 65535)
                throw new ArgumentException("Limite de Trap invalido");
            if (thresholdById.ContainsKey(id))
                throw new ArgumentException("Trap duplicada: " + id);
            thresholdById.Add(id, maximumAttack);
        }

        public bool TryMatch(CardDefinition card, int attack, out int threshold)
        {
            threshold = 0;
            return card != null && card.Kind == CardKind.Trap &&
                thresholdById.TryGetValue(card.Id, out threshold) &&
                attack <= threshold;
        }

        public static AttackTrapCatalog OriginalPs1()
        {
            var result = new AttackTrapCatalog();
            result.Add("681", 500);
            result.Add("682", 1000);
            result.Add("683", 1500);
            result.Add("684", 2000);
            result.Add("685", 3000);
            result.Add("686", 25500);
            return result;
        }
    }
}
