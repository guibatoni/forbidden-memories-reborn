using System;
using System.Collections.Generic;

namespace Reborn.Core
{
    public enum AiTactic { Balanced, Aggressive, Defensive, FusionFocused, Counterplay }
    public enum AiBrainVersion { Legacy = 1, Learning2 = 2 }

    /// <summary>
    /// Small anonymized persistent learning record, one per opponent and content edition.
    /// There are no stored hands, card IDs, names, conversations or account identifiers.
    /// Fields are public for Unity's JsonUtility; decisions are enforced by DuelMatch.
    /// </summary>
    [Serializable]
    public sealed class AiLearningState
    {
        public string key;
        public int duels;
        public int wins;
        public int playerAttacks;
        public int playerFusions;
        public int previousTactic = -1;
        public float[] tacticValues = new float[5];
        public int[] tacticGames = new int[5];
        public int[] tacticWins = new int[5];

        public AiLearningState() { }
        public AiLearningState(string identity) { key = identity; }

        public void Sanitize()
        {
            if (duels < 0) duels = 0;
            if (duels > 1000000) duels = 1000000;
            if (wins < 0) wins = 0;
            if (wins > duels) wins = duels;
            if (playerAttacks < 0) playerAttacks = 0;
            if (playerFusions < 0) playerFusions = 0;
            if (previousTactic < -1 || previousTactic > 4) previousTactic = -1;
            if (tacticValues == null || tacticValues.Length != 5)
                tacticValues = new float[5];
            if (tacticGames == null || tacticGames.Length != 5)
                tacticGames = new int[5];
            if (tacticWins == null || tacticWins.Length != 5)
                tacticWins = new int[5];
            for (int i = 0; i < 5; i++)
            {
                tacticGames[i] = Math.Max(0, Math.Min(1000000, tacticGames[i]));
                tacticWins[i] = Math.Max(0, Math.Min(tacticGames[i], tacticWins[i]));
            }
            for (int i = 0; i < tacticValues.Length; i++)
            {
                if (float.IsNaN(tacticValues[i]) || float.IsInfinity(tacticValues[i]))
                    tacticValues[i] = 0;
                tacticValues[i] = Math.Max(-2f, Math.Min(2f, tacticValues[i]));
            }
        }
    }

    /// <summary>
    /// Offline contextual bandit: explores among five play styles, evaluates
    /// legal actions only, and improves style preference from match outcomes.
    /// This is adaptive heuristic learning, not an LLM or a trained neural net.
    /// </summary>
    public sealed class AdaptiveOpponentBrain
    {
        private readonly Random random;
        private DuelMatch recordedDuel;
        public AiBrainVersion Version { get; }
        public AiLearningState State { get; private set; }
        public AiTactic Tactic { get; private set; }
        public PlannedAttackSequence LastAttackPlan { get; private set; }
        public string TacticLabel
        {
            get
            {
                switch (Tactic)
                {
                    case AiTactic.Aggressive: return "Ofensiva";
                    case AiTactic.Defensive: return "Defensiva";
                    case AiTactic.FusionFocused: return "Fusoes";
                    case AiTactic.Counterplay: return "Contra-ataque";
                    default: return "Equilibrada";
                }
            }
        }

        public AdaptiveOpponentBrain(Random rng,
            AiBrainVersion version = AiBrainVersion.Learning2)
        {
            random = rng ?? throw new ArgumentNullException(nameof(rng));
            if (version != AiBrainVersion.Legacy && version != AiBrainVersion.Learning2)
                throw new ArgumentOutOfRangeException(nameof(version));
            Version = version;
        }

