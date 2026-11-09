using Reborn.Core;

namespace Reborn.Presentation
{
    /// <summary>PT-BR combat diagnostics without revealing hidden hand/deck cards.</summary>
    public static class CombatExplanation
    {
        public static string Describe(CombatForecast battle)
        {
            if (battle == null) return "Resultado do ataque indisponivel.";
            if (battle.IsDirect)
                return "Ataque direto: " + battle.EffectiveAttack + " ATK; -" +
                    battle.DamageToDefender + " LP do defensor.";

            string label = battle.DefenderPosition == BattlePosition.Defense ? "DEF " : "ATK ";
            string output = "ATK " + battle.AttackerBase;
            if (battle.AttackerGuardianBonus > 0)
                output += " + " + battle.AttackerGuardianBonus + " Guardian";
            output += " vs " + label + battle.DefenderBase;
            if (battle.DefenderGuardianBonus > 0)
                output += " + " + battle.DefenderGuardianBonus + " Guardian";
            output += " (efetivo " + battle.EffectiveAttack + " vs " +
                battle.EffectiveDefense + "): ";

            if (battle.AttackerDestroyed && battle.DefenderDestroyed)
                output += "ambos destruidos";
            else if (battle.DefenderDestroyed)
                output += "defensor destruido";
            else if (battle.AttackerDestroyed)
                output += "atacante destruido";
            else if (battle.DefenderPosition == BattlePosition.Defense)
                output += "defesa resistiu";
            else output += "ambos permaneceram";

            if (battle.DamageToAttacker > 0)
                output += "; atacante perdeu " + battle.DamageToAttacker + " LP";
            if (battle.DamageToDefender > 0)
                output += "; defensor perdeu " + battle.DamageToDefender + " LP";
            if (battle.DamageToAttacker == 0 && battle.DamageToDefender == 0)
                output += "; sem dano aos LP";
            return output + ".";
        }
    }
}
