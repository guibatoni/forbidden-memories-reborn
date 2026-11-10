using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Reborn.Core;

namespace Reborn.AiLab
{
    public sealed class RunSummary
    {
        public int Games { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }
        public int Unresolved { get; set; }
        public double WinRate { get; set; }
        public double AverageTurns { get; set; }
        public int TotalAttacks { get; set; }
        public int TotalFusions { get; set; }
    }

    public sealed class BenchmarkReport
    {
        public string Schema { get; set; } = "reborn-ai-lab-v1";
        public string Description { get; set; } = "Synthetic monster-only cards, deterministic headless evaluation; no retail ISO data.";
        public string ResultMeaning { get; set; } = "Results are empirical. A trained profile is not guaranteed to outperform a fresh profile.";
        public int Seed { get; set; }
        public int TrainingGames { get; set; }
        public int EvaluationGames { get; set; }
        public int TrainingCompleted { get; set; }
        public int LearnedDuels { get; set; }
        public RunSummary ScriptedReference { get; set; }
        public RunSummary AdaptiveBefore { get; set; }
        public RunSummary AdaptiveAfter { get; set; }
        public double WinRateDifference { get; set; }
        public float[] LearnedTacticValues { get; set; }
    }

    internal sealed class GameResult
    {
        public DuelSide? Winner;
        public int Turns;
        public int OpponentAttacks;
        public int OpponentFusions;
    }

    public static class AiLab
    {
        private const int MaxTurns = 100;
        private static readonly GuardianStar[] Stars = {
            GuardianStar.Mercury, GuardianStar.Sun, GuardianStar.Moon,
            GuardianStar.Venus, GuardianStar.Mars, GuardianStar.Jupiter,
            GuardianStar.Saturn, GuardianStar.Uranus, GuardianStar.Pluto,
            GuardianStar.Neptune
        };

        // The cards are synthetic, uniquely identified, and generated from
        // a seed. Neither game engine nor AI receives any retail/ROM assets.
        private static IReadOnlyList<CardDefinition> SyntheticDeck(int seed, string prefix)
        {
            var rng = new Random(seed);
            var cards = new List<CardDefinition>(DuelMatch.DeckSize);
            for (int i = 0; i < DuelMatch.DeckSize; i++)
            {
                int attack = 800 + rng.Next(0, 24) * 100;
                int defense = 600 + rng.Next(0, 24) * 100;
                var starA = Stars[rng.Next(Stars.Length)];
                var starB = Stars[rng.Next(Stars.Length)];
                cards.Add(new CardDefinition(prefix + i.ToString("D2"),
                    "Synthetic " + prefix + i, CardKind.Monster,
                    attack, defense, "Headless test card", starA, starB));
            }
            return DeckRandomizer.Shuffle(cards, new Random(seed ^ 0x12345));
        }

        private static void TakeReferencePlacement(DuelMatch duel, DuelSide side)
        {
            if (duel.Phase != MatchPhase.Main || duel.PlayedThisTurn) return;
            var hand = duel.Side(side).Hand;
            int bestHand = -1, bestSlot = -1, bestScore = int.MinValue;
            GuardianStar bestStar = GuardianStar.None;
            BattlePosition bestPosition = BattlePosition.Attack;
            for (int h = 0; h < hand.Count; h++)
            {
                var card = hand[h];
                if (card.Kind != CardKind.Monster) continue;
                for (int slot = 0; slot < DuelMatch.ZoneCount; slot++)
                {
                    var stars = card.StarA == card.StarB
                        ? new[] { card.StarA } : new[] { card.StarA, card.StarB };
                    foreach (var star in stars)
                    {
                        if (!duel.CanPlay(side, new[] { h }, FieldLane.Monster,
                            slot, star, out _)) continue;
                        int score = card.Attack * 3 + card.Defense +
                            (star == card.StarA ? 1 : 0);
                        if (score <= bestScore) continue;
                        bestScore = score;
                        bestHand = h;
                        bestSlot = slot;
                        bestStar = star;
                        bestPosition = card.Defense > card.Attack + 500
                            ? BattlePosition.Defense : BattlePosition.Attack;
                    }
                }
            }
            if (bestHand >= 0 && !duel.TryPlay(side, new[] { bestHand },
                FieldLane.Monster, bestSlot, bestPosition, bestStar, out var reason))
                throw new InvalidOperationException("Reference placement illegal: " + reason);
        }

