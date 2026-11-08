# Forbidden Memories Reborn — Prototype 0.9: Adaptive Opponents

## What this version really is
A **local contextual-bandit-style adaptive AI**, not a trained neural network.
It learns simple preferences from completed duels and alternates between five tactics:
Balanced, Aggressive, Defensive, Fusion-focused and Counterplay.

### Player experience
- Each new duel picks a strategy using learned results and randomized exploration.
- Card choices and legal attack targeting are scored with tactic-specific priorities.
- Prioritized attacks avoid known losing combat; opponents never read player hand or deck.
- After victory or defeat, the opponent learns from the winner, remaining life points, and
  how many attacks/fusions the player performed.
- Partial/abandoned matches do not update learning.
- A match is counted only once; strategy selection is saved per opponent.
- The right-hand panel shows the current tactic and number of finished learning duels.
- A modded ISO has separate learning records from another disc edition.

### Where the memory is stored
Local only: Application.persistentDataPath/Reborn/adaptive-ai-v1.json

This file stores only a per-opponent key, total duels/wins, the aggregate numbers of
player attacks/fusions and five small tactic scores. There are no saved hands,
card names, raw game logs, personal identifiers or cloud uploads.

To reset all learned behavior, **close the game**, make a backup of the file above,
then delete adaptive-ai-v1.json. The game recreates it automatically on later duels.

### Limitations
- Randomized tactics and shuffled decks improve replayability but cannot
  **guarantee** that no two games ever follow the same sequence.
- This is v1 online learning with a heuristic action evaluator. It does not yet
  perform deep lookahead, self-play training, neural reinforcement learning or
  full evaluation of magics, traps, equip effects and ritual outcomes.
- All rules are enforced by the independent DuelMatch C# engine.
- The camera and fixed five monster/five support slots per player are unchanged.
- No original copyrighted card art, ISO assets, or extracted dialogue are published.

### Verification
GitHub Actions compiles Core on .NET 8 and executes seeded smoke assertions.
Additional editor tests are included for Unity. Unity scenes and JSON persistence
must be validated on Windows Unity 6000.3.28f1.
