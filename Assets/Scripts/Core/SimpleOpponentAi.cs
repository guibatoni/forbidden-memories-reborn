using System;

namespace Reborn.Core
{
    /// <summary>
    /// AI de demonstração, determinística. Fornece decisões individuais para que
    /// o Unity apresente UMA ação de cada vez. Não representa a IA da ISO original.
    /// </summary>
    public static class SimpleOpponentAi
    {
        public static bool TryChoosePlay(DuelMatch match, out int handIndex, out int slot)
        {
            handIndex = -1;
            slot = -1;
            if (match == null) throw new ArgumentNullException(nameof(match));
            if (match.Phase != MatchPhase.Main || match.CurrentSide != DuelSide.Opponent ||
                match.PlayedThisTurn) return false;
            var side = match.Opponent;
            for (int i = 0; i < side.Hand.Count; i++)
            {
                if (side.Hand[i].Kind != CardKind.Monster) continue;
                if (handIndex < 0 || side.Hand[i].Attack > side.Hand[handIndex].Attack)
                    handIndex = i;
            }
            if (handIndex < 0) return false;
            for (int i = 0; i < DuelMatch.ZoneCount; i++)
                if (side.Monster(i) == null)
                {
                    slot = i;
                    return true;
                }
            handIndex = -1;
            return false;
        }

        public static bool TryChooseAttack(DuelMatch match, out int attackerSlot, out int targetSlot)
        {
            attackerSlot = -1;
            targetSlot = -1;
            if (match == null) throw new ArgumentNullException(nameof(match));
            if (match.Phase != MatchPhase.Battle || match.CurrentSide != DuelSide.Opponent)
                return false;
            for (int slot = 0; slot < DuelMatch.ZoneCount; slot++)
            {
                var attacker = match.Opponent.Monster(slot);
                if (attacker == null || attacker.Position != BattlePosition.Attack ||
                    attacker.AttackedThisTurn) continue;
                attackerSlot = slot;
                int weakestPower = int.MaxValue;
                for (int i = 0; i < DuelMatch.ZoneCount; i++)
                {
                    var foe = match.Player.Monster(i);
                    if (foe == null) continue;
                    int power = foe.Position == BattlePosition.Attack ?
                        foe.Card.Attack : foe.Card.Defense;
                    if (power < weakestPower)
                    {
                        weakestPower = power;
                        targetSlot = i;
                    }
                }
                return true; // target -1 significa ataque direto, somente se campo estiver vazio.
            }
            return false;
        }

        /// <summary>Execução imediata preservada para testes do núcleo e compatibilidade.</summary>
        public static void TakeTurn(DuelMatch match)
        {
            if (match == null) throw new ArgumentNullException(nameof(match));
            if (match.Phase == MatchPhase.Finished || match.CurrentSide != DuelSide.Opponent) return;
            if (StrategicOpponentAi.TryChoosePlacement(match, out var plan))
            {
                match.TryPlay(DuelSide.Opponent, plan.HandIndices, FieldLane.Monster,
                    plan.Slot, plan.Position, plan.Star, out _);
            }
            if (!match.PlayedThisTurn &&
                match.TryChooseFieldCombination(DuelSide.Opponent, true,
                    out int fusionHand, out int fusionSlot, out var fusionResult))
            {
                match.TryPlayOnField(DuelSide.Opponent, new[] { fusionHand },
                    fusionSlot, BattlePosition.Attack, fusionResult.StarA, false,
                    out _, out _, out _);
            }
            if (!match.PlayedThisTurn &&
                match.TryChooseSupportSet(DuelSide.Opponent,
                    out int supportHand, out int supportSlot))
            {
                match.TryPlay(DuelSide.Opponent, new[] {supportHand},
                    FieldLane.Support, supportSlot, BattlePosition.Defense,
                    GuardianStar.None, true, out _);
            }
            if (!match.PlayedThisTurn &&
                match.TryChooseFieldCombination(DuelSide.Opponent, false,
                    out int replacementHand, out int replacementSlot,
                    out var replacementResult))
            {
                match.TryPlayOnField(DuelSide.Opponent, new[] { replacementHand },
                    replacementSlot, BattlePosition.Attack,
                    replacementResult.StarA, false, out _, out _, out _);
            }
            // Never bypass compulsory placement in this headless test AI.
            if (!match.PlayedThisTurn) return;
            if (match.Phase == MatchPhase.Main)
                match.TryBeginBattle(DuelSide.Opponent, out _);
            while (StrategicOpponentAi.TryChooseAttack(match, out int from, out int target))
            {
                if (!match.TryAttack(DuelSide.Opponent, from, target, out _)) break;
                if (match.Phase == MatchPhase.Finished) return;
            }
            if (match.Phase != MatchPhase.Finished)
                match.TryEndTurn(DuelSide.Opponent, out _);
        }
    }
}
