#!/usr/bin/env python3
"""Validate field targeting/placement visuals. Does not run the Unity Editor."""
from pathlib import Path
root = Path(__file__).resolve().parents[1]
def read(path):
    return (root / path).read_text(encoding="utf-8")

zone = read("Assets/Scripts/Presentation/ZoneView.cs")
factory = read("Assets/Scripts/Presentation/ArenaFactory.cs")
input_code = read("Assets/Scripts/Presentation/ArenaInteraction.cs")
motion = read("Assets/Scripts/Presentation/CardMotion.cs")
hud = read("Assets/Scripts/Presentation/ArenaHud.cs")
core = read("Assets/Scripts/Core/DuelMatch.cs")

checks = {
    "all zone states are presentation-only": "public sealed class ZoneView : MonoBehaviour" in zone and
        "TryAttack(" not in zone and "TryPlay(" not in zone,
    "legal zones use own independent visual state": "private bool legalTarget;" in zone and
        "private bool pointerHover;" in zone,
    "illegal targets cannot look hovered": "bool isHovered = pointerHover && legalTarget;" in zone,
    "hovered legal target gets a brighter outline": "isHovered ? trimHover" in zone,
    "source monster is visually distinct": "selectedAttacker ? trimSource" in zone and
        "public void SetCombatSource(bool source)" in zone,
    "hovered border becomes thicker": "baseScale.x * 1.14f" in zone and
        "baseScale.y * 1.35f" in zone,
    "arena uses distinct valid and hover materials": "validHover" in factory and
        "attackHover" in factory and "attackSelected" in factory,
    "player placement calls actual CanPlay": "match.CanPlay(DuelSide.Player, selection," in input_code,
    "opponent target eligibility calls actual CanAttack": "match.CanAttack(DuelSide.Player," in input_code and
        "hud.SelectedAttacker, zone.Index" in input_code,
    "direct attacks only when field is clear": "match.Opponent.MonsterCount == 0" in input_code and
        "hud.SelectedAttacker, -1" in input_code,
    "own attacker must be legal": "match.CanAttack(DuelSide.Player," in input_code and
        "zone.Index, target, out _" in input_code,
    "match mouse updates zone hover": input_code.count("if (hoveredZone != zone)") >= 2 and
        "hoveredZone.SetHighlight(true);" in input_code,
    "selected source persists during battle": "zone.SetCombatSource(!zone.Opponent" in input_code,
    "no illegal selection persists after returning to main": "selectedAttacker = false;" in zone,
    "summon animation still starts from staged hand": "Play(pendingOrigin,placed.transform.position)" in factory and
        "AddComponent<CardMotion>()" in factory,
    "summon arc uses gentle travel ease": "Mathf.Sin(t * Mathf.PI) * 0.88f" in motion and
        "1f - Mathf.Pow(1f - t, 3f)" in motion,
    "summon retains target orientation": "restingRotation = transform.rotation;" in motion and
        "transform.rotation = restingRotation;" in motion,
    "summon settles at exact destination": "transform.position = to;" in motion and
        "transform.localScale = restingScale;" in motion,
    "smoothing is purely visual": "using Reborn.Core;" not in motion and
        "TryAttack(" not in motion and "TryPlay(" not in motion,
    "original fixed camera is unchanged": "Camera.fieldOfView = 48f;" in factory and
        "Orbit.ResetView();" in factory,
    "duel rules unchanged": "public bool CanAttack(" in core and
        "public bool CanPlay(" in core,
    "HUD supports the current duel mode": "DuelModeActive" in hud,
}
for name, ok in checks.items():
    print(("OK " if ok else "FAIL ") + name)
if not all(checks.values()):
    raise SystemExit("Field interaction/animation contracts failed")
print(f"PASS: {len(checks)} field interaction and animation checks (Unity visual test pending)")
