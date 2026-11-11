#!/usr/bin/env python3
"""0.24.2 release gate for keyboard state and offline fusion diagnosis.
Static contracts are not Unity Player Mode tests; use EditMode NUnit too.
"""
from pathlib import Path

root=Path(__file__).resolve().parents[1]
def read(name):
    return (root/name).read_text(encoding="utf-8")
hud=read("Assets/Scripts/Presentation/ArenaHud.DuelFlow.cs")
keyboard=read("Assets/Scripts/Presentation/ArenaInteraction.Keyboard.cs")
inspect=read("tools/diagnosticar_fusoes.py")
tests=read("tools/test_fusion_diagnostics.py")
unity=read("Assets/Tests/Editor/KeyboardPositionTests.cs")
controls=read("Assets/Scripts/Presentation/ArenaHud.KeyboardSettings.cs")
checks={
    "Q/E sets explicit positions rather than toggling":
        "hud.TrySetSelectedMonsterPosition(requestAttack ?" in keyboard and
        "BattlePosition.Attack : BattlePosition.Defense" in keyboard,
    "mouse toggle remains available as a separate action":
        "public void TryChangeSelectedMonster()" in hud and
        "TrySetSelectedMonsterPosition(monster.Position ==" in hud,
    "same-position keyboard command emits no events":
        "if (monster.Position == position) return;" in hud,
    "position still changes only through DuelMatch rules":
        "match.TryChangePosition(DuelSide.Player, slot, position" in hud,
    "field selection restored after permitted position change":
        "SelectedFieldMonster = slot;" in hud,
    "face-down logic is independent of ATK/DEF selector":
        "FaceDown = false" not in hud and
        "FaceDown = false" not in keyboard,
    "EditMode test exercises explicit Q/E, no-op and concealment":
        "ExplicitPositionsAreIdempotentAndDoNotRevealFaceDownCards" in unity and
        "hud.TrySetSelectedMonsterPosition(BattlePosition.Attack)" in unity and
        "hud.TrySetSelectedMonsterPosition(BattlePosition.Defense)" in unity,
    "fusion diagnosis is local and never changes ISO or uploads data":
        "original_export.json" in inspect and
        "path.read_text" in inspect and
        "write_text(" not in inspect and
        "requests" not in inspect,
    "catalog can contain 722 cards and still fail with zero fusions":
        "ZERO RECEITAS DE FUSAO" in inspect and
        "return 1 if errors else 0" in inspect and
        "722 cards alone cannot pass" in tests,
    "fusion recipes checked for IDs, real monsters and verified flag":
        "f.get(\"verified\") is not True" in inspect and
        'get("kind") or "").lower() != "monster"' in inspect and
        "if key in unique:" in inspect,
    "Windows command is available to audit prior imports":
        (root/"tools/DIAGNOSTICAR_FUSOES.ps1").is_file(),
    "configuration load remains in Awake after hotfix":
        "keyBindings.LoadSaved();" in controls and
        "private void Awake()" in controls,
}
for title,ok in checks.items():
    print(("OK " if ok else "FAIL ")+title)
if not all(checks.values()):
    raise SystemExit("0.24.2 stability regressions failed")
print(f"PASS: {len(checks)} QA invariants (Unity Game View validation still required)")
