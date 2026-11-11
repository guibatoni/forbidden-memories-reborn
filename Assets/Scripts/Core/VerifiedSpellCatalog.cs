using System;
using System.Collections.Generic;

namespace Reborn.Core
{
    /// <summary>Small, auditable operations from the original PS1 game.
    /// Text imported from DUELTRACE is informational and never executable.
    /// No equip/trap/field behavior is inferred from names or descriptions.</summary>
    public enum VerifiedSpellOperation
    {
        RestoreLife,
        DirectDamage,
        DestroyEnemyMonsters,
        DestroyAllFieldCards,
        SetTerrain
    }

    public sealed class VerifiedSpellRule
    {
        public string CardId { get; }
        public VerifiedSpellOperation Operation { get; }
        public int Amount { get; }

        public VerifiedSpellRule(string id, VerifiedSpellOperation operation, int amount = 0)
        {
            if (string.IsNullOrWhiteSpace(id) || amount < 0)
                throw new ArgumentException("Regra de magia invalida");
            if ((operation == VerifiedSpellOperation.RestoreLife ||
                 operation == VerifiedSpellOperation.DirectDamage) && amount <= 0)
                throw new ArgumentException("Efeito de LP exige valor positivo");
            if ((operation == VerifiedSpellOperation.DestroyEnemyMonsters ||
                 operation == VerifiedSpellOperation.DestroyAllFieldCards) && amount != 0)
                throw new ArgumentException("Efeito de destruicao nao aceita pontos");
            CardId = id;
            Operation = operation;
            Amount = amount;
        }
    }

    /// <summary>
    /// An explicit allowlist of card IDs and verified PS1 behaviors.
    /// Cards that merely have effect descriptions are NOT executable.
    /// </summary>
    public sealed class VerifiedSpellCatalog
    {
        private readonly Dictionary<string, VerifiedSpellRule> rules =
            new Dictionary<string, VerifiedSpellRule>(StringComparer.Ordinal);
        public int Count => rules.Count;

        public void Add(string id, VerifiedSpellOperation operation, int amount = 0)
        {
            if (rules.ContainsKey(id))
                throw new ArgumentException("Efeito duplicado: " + id);
            rules.Add(id, new VerifiedSpellRule(id, operation, amount));
        }

        public bool TryGet(CardDefinition card, out VerifiedSpellRule rule)
        {
            rule = null;
            return card != null && card.Kind == CardKind.Spell &&
                   rules.TryGetValue(card.Id, out rule);
        }

        /// <summary>
        /// Original retail IDs only. Give this catalog to duels with a
        /// locally identified Original PS1 edition, not other arbitrary mods.
        /// Other magic/trap/equip behavior requires individually validated
        /// imports and new engine implementations.
        /// </summary>
        public static VerifiedSpellCatalog OriginalPs1Core()
        {
            var result = new VerifiedSpellCatalog();
            result.Add("330", VerifiedSpellOperation.SetTerrain);
            result.Add("331", VerifiedSpellOperation.SetTerrain);
            result.Add("332", VerifiedSpellOperation.SetTerrain);
            result.Add("333", VerifiedSpellOperation.SetTerrain);
            result.Add("334", VerifiedSpellOperation.SetTerrain);
            result.Add("335", VerifiedSpellOperation.SetTerrain);
            result.Add("336", VerifiedSpellOperation.DestroyAllFieldCards);
            result.Add("337", VerifiedSpellOperation.DestroyEnemyMonsters);
            result.Add("338", VerifiedSpellOperation.RestoreLife, 200);
            result.Add("339", VerifiedSpellOperation.RestoreLife, 500);
            result.Add("340", VerifiedSpellOperation.RestoreLife, 1000);
            result.Add("341", VerifiedSpellOperation.RestoreLife, 2000);
            result.Add("342", VerifiedSpellOperation.RestoreLife, 5000);
            result.Add("343", VerifiedSpellOperation.DirectDamage, 50);
            result.Add("344", VerifiedSpellOperation.DirectDamage, 100);
            result.Add("345", VerifiedSpellOperation.DirectDamage, 200);
            result.Add("346", VerifiedSpellOperation.DirectDamage, 500);
            result.Add("347", VerifiedSpellOperation.DirectDamage, 1000);
            return result;
        }
    }
}
