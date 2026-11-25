#!/usr/bin/env python3
"""Static architecture guard for multi-step verified fusion recommendations.

This check is not a Unity Editor compilation or an ISO validation.
"""
from pathlib import Path

root = Path(__file__).resolve().parents[1]

def read(path):
    return (root / path).read_text(encoding="utf-8")

engine = read("Assets/Scripts/Core/DuelMatch.FusionOptions.cs")
keyboard = read("Assets/Scripts/Presentation/ArenaInteraction.Keyboard.cs")
dialog = read("Assets/Scripts/Presentation/ArenaHud.FusionAssistant.cs")
smoke = read("tools/Reborn.Core.Smoke/Program.cs")
unity_tests = read("Assets/Tests/Editor/FusionAssistantTests.cs")
validator = read("tools/VALIDAR_REBORN.ps1")
checks = {
    "Only confirmed FusionCatalog recipes drive multi-step exploration":
        "ExploreHandChains(" in engine and
        "ExploreFieldChains(" in engine and
        "fusions.TryFuse(current, owner.Hand[next]" in engine and
        "fusions.TryFuse(field.Card, owner.Hand[hand]" in engine,
    "Suggestions use an immutable ordered copy of hand indices":
        "HandIndices = Array.AsReadOnly(copy);" in engine and
        "public int FusionSteps" in engine,
    "Every suggestion still passes authoritative placement validation":
        "CanPlay(side, path" in engine and
        "CanPlayOnField(side, path" in engine,
    "Exploration never touches hidden enemy state or emits a card play":
        "Side(Other(side))" not in engine and
        "TryPlay(" not in engine and "TryPlayOnField(" not in engine and
        "Emit(" not in engine and "state.Take(" not in engine,
    "UI renders and selects the entire ordered chain":
        "option.HandIndices.Count" in dialog and
        "SameHandOrder(choice.HandIndices,selected.HandIndices)" in keyboard and
        "fusionSelection.AddRange(legal.HandIndices);" in keyboard,
    "Long fusion lists keep keyboard selection in view":
        "fusionScroll.y = top + 65f - viewport" in dialog,
    "F1 cannot open the key editor behind an active fusion modal":
        keyboard.index("if (hud.FusionAssistantOpen)") <
        keyboard.index("if (keyboard.f1Key.wasPressedThisFrame)"),
    "Deterministic and Unity EditMode checks cover multi-material selection":
        "CheckVerifiedFusionChains()" in smoke and
        "ChainedSelectionStaysReadOnlyAndKeepsFullOrderedMaterials" in unity_tests,
    "Local Windows validation includes this regression guard":
        '"check_fusion_chains_v026.py"' in validator,
}
for label, passed in checks.items():
    print(("OK   " if passed else "FAIL ") + label)
if not all(checks.values()):
    raise SystemExit("Missing verified-chain integration contract")
print(f"PASS: {len(checks)} v0.26 source contracts (Unity Game View still pending)")
