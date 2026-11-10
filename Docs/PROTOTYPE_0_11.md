# Forbidden Memories Reborn — Prototype 0.11: Multi-attack planning

## Purpose
The offline opponent AI now evaluates sequences of attacks instead of selecting
each attack greedily. It can preserve a stronger monster for a direct strike
while using a weaker monster to clear an opposing defense position.

Example: opponent has 2100 ATK and 3200 ATK; player has one 1800 DEF monster.
The search prefers 2100 ATK -> 1800 DEF (no LP loss), then 3200 ATK -> direct.
This is a **planning example**, not a guarantee that every duel has the same result.

## Algorithm / safety
- Explores up to 5 attacking monsters and 5 visible defenders using bitmasks
  with memoization; the search never modifies the real DuelMatch state.
- Uses DuelMatch.TryPreviewAttack and CombatForecast to calculate each existing
  legal battle, including winning/losing Guardian Star bonuses.
- For hypothetical direct attacks after the last defender is destroyed, uses
  CombatForecast's existing direct-attack calculation.
- Scores removed monsters, LP damage, tactical variation and lethal attacks.
  It excludes attacks that cannot destroy a defender or cause LP backlash.
  Trades may be considered only where their expected tactical value is positive.
- Each proposed first move is checked against DuelMatch.CanAttack. The Unity
  controller checks legality again just before animation/resolution.
- After every completed attack, the AI replans using the **actual** current
  duel state. No predicted future is treated as authoritative.
- The opponent learns tactical preferences from completed duels using the
  existing locally stored adaptive profiles. No LLM or cloud connection.

## Presentation
The opponent's staged action flow is unchanged. If it chooses multiple
attacks, it briefly announces the expected sequence length, then selects,
animates and resolves each real attack separately. Animation speed is
still controlled by the existing pace selector.

## Scope limitations
The model is a heuristic search over the currently visible board. It does not
simulate unsupported spell/trap/equip/ritual effects, unknown face-down cards,
the human player's next turn, or the retail game's exact AI scripts. The game
must not use this exact forecast against concealed information without first
adding an uncertainty model. No algorithm can promise that no duel will ever
repeat.

## Local test
1. Preserve a copy of your 0.10 project, then install 0.11 sources.
2. Open with Unity 6000.3.28f1, clear Console, and enter an experimental duel.
3. When the AI controls multiple attackers, watch the attack sequence message.
4. Observe its attempts to clear weak defenders before using stronger attackers
   for direct damage; confirm LP changes match the combat preview.
5. Compare its play across multiple duels and verify learning persistence.
6. Report any Console errors or cases where attacking the correct target was
   possible but the AI passed instead.

## Verification and backups
GitHub Actions compiles Reborn.Core and executes seeded deterministic assertions.
Static checks inspect Unity integration source code; they do NOT compile the
entire Unity Editor or validate 3D animation. The 0.11 source archive excludes
the disc, licensed card art, extracted text and local AI learning profiles.
Recovery branch: backup/prototype-0.11.
ChatGPT Library: Forbidden Memories Reborn/Backups/Prototype 0.11.
