#!/usr/bin/env python3
"""Source-only contracts for v0.25 validated Fusion Assistant.

Does not substitute Unity 6000.3 EditMode/Game View validation.
"""
from pathlib import Path
root = Path(__file__).resolve().parents[1]
def read(p): return (root/p).read_text(encoding="utf-8")
engine=read("Assets/Scripts/Core/DuelMatch.FusionOptions.cs")
preview=read("Assets/Scripts/Core/DuelMatch.FusionPreview.cs")
dialog=read("Assets/Scripts/Presentation/ArenaHud.FusionAssistant.cs")
hud=read("Assets/Scripts/Presentation/ArenaHud.cs")
keys=read("Assets/Scripts/Presentation/RebornKeyBindings.cs")
keyboard=read("Assets/Scripts/Presentation/ArenaInteraction.Keyboard.cs")
mouse=read("Assets/Scripts/Presentation/ArenaInteraction.cs")
wizard=read("Assets/Scripts/Presentation/ArenaHud.PlacementWizard.cs")
smoke=read("tools/Reborn.Core.Smoke/Program.cs")
nunit=read("Assets/Tests/Editor/FusionAssistantTests.cs")
workflow=read(".github/workflows/core-smoke.yml")
checks={
 "Only registered original fusion recipes generate suggestions":
    "fusions.TryFuse(owner.Hand[a], owner.Hand[b]" in engine and
    "fusions.TryFuse(field.Card, owner.Hand[hand]" in engine,
 "Only own hand and own field are inspected":
    "var owner = Side(side);" in engine and
    "Side(Other(side))" not in engine,
 "Assistant never spends cards on preview":
    "TryPlay(" not in engine and "TryPlayOnField(" not in engine and
    "state.Take(" not in engine and "Emit(" not in engine,
 "Available recipes respect current turn and placement state":
    "hasPlayed || fusions.Count == 0" in engine and
    "CanPlay(side, path, FieldLane.Monster" in engine and
    "CanPlayOnField(side, path, slot" in engine,
 "UI offers an honest zero-recipe diagnostic":
    "match.FusionRecipeCount == 0" in dialog and
    "DIAGNOSTICAR_FUSOES.ps1" in dialog,
 "F hotkey exists and is configurable":
    "FusionAssistant" in keys and
    "case RebornKeyAction.FusionAssistant: return Key.F;" in keys and
    "Shortcut(keyboard,RebornKeyAction.FusionAssistant)" in keyboard,
 "Clicking or keyboard-selecting closes modal before callback":
    "CloseFusionAssistant(); // Unblock" in dialog and
    "FusionRequested?.Invoke(chosen)" in dialog and
    "hud.FusionRequested += ChooseFusionOption;" in mouse,
 "Modal locks all gameplay input and releases it on reset":
    "FusionAssistantOpen;" in hud and
    "CloseFusionAssistant();" in hud and
    "if (hud.FusionAssistantOpen)" in keyboard,
 "Keyboard arrows no longer erase selected fusion materials":
    "Navigation does NOT cancel the ordered materials" in keyboard and
    "if (fusionSelection.Count > 0)" in keyboard,
 "Double-click on selected material preserves 2+ material selection":
    "fusionSelection.Contains(index)" in mouse and
    "bool preserveSelection = shift ||" in mouse and
    "TryAutoPlaceSelected()" in mouse,
 "Auto-placement refuses no-recipe multi-material discards":
    "order.Count > 1" in keyboard and
    "match.CountHandFusionSteps(DuelSide.Player, order) == 0" in keyboard,
 "Field replacement remains legal but is visually labeled, not a fusion":
    "CountFieldFusionSteps" in preview and
    "replacesWithoutFusion" in mouse and
    "placementOverwritesField" in wizard and
    "SEM RECEITA:" in wizard,
 "NUnit verifies modal does not mutate cards and unlocks before callback":
    "FusionModalDoesNotSpendCardsAndUnblocksBeforeCallback" in nunit and
    "NoRecipeModalIsInformativeButNeverFabricatesOptions" in nunit,
 "Core smoke verifies real hand and field combinations":
    "CheckAvailableFusionAssistant()" in smoke and
    "0.25 field assistant considers own field monster" in smoke,
 "No duplicate Actions build on the same feature-branch push and PR":
    "branches: [main]" in workflow and
    "pull_request:" in workflow and
    "cancel-in-progress: true" in workflow,
}
for label, ok in checks.items():
    print(("OK " if ok else "FAIL ") + label)
if not all(checks.values()):
    raise SystemExit("FAIL: 0.25 verified fusion assistant contracts")
print(f"PASS: {len(checks)} source-only checks (Unity Editor test pending)")
