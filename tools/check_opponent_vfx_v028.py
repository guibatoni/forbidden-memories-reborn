#!/usr/bin/env python3
"""Static presentation contract for v0.28, not a Unity Game View test."""
from pathlib import Path
root = Path(__file__).resolve().parents[1]
def read(name):
    return (root / name).read_text(encoding="utf-8")

arena = read("Assets/Scripts/Presentation/ArenaFactory.cs")
director = read("Assets/Scripts/Presentation/OpponentTurnDirector.cs")
local = read("tools/VALIDAR_REBORN.ps1")
checks = {
    "Trails support player and opponent lanes without touching duel rules":
        "DuelSide side = DuelSide.Player)" in arena and
        "zone.Opponent != (side == DuelSide.Opponent)" in arena,
    "Opponent hand/field streams are visual only and conceal identities":
        director.count("arena.PlayFusionMaterialTrails(") >= 2 and
        "DuelSide.Opponent);" in director and
        "new List<Vector3>" in director,
    "Opponent multi-hand chains animate only after confirmed Fusion events":
        "game.Events[e].Kind == MatchEventKind.Fusion" in director and
        "if (verifiedFusionSteps > 0)" in director and
        "arena.PlayFieldCombinationEffect" in director,
    "Scene waits for completion of staggered VFX even at fast pace":
        "Mathf.Max(1.12f, 1.20f * hud.OpponentPace)" in director and
        "yield return PauseForPlacement();" in director,
    "Visuals never control LP, graveyard or hidden card contents":
        "TryAttack(" not in arena and
        "TryPlayOnField(" not in arena,
    "Windows local validator includes these contracts":
        '"check_opponent_vfx_v028.py"' in local,
}
for label, passed in checks.items():
    print(("OK " if passed else "FAIL ") + label)
if not all(checks.values()):
    raise SystemExit("Incomplete 0.28 opponent visual contract")
print(f"PASS: {len(checks)} static presentation checks; Unity Editor verification pending")
