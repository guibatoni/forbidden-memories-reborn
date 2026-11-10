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