        private static void TakeReferenceAttacks(DuelMatch duel, DuelSide side)
        {
            for (int step = 0; step < DuelMatch.ZoneCount; step++)
            {
                int bestFrom = -1, bestTarget = -1, bestScore = int.MinValue;
                for (int from = 0; from < DuelMatch.ZoneCount; from++)
                {
                    if (duel.Side(side).Monster(from) == null) continue;
                    for (int target = -1; target < DuelMatch.ZoneCount; target++)
                    {
                        if (!duel.TryPreviewAttack(side, from, target,
                            out var forecast, out _)) continue;
                        if (forecast.DamageToAttacker > 0) continue;
                        if (!forecast.IsDirect && !forecast.DefenderDestroyed) continue;
                        int score = forecast.DamageToDefender * 3 +
                            (forecast.DefenderDestroyed ? 700 +
                                forecast.DefenderCard.Attack / 2 : 200);
                        if (forecast.AttackerDestroyed) score -= 950;
                        if (score <= bestScore) continue;
                        bestScore = score;
                        bestFrom = from;
                        bestTarget = target;
                    }
                }
                if (bestFrom < 0 || bestScore <= 0) break;
                if (!duel.TryAttack(side, bestFrom, bestTarget, out var reason))
                    throw new InvalidOperationException("Reference attack illegal: " + reason);
                if (duel.Phase == MatchPhase.Finished) return;
            }
        }

        private static void TakeAdaptiveTurn(DuelMatch duel, AdaptiveOpponentBrain brain)
        {
            if (brain.TryChoosePlacement(duel, out var placement))
            {
                if (!duel.TryPlay(DuelSide.Opponent, placement.HandIndices,
                    FieldLane.Monster, placement.Slot, placement.Position,
                    placement.Star, out var reason))
                    throw new InvalidOperationException("Adaptive placement illegal: " + reason);
            }
            if (!duel.TryBeginBattle(DuelSide.Opponent, out var battleError))
                throw new InvalidOperationException("Adaptive battle illegal: " + battleError);
            for (int i = 0; i < DuelMatch.ZoneCount &&
                duel.Phase != MatchPhase.Finished; i++)
            {
                if (!brain.TryChooseAttack(duel, out var from, out var to)) break;
                if (!duel.TryAttack(DuelSide.Opponent, from, to, out var reason))
                    throw new InvalidOperationException("Adaptive attack illegal: " + reason);
            }
        }

        private static GameResult Simulate(int seed, AdaptiveOpponentBrain brain,
            AiLearningState profile, bool train, bool referenceOpponent)
        {
            var player = SyntheticDeck(unchecked(seed * 2 + 127), "P");
            var opponent = SyntheticDeck(unchecked(seed * 2 + 991), "O");
            var duel = new DuelMatch(player, opponent);
            if (!referenceOpponent)
            {
                if (brain == null || profile == null)
                    throw new ArgumentException("Adaptive benchmark requires a profile and brain");
                brain.StartDuel(profile);
            }
            int actions = 0;
            while (duel.Phase != MatchPhase.Finished && duel.Turn <= MaxTurns)
            {
                DuelSide side = duel.CurrentSide;
                if (side == DuelSide.Opponent && !referenceOpponent)
                    TakeAdaptiveTurn(duel, brain);
                else
                {
                    TakeReferencePlacement(duel, side);
                    if (!duel.TryBeginBattle(side, out var reason))
                        throw new InvalidOperationException("Reference battle illegal: " + reason);
                    TakeReferenceAttacks(duel, side);
                }
                if (duel.Phase == MatchPhase.Finished) break;
                if (!duel.TryEndTurn(side, out var turnReason))
                    throw new InvalidOperationException("Turn end illegal: " + turnReason);
                actions++;
                if (actions > MaxTurns + 2)
                    throw new InvalidOperationException("Simulation exceeded bounded action budget");
            }
            if (train && duel.Phase == MatchPhase.Finished)
                if (!brain.RecordCompletedDuel(duel))
                    throw new InvalidOperationException("Completed duel was not learned");
            return new GameResult {
                Winner = duel.Winner,
                Turns = duel.Turn,
                OpponentAttacks = duel.Events.Count(e =>
                    e.Kind == MatchEventKind.Attack && e.Side == DuelSide.Opponent),
                OpponentFusions = duel.Events.Count(e =>
                    e.Kind == MatchEventKind.Fusion && e.Side == DuelSide.Opponent)
            };
        }

        private static RunSummary Evaluate(int seed, int games,
            AiLearningState profile, bool referenceOpponent)
        {
            var result = new RunSummary { Games = games };
            long turnSum = 0;
            for (int i = 0; i < games; i++)
            {
                int gameSeed = unchecked(seed + i * 7919);
                // Brain RNG is fresh for each paired test; profile values
                // are held constant during the entire evaluation set.
                var brain = referenceOpponent ? null :
                    new AdaptiveOpponentBrain(new Random(unchecked(gameSeed + 5137)));
                var single = Simulate(gameSeed, brain, profile, false, referenceOpponent);
                turnSum += single.Turns;
                result.TotalAttacks += single.OpponentAttacks;
                result.TotalFusions += single.OpponentFusions;
                if (single.Winner == DuelSide.Opponent) result.Wins++;
                else if (single.Winner == DuelSide.Player) result.Losses++;
                else result.Unresolved++;
            }
            result.WinRate = games == 0 ? 0 : (double)result.Wins / games;
            result.AverageTurns = games == 0 ? 0 : (double)turnSum / games;
            return result;
        }

