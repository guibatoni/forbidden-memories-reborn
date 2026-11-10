using System;
using System.Collections.Generic;

namespace Reborn.Core
{
    /// <summary>
    /// Documented PS1 equip effect. Retail cards disappear after application,
    /// permanently increasing both statistics on the affected field monster.
    /// Compatibilities must be explicitly verified (all or listed monster IDs).
    /// </summary>
    public sealed class VerifiedEquipRule
    {
        public string EquipCardId { get; }
        public int AttackGain { get; }
        public int DefenseGain { get; }
        public bool AnyMonster { get; }
        private readonly HashSet<string> eligible;

        internal VerifiedEquipRule(string equipId, int attack, int defense,
            bool anyMonster, IReadOnlyList<string> eligibleMonsterIds)
        {
            if (string.IsNullOrWhiteSpace(equipId) ||
                attack <= 0 || defense <= 0 ||
                attack > 10000 || defense > 10000)
                throw new ArgumentException("Bonus de Equip invalido");
            if (anyMonster && eligibleMonsterIds != null && eligibleMonsterIds.Count > 0)
                throw new ArgumentException("Regra universal nao admite alvos especificos");
            if (!anyMonster && (eligibleMonsterIds == null ||
                                eligibleMonsterIds.Count == 0))
                throw new ArgumentException("Compatibilidade especifica exige alvos");
            EquipCardId = equipId;
            AttackGain = attack;
            DefenseGain = defense;
            AnyMonster = anyMonster;
            eligible = new HashSet<string>(StringComparer.Ordinal);
            if (!anyMonster)
            {
                foreach (var id in eligibleMonsterIds)
                {
                    if (string.IsNullOrWhiteSpace(id) || !eligible.Add(id))
                        throw new ArgumentException("Alvo invalido ou duplicado em Equip");
                }
            }
        }

        public bool Accepts(CardDefinition monster) =>
            monster != null && monster.Kind == CardKind.Monster &&
            (AnyMonster || eligible.Contains(monster.Id));
    }

    /// <summary>
    /// Strict allowlist: no inference from type labels, card names or text.
    /// Additional equip compatibilities can be read from a SHA-matched,
    /// individually verified local table; never from arbitrary mods.
    /// </summary>
    public sealed class VerifiedEquipCatalog
    {
        private readonly Dictionary<string, VerifiedEquipRule> rules =
            new Dictionary<string, VerifiedEquipRule>(StringComparer.Ordinal);
        public int Count => rules.Count;

        public void AddUniversal(string equipId, int attack, int defense)
        {
            AddRule(new VerifiedEquipRule(equipId, attack, defense, true, null));
        }

        public void AddTargets(string equipId, int attack, int defense,
            IReadOnlyList<string> monsterIds)
        {
            AddRule(new VerifiedEquipRule(equipId, attack, defense,
                false, monsterIds));
        }

        private void AddRule(VerifiedEquipRule rule)
        {
            if (rules.ContainsKey(rule.EquipCardId))
                throw new ArgumentException("Equip duplicado: " + rule.EquipCardId);
            rules.Add(rule.EquipCardId, rule);
        }

        public bool TryGet(CardDefinition card, out VerifiedEquipRule rule)
        {
            rule = null;
            return card != null && card.Kind == CardKind.Equip &&
                rules.TryGetValue(card.Id, out rule);
        }

        public bool CanApply(CardDefinition equip, CardDefinition monster,
            out VerifiedEquipRule rule)
        {
            return TryGet(equip, out rule) && rule.Accepts(monster);
        }

        /// <summary>
        /// Two universal retail equips with verified IDs, rather than
        /// fabricating the 32 remaining compatibility tables.
        /// </summary>
        public static VerifiedEquipCatalog OriginalPs1Foundation()
        {
            var catalog = new VerifiedEquipCatalog();
            catalog.AddUniversal("657", 1000, 1000); // Megamorph
            catalog.AddUniversal("668", 500, 500);  // Bright Castle
            return catalog;
        }
    }
}
