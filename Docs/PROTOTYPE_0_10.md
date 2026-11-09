# Forbidden Memories Reborn — Prototype 0.10

## Scope
This milestone joins the combat preview, adaptive opponent decisions and authoritative
DuelMatch calculation in one shared read-only CombatForecast. Cards, fixed five
monster/five support zones per side, scene and camera remain unchanged.

## In the game
- Enter the battle phase and click **Selecionar ataque**.
- Select your attacking monster.
- Hover over one of the opponent's monster zones. The reserved left duel log panel
  shows effective attack/defense, Guardian Star +500 where appropriate,
  predicted destruction and LP consequences.
- When the opposing board is empty, the panel displays the direct-attack estimate.
  Use **Atacar diretamente** to execute it. Hovering on an empty zone does not
  trigger a direct attack.
- Combat preview does not change monster state, life, attack usage, or event log.
  The projection is recalculated on execution after the attack animation.
- Outside the preview, the standard recent-actions list remains available.
- Unity's enlarged card inspection gives way to the battle estimate only while
  attack selection is active.

## Adaptive AI improvement
The opponent now uses DuelMatch.TryPreviewAttack to evaluate its targets.
This removes the independently duplicated Guardian Star comparison from the
AI's attack decision and prevents attacks that would deal backlash damage into
higher defense. Legal destructive attacks and direct lethal attacks are
evaluated according to the opponent's currently learned style.
This is still an offline heuristic contextual-bandit AI, not neural self-play.

## Known limitations
- Enemy field monsters in the prototype are displayed openly, including defense
  positions. Face-down concealment and an uncertainty-aware preview are for
  a later version. We must not use this forecast to expose truly hidden cards.
- Spells, equips, traps, rituals and their modifiers remain pending validation.
- Strategy currently only evaluates attacks one at a time; future versions can
  search multiple attacks and turns and train via self-play.
- The effective values are the rules engine's prototype interpretation until
  all original mechanics and modded effects are validated.
- Static checks and .NET core tests do not compile or render the Unity project.

## Validation on Unity 6000.3.28f1
1. Back up the existing 0.9.3 project, stop Play and close the editor.
2. Install the clean source or merge the incremental patch.
3. Start a duel; enter battle; activate attack selection; select a monster.
4. Hover a defensive target and check whether effective ATK/DEF and Guardian
   Star adjustments match the result after executing the attack.
5. Try 2100 ATK versus 1800 DEF, both with and without the defender's +500 star.
6. Observe whether the adaptive opponent avoids known harmful attacks.
7. Check the Unity Console for red errors and report any divergence.

## Backups and privacy
- Branch: backup/prototype-0.10
- ChatGPT Library folder: Forbidden Memories Reborn/Backups/Prototype 0.10
- No ROM, proprietary card art, extracted card text or local learning profiles
  are committed to this public repository.
