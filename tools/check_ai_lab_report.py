#!/usr/bin/env python3
"""Verify benchmark accounting, not statistical significance or model quality."""
import csv
import json
import math
import sys
from collections import Counter
from pathlib import Path

if len(sys.argv) != 3:
    raise SystemExit("usage: check_ai_lab_report.py report.json report.csv")
report = json.loads(Path(sys.argv[1]).read_text(encoding="utf-8"))
with Path(sys.argv[2]).open(encoding="utf-8", newline="") as handle:
    rows = list(csv.DictReader(handle))

assert report["Schema"] == "reborn-ai-lab-batch-v1"
assert report["SeedRepetitions"] == 10
assert report["TrainingGamesPerSeed"] == 20
assert report["EvaluationGamesPerSeed"] == 25
assert report["TotalSimulatedDuels"] == 3600
assert len(report["Runs"]) == len(rows) == 30
expected = {"balanced", "defensive", "aggressive"}
assert {s["Scenario"] for s in report["Scenarios"]} == expected
counts = Counter(r["Scenario"] for r in report["Runs"])
assert counts == dict.fromkeys(expected, 10)
for scenario in report["Scenarios"]:
    cases = [r for r in report["Runs"] if r["Scenario"] == scenario["Scenario"]]
    assert len({case["Seed"] for case in cases}) == 10
    assert scenario["Repetitions"] == 10
    assert scenario["EvaluationGames"] == 250
    assert scenario["TrainingScheduled"] == 200
    assert scenario["TrainingCompleted"] <= 200
    assert scenario["LegacyUnresolved"] >= 0
    assert 0 <= scenario["LegacyBeforeWinRate"] <= 1
    assert -1 <= scenario["LegacyVsLearning2ConfidenceLower"] <= scenario["LegacyVsLearning2ConfidenceUpper"] <= 1
    assert scenario["UnresolvedBefore"] >= 0
    assert scenario["UnresolvedAfter"] >= 0
    assert -1 <= scenario["WinRateConfidenceLower"] <= scenario["WinRateConfidenceUpper"] <= 1
    assert scenario["PolicyInterpretation"] in {
        "positive-signal-not-proof", "negative-signal", "inconclusive"}
    assert scenario["Interpretation"] in {
        "positive-signal-not-proof", "negative-signal", "inconclusive"}
    legacy = sum(r["LegacyWins"] for r in cases) / 250
    before = sum(r["BeforeWins"] for r in cases) / 250
    after = sum(r["AfterWins"] for r in cases) / 250
    assert math.isclose(scenario["LegacyBeforeWinRate"], legacy, abs_tol=0.00011)
    assert math.isclose(scenario["LegacyVsLearning2"], before - legacy, abs_tol=0.00011)
    assert math.isclose(scenario["BeforeWinRate"], before, abs_tol=0.00011)
    assert math.isclose(scenario["AfterWinRate"], after, abs_tol=0.00011)
    for case in cases:
        assert 0 <= case["LegacyWins"] <= 25
        assert 0 <= case["LegacyUnresolved"] <= 25
        assert case["LegacyWins"] + case["LegacyUnresolved"] <= 25
        assert 0 <= case["BeforeWins"] <= 25
        assert 0 <= case["AfterWins"] <= 25
        assert case["TrainingCompleted"] <= 20
        assert case["BeforeWins"] + case["BeforeUnresolved"] <= 25
        assert case["AfterWins"] + case["AfterUnresolved"] <= 25
        assert math.isclose(case["ImprovementOverLegacy"],
            case["BeforeWinRate"] - case["LegacyWinRate"], abs_tol=1e-10)
        assert math.isclose(case["WinRateDifference"],
            case["AfterWinRate"] - case["BeforeWinRate"], abs_tol=1e-10)
for row, case in zip(rows, report["Runs"]):
    assert row["scenario"] == case["Scenario"]
    assert int(row["seed"]) == case["Seed"]
    assert int(row["after_wins"]) == case["AfterWins"]
    assert int(row["legacy_wins"]) == case["LegacyWins"]
    assert int(row["legacy_unresolved"]) == case["LegacyUnresolved"]
print("PASS: 30 paired seed evaluations, 3 synthetic scenarios, 3,600 duels, CSV/JSON reconciliation and bootstrap bounds")
