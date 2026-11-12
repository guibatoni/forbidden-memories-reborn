#!/usr/bin/env python3
"""Read-only source contract for Learning 2.0 opponent fusion planning.

Unity presentation and real ISO behavior still require local verification.
"""
from pathlib import Path

root = Path(__file__).resolve().parents[1]
def read(name):
    return (root / name).read_text(encoding="utf-8")

brain = read("Assets/Scripts/Core/AdaptiveOpponentBrain.cs")
director = read("Assets/Scripts/Presentation/OpponentTurnDirector.cs")
smoke = read("tools/Reborn.Core.Smoke/Program.cs")
local = read("tools/VALIDAR_REBORN.ps1")
checks = {
    "Learning 2.0 reads only verified, legal catalog choices":
        "match.FindAvailableFusions(DuelSide.Opponent)" in brain and
        "option.UsesField || option.HandIndices.Count < 3" in brain,
    "New multi-material behavior is gated to Learning 2.0":
        "if (Version == AiBrainVersion.Learning2)" in brain and
        "AiBrainVersion.Legacy" in smoke,
    "Long chains pay a bounded material-cost penalty":
        "Math.Max(0, indices.Length - 2)" in brain and
        "Tactic == AiTactic.FusionFocused ? 120" in brain,
    "AI considers all selected material indices, not just first pair":
        "indices[n] = option.HandIndices[n]" in brain and
        "EvaluatePlacementV2(match, indices, option.Result" in brain,
    "Only real fusion events trigger opponent VFX":
        "beforePlacementEvents = game.Events.Count;" in director and
        "game.Events[e].Kind == MatchEventKind.Fusion" in director and
        "if (verifiedFusionSteps > 0)" in director and
        "arena.PlayFieldCombinationEffect(DuelSide.Opponent" in director,
    "Deterministic smoke covers new policy and legacy comparison":
        "CheckAdaptiveVerifiedFusionChains()" in smoke and
        "0.27 legacy AI remains pair-only" in smoke,
    "Local validation includes AI integration check":
        '"check_ai_fusion_v027.py"' in local,
}
for label, passed in checks.items():
    print(("OK " if passed else "FAIL ") + label)
if not all(checks.values()):
    raise SystemExit("Incomplete 0.27 adaptive fusion integration")
print(f"PASS: {len(checks)} v0.27 contracts (not Unity Editor compilation)")
