using System;
using System.Collections.Generic;

namespace Reborn.Core
{
    public enum AiTactic { Balanced, Aggressive, Defensive, FusionFocused, Counterplay }

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

        public AdaptiveOpponentBrain(Random rng)
        {
            random = rng ?? throw new ArgumentNullException(nameof(rng));
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
                distribution[i] = 0.24 + Math.Exp(State.tacticValues[i] * 1.6 + context);
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
                EvaluatePlacement(match, new[] { i }, hand[i], ref chosen, ref bestScore);
                for (int j = i + 1; j < hand.Count; j++)
                {
                    if (hand[j].Kind != CardKind.Monster) continue;
                    var result = match.PreviewResult(DuelSide.Opponent, new[] { i, j });
                    if (result == null || result.Kind != CardKind.Monster ||
                        result.Id == hand[i].Id || result.Id == hand[j].Id) continue;
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
            // Secondary signal rewards surviving with LP, not only wins.
            reward += (match.Opponent.Life - match.Player.Life) / 16000.0;
            int index = (int)Tactic;
            float old = State.tacticValues[index];
            State.tacticValues[index] = (float)Math.Max(-2.0, Math.Min(2.0,
                old + 0.24 * (reward - old)));
            State.duels = Math.Min(1000000, State.duels + 1);
            if (won) State.wins = Math.Min(State.duels, State.wins + 1);
            State.playerAttacks = Math.Min(10000000, State.playerAttacks + attacks);
            State.playerFusions = Math.Min(10000000, State.playerFusions + fusions);
            State.previousTactic = index;
            return true;
        }
    }
}
