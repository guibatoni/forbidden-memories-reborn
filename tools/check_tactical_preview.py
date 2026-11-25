#!/usr/bin/env python3
"""Static Unity wiring checks. These are not a Unity Editor compile/run."""
from pathlib import Path
root = Path(__file__).resolve().parents[1]

def read(path):
    return (root / path).read_text(encoding="utf-8")

engine = read("Assets/Scripts/Core/DuelMatch.cs")
brain = read("Assets/Scripts/Core/AdaptiveOpponentBrain.cs")
planner = read("Assets/Scripts/Core/MultiAttackPlanner.cs")
hud = read("Assets/Scripts/Presentation/ArenaHud.cs")
input_script = read("Assets/Scripts/Presentation/ArenaInteraction.cs")
combat = read("Assets/Scripts/Presentation/CombatExplanation.cs")
smoke = read("tools/Reborn.Core.Smoke/Program.cs")
tests = read("Assets/Tests/Editor/DuelMatchTests.cs")
checks = {
    "player preview uses authoritative forecast": "match.TryPreviewAttack(DuelSide.Player,SelectedAttacker" in hud,
    "preview is gated by battle and player selection": "AttackMode && SelectedAttacker >= 0" in hud and
        "match.Phase == MatchPhase.Battle" in hud,
    "preview is not an attack": "match.TryAttack(" not in hud,
    "mouse hover tracks authorized opponent monster slot": "hud.PreviewTargetSlot = legalAim && aimTarget >= 0" in input_script and
        "zone.Lane == FieldLane.Monster" in input_script and
        "match.CanAttack(DuelSide.Player, hud.SelectedAttacker, aimTarget, out _)" in input_script,
    "preview hides on disabled/locked inputs": "hud.PreviewTargetSlot = -1;" in input_script,
    "combat details use real projected bonuses": "CombatExplanation.Describe(estimate)" in hud and
        "DefenderGuardianBonus" in combat,
    "same forecast drives AI planner": "MultiAttackPlanner.TryPlan(match" in brain and "match.TryPreviewAttack(DuelSide.Opponent" in planner,
    "planner avoids defense backlash": "forecast.DamageToAttacker > 0" in planner,
    "planner only considers destructive attacks": "if (!forecast.IsDirect && !forecast.DefenderDestroyed) return;" in planner,
    "empty slots reject attack": "targetSlot >= 0 && defender == null" in engine,
    "explicit direct attack remains": "targetSlot == -1 && enemy.MonsterCount != 0" in engine,
    "smoke covers defensive Guardian and target safety": "adaptive AI refuses losing defense" in smoke and
        "clicking an empty zone cannot turn into a direct attack" in smoke,
    "Unity Editor regressions written": "TacticalAiSkipsLosingGuardianDefenseAndChoosesWinningBattle" in tests,
}
for title, passed in checks.items():
    print(("OK " if passed else "FAIL ") + title)
if not all(checks.values()):
    raise SystemExit("Tactical preview integration contract failed")
print("PASS: " + str(len(checks)) + " tactical source checks (Unity execution pending)")
