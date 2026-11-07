using System;
using System.Collections.Generic;

namespace Reborn.Core
{
    /// <summary>Only explicitly supplied, validated ID-based fusion recipes are executed.
    /// Do not infer a result from names, card types, ATK or visuals.</summary>
    public sealed class FusionCatalog
    {
        private readonly Dictionary<string, CardDefinition> results =
            new Dictionary<string, CardDefinition>(StringComparer.Ordinal);

        private static string Key(string a, string b)
        {
            return string.CompareOrdinal(a, b) <= 0 ? a + "|" + b : b + "|" + a;
        }

        public void Add(string aId, string bId, CardDefinition result)
        {
            if (string.IsNullOrWhiteSpace(aId) || string.IsNullOrWhiteSpace(bId) || result == null)
                throw new ArgumentException("Fusion data incomplete");
            var key = Key(aId, bId);
            if (results.ContainsKey(key)) throw new ArgumentException("Duplicate fusion recipe: " + key);
            results.Add(key, result);
        }

        public bool TryFuse(CardDefinition a, CardDefinition b, out CardDefinition result)
        {
            result = null;
            return a != null && b != null && results.TryGetValue(Key(a.Id, b.Id), out result);
        }

        public int Count => results.Count;
    }
}