        public void StartDuel(AiLearningState state)
        {
            State = state ?? throw new ArgumentNullException(nameof(state));
            State.Sanitize();
            recordedDuel = null;
            LastAttackPlan = null;
            // Softmax balances exploitation and experimentation, with a minimum
            // exploration floor so opponents do not converge to one fixed script.
            double sum = 0;
            var distribution = new double[5];
            for (int i = 0; i < distribution.Length; i++)
            {
                double context = 0;
                // Counterplay and defense become slightly more attractive against
                // players who repeatedly attack; fusion diversity is preserved.
                double attackRate = State.duels > 0
                    ? (double)State.playerAttacks / State.duels : 0;
                double fusionRate = State.duels > 0
                    ? (double)State.playerFusions / State.duels : 0;
                if (i == (int)AiTactic.Defensive)
                    context += Math.Min(0.50, attackRate * 0.05);
                if (i == (int)AiTactic.Counterplay)
                    context += Math.Min(0.35, fusionRate * 0.09);
                if (i == State.previousTactic) context -= 0.70;
                // The new policy explicitly explores under-tested tactics.
                // Legacy mode retains the 0.12.1 probabilities for A/B evaluation.
                double explore = Version == AiBrainVersion.Learning2
                    ? 0.60 / Math.Sqrt(1.0 + State.tacticGames[i]) : 0;
                distribution[i] = 0.24 + Math.Exp(
                    State.tacticValues[i] * (Version == AiBrainVersion.Learning2 ? 1.9 : 1.6)
                    + context + explore);
                sum += distribution[i];
            }
            double choice = random.NextDouble() * sum;
            int selected = 4;
            for (int i = 0; i < 5; i++)
            {
                choice -= distribution[i];
                if (choice < 0) { selected = i; break; }
            }
            Tactic = (AiTactic)selected;
        }

        public bool TryChoosePlacement(DuelMatch match, out StrategicPlacement chosen)
        {
            chosen = null;
            if (State == null || match == null ||
                match.CurrentSide != DuelSide.Opponent ||
                match.Phase != MatchPhase.Main || match.PlayedThisTurn) return false;
            int bestScore = int.MinValue;
            var hand = match.Opponent.Hand;
            for (int i = 0; i < hand.Count; i++)
            {
                if (hand[i].Kind != CardKind.Monster) continue;
                if (Version == AiBrainVersion.Learning2)
                    EvaluatePlacementV2(match, new[] { i }, hand[i], ref chosen, ref bestScore);
                else
                    EvaluatePlacement(match, new[] { i }, hand[i], ref chosen, ref bestScore);
                for (int j = i + 1; j < hand.Count; j++)
                {
                    if (hand[j].Kind != CardKind.Monster) continue;
                    var result = match.PreviewResult(DuelSide.Opponent, new[] { i, j });
                    if (result == null || result.Kind != CardKind.Monster ||
                        result.Id == hand[i].Id || result.Id == hand[j].Id) continue;
                    if (Version == AiBrainVersion.Learning2)
                        EvaluatePlacementV2(match, new[] { i, j }, result, ref chosen, ref bestScore);
                    else
                        EvaluatePlacement(match, new[] { i, j }, result, ref chosen, ref bestScore);
                }
            }
            return chosen != null;
        }

        private void EvaluatePlacement(DuelMatch match, int[] indices,
            CardDefinition card, ref StrategicPlacement chosen, ref int bestScore)
        {
            var stars = card.StarA == card.StarB
                ? new[] { card.StarA } : new[] { card.StarA, card.StarB };
            int maxEnemy = 0;
            for (int j = 0; j < DuelMatch.ZoneCount; j++)
            {
                var enemy = match.Player.Monster(j);
                if (enemy != null) maxEnemy = Math.Max(maxEnemy, enemy.Card.Attack);
            }
            foreach (var star in stars)
            {
                for (int slot = 0; slot < DuelMatch.ZoneCount; slot++)
                {
                    if (!match.CanPlay(DuelSide.Opponent, indices,
                        FieldLane.Monster, slot, star, out _)) continue;
                    int killCount = 0;
                    for (int j = 0; j < DuelMatch.ZoneCount; j++)
                    {
                        var enemy = match.Player.Monster(j);
                        if (enemy == null) continue;
                        int attack = card.Attack + GuardianStarRules.Bonus(star, enemy.Star);
                        int defense = (enemy.Position == BattlePosition.Attack
                            ? enemy.Card.Attack : enemy.Card.Defense)
                            + GuardianStarRules.Bonus(enemy.Star, star);
                        if (attack > defense) killCount++;
                    }
                    bool danger = maxEnemy > card.Attack && killCount == 0;
                    var position = danger && card.Defense >= card.Attack
                        ? BattlePosition.Defense : BattlePosition.Attack;
                    if (Tactic == AiTactic.Defensive && maxEnemy > 0 &&
                        maxEnemy >= card.Attack && card.Defense > card.Attack * 0.80)
                        position = BattlePosition.Defense;
                    int score = card.Attack + card.Defense / 5 + killCount * 300;
                    if (position == BattlePosition.Defense) score += card.Defense / 4 - 100;
                    if (indices.Length > 1) score += Tactic == AiTactic.FusionFocused ? 1100 : 160;
                    if (Tactic == AiTactic.Aggressive) score += card.Attack / 3 + killCount * 400;
                    if (Tactic == AiTactic.Defensive) score += card.Defense / 3;
                    if (Tactic == AiTactic.Counterplay) score += killCount * 600;
                    if (danger && card.Defense < maxEnemy) score -= 650;
                    if (match.Player.MonsterCount == 0 && card.Attack >= match.Player.Life)
                        score += 15000;
                    score += random.Next(0, 161); // tie-break variability only
                    if (score <= bestScore) continue;
                    bestScore = score;
                    chosen = new StrategicPlacement(indices, slot, card, position, star, score);
                }
            }
        }


