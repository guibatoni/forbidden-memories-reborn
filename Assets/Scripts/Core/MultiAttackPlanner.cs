using System;
using System.Collections.Generic;

namespace Reborn.Core
{
    /// <summary>One predicted legal attack, never an executed action.</summary>
    public sealed class PlannedStrike
    {
        public int AttackerSlot { get; }
        public int TargetSlot { get; }
        public CombatForecast Forecast { get; }

        internal PlannedStrike(int attackerSlot, int targetSlot, CombatForecast forecast)
        {
            AttackerSlot = attackerSlot;
            TargetSlot = targetSlot;
            Forecast = forecast ?? throw new ArgumentNullException(nameof(forecast));
        }
    }

    /// <summary>Read-only plan for the currently visible battlefield.</summary>
    public sealed class PlannedAttackSequence
    {
        public IReadOnlyList<PlannedStrike> Steps { get; }
        public int ExpectedDamage { get; }
        public int ExpectedDestroyedDefenders { get; }
        public int Score { get; }

        internal PlannedAttackSequence(IReadOnlyList<PlannedStrike> steps,
            int expectedDamage, int expectedDestroyedDefenders, int score)
        {
            Steps = steps;
            ExpectedDamage = expectedDamage;
            ExpectedDestroyedDefenders = expectedDestroyedDefenders;
            Score = score;
        }
    }

    /// <summary>
    /// Searches up to five attacker/defender slots for a beneficial attack
    /// sequence. Uses DuelMatch.TryPreviewAttack for CURRENT legal targets,
    /// then simulates only removal of destroyed monsters and direct damage.
    /// Never clones hidden hands or mutates the real DuelMatch.
    /// </summary>
    public static class MultiAttackPlanner
    {
        public static bool TryPlan(DuelMatch match, AiTactic tactic, Random random,
            out PlannedAttackSequence plan)
        {
            plan = null;
            if (match == null || random == null) return false;
            if (match.CurrentSide != DuelSide.Opponent ||
                match.Phase != MatchPhase.Battle) return false;

            var attacker = new MonsterOnField[DuelMatch.ZoneCount];
            var defender = new MonsterOnField[DuelMatch.ZoneCount];
            var clashes = new CombatForecast[DuelMatch.ZoneCount, DuelMatch.ZoneCount];
            int attackMask = 0, defenseMask = 0;
            for (int i = 0; i < DuelMatch.ZoneCount; i++)
            {
                var unit = match.Opponent.Monster(i);
                if (unit != null && unit.Position == BattlePosition.Attack &&
                    !unit.AttackedThisTurn)
                {
                    attacker[i] = unit;
                    attackMask |= 1 << i;
                }
                var target = match.Player.Monster(i);
                if (target != null)
                {
                    defender[i] = target;
                    defenseMask |= 1 << i;
                }
            }
            if (attackMask == 0) return false;
            for (int i = 0; i < DuelMatch.ZoneCount; i++)
                if (attacker[i] != null)
                    for (int j = 0; j < DuelMatch.ZoneCount; j++)
                        if (defender[j] != null &&
                            match.TryPreviewAttack(DuelSide.Opponent, i, j,
                                out var forecast, out _))
                            clashes[i, j] = forecast;

            // Future direct damage uses the same CombatForecast implementation.
            // It is hypothetical only, and the real director revalidates every
            // action using DuelMatch.CanAttack before it is executed.
            var direct = new CombatForecast[DuelMatch.ZoneCount];
            for (int i = 0; i < DuelMatch.ZoneCount; i++)
                if (attacker[i] != null)
                {
                    if (defenseMask == 0)
                    {
                        if (!match.TryPreviewAttack(DuelSide.Opponent, i, -1,
                            out direct[i], out _)) continue;
                    }
                    else direct[i] = new CombatForecast(attacker[i], null);
                }

            var memo = new Dictionary<long, PlannedAttackSequence>();
            var best = Search(attackMask, defenseMask,
                match.Player.Life, attacker, defender, clashes, direct,
                tactic, random, memo);
            if (best.Steps.Count == 0) return false;
            // Defensive invariant: the very first action must be legal NOW.
            var first = best.Steps[0];
            if (!match.CanAttack(DuelSide.Opponent, first.AttackerSlot,
                first.TargetSlot, out _)) return false;
            plan = best;
            return true;
        }

