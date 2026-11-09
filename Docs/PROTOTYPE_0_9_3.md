# Prototype 0.9.3 — Guardian Star Combat Diagnostics

## Reported symptom
A 2100 ATK monster attacked a monster displaying 1800 DEF, but the defender
remained on the board. Without additional modifiers it should have been destroyed.

## Rule and investigation
The Guardian Star bonus can add 500 to the defender's effective defense:
- 2100 ATK vs 1800 DEF, no bonus: defender is destroyed, no LP damage.
- 2100 ATK vs 1800 DEF with 500 defender Guardian bonus (2300 effective DEF):
  defender survives, attacker loses 200 LP.
- 2100 ATK vs 2100 effective DEF: neither is destroyed and neither takes damage.

The actual stars from the player's reported duel were not recorded, so we
cannot prove whether that specific attack was a bug or a legitimate bonus.

## Changes
- A single, read-only CombatForecast in Reborn.Core calculates Guardian Star
  bonuses, attack/defense values, destruction and LP damage.
- The authoritative DuelMatch.TryAttack invokes that same calculation.
- Both player and opponent attack directors announce actual effective strengths
  and outcomes once the combat resolves.
- Preview does not emit events, consume an attack or alter life points.
- Regression tests cover 2100 ATK vs 1800 DEF with and without +500, ties,
  preview immutability, and the defender's graveyard entry.
- No camera, arena, deck or AI behavioral changes.

## Local validation in Unity 6000.3.28f1
After updating the project, clear the Console and start a test duel. With a
2100 ATK monster, attack an 1800 DEF defender. Read the bottom combat log:
  ATK 2100 vs DEF 1800 ... defensor destruido.
When the opponent has winning Guardian advantage, expect:
  ATK 2100 vs DEF 1800 + 500 Guardian (efetivo 2100 vs 2300):
  defesa resistiu; atacante perdeu 200 LP.

These are prototype rules, without active spell/equip modifiers. If an actual
battle behaves differently from the reported effective values, capture the
turn, card IDs, Guardian Stars and Console for diagnosis.

## Testing and backups
GitHub Actions compiles Reborn.Core (.NET 8), runs deterministic checks, then
runs static presentation contracts. Those tests do not compile Unity rendering.
A source-only ZIP is uploaded to CI and a recovery branch is created; disc
images, extracted art and adaptive AI saves are deliberately excluded.
