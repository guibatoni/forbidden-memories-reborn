using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Reborn.Core;

namespace Reborn.AiLab
{
    /// <summary>A paired evaluation of the same seed and synthetic scenario.</summary>
    public sealed class BatchRun
    {
        public string Scenario { get; set; }
        public int Seed { get; set; }
        public int TrainingScheduled { get; set; }
        public int TrainingCompleted { get; set; }
        public int EvaluationGames { get; set; }
        public int ReferenceWins { get; set; }
        public int LegacyWins { get; set; }
        public int LegacyUnresolved { get; set; }
        public double LegacyWinRate { get; set; }
        public double ImprovementOverLegacy { get; set; }
        public int BeforeWins { get; set; }
        public int AfterWins { get; set; }
        public int BeforeUnresolved { get; set; }
        public int AfterUnresolved { get; set; }
        public double BeforeWinRate { get; set; }
        public double AfterWinRate { get; set; }
        public double WinRateDifference { get; set; }
        public double BeforeAverageTurns { get; set; }
        public double AfterAverageTurns { get; set; }
    }

    /// <summary>
    /// Cluster confidence bounds resample full seed blocks, not individual
    /// games. Correlation within the same trained profile is preserved.
    /// This is exploratory, not a guarantee of generalization to real players.
    /// </summary>
    public sealed class ScenarioAggregate
    {
        public string Scenario { get; set; }
        public int Repetitions { get; set; }
        public int EvaluationGames { get; set; }
        public int TrainingScheduled { get; set; }
        public int TrainingCompleted { get; set; }
        public int UnresolvedBefore { get; set; }
        public int UnresolvedAfter { get; set; }
        public double ScriptedReferenceWinRate { get; set; }
        public double LegacyBeforeWinRate { get; set; }
        public double LegacyVsLearning2 { get; set; }
        public double LegacyVsLearning2ConfidenceLower { get; set; }
        public double LegacyVsLearning2ConfidenceUpper { get; set; }
        public int LegacyUnresolved { get; set; }
        public double BeforeWinRate { get; set; }
        public double AfterWinRate { get; set; }
        public double WinRateDifference { get; set; }
        public double WinRateConfidenceLower { get; set; }
        public double WinRateConfidenceUpper { get; set; }
        public double BeforeAverageTurns { get; set; }
        public double AfterAverageTurns { get; set; }
        public string Interpretation { get; set; }
    }

    public sealed class BatchReport
    {
        public string Schema { get; set; } = "reborn-ai-lab-batch-v1";
        public string Method { get; set; } = "paired synthetic seeded evaluations; 95% seed-cluster bootstrap interval (2000 resamples)";
        public string Limitations { get; set; } =
            "Exploratory synthetic monster-only duels; results are not proof of stronger in-game AI. " +
            "Separate in-memory profiles per seed and scenario; no private saves or real disc data.";
        public int Seed { get; set; }
        public int SeedRepetitions { get; set; }
        public int TrainingGamesPerSeed { get; set; }
        public int EvaluationGamesPerSeed { get; set; }
        public int TotalSimulatedDuels { get; set; }
        public List<BatchRun> Runs { get; set; } = new List<BatchRun>();
        public List<ScenarioAggregate> Scenarios { get; set; } = new List<ScenarioAggregate>();
    }

    public static class AiLabBatch
    {
        private static double Round(double value) => Math.Round(value, 4,
            MidpointRounding.AwayFromZero);

