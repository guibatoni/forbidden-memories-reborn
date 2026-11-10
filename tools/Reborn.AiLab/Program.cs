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
        public string Scenario { get; set; } = "balanced";
        public string Policy { get; set; } = "learning2";
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
        private static IReadOnlyList<CardDefinition> SyntheticDeck(int seed, string prefix, string scenario)
        {
            var rng = new Random(seed);
            var cards = new List<CardDefinition>(DuelMatch.DeckSize);
            for (int i = 0; i < DuelMatch.DeckSize; i++)
            {
                int attack = 800 + rng.Next(0, 24) * 100;
                int defense = 600 + rng.Next(0, 24) * 100;
                var starA = Stars[rng.Next(Stars.Length)];
                var starB = Stars[rng.Next(Stars.Length)];
                if (prefix == "P" && scenario == "defensive")
                {
                    defense += 800;
                    attack = Math.Max(500, attack - 100);
                }
                else if (prefix == "P" && scenario == "aggressive")
                {
                    attack += 650;
                    defense = Math.Max(300, defense - 300);
                }
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
            // Match the Unity AI: set defensive monsters can be flipped
            // in later turns, subject to the DuelMatch legality checks.
            for (int i = 0; i < DuelMatch.ZoneCount; i++)
            {
                if (!brain.TryChoosePositionChange(duel,
                    out int monsterSlot, out var position)) break;
                if (!duel.TryChangePosition(DuelSide.Opponent, monsterSlot,
                    position, out var switchError))
                    throw new InvalidOperationException(
                        "Adaptive position change illegal: " + switchError);
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
            AiLearningState profile, bool train, bool referenceOpponent, string scenario)
        {
            var player = SyntheticDeck(unchecked(seed * 2 + 127), "P", scenario);
            var opponent = SyntheticDeck(unchecked(seed * 2 + 991), "O", scenario);
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
            AiLearningState profile, bool referenceOpponent, string scenario,
            AiBrainVersion version)
        {
            var result = new RunSummary { Games = games };
            long turnSum = 0;
            for (int i = 0; i < games; i++)
            {
                int gameSeed = unchecked(seed + i * 7919);
                // Brain RNG is fresh for each paired test; profile values
                // are held constant during the entire evaluation set.
                var brain = referenceOpponent ? null :
                    new AdaptiveOpponentBrain(new Random(unchecked(gameSeed + 5137)), version);
                var single = Simulate(gameSeed, brain, profile, false, referenceOpponent, scenario);
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

        public static RunSummary EvaluatePolicy(int seed, int games,
            string scenario, AiBrainVersion version)
        {
            if (games < 1 || games > 250) throw new ArgumentOutOfRangeException(nameof(games));
            if (scenario != "balanced" && scenario != "defensive" && scenario != "aggressive")
                throw new ArgumentException("Unknown scenario", nameof(scenario));
            if (version != AiBrainVersion.Legacy && version != AiBrainVersion.Learning2)
                throw new ArgumentOutOfRangeException(nameof(version));
            return Evaluate(seed, games, new AiLearningState("lab:isolated"),
                false, scenario, version);
        }

        private static AiLearningState Copy(AiLearningState state)
        {
            return new AiLearningState("lab:ephemeral") {
                duels = state.duels, wins = state.wins,
                playerAttacks = state.playerAttacks,
                playerFusions = state.playerFusions,
                previousTactic = state.previousTactic,
                tacticValues = (float[])state.tacticValues.Clone(),
                tacticGames = (int[])state.tacticGames.Clone(),
                tacticWins = (int[])state.tacticWins.Clone()
            };
        }

        public static BenchmarkReport Run(int seed, int trainingGames, int evalGames,
            string scenario = "balanced",
            AiBrainVersion version = AiBrainVersion.Learning2)
        {
            if (scenario != "balanced" && scenario != "defensive" && scenario != "aggressive")
                throw new ArgumentException("Unknown synthetic scenario: " + scenario);
            if (trainingGames < 1 || trainingGames > 250)
                throw new ArgumentOutOfRangeException(nameof(trainingGames), "Use 1..250");
            if (evalGames < 1 || evalGames > 250)
                throw new ArgumentOutOfRangeException(nameof(evalGames), "Use 1..250");
            if (version != AiBrainVersion.Legacy && version != AiBrainVersion.Learning2)
                throw new ArgumentException("Invalid policy version", nameof(version));
            var fresh = new AiLearningState("lab:ephemeral");
            var report = new BenchmarkReport {
                Seed = seed, Scenario = scenario,
                Policy = version == AiBrainVersion.Legacy ? "legacy" : "learning2",
                TrainingGames = trainingGames, EvaluationGames = evalGames,
                ScriptedReference = Evaluate(unchecked(seed + 200000), evalGames,
                    null, true, scenario, version),
                AdaptiveBefore = Evaluate(unchecked(seed + 200000), evalGames,
                    Copy(fresh), false, scenario, version)
            };
            var trained = Copy(fresh);
            int completed = 0;
            for (int i = 0; i < trainingGames; i++)
            {
                int gameSeed = unchecked(seed + 900000 + i * 7919);
                var brain = new AdaptiveOpponentBrain(new Random(unchecked(gameSeed + 5137)), version);
                var match = Simulate(gameSeed, brain, trained, true, false, scenario);
                if (match.Winner.HasValue) completed++;
            }
            report.TrainingCompleted = completed;
            report.LearnedDuels = trained.duels;
            report.LearnedTacticValues = (float[])trained.tacticValues.Clone();
            report.AdaptiveAfter = Evaluate(unchecked(seed + 200000),
                evalGames, Copy(trained), false, scenario, version);
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
            var batch = AiLabBatch.Run(61, 2, 3, 3,
                new[] { "balanced", "defensive", "aggressive" });
            var batchAgain = AiLabBatch.Run(61, 2, 3, 3,
                new[] { "balanced", "defensive", "aggressive" });
            if (JsonSerializer.Serialize(batch) != JsonSerializer.Serialize(batchAgain))
                throw new Exception("Batch benchmark not reproducible");
            if (batch.Scenarios.Count != 3 || batch.Runs.Count != 9 ||
                batch.TotalSimulatedDuels != 126 ||
                batch.Scenarios.Any(x => x.Repetitions != 3 ||
                    x.LegacyBeforeWinRate < 0 || x.LegacyBeforeWinRate > 1 ||
                    x.LegacyVsLearning2ConfidenceLower > x.LegacyVsLearning2ConfidenceUpper ||
                    x.TrainingCompleted > x.TrainingScheduled ||
                    x.EvaluationGames != 9 ||
                    x.WinRateConfidenceLower > x.WinRateConfidenceUpper ||
                    x.WinRateConfidenceLower < -1 ||
                    x.WinRateConfidenceUpper > 1))
                throw new Exception("Batch summary accounting is inconsistent");
            if (batch.Runs.Select(x => x.Scenario).Distinct().Count() != 3 ||
                batch.Runs.GroupBy(x => x.Scenario).Any(group =>
                    group.Select(x => x.Seed).Distinct().Count() != 3))
                throw new Exception("Batch seeds or scenario isolation failed");
            bool invalidRejected = false;
            try { AiLabBatch.Run(1, 2, 2, 1, new[] { "balanced", "unknown" }); }
            catch (ArgumentException) { invalidRejected = true; }
            if (!invalidRejected) throw new Exception("Invalid scenario was accepted");
            var upgraded = new AdaptiveOpponentBrain(new Random(17), AiBrainVersion.Learning2);
            var preserved = new AdaptiveOpponentBrain(new Random(17), AiBrainVersion.Legacy);
            if (upgraded.Version != AiBrainVersion.Learning2 ||
                preserved.Version != AiBrainVersion.Legacy)
                throw new Exception("Policy version selection failed");
            Console.WriteLine("PASS: deterministic batch comparisons, v1/v2 policy isolation, three scenarios, paired seeds, bootstrap intervals and consistent accounting");
        }

        private static void Save(string path, string contents)
        {
            string full = Path.GetFullPath(path);
            Directory.CreateDirectory(Path.GetDirectoryName(full));
            File.WriteAllText(full, contents + Environment.NewLine);
            Console.WriteLine("AI Lab report: " + full);
        }

        private static int Main(string[] args)
        {
            try
            {
                if (Array.IndexOf(args, "--help") >= 0)
                {
                    Console.WriteLine("AI Lab: --batch --seeds 5 --train 30 --eval 30 --seed 1209 --scenarios balanced,defensive,aggressive --out report.json --csv seeds.csv");
                    Console.WriteLine("Legacy single-seed mode (omit --batch): --train 20 --eval 20 --seed 1209 --out report.json; --self-test");
                    return 0;
                }
                if (Array.IndexOf(args, "--self-test") >= 0)
                {
                    SelfTest();
                    return 0;
                }
                bool batchMode = Array.IndexOf(args, "--batch") >= 0;
                int seed = ArgInt(args, "--seed", 1209);
                int trainingGames = ArgInt(args, "--train", batchMode ? 30 : 20);
                int evaluationGames = ArgInt(args, "--eval", batchMode ? 30 : 20);
                int repetitions = ArgInt(args, "--seeds", 5);
                if (batchMode)
                {
                    string scenarioArg = ArgString(args, "--scenarios") ?? "balanced,defensive,aggressive";
                    var scenarioNames = scenarioArg.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    var aggregate = AiLabBatch.Run(seed, trainingGames, evaluationGames,
                        repetitions, scenarioNames);
                    string outputPath = ArgString(args, "--out");
                    if (outputPath != null) Save(outputPath, JsonSerializer.Serialize(aggregate,
                        new JsonSerializerOptions { WriteIndented = true }));
                    string csvPath = ArgString(args, "--csv");
                    if (csvPath != null) Save(csvPath, AiLabBatch.ToCsv(aggregate));
                    Console.WriteLine(JsonSerializer.Serialize(aggregate,
                        new JsonSerializerOptions { WriteIndented = true }));
                    return 0;
                }
                var report = AiLab.Run(seed,
                    trainingGames, evaluationGames);
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