        /// <summary>
        /// Learning 2.0 placement evaluator: compare offensive opportunities
        /// and enemy counterattacks through the same CombatForecast rules as
        /// the duel. Only visible field monsters are considered. All actions
        /// are validated by CanPlay; the preview never changes DuelMatch.
        /// </summary>
        private void EvaluatePlacementV2(DuelMatch match, int[] indices,
            CardDefinition card, ref StrategicPlacement chosen, ref int bestScore)
        {
            var stars = card.StarA == card.StarB
                ? new[] { card.StarA } : new[] { card.StarA, card.StarB };
            int opposingMonsters = match.Player.MonsterCount;
            int defendingWalls = 0;
            int activeThreats = 0;
            for (int i = 0; i < DuelMatch.ZoneCount; i++)
            {
                var enemy = match.Player.Monster(i);
                if (enemy == null) continue;
                if (enemy.Position == BattlePosition.Defense) defendingWalls++;
                else activeThreats++;
            }

            foreach (var star in stars)
            {
                // Every slot is tried, but the rules engine decides legality.
                for (int slot = 0; slot < DuelMatch.ZoneCount; slot++)
                {
                    if (!match.CanPlay(DuelSide.Opponent, indices,
                        FieldLane.Monster, slot, star, out _)) continue;

                    foreach (var position in new[] {
                        BattlePosition.Attack, BattlePosition.Defense })
                    {
                        var candidate = new MonsterOnField(card, position, star);
                        int kills = 0, winningAttacks = 0, incomingLosses = 0;
                        int possibleDamage = 0;
                        int score = card.Attack / 3 + card.Defense / 6;
                        if (position == BattlePosition.Attack) score += 310;
                        else score += card.Defense / 6;

                        for (int i = 0; i < DuelMatch.ZoneCount; i++)
                        {
                            var enemy = match.Player.Monster(i);
                            if (enemy == null) continue;
                            if (position == BattlePosition.Attack)
                            {
                                var outgoing = new CombatForecast(candidate, enemy);
                                if (outgoing.DefenderDestroyed && !outgoing.AttackerDestroyed &&
                                    outgoing.DamageToAttacker == 0)
                                {
                                    kills++;
                                    score += 1250 + enemy.Card.Attack / 3;
                                    if (enemy.Position == BattlePosition.Defense)
                                        score += 550; // break the defensive stalemate
                                    possibleDamage = Math.Max(possibleDamage,
                                        outgoing.DamageToDefender);
                                    winningAttacks++;
                                }
                            }
                            if (enemy.Position == BattlePosition.Attack)
                            {
                                var incoming = new CombatForecast(enemy, candidate);
                                if (incoming.DefenderDestroyed)
                                {
                                    incomingLosses++;
                                    score -= 1350 + card.Attack / 6;
                                }
                                else if (position == BattlePosition.Defense &&
                                    incoming.DamageToAttacker > 0)
                                    score += 240;
                            }
                        }

                        // An empty opposing field is an immediate direct-attack
                        // opportunity; defensive placement cannot deal damage.
                        if (opposingMonsters == 0)
                        {
                            if (position == BattlePosition.Attack)
                                score += 1250 + card.Attack / 2 +
                                    (card.Attack >= match.Player.Life ? 100000 : 0);
                            else score -= 2500;
                        }

                        if (position == BattlePosition.Defense)
                        {
                            // Don't build a second wall while an opposing
                            // defender is waiting to be cleared.
                            if (defendingWalls > 0 && activeThreats == 0)
                                score -= 1550;
                            if (incomingLosses == 0 && activeThreats > 0)
                                score += 250;
                        }
                        else
                        {
                            if (kills > 0) score += Math.Min(900, possibleDamage);
                            if (defendingWalls > 0 && winningAttacks == 0)
                                score -= 450;
                        }

                        if (Tactic == AiTactic.Aggressive && position == BattlePosition.Attack)
                            score += card.Attack / 3 + 250 * kills;
                        if (Tactic == AiTactic.Defensive)
                            score += position == BattlePosition.Defense
                                ? card.Defense / 3 : card.Defense / 9;
                        if (Tactic == AiTactic.Counterplay && position == BattlePosition.Attack)
                            score += kills * 420;
                        if (indices.Length > 1)
                            score += Tactic == AiTactic.FusionFocused ? 750 : 120;

                        // Seeded tie-break randomness for variety, but strong
                        // tactical differences dominate the small noise.
                        score += random.Next(0, 71);
                        if (score <= bestScore) continue;
                        bestScore = score;
                        chosen = new StrategicPlacement(indices, slot, card,
                            position, star, score);
                    }
                }
            }
        }

