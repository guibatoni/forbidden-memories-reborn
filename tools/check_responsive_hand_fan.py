#!/usr/bin/env python3
"""Hand fan focus/inversion source contracts. Not a Unity render test."""
from pathlib import Path
p = Path(__file__).resolve().parents[1]
def read(name):
    return (p / name).read_text(encoding="utf-8")

geometry = read("Assets/Scripts/Core/HandFanGeometry.cs")
factory = read("Assets/Scripts/Presentation/ArenaFactory.cs")
view = read("Assets/Scripts/Presentation/CardView.cs")
interaction = read("Assets/Scripts/Presentation/ArenaInteraction.cs")
smoke = read("tools/Reborn.Core.Smoke/Program.cs")
hud = read("Assets/Scripts/Presentation/ArenaHud.cs")
checks = {
    "old card spacing remains documented": "1.05 + 0.90 = 1.95" in geometry,
    "expanded card spacing is 1.95": "ExpandedSpacing = 1.95f" in geometry,
    "compact spacing is tighter": "CompactSpacing = 1.15f" in geometry,
    "compact layout is lower": "CompactBaseY = 0.44f" in geometry and
        "ExpandedBaseY = 0.58f" in geometry,
    "outer cards curve toward player": "baseZ - Math.Abs(spread) * depth" in geometry,
    "tilt inverted": "spread * angle" in geometry,
    "five card order not inverted": "index - (count - 1) * 0.5f" in geometry,
    "factory uses shared geometry": "HandFanGeometry.At(index, count, handExpanded)" in factory,
    "card gets smoothly moving target": "card.SetHandLayoutTarget(" in factory and
        "homeLocalPosition + Vector3.up * raise" in view,
    "no camera control modified": "Camera.fieldOfView = 48f;" in factory and
        "Orbit.ResetView();" in factory,
    "focus changes targets not instances": "public void SetHandFocus(bool expanded)" in factory and
        "handExpanded = expanded;" in factory and
        "Object.Destroy(" not in factory[factory.index("public void SetHandFocus("):
            factory.index("public bool IsPointerNearHand(")],
    "input recognises hand and arena": interaction.count("arena.IsPointerNearHand(pointer)") == 2,
    "drag keeps fan open": interaction.count("(dragging ||") >= 2,
    "HUD region prevents focus": interaction.count("arena.SetHandFocus(!overHud") == 2,
    "opponent turn does not expose hand": "arena.SetHandFocus(false);" in interaction,
    "focus stable across both projected layouts": "for (int mode = 0; mode < 2; mode++)" in factory and
        "Camera.WorldToScreenPoint(" in factory,
    "root collider follows card": "transform.localPosition = Vector3.Lerp(" in view,
    "fusion selection is retained": "view.SetFusionOrder(fusionSelection.IndexOf(view.HandIndex) + 1)" in interaction,
    "hand geometry C# assertions": "responsive fan preserves left-to-right original card order" in smoke and
        "outer hand cards curve toward player" in smoke,
    "HUD remains compatible": "DuelModeActive" in hud,
    "new hover gap is modeled in core": "public static HandFanPose WithHoveredCard(" in geometry,
    "focused card does not shift": "index == hoveredIndex" in geometry and
        "return pose;" in geometry,
    "left and right neighbors move apart": "index < hoveredIndex ? -1f : 1f" in geometry,
    "compact arena view has no hover gap": "!expanded || hoveredIndex < 0" in geometry,
    "arena recomputes targets instead of rebuilding": "public void SetHandHover(" in factory and
        "private void RefreshHandTargets()" in factory,
    "mouse input pins card while pressed": interaction.count("arena.SetHandHover(") == 3 and
        interaction.count("mouse.leftButton.isPressed") >= 3,
    "larger hover lift preserves subtle selections": "hovered ? 0.80f : (selected ? 0.22f : 0f)" in view and
        "selected ? 1.065f" in view,
    "expanded projected hit area covers lifted card": "0.95f" in factory and
        "const float marginX = 30f" in factory,
    "smoke covers 1-5 focus indices": "for (int handCount = 1; handCount <= 5; handCount++)" in smoke,
}
for name, passed in checks.items():
    print(("OK " if passed else "FAIL ") + name)
if not all(checks.values()):
    raise SystemExit("Responsive hand fan contracts failed")
print(f"PASS: {len(checks)} responsive hand fan source checks (Unity render pending)")
