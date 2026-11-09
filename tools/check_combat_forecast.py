#!/usr/bin/env python3
"""Source-level guard for the common combat forecast; Unity rendering is not tested."""
from pathlib import Path
r = Path(__file__).resolve().parents[1]
def get(path):
    return (r / path).read_text(encoding="utf-8")
engine = get("Assets/Scripts/Core/DuelMatch.cs")
forecast = get("Assets/Scripts/Core/CombatForecast.cs")
narration = get("Assets/Scripts/Presentation/CombatExplanation.cs")
player = get("Assets/Scripts/Presentation/PlayerAttackDirector.cs")
opponent = get("Assets/Scripts/Presentation/OpponentTurnDirector.cs")
smoke = get("tools/Reborn.Core.Smoke/Program.cs")
checks = {
    "readonly combat preview": "public bool TryPreviewAttack(" in engine and
        "forecast = new CombatForecast(" in engine,
    "execution uses forecast": "if (!TryPreviewAttack(side, fromSlot, targetSlot, out var forecast" in engine and
        "if (forecast.DefenderDestroyed)" in engine,
    "guardian bonuses for attacker and defender": "GuardianStarRules.Bonus(attacker.Star, defender.Star)" in forecast and
        "GuardianStarRules.Bonus(defender.Star, attacker.Star)" in forecast,
    "defense comparison uses DEF": "defender.Card.Attack : defender.Card.Defense" in forecast,
    "defender destruction uses effective attack": "EffectiveAttack > EffectiveDefense" in forecast,
    "defense battle does not damage defender's LP": "if (DefenderPosition == BattlePosition.Attack)" in forecast,
    "both sides announce actual forecast": "CombatExplanation.Describe(preview)" in player and
        "CombatExplanation.Describe(preview)" in opponent,
    "nonmutating forecast precedes execution": player.index("match.TryPreviewAttack(") <
        player.index("match.TryAttack("),
    "diagnostic bonus shown": "DefenderGuardianBonus" in narration and
        "EffectiveDefense" in narration,
    "2100 vs 1800 regression tests": "2100 ATK destroys 1800 DEF" in smoke and
        "2300 effective DEF" in smoke,
}
for label, ok in checks.items():
    print(("OK " if ok else "FAIL ") + label)
if not all(checks.values()):
    raise SystemExit("Combat forecast contract failed")
print("PASS: " + str(len(checks)) + " static combat forecast checks (no Unity compilation)")
