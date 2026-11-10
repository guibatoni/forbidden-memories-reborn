#!/usr/bin/env python3
"""Classic-turn and inspection source contracts. Not a Unity Game View test."""
from pathlib import Path

root = Path(__file__).resolve().parents[1]
def read(path):
    return (root / path).read_text(encoding="utf-8")

core = read("Assets/Scripts/Core/DuelMatch.cs")
flow = read("Assets/Scripts/Presentation/ArenaHud.DuelFlow.cs")
input_code = read("Assets/Scripts/Presentation/ArenaInteraction.cs")
hud = read("Assets/Scripts/Presentation/ArenaHud.cs")
inspect = read("Assets/Scripts/Presentation/ArenaHud.CardInspect.cs")
orbit = read("Assets/Scripts/Presentation/OrbitCamera.cs")
view = read("Assets/Scripts/Presentation/CardView.cs")
factory = read("Assets/Scripts/Presentation/ArenaFactory.cs")
smoke = read("tools/Reborn.Core.Smoke/Program.cs")

checks = {
    "passing without playing is rejected": "Baixe ao menos uma carta antes de passar o turno" in core,
    "entering battle without playing is rejected": "Baixe uma carta antes de entrar em batalha" in core,
    "one hand play remains the limit": "if (hasPlayed) return Reject(" in core,
    "position not locked to placement turn": "if (monster.TurnPlaced == Turn)" not in core,
    "position not locked to first switch": "if (monster.PositionChangedThisTurn)" not in core,
    "position locked after an attack": "if (monster.AttackedThisTurn)" in core,
    "battle enters after placement": "match.TryBeginBattle(DuelSide.Player" in flow,
    "no automatic pass after playing": "Sem ataque disponivel. Passando" not in flow,
    "no automatic pass after last attack": "TryPassPlayerTurn();" not in flow,
    "SPACE explicitly passes": "keys.spaceKey.wasPressedThisFrame" in input_code and "hud.TryPassPlayerTurn();" in input_code,
    "Q and E toggle positions": "keys.qKey.wasPressedThisFrame" in input_code and "keys.eKey.wasPressedThisFrame" in input_code,
    "toggle selection survives match redraw": "SelectedFieldMonster = slot;" in flow,
    "new monster remains selected": "zone.Index : -1;" in input_code and "hud.AfterHandPlacement();" in input_code,
    "right-click no longer rotates the camera": "rightButton." not in orbit,
    "right-click inspects cards": "mouse.rightButton.wasPressedThisFrame" in input_code and "hud.ToggleCardInspection(card.Card)" in input_code,
    "hidden opponent card cannot be inspected": "card.Facedown && !owned" in input_code,
    "inspector displays no invented effect": "Nenhum texto ou efeito verificado" in inspect and "card.Description" in inspect,
    "inspector renders image and both Guardian stars": "CardArtCatalog.Get(card)" in inspect and "card.StarA" in inspect and "card.StarB" in inspect,
    "modal blocks HUD gameplay": "if (CardInspectionOpen)" in hud and "DrawPinnedCardInspection();" in hud,
    "hand shape code remains unchanged": "HandFanGeometry.WithHoveredCard(index, count," in factory,
    "new draw is smoothly animated": "view.AnimateDrawFrom(" in factory and "public void AnimateDrawFrom(" in view,
    "new rules covered in deterministic smoke": "START cannot pass before one card" in smoke and
        "Q/E can alternate positions repeatedly" in smoke,
}
for title, ok in checks.items():
    print(("OK " if ok else "FAIL ") + title)
if not all(checks.values()):
    raise SystemExit("Classic-turn/inspection source contracts failed")
print(f"PASS: {len(checks)} classic-turn/inspection checks (Unity testing pending)")
