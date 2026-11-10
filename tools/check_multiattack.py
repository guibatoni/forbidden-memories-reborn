#!/usr/bin/env python3
"""Structural checks only: not a Unity compilation or gameplay simulation."""
from pathlib import Path
r = Path(__file__).resolve().parents[1]
def src(path):
    return (r / path).read_text(encoding="utf-8")
planner = src("Assets/Scripts/Core/MultiAttackPlanner.cs")
ai = src("Assets/Scripts/Core/AdaptiveOpponentBrain.cs")
controller = src("Assets/Scripts/Presentation/OpponentTurnDirector.cs")
smoke = src("tools/Reborn.Core.Smoke/Program.cs")
editor = src("Assets/Tests/Editor/AdaptiveOpponentBrainTests.cs")
checks = {
    "planner stays independent of Unity": "UnityEngine" not in planner,
    "never inspects hidden hands or decks": ".Hand" not in planner and ".Deck" not in planner,
    "only plans during opponent Battle phase": "match.CurrentSide != DuelSide.Opponent" in planner and "match.Phase != MatchPhase.Battle" in planner,
    "uses real duel forecast for legal initial clashes": "match.TryPreviewAttack(DuelSide.Opponent, i, j" in planner,
    "simulated direct attacks share combat math": "new CombatForecast(attacker[i], null)" in planner,
    "does not mutate the match": "match.TryAttack(" not in planner and "match.TryPlay(" not in planner,
    "5x5 slot masks and bounded search": "attackerMask" in planner and "defenderMask" in planner and "Dictionary<long, PlannedAttackSequence>" in planner,
    "does not attack a surviving defender": "if (!forecast.IsDirect && !forecast.DefenderDestroyed) return;" in planner,
    "blocks losing defense attacks": "if (forecast.DamageToAttacker > 0) return;" in planner,
    "lethal moves prioritized": "if (lethal) utility += 200000;" in planner,
    "planner's first action is revalidated": "match.CanAttack(DuelSide.Opponent, first.AttackerSlot" in planner,
    "real director checks before execution": "game.CanAttack(DuelSide.Opponent,attackerSlot,targetSlot" in controller,
    "replans every real action": "brain.TryChooseAttack(game" in controller,
    "adaptive training unchanged": "RecordCompletedDuel" in ai and "LastAttackPlan = sequence;" in ai,
    "smoke tests sequence and non-mutation": "planner clears 1800 DEF with 2100 ATK" in smoke and "planning multiple attacks never changes duel state" in smoke,
    "Unity Editor test authored": "MultistepSearchClearsWallWithWeakerMonsterBeforeDirectAttack" in editor,
}
for name, good in checks.items():
    print(("OK " if good else "FAIL ") + name)
if not all(checks.values()):
    raise SystemExit("Multi-attack planner contract failed")
print("PASS: " + str(len(checks)) + " static multistep planner checks (Unity execution pending)")
