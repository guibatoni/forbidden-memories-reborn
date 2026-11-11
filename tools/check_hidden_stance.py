#!/usr/bin/env python3
"""Static contracts for face-down monsters and visual identity (not a Unity Play test)."""
from pathlib import Path
root=Path(__file__).resolve().parents[1]
def read(path):
    return (root/path).read_text(encoding="utf-8")
core=read("Assets/Scripts/Core/DuelMatch.cs")
arena=read("Assets/Scripts/Presentation/ArenaFactory.cs")
wizard=read("Assets/Scripts/Presentation/ArenaHud.PlacementWizard.cs")
hud=read("Assets/Scripts/Presentation/ArenaHud.DuelFlow.cs")
smoke=read("tools/Reborn.Core.Smoke/Program.cs")
unity=read("Assets/Tests/Editor/DuelMatchTests.cs")
between=lambda start,end: core.split(start,1)[1].split(end,1)[0]
setposition=between("internal void SetPosition(BattlePosition position)", "internal void Reveal()")
attack=between("public bool TryAttack(DuelSide side", "private void Damage(DuelSide side")
checks={
    "stance changes do not flip a concealed card": "Position = position;" in setposition and
        "FaceDown = false" not in setposition,
    "revealing is an explicit operation": "internal void Reveal() { FaceDown = false; }" in core,
    "same-position input no longer used as an implicit reveal":
        "if (monster.Position == newPosition)" in core and
        "&& !monster.FaceDown" not in between("public bool CanChangePosition(", "public bool TryChangePosition("),
    "duel combat reveals attacker/target only when attack commits":
        "if (attacker.FaceDown) attacker.Reveal();" in attack and
        "if (defender != null && defender.FaceDown) defender.Reveal();" in attack,
    "combat previews do not reveal a card":
        "Reveal()" not in between("public bool TryPreviewAttack(", "public bool TryAttack("),
    "arena render uses actual face-down state":
        "monster.FaceDown" in arena and "MakeCard(side+\"_Monster_\"" in arena,
    "ATK/DEF placement wizard preserves engine state":
        "UseDefense = defense;" in wizard and
        "Reveal()" not in wizard and
        "PlaceFaceDown = !placementIsRitual && secondary;" in wizard and
        "GUIUtility.RotateAroundPivot" in wizard,
    "Q/E still changes position through DuelMatch":
        "match.TryChangePosition(DuelSide.Player, slot, next" in hud,
    "headless regression asserts hidden after toggling":
        "new facedown monster can toggle ATK/DEF repeatedly without revealing" in smoke and
        "set monster remains hidden when switched to ATK on a later turn" in smoke,
    "headless regression asserts hidden AI movement":
        "AI switches previously set DEF into ATK while remaining hidden" in smoke,
    "Unity regression covers both attack and target reveal":
        "SetMonsterRemainsHiddenOnPositionChangesUntilItAttacks" in unity and
        "FacedownTargetCanSwitchToAttackWithoutRevealingUntilHit" in unity,
}
for desc,ok in checks.items():print(("OK " if ok else "FAIL ")+desc)
if not all(checks.values()):
    raise SystemExit("FaceDown persistence source contracts failed")
print(f"PASS: {len(checks)} static persistence contracts; Unity visual test required")
