# Forbidden Memories Reborn — Prototype 0.13: AI Learning 2.0

## Why the upgrade
The 0.12.1 benchmark found **no consistent before/after win-rate improvement**
across 2,850 synthetic games. In the defensive scenario, many evaluations
were unresolved. The existing strategy preferences changed, but had
little influence on actual placement decisions.

## Two policies, evaluated independently
- **Legacy (1.0)**: preserved placement scoring and outcome-based training.
- **Learning 2.0** (default in Unity): evaluates both attack/defense placement
  using authoritative CombatForecast results against the visible field.
  It estimates immediate removals, retaliation, Guardian Star bonuses,
  risk of losing the summoned monster and opportunities to break stalled
  defenses. Offensive, defensive and counterplay styles now influence the
  scored placements more directly.

This is still a seeded, offline heuristic AI with tactical adaptation,
not a neural network or full reinforcement learning algorithm.

### What persists
Learning 2.0 also maintains per-tactic games and wins alongside the v1
profile. Old adaptive-ai-v1.json files remain readable; missing arrays
are initialized safely. Only completed duels change the record. There
are no cloud requests or player hand/deck reads by the AI.

## Headless A/B benchmark
The expanded AI Lab runs **3,600 synthetic duels**:

- 10 independent training seeds per scenario
- Three scenarios: balanced, defensive and aggressive
- 20 games training each new profile
- 25 same-seed games for each of: scripted reference, legacy AI, Learning 2.0
  untrained, and Learning 2.0 trained
- 95% cluster bootstrap intervals for:
  1. Learning 2.0 untrained vs legacy, and
  2. Learning 2.0 trained vs untrained

Run from a project checkout with .NET 8:

    dotnet run --project tools/Reborn.AiLab/Reborn.AiLab.csproj -- --self-test

    dotnet run --project tools/Reborn.AiLab/Reborn.AiLab.csproj -- --batch --seeds 10 --train 20 --eval 25 --seed 1209 --scenarios balanced,defensive,aggressive --out ai-lab/batch.json --csv ai-lab/batch.csv

JSON and CSV can be downloaded from GitHub Actions.

## Interpreting results
These experiments isolate two questions:
- Does the **new placement policy** outperform the 1.0 placement policy?
- Does **training the new policy** improve outcomes versus a fresh 2.0 profile?

Comparisons use the same holdout seeds and must not be interpreted as
claims of stronger play in the real game. Intervals that cover zero mean
the observed difference is inconclusive; multiple scenarios are exploratory.

## Current limitations
- Only visible monsters and the already implemented combat/fusion rules
  are scored; there is no legitimate preview of hidden face-down cards.
- No support for spell, trap, equip and ritual effects during lookahead.
- Unresolved games at the turn cap remain unresolved (not converted
  into false wins). A different board evaluation may reduce or increase
  the stall rate; benchmark results decide.
- Unity runtime compilation, visual animation and user-game experience
  require local validation in Unity 6000.3.28f1.
- The fixed camera, arena layout, five monster zones and five support
  zones per side remain unchanged.

## Backups
Recovery branch: backup/prototype-0.13
ChatGPT Library: Forbidden Memories Reborn / Backups / Prototype 0.13
Only source and synthetic benchmark metrics are shared publicly; disc
images, extracted card artworks, scripts and local AI saves are not.


## First measured A/B run (seed 1209; 10 training seeds per scenario)
Automated test: https://github.com/guibatoni/forbidden-memories-reborn/actions/runs/42737466287

| Scenario | Legacy v1 | 2.0 untrained | 2.0 trained | 2.0 vs v1 95% seed-block bootstrap CI | Unresolved v1 / 2.0 fresh / 2.0 trained |
| --- | ---: | ---: | ---: | ---: | ---: |
| Balanced | 49.2% | 52.4% | 52.4% | +1.2 to +5.2 percentage points | 0 / 0 / 0 |
| Defensive | 30.0% | 28.8% | 29.6% | -4.4 to +1.6 percentage points | 102 / 109 / 112 |
| Aggressive | 4.4% | 4.0% | 4.0% | -1.2 to 0 percentage points | 0 / 0 / 0 |

The **policy change** has a positive exploratory signal on the balanced
scenario but not on the others. The **additional training** does not have a
consistent, positive signal for any scenario in this experiment.

Keep these two separate questions in mind: improved tactical placement is not
the same thing as evidence that the preference-learning routine is effective.
The defensive scenario's unresolved games have become slightly more frequent.
We should prioritize legal ways to break board stalls and test using verified
card mechanics, not invent rules to force a winner.

The 95% intervals are seed-block bootstrap estimates from only ten independent
training seeds and should not be treated as proof of generalization.
