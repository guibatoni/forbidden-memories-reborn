# Prototype 0.12.1 — Multi-seed AI Lab

## Expanded evaluation
This adds a multi-seed, scenario-aware headless benchmark for adaptive AI.
It does not modify Unity scenes, camera, DuelMatch rules, user save files or
opponent decision logic.

Three synthetic scenarios:
- balanced: existing reference decks with mixed ATK and DEF
- defensive: reference player's monsters receive +800 DEF and slightly lower ATK
- aggressive: reference player's monsters receive +650 ATK and reduced DEF

For each scenario and independent training seed:
1. Evaluate a **fresh** adaptive opponent versus the fixed scripted player.
2. Train a fresh isolated profile using the configured training games.
3. Evaluate the **trained** profile on the exact same evaluation seeds.
4. Record wins, unresolved games, average turns and the paired win-rate change.
5. Also evaluate a scripted opponent for context.

The same evaluation seed is paired between before/after, but each training
seed starts with a separate fresh profile. Results for separate games sharing
one profile are correlated, so the 95% interval resamples **seed clusters**,
not individual duels. We use 2,000 reproducible bootstrap resamples per scenario.

## Run on Windows (with .NET 8 SDK)
From the project root:

    dotnet run --project tools/Reborn.AiLab/Reborn.AiLab.csproj -- --self-test

For the expanded standard benchmark:

    dotnet run --project tools/Reborn.AiLab/Reborn.AiLab.csproj -- --batch --seeds 10 --train 20 --eval 25 --seed 1209 --scenarios balanced,defensive,aggressive --out ai-lab/batch.json --csv ai-lab/batch.csv

This runs 2,850 total duels: 10 seeds x 3 scenarios x
(20 training + 25 scripted reference + 25 adaptive before +
25 adaptive after) = 2,850.

Larger experimental runs, within the 20,000-duel safety limit, can use
--seeds 20 --train 30 --eval 40. The seed values and report parameters
make a run reproducible.

## Report
The JSON provides:
- Runs: raw per-seed records, including number of training games completed
- Scenarios: pooled win rates, seed-level paired differences and bootstrap
  confidence bounds for the mean before-to-after change
- Interpretation: positive-signal-not-proof, negative-signal, inconclusive,
  or exploratory-too-few-independent-seeds when fewer than 10 seeds are used
- TotalSimulatedDuels: total cost of evaluating and training the batch

The CSV makes per-seed comparisons easy to plot in Excel or other tools.

A confidence interval that excludes zero is not proof of learning
generalization. This is an exploratory bootstrap using synthetic opponents
and monster-only rules, not a controlled study on real players or authentic
Forbidden Memories decks. Multiple scenario testing also increases false
positive risk; no automatic deployment decision is made from this report.

## Privacy and limitations
- The benchmark runs on synthetic cards only and never accesses the
  user's local ISO, artwork, opponent decks or persisted AI training data.
- It does not perform two-agent learned-vs-learned self-play.
- It does not yet model traps, spells, equipment effects, rituals,
  unknown face-down cards, or deep lookahead across turns.
- No inference server, Vercel or cloud LLM is required for gameplay.
- There is no change to the camera, game zones or current Unity controls.

## Verification
GitHub Actions compiles .NET 8, runs the original core smoke tests,
checks the deterministic single- and multi-seed self-tests, generates
2,850 simulation games, and reconciles CSV/JSON metrics.
Unity rendering is not tested in CI.

Recovery branch: backup/prototype-0.12.1
ChatGPT Library: Forbidden Memories Reborn/Backups/Prototype 0.12.1.