        private static AiLearningState Copy(AiLearningState state)
        {
            return new AiLearningState("lab:ephemeral") {
                duels = state.duels, wins = state.wins,
                playerAttacks = state.playerAttacks,
                playerFusions = state.playerFusions,
                previousTactic = state.previousTactic,
                tacticValues = (float[])state.tacticValues.Clone()
            };
        }

        public static BenchmarkReport Run(int seed, int trainingGames, int evalGames)
        {
            if (trainingGames < 1 || trainingGames > 250)
                throw new ArgumentOutOfRangeException(nameof(trainingGames), "Use 1..250");
            if (evalGames < 1 || evalGames > 250)
                throw new ArgumentOutOfRangeException(nameof(evalGames), "Use 1..250");
            var fresh = new AiLearningState("lab:ephemeral");
            var report = new BenchmarkReport {
                Seed = seed, TrainingGames = trainingGames, EvaluationGames = evalGames,
                ScriptedReference = Evaluate(unchecked(seed + 200000), evalGames, null, true),
                AdaptiveBefore = Evaluate(unchecked(seed + 200000), evalGames, Copy(fresh), false)
            };
            var trained = Copy(fresh);
            int completed = 0;
            for (int i = 0; i < trainingGames; i++)
            {
                int gameSeed = unchecked(seed + 900000 + i * 7919);
                var brain = new AdaptiveOpponentBrain(new Random(unchecked(gameSeed + 5137)));
                var match = Simulate(gameSeed, brain, trained, true, false);
                if (match.Winner.HasValue) completed++;
            }
            report.TrainingCompleted = completed;
            report.LearnedDuels = trained.duels;
            report.LearnedTacticValues = (float[])trained.tacticValues.Clone();
            report.AdaptiveAfter = Evaluate(unchecked(seed + 200000),
                evalGames, Copy(trained), false);
            report.WinRateDifference = report.AdaptiveAfter.WinRate -
                report.AdaptiveBefore.WinRate;
            return report;
        }
    }

    internal static class Program
    {
        private static int ArgInt(string[] args, string key, int fallback)
        {
            int index = Array.IndexOf(args, key);
            if (index < 0) return fallback;
            if (index + 1 >= args.Length ||
                !int.TryParse(args[index + 1], out int value))
                throw new ArgumentException("Expected integer after " + key);
            return value;
        }

        private static string ArgString(string[] args, string key)
        {
            int index = Array.IndexOf(args, key);
            if (index < 0) return null;
            if (index + 1 >= args.Length)
                throw new ArgumentException("Expected path after " + key);
            return args[index + 1];
        }

        private static void SelfTest()
        {
            var first = AiLab.Run(101, 3, 4);
            var second = AiLab.Run(101, 3, 4);
            string a = JsonSerializer.Serialize(first);
            string b = JsonSerializer.Serialize(second);
            if (a != b) throw new Exception("Failed seeded reproducibility");
            foreach (var summary in new[] {
                first.ScriptedReference, first.AdaptiveBefore, first.AdaptiveAfter })
            {
                if (summary.Games != 4 ||
                    summary.Wins + summary.Losses + summary.Unresolved != 4 ||
                    summary.WinRate < 0 || summary.WinRate > 1 ||
                    summary.TotalAttacks < 0 || summary.AverageTurns < 1)
                    throw new Exception("Invalid benchmark summary accounting");
            }
            if (first.TrainingCompleted != first.LearnedDuels)
                throw new Exception("Learning count differs from finished training duels");
            Console.WriteLine("PASS: deterministic paired seeds, bounded games, independent evaluation and consistent learning counts");
        }

        private static int Main(string[] args)
        {
            try
            {
                if (Array.IndexOf(args, "--help") >= 0)
                {
                    Console.WriteLine("AiLab --train 20 --eval 20 --seed 1209 --out <path.json> | --self-test");
                    return 0;
                }
                if (Array.IndexOf(args, "--self-test") >= 0)
                {
                    SelfTest();
                    return 0;
                }
                var report = AiLab.Run(ArgInt(args, "--seed", 1209),
                    ArgInt(args, "--train", 20), ArgInt(args, "--eval", 20));
                string output = JsonSerializer.Serialize(report,
                    new JsonSerializerOptions { WriteIndented = true });
                string path = ArgString(args, "--out");
                if (path != null)
                {
                    string full = Path.GetFullPath(path);
                    Directory.CreateDirectory(Path.GetDirectoryName(full));
                    File.WriteAllText(full, output + Environment.NewLine);
                    Console.WriteLine("Lab report: " + full);
                }
                Console.WriteLine(output);
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("AiLab failed: " + ex.Message);
                return 1;
            }
        }
    }
}
