#!/usr/bin/env python3
"""Source-level regression contracts; does NOT compile/run Unity graphics."""
import pathlib
root = pathlib.Path(__file__).resolve().parents[1]
def read(path):
    return (root / path).read_text(encoding="utf-8")

factory = read("Assets/Scripts/Presentation/ArenaFactory.cs")
view = read("Assets/Scripts/Presentation/CardView.cs")
builder = read("Assets/Scripts/Presentation/CardFaceBuilder.cs")
art = read("Assets/Scripts/Presentation/CardArtCatalog.cs")
hud = read("Assets/Scripts/Presentation/ArenaHud.cs")
interaction = read("Assets/Scripts/Presentation/ArenaInteraction.cs")
core = read("Assets/Scripts/Core/DuelMatch.cs")
checks = {
    "five fixed monster/support slots": "public const int ZoneCount = 5;" in core and "for (int i = 0; i < 5; i++)" in factory,
    "camera settings kept in ArenaFactory": "Camera.fieldOfView = 48f;" in factory and "Orbit.ResetView();" in factory,
    "all cards created with 2.5D builder": "CardFaceBuilder.Create(name, dynamicRoot, card, handIndex, pos, size, hidden)" in factory,
    "single root physics collider": "root.AddComponent<BoxCollider>()" in builder and "box.enabled = false" in builder,
    "card text not flattened by card mesh": "root.transform.localPosition = position" in builder and 'Block("Border_Selectable", root.transform' in builder,
    "unique monster/spell/trap/equip/ritual treatments": all(k in builder for k in ("CardKind.Spell", "CardKind.Trap", "CardKind.Equip", "CardKind.Ritual", "default: return")),
    "art is optional local Resources only": 'Resources.Load<Texture2D>("RebornCardArt/" + card.Id)' in art and "HttpClient" not in art,
    "fallback artwork deterministic and cached": "StableSeed" in art and "Cache[card.Id]" in art,
    "facedown secrets not shown in preview": "!hoveredCard.Facedown" in interaction,
    "card detail preview is read-only": "private void DrawCardInspection()" in hud and "CardArtCatalog.Get(card)" in hud and "match.TryPlay(" not in hud,
    "HUD pointer guards retained": "IsPointerOverHud(pointer)" in interaction,
    "hand motion retained": "homeLocalPosition + Vector3.up * raise" in view and "StopDragging()" in view,
    "opponent remains a separate field lane": "sign*2.0f" in factory and "sign*4.35f" in factory,
    "no bundled official card arts": not (root / "Assets/Resources/RebornCardArt").exists(),
}
for label, passed in checks.items():
    print(("OK" if passed else "FAIL") + " " + label)
if not all(checks.values()):
    raise SystemExit("Card visual contract checks failed")
print(f"PASS: {len(checks)} static 0.6 card presentation checks (Unity execution pending)")
