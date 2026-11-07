using System;
using System.Collections.Generic;

namespace Reborn.Core
{
    /// <summary>A single planned action, not an executed action. Never changes DuelMatch.</summary>
    public sealed class StrategicPlacement
    {
        public IReadOnlyList<int> HandIndices { get; }
        public int Slot { get; }
        public CardDefinition Result { get; }
        public BattlePosition Position { get; }
        public GuardianStar Star { get; }
        public int Score { get; }
        public bool IsFusion => HandIndices.Count > 1;

        internal StrategicPlacement(int[] indices, int slot, CardDefinition card,
            BattlePosition position, GuardianStar star, int score)
        {
            HandIndices = indices;
            Slot = slot;
            Result = card;
            Position = position;
            Star = star;
            Score = score;
        }
    }

    /// <summary>
    /// Offline deterministic decision evaluator. Searches legal card placements,
    /// two-card validated fusions, Guardian Stars and tactical trades.
    /// It cannot simulate unevaluated spell/trap effects or read face-down cards.
    /// </summary>
    public static class StrategicOpponentAi
    {
        public static bool TryChoosePlacement(DuelMatch match, out StrategicPlacement best)
        {
            if (match == null) throw new ArgumentNullException(nameof(match));
            best = null;
            if (match.CurrentSide != DuelSide.Opponent ||
                match.Phase != MatchPhase.Main || match.PlayedThisTurn)
                return false;

            var hand = match.Opponent.Hand;
            for (int i = 0; i < hand.Count; i++)
            {
                if (hand[i].Kind == CardKind.Monster)
                    Consider(match, new[] { i }, hand[i], false, ref best);
                // Two-card fusions only. Complex five-card chains remain for the
                // verified fusion evaluator, not an unbounded heuristic here.
                for (int j = i + 1; j < hand.Count; j++)
                {
                    if (hand[i].Kind != CardKind.Monster ||
                        hand[j].Kind != CardKind.Monster) continue;
                    var outcome = match.PreviewResult(DuelSide.Opponent, new[] { i, j });
                    if (outcome == null || outcome.Kind != CardKind.Monster ||
                        outcome.Id == hand[i].Id || outcome.Id == hand[j].Id) continue;
                    Consider(match, new[] { i, j }, outcome, true, ref best);
                }
            }
            return best != null;
        }

        private static void Consider(DuelMatch match, int[] indices,
            CardDefinition card, bool fused, ref StrategicPlacement best)
        {
            var stars = card.StarA == card.StarB
                ? new[] { card.StarA }
                : new[] { card.StarA, card.StarB };
            for (int slot = 0; slot < DuelMatch.ZoneCount; slot++)
            {
                foreach (var star in stars)
                {
                    if (!match.CanPlay(DuelSide.Opponent, indices, FieldLane.Monster,
                        slot, star, out _)) continue;

                    int highThreat = 0;
                    int canDefeat = 0;
                    for (int target = 0; target < DuelMatch.ZoneCount; target++)
                    {
                        var enemy = match.Player.Monster(target);
                        if (enemy == null) continue;
                        int enemyOffense = enemy.Card.Attack +
                            GuardianStarRules.Bonus(enemy.Star, star);
                        highThreat = Math.Max(highThreat, enemyOffense);
                        int defense = (enemy.Position == BattlePosition.Attack
                            ? enemy.Card.Attack : enemy.Card.Defense) +
                            GuardianStarRules.Bonus(enemy.Star, star);
                        if (card.Attack + GuardianStarRules.Bonus(star, enemy.Star) > defense)
                            canDefeat++;
                    }

                    var position = highThreat > card.Attack &&
                        card.Defense > card.Attack && canDefeat == 0
                        ? BattlePosition.Defense : BattlePosition.Attack;
                    int score = card.Attack + card.Defense / 4 +
                        canDefeat * 450 + (fused ? 220 : 0) -
                        (position == BattlePosition.Defense ? 80 : 0);
                    if (highThreat > card.Attack && highThreat > card.Defense)
                        score -= 600;
                    if (match.Player.MonsterCount == 0 &&
                        card.Attack >= match.Player.Life) score += 15000;
                    // Predictable tie-break: lower hand index, then first open zone.
                    if (best == null || score > best.Score)
                        best = new StrategicPlacement(indices, slot, card,
                            position, star, score);
                }
            }
        }

        public static bool TryChooseAttack(DuelMatch match,
            out int attackerSlot, out int targetSlot)
        {
            if (match == null) throw new ArgumentNullException(nameof(match));
            attackerSlot = -1; targetSlot = -1;
            if (match.Phase != MatchPhase.Battle ||
                match.CurrentSide != DuelSide.Opponent) return false;

            var bestScore = int.MinValue;
            for (int i = 0; i < DuelMatch.ZoneCount; i++)
            {
                var attacker = match.Opponent.Monster(i);
                if (attacker == null || attacker.Position != BattlePosition.Attack ||
                    attacker.AttackedThisTurn) continue;
                if (match.Player.MonsterCount == 0)
                {
                    if (!match.CanAttack(DuelSide.Opponent, i, -1, out _)) continue;
                    int score = attacker.Card.Attack;
                    if (score >= match.Player.Life) score += 100000;
                    if (score > bestScore)
                    {
                        bestScore = score; attackerSlot = i; targetSlot = -1;
                    }
                    continue;
                }
                for (int j = 0; j < DuelMatch.ZoneCount; j++)
                {
                    var foe = match.Player.Monster(j);
                    if (foe == null ||
                        !match.CanAttack(DuelSide.Opponent, i, j, out _)) continue;
                    int attack = attacker.Card.Attack +
                        GuardianStarRules.Bonus(attacker.Star, foe.Star);
                    int defense = (foe.Position == BattlePosition.Attack
                        ? foe.Card.Attack : foe.Card.Defense) +
                        GuardianStarRules.Bonus(foe.Star, attacker.Star);
                    int score;
                    if (attack > defense)
                    {
                        int damage = foe.Position == BattlePosition.Attack
                            ? attack - defense : 0;
                        score = 1000 + foe.Card.Attack / 2 + damage;
                        if (damage >= match.Player.Life) score += 100000;
                    }
                    else if (attack == defense &&
                             foe.Position == BattlePosition.Attack &&
                             foe.Card.Attack > attacker.Card.Attack + 200)
                        score = 100 + foe.Card.Attack / 3; // acceptable trade
                    else continue; // no blind suicidal attack
                    if (score > bestScore)
                    {
                        bestScore = score; attackerSlot = i; targetSlot = j;
                    }
                }
            }
            return attackerSlot >= 0;
        }
    }
}
