#!/usr/bin/env python3
"""Check legal targeting and rendering boundaries (not a Unity runtime test)."""
from pathlib import Path
root = Path(__file__).resolve().parents[1]
def read(path):
    return (root / path).read_text(encoding="utf-8")
aim = read("Assets/Scripts/Presentation/AttackAimLine.cs")
factory = read("Assets/Scripts/Presentation/ArenaFactory.cs")
interaction = read("Assets/Scripts/Presentation/ArenaInteraction.cs")
checks = {
    "visual aim has no engine dependencies": "using Reborn.Core;" not in aim and "TryAttack(" not in aim,
    "aim line disables casting and colliders": "LineRenderer" in aim and "shadowCastingMode = ShadowCastingMode.Off" in aim and "AddComponent<Collider>" not in aim,
    "aim trajectory does not move camera": "Camera." not in aim and "transform.position =" not in aim,
    "arena creates a reusable aim component": "private AttackAimLine attackAim;" in factory and "visual.AddComponent<AttackAimLine>()" in factory,
    "arena accepts direct attack sentinel": "targetSlot < -1" in factory and "targetSlot >= 0" in factory,
    "arena hides aim on match redraw": "SetAttackAim(-1, -1);" in factory,
    "only authoritative CanAttack enables aim": "match.CanAttack(DuelSide.Player, hud.SelectedAttacker, aimTarget, out _)" in interaction,
    "aim disabled for illegal attacks": "arena.SetAttackAim(legalAim ? hud.SelectedAttacker : -1," in interaction,
    "face-down cards are not read": "FaceDown" not in aim and "CardDefinition" not in aim,
    "aim cleared when input is locked": "arena.SetAttackAim(-1, -1);" in interaction,
    "HUD preview honors actual target": "hud.PreviewTargetSlot = legalAim && aimTarget >= 0 ? aimTarget : -1;" in interaction,
    "direct attack is gated by empty field": "match.Opponent.MonsterCount == 0" in interaction,
}
for name, passed in checks.items():
    print(("OK " if passed else "FAIL ") + name)
if not all(checks.values()):
    raise SystemExit("Attack aim source checks failed")
print(f"PASS: {len(checks)} attack aim source checks (Unity runtime validation pending)")