        private static PlannedAttackSequence Empty()
        {
            return new PlannedAttackSequence(
                Array.AsReadOnly(new PlannedStrike[0]), 0, 0, 0);
        }

        private static PlannedAttackSequence Search(
            int attackerMask, int defenderMask, int remainingLife,
            MonsterOnField[] attackers, MonsterOnField[] defenders,
            CombatForecast[,] clashes, CombatForecast[] direct,
            AiTactic tactic, Random random,
            Dictionary<long, PlannedAttackSequence> memo)
        {
            if (attackerMask == 0 || remainingLife <= 0) return Empty();
            long key = ((long)remainingLife << 10) |
                ((long)attackerMask << 5) | (uint)defenderMask;
            if (memo.TryGetValue(key, out var cached)) return cached;
            var best = Empty();
            for (int i = 0; i < DuelMatch.ZoneCount; i++)
            {
                if ((attackerMask & (1 << i)) == 0) continue;
                if (defenderMask == 0)
                {
                    if (direct[i] != null)
                        Consider(i, -1, direct[i], attackerMask, defenderMask,
                            remainingLife, attackers, defenders, clashes, direct,
                            tactic, random, memo, ref best);
                }
                else for (int j = 0; j < DuelMatch.ZoneCount; j++)
                {
                    if ((defenderMask & (1 << j)) == 0 || clashes[i, j] == null)
                        continue;
                    Consider(i, j, clashes[i, j], attackerMask, defenderMask,
                        remainingLife, attackers, defenders, clashes, direct,
                        tactic, random, memo, ref best);
                }
            }
            memo[key] = best;
            return best;
        }

        private static void Consider(int attackerSlot, int targetSlot,
            CombatForecast forecast, int attackerMask, int defenderMask,
            int remainingLife, MonsterOnField[] attackers,
            MonsterOnField[] defenders, CombatForecast[,] clashes,
            CombatForecast[] direct, AiTactic tactic, Random random,
            Dictionary<long, PlannedAttackSequence> memo,
            ref PlannedAttackSequence best)
        {
            // An opponent may decline attacks that cannot destroy the target
            // or would needlessly lose LP. A deliberate high-value trade is
            // still considered by the search.
            if (!forecast.IsDirect && !forecast.DefenderDestroyed) return;
            if (forecast.DamageToAttacker > 0) return;

            int damage = Math.Min(remainingLife, forecast.DamageToDefender);
            int utility;
            if (forecast.IsDirect)
            {
                utility = 250 + damage * 3;
                if (tactic == AiTactic.Aggressive) utility += damage * 2;
            }
            else
            {
                var enemy = defenders[targetSlot];
                utility = 650 + enemy.Card.Attack / 3 + enemy.Card.Defense / 10 +
                    damage * 2;
                if (tactic == AiTactic.Counterplay)
                    utility += enemy.Card.Attack / 2;
                if (tactic == AiTactic.Aggressive) utility += damage * 2;
                if (forecast.AttackerDestroyed)
                {
                    int sacrifice = 450 + attackers[attackerSlot].Card.Attack / 2;
                    if (tactic == AiTactic.Defensive) sacrifice += 350;
                    utility -= sacrifice;
                }
            }
            if (utility <= 0) return;
            // Small seeded variation breaks near-ties without overriding
            // a clear tactical advantage or a possible lethal attack.
            utility += random.Next(0, 24);
            bool lethal = damage >= remainingLife;
            if (lethal) utility += 200000;
            int nextAttackers = attackerMask & ~(1 << attackerSlot);
            int nextDefenders = targetSlot < 0
                ? defenderMask : defenderMask & ~(1 << targetSlot);
            var continuation = lethal ? Empty() :
                Search(nextAttackers, nextDefenders, remainingLife - damage,
                    attackers, defenders, clashes, direct, tactic, random, memo);
            int score = utility + continuation.Score;
            if (score <= best.Score) return;
            var steps = new List<PlannedStrike>(continuation.Steps.Count + 1)
            {
                new PlannedStrike(attackerSlot, targetSlot, forecast)
            };
            foreach (var action in continuation.Steps) steps.Add(action);
            best = new PlannedAttackSequence(steps.AsReadOnly(),
                damage + continuation.ExpectedDamage,
                (targetSlot >= 0 ? 1 : 0) +
                    continuation.ExpectedDestroyedDefenders,
                score);
        }
    }
}
