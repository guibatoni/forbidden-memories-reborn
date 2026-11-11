#!/usr/bin/env python3
"""Static UX contracts for v0.24, not a substitute for Unity Play tests."""
from pathlib import Path
root=Path(__file__).resolve().parents[1]
def read(p): return (root/p).read_text(encoding="utf-8")
main=read("Assets/Scripts/Presentation/ArenaInteraction.cs")
keyboard=read("Assets/Scripts/Presentation/ArenaInteraction.Keyboard.cs")
bindings=read("Assets/Scripts/Presentation/RebornKeyBindings.cs")
settings=read("Assets/Scripts/Presentation/ArenaHud.KeyboardSettings.cs")
hud=read("Assets/Scripts/Presentation/ArenaHud.cs")
wizard=read("Assets/Scripts/Presentation/ArenaHud.PlacementWizard.cs")
equip=read("Assets/Scripts/Presentation/ArenaHud.EquipChoice.cs")
arena=read("Assets/Scripts/Presentation/ArenaFactory.cs")
engine=read("Assets/Scripts/Core/DuelMatch.FusionPreview.cs")
starter=read("Assets/Scripts/Presentation/ArenaHud.StarterDeckChoice.cs")
checks={
 "hotkeys configured with requested Space/Q/E/Z/X/S/W/arrows":
    all(f"return Key.{key};" in bindings for key in
        ("Space","Q","E","Z","X","S","W","LeftArrow","RightArrow",
         "UpArrow","DownArrow")),
 "key mappings are saved locally and conflicts rejected":
    "PlayerPrefs.SetString" in bindings and "PlayerPrefs.Save" in bindings and
    "keys[other] == key" in bindings,
 "keyboard config overlay offers reset and per-action remapping":
    "DrawKeyboardSettings()" in hud and
    "ResetDefaults()" in settings and
    "capturingKey" in settings and
    "CONFIGURAR TECLADO" in settings,
 "keyboard remapping cannot trigger duel commands":
    "if (hud.KeyboardSettingsOpen)" in keyboard and
    keyboard.index("if (hud.KeyboardSettingsOpen)") <
    keyboard.index("if (hud.PlacementWizardOpen)"),
 "mouse and keyboard pass through same match command":
    "ProcessMatchKeyboard()" in main and
    "hud.TryPassPlayerTurn();" in keyboard,
 "X cancels without consuming cards":
    "ClearPlayerSelection()" in keyboard and
    "CancelPlacementChoices()" in wizard and
    "match.TryPlay" not in wizard,
 "up marks numbered fusion and down removes ordered material":
    "MarkHandForFusion(keyboardIndex)" in keyboard and
    "UnmarkHandFusion(keyboardIndex)" in keyboard and
    "SetFusionOrder(fusionSelection.IndexOf(view.HandIndex) + 1)" in main,
 "double click chooses automatic legal slot":
    "bool doubleClick =" in main and "TryAutoPlaceSelected()" in main and
    "Time.unscaledTime-lastHandClickTime <= 0.40f" in main,
 "auto-slot is ascending and never replaces occupied zones":
    "for (int slot = 0; slot < DuelMatch.ZoneCount; slot++)" in keyboard and
    "match.Player.Monster(slot) == null" in keyboard and
    "match.Player.Support(slot) == null" in keyboard,
 "auto-slot only executes preflight not immediate TryPlay":
    "match.CanPlay(DuelSide.Player, order, lane, slot" in keyboard and
    "PlaceMatchCard(zone);" in keyboard and
    "match.TryPlay(" not in keyboard,
 "true ordered fusion counted from exact catalog":
    "fusions.TryFuse(last, next" in engine and
    "FusionRecipeCount => fusions.Count" in engine and
    "CountHandFusionSteps" in keyboard,
 "monster position decided before actual hand commit":
    wizard.index("placementStep = PlacementChoiceStep.Position") <
    wizard.index("private void CommitPlacement()") and
    "if (placementPreview != null &&" in wizard and
    "else CommitPlacement();" in wizard,
 "ATK/DEF is a visual rotating card, not two written choice buttons":
    "GUIUtility.RotateAroundPivot" in wizard and
    '"ATAQUE"' not in wizard and '"DEFESA"' not in wizard and
    "CardArtCatalog.Get(placementPreview)" in wizard,
 "wizard exposes remappable Z / X / Q/E":
    "hud.RotatePlacementPreview(true)" in keyboard and
    "hud.ConfirmPlacementChoice()" in keyboard and
    "hud.CancelPlacementChoices()" in keyboard,
 "keyboard board navigation can attack and inspect":
    "KeyboardLocation.EnemyMonster" in keyboard and
    "hud.RequestPlayerAttack" in keyboard and
    "hud.ToggleCardInspection" in keyboard,
 "starter choice is possible with keyboard":
    "hud.ConfirmStarterChoice();" in keyboard and
    "MoveStarterChoice" in starter,
 "equip modal offers direct application or face down setting":
    "ConfirmEquipSetChoice" in equip and
    "CommitSetEquipOverMonster" in main and
    "CanEquipFromHand" in main,
 "face down equip stack has true support zone storage":
    "overlayEquip" in arena and
    '"Player_EquipOverlay_"' in main and
    "match.TryPlay(DuelSide.Player,new[] {handIndex}" in main and
    "FieldLane.Support,monsterSlot" in main,
 "mouse continues to work after keyboard actions":
    "TrackMouseActivity(pointer);" in main and
    "mouse.rightButton.wasPressedThisFrame" in keyboard,
}
for label,ok in checks.items():
    print(("OK " if ok else "FAIL ")+label)
if not all(checks.values()):
    raise SystemExit("v0.24 UX contracts failed")
print(f"PASS: {len(checks)} v0.24 source-only keyboard/fusion/auto-slot contracts")
