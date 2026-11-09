using System;

namespace Reborn.Core
{
    /// <summary>
    /// Read-only combat calculation. Calculated by the rules engine before any
    /// damage or destruction, so animation, logs and gameplay see the same result.
    /// Defender's Guardian Star can increase DEF by 500 even in defense position.
    /// </summary>
    public sealed class CombatForecast
    {
        public CardDefinition AttackerCard { get; }
        public CardDefinition DefenderCard { get; }
        public GuardianStar AttackerStar { get; }
        public GuardianStar DefenderStar { get; }
        public BattlePosition DefenderPosition { get; }
        public bool IsDirect => DefenderCard == null;
        public int AttackerBase { get; }
        public int DefenderBase { get; }
        public int AttackerGuardianBonus { get; }
        public int DefenderGuardianBonus { get; }
        public int EffectiveAttack => AttackerBase + AttackerGuardianBonus;
        public int EffectiveDefense => DefenderBase + DefenderGuardianBonus;
        public bool AttackerDestroyed { get; }
        public bool DefenderDestroyed { get; }
        public int DamageToAttacker { get; }
        public int DamageToDefender { get; }

        internal CombatForecast(MonsterOnField attacker, MonsterOnField defender)
        {
            if (attacker == null) throw new ArgumentNullException(nameof(attacker));
            AttackerCard = attacker.Card;
            AttackerStar = attacker.Star;
            AttackerBase = attacker.Card.Attack;
            if (defender == null)
            {
                DamageToDefender = AttackerBase;
                return;
            }

            DefenderCard = defender.Card;
            DefenderStar = defender.Star;
            DefenderPosition = defender.Position;
            DefenderBase = defender.Position == BattlePosition.Attack
                ? defender.Card.Attack : defender.Card.Defense;
            AttackerGuardianBonus = GuardianStarRules.Bonus(attacker.Star, defender.Star);
            DefenderGuardianBonus = GuardianStarRules.Bonus(defender.Star, attacker.Star);

            if (EffectiveAttack > EffectiveDefense)
            {
                DefenderDestroyed = true;
                if (DefenderPosition == BattlePosition.Attack)
                    DamageToDefender = EffectiveAttack - EffectiveDefense;
            }
            else if (EffectiveAttack < EffectiveDefense)
            {
                AttackerDestroyed = DefenderPosition == BattlePosition.Attack;
                DamageToAttacker = EffectiveDefense - EffectiveAttack;
            }
            else if (DefenderPosition == BattlePosition.Attack)
            {
                AttackerDestroyed = true;
                DefenderDestroyed = true;
            }
        }
    }
}