        /// <summary>
        /// Evaluates the entire current battle phase. The director executes
        /// only the first legal step, then replans after its real resolution.
        /// This keeps animation timing and duel rules authoritative.
        /// </summary>
        public bool TryChooseAttack(DuelMatch match, out int fromSlot, out int targetSlot)
        {
            fromSlot = -1;
            targetSlot = -1;
            LastAttackPlan = null;
            if (State == null || match == null) return false;
            if (!MultiAttackPlanner.TryPlan(match, Tactic, random,
                out var sequence)) return false;
            LastAttackPlan = sequence;
            var first = sequence.Steps[0];
            fromSlot = first.AttackerSlot;
            targetSlot = first.TargetSlot;
            return true;
        }

        public bool RecordCompletedDuel(DuelMatch match)
        {
            if (State == null || match == null ||
                match.Phase != MatchPhase.Finished ||
                !match.Winner.HasValue || ReferenceEquals(match, recordedDuel)) return false;
            recordedDuel = match;
            int attacks = 0, fusions = 0;
            foreach (var evt in match.Events)
            {
                if (evt.Side != DuelSide.Player) continue;
                if (evt.Kind == MatchEventKind.Attack) attacks++;
                if (evt.Kind == MatchEventKind.Fusion) fusions++;
            }
            bool won = match.Winner.Value == DuelSide.Opponent;
            double reward = won ? 1.0 : -1.0;
            // Preserve the legacy outcome for controlled A/B comparisons.
            if (Version == AiBrainVersion.Legacy)
                reward += (match.Opponent.Life - match.Player.Life) / 16000.0;
            else
            {
                // Reward meaningful progress while prioritizing wins. Avoid
                // reinforcing stall tactics that merely preserve a wall.
                double lifeAdvantage = (match.Opponent.Life - match.Player.Life) / 8000.0;
                int enemyDestroyed = 0, ownDestroyed = 0;
                foreach (var e in match.Events)
                {
                    if (e.Kind != MatchEventKind.Destroy) continue;
                    if (e.Side == DuelSide.Player) enemyDestroyed++;
                    else ownDestroyed++;
                }
                reward += Math.Max(-0.35, Math.Min(0.35, lifeAdvantage * 0.35));
                reward += Math.Max(-0.30, Math.Min(0.30,
                    (enemyDestroyed - ownDestroyed) * 0.06));
            }
            int index = (int)Tactic;
            float old = State.tacticValues[index];
            State.tacticValues[index] = (float)Math.Max(-2.0, Math.Min(2.0,
                old + 0.24 * (reward - old)));
            State.duels = Math.Min(1000000, State.duels + 1);
            if (won) State.wins = Math.Min(State.duels, State.wins + 1);
            State.playerAttacks = Math.Min(10000000, State.playerAttacks + attacks);
            State.playerFusions = Math.Min(10000000, State.playerFusions + fusions);
            State.previousTactic = index;
            if (Version == AiBrainVersion.Learning2)
            {
                State.tacticGames[index] =
                    Math.Min(1000000, State.tacticGames[index] + 1);
                if (won) State.tacticWins[index] =
                    Math.Min(State.tacticGames[index], State.tacticWins[index] + 1);
            }
            return true;
        }
    }
}