        /// <summary>
        /// Reruns the existing 0.12 benchmark with independent training seeds.
        /// Fresh/trained policies use identical evaluation seeds within each row.
        /// </summary>
        public static BatchReport Run(int baseSeed, int trainGames, int evalGames,
            int repetitions, IReadOnlyList<string> scenarios)
        {
            if (repetitions < 2 || repetitions > 40)
                throw new ArgumentOutOfRangeException(nameof(repetitions), "Use 2..40 seed repetitions");
            if (trainGames < 1 || trainGames > 250)
                throw new ArgumentOutOfRangeException(nameof(trainGames), "Use 1..250");
            if (evalGames < 1 || evalGames > 250)
                throw new ArgumentOutOfRangeException(nameof(evalGames), "Use 1..250");
            if (scenarios == null || scenarios.Count < 1 || scenarios.Count > 3)
                throw new ArgumentException("Choose one to three known scenarios");
            var unique = new HashSet<string>(StringComparer.Ordinal);
            foreach (var scenario in scenarios)
                if ((scenario != "balanced" && scenario != "defensive" &&
                    scenario != "aggressive") || !unique.Add(scenario))
                    throw new ArgumentException("Unknown or duplicate scenario: " + scenario);
            long total = (long)scenarios.Count * repetitions *
                (trainGames + 4L * evalGames);
            if (total > 20000)
                throw new ArgumentException("Requested more than 20,000 simulated duels; reduce --seeds, --train or --eval");

            var report = new BatchReport {
                Seed = baseSeed,
                SeedRepetitions = repetitions,
                TrainingGamesPerSeed = trainGames,
                EvaluationGamesPerSeed = evalGames,
                TotalSimulatedDuels = (int)total
            };

            for (int scenarioIndex = 0; scenarioIndex < scenarios.Count; scenarioIndex++)
            {
                string scenario = scenarios[scenarioIndex];
                var runs = new List<BatchRun>(repetitions);
                for (int i = 0; i < repetitions; i++)
                {
                    int experimentSeed = unchecked(baseSeed +
                        scenarioIndex * 1597334677 + i * 1000003);
                    var result = AiLab.Run(experimentSeed, trainGames, evalGames, scenario,
                        AiBrainVersion.Learning2);
                    var legacy = AiLab.EvaluatePolicy(unchecked(experimentSeed + 200000),
                        evalGames, scenario, AiBrainVersion.Legacy);
                    var single = new BatchRun {
                        Scenario = scenario, Seed = experimentSeed,
                        TrainingScheduled = trainGames,
                        TrainingCompleted = result.TrainingCompleted,
                        EvaluationGames = evalGames,
                        ReferenceWins = result.ScriptedReference.Wins,
                        LegacyWins = legacy.Wins,
                        LegacyUnresolved = legacy.Unresolved,
                        LegacyWinRate = legacy.WinRate,
                        ImprovementOverLegacy = result.AdaptiveBefore.WinRate - legacy.WinRate,
                        BeforeWins = result.AdaptiveBefore.Wins,
                        AfterWins = result.AdaptiveAfter.Wins,
                        BeforeUnresolved = result.AdaptiveBefore.Unresolved,
                        AfterUnresolved = result.AdaptiveAfter.Unresolved,
                        BeforeWinRate = result.AdaptiveBefore.WinRate,
                        AfterWinRate = result.AdaptiveAfter.WinRate,
                        WinRateDifference = result.WinRateDifference,
                        BeforeAverageTurns = result.AdaptiveBefore.AverageTurns,
                        AfterAverageTurns = result.AdaptiveAfter.AverageTurns
                    };
                    runs.Add(single);
                    report.Runs.Add(single);
                }
                report.Scenarios.Add(Summarize(scenario, runs, baseSeed, scenarioIndex));
            }
            return report;
        }

        private static ScenarioAggregate Summarize(string scenario,
            List<BatchRun> runs, int seed, int scenarioIndex)
        {
            int n = runs.Count;
            int games = runs.Sum(x => x.EvaluationGames);
            double rawDelta = runs.Average(x => x.WinRateDifference);
            var diffs = runs.Select(x => x.WinRateDifference).ToArray();
            var legacyDiffs = runs.Select(x => x.ImprovementOverLegacy).ToArray();
            var draws = new double[2000];
            var drawsLegacy = new double[2000];
            var random = new Random(unchecked(seed ^
                (0x5F3759DF + scenarioIndex * 8191)));
            for (int b = 0; b < draws.Length; b++)
            {
                double sampleMean = 0;
                double sampleLegacy = 0;
                for (int j = 0; j < n; j++)
                {
                    int chosen = random.Next(n);
                    sampleMean += diffs[chosen];
                    sampleLegacy += legacyDiffs[chosen];
                }
                draws[b] = sampleMean / n;
                drawsLegacy[b] = sampleLegacy / n;
            }
            Array.Sort(draws);
            Array.Sort(drawsLegacy);
            double lower = draws[(int)(0.025 * (draws.Length - 1))];
            double upper = draws[(int)(0.975 * (draws.Length - 1))];
            double legacyLower = drawsLegacy[(int)(0.025 * (drawsLegacy.Length - 1))];
            double legacyUpper = drawsLegacy[(int)(0.975 * (drawsLegacy.Length - 1))];
            // With only a few independent training seeds, a cluster
            // interval is highly unstable; never label it a breakthrough.
            string interpretation;
            if (n < 10) interpretation = "exploratory-too-few-independent-seeds";
            else if (lower > 0) interpretation = "positive-signal-not-proof";
            else if (upper < 0) interpretation = "negative-signal";
            else interpretation = "inconclusive";
            return new ScenarioAggregate {
                Scenario = scenario, Repetitions = n,
                EvaluationGames = games,
                TrainingScheduled = runs.Sum(x => x.TrainingScheduled),
                TrainingCompleted = runs.Sum(x => x.TrainingCompleted),
                UnresolvedBefore = runs.Sum(x => x.BeforeUnresolved),
                UnresolvedAfter = runs.Sum(x => x.AfterUnresolved),
                ScriptedReferenceWinRate = Round(
                    (double)runs.Sum(x => x.ReferenceWins) / games),
                LegacyBeforeWinRate = Round((double)runs.Sum(x => x.LegacyWins) / games),
                LegacyVsLearning2 = Round(legacyDiffs.Average()),
                LegacyVsLearning2ConfidenceLower = Round(legacyLower),
                LegacyVsLearning2ConfidenceUpper = Round(legacyUpper),
                LegacyUnresolved = runs.Sum(x => x.LegacyUnresolved),
                BeforeWinRate = Round((double)runs.Sum(x => x.BeforeWins) / games),
                AfterWinRate = Round((double)runs.Sum(x => x.AfterWins) / games),
                WinRateDifference = Round(rawDelta),
                WinRateConfidenceLower = Round(lower),
                WinRateConfidenceUpper = Round(upper),
                BeforeAverageTurns = Round(runs.Average(x => x.BeforeAverageTurns)),
                AfterAverageTurns = Round(runs.Average(x => x.AfterAverageTurns)),
                Interpretation = interpretation
            };
        }

        public static string ToCsv(BatchReport report)
        {
            if (report == null) throw new ArgumentNullException(nameof(report));
            var output = new StringBuilder();
            output.AppendLine("scenario,seed,train_scheduled,train_completed,eval_games,reference_wins,legacy_wins,legacy_unresolved,legacy_win_rate,learning2_vs_legacy,before_wins,after_wins,before_unresolved,after_unresolved,before_win_rate,after_win_rate,delta");
            foreach (var row in report.Runs)
            {
                output.Append(row.Scenario).Append(',').Append(row.Seed).Append(',')
                    .Append(row.TrainingScheduled).Append(',')
                    .Append(row.TrainingCompleted).Append(',')
                    .Append(row.EvaluationGames).Append(',')
                    .Append(row.ReferenceWins).Append(',')
                    .Append(row.LegacyWins).Append(',')
                    .Append(row.LegacyUnresolved).Append(',')
                    .Append(row.LegacyWinRate.ToString("G17", CultureInfo.InvariantCulture)).Append(',')
                    .Append(row.ImprovementOverLegacy.ToString("G17", CultureInfo.InvariantCulture)).Append(',')
                    .Append(row.BeforeWins).Append(',')
                    .Append(row.AfterWins).Append(',')
                    .Append(row.BeforeUnresolved).Append(',')
                    .Append(row.AfterUnresolved).Append(',')
                    .Append(row.BeforeWinRate.ToString("G17", CultureInfo.InvariantCulture)).Append(',')
                    .Append(row.AfterWinRate.ToString("G17", CultureInfo.InvariantCulture)).Append(',')
                    .Append(row.WinRateDifference.ToString("G17", CultureInfo.InvariantCulture))
                    .AppendLine();
            }
            return output.ToString().TrimEnd('\r', '\n');
        }
    }
}
