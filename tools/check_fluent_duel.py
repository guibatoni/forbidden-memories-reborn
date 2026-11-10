#!/usr/bin/env python3
"""Fluent PS1-like duel source invariants, not a full Unity Editor compilation."""
from pathlib import Path

root = Path(__file__).resolve().parents[1]
def read(name):
    return (root / name).read_text(encoding="utf-8")

core = read("Assets/Scripts/Core/DuelMatch.cs")
brain = read("Assets/Scripts/Core/AdaptiveOpponentBrain.cs")
hud = read("Assets/Scripts/Presentation/ArenaHud.cs")
flow = read("Assets/Scripts/Presentation/ArenaHud.DuelFlow.cs")
input_script = read("Assets/Scripts/Presentation/ArenaInteraction.cs")
card = read("Assets/Scripts/Presentation/CardView.cs")
arena = read("Assets/Scripts/Presentation/ArenaFactory.cs")
opponent = read("Assets/Scripts/Presentation/OpponentTurnDirector.cs")
attacks = read("Assets/Scripts/Presentation/PlayerAttackDirector.cs")
game = read("Assets/Scripts/Presentation/RebornGame.cs")
smoke = read("tools/Reborn.Core.Smoke/Program.cs")
editor = read("Assets/Tests/Editor/DuelMatchTests.cs")
checks = {
    "fixed five slots retained": "public const int ZoneCount = 5;" in core,
    "only one hand placement per turn": "if (hasPlayed) return Reject(" in core,
    "card face choice saved in rules": "bool faceDown, out string reason)" in core and
        "new MonsterOnField(result, position, star, faceDown, Turn)" in core,
    "support face-down status and set turn tracked": "SupportIsFaceDown" in core and
        "SupportSetTurn" in core,
    "previously set support cannot fake unverified effects": "Efeito nao validado" in core and
        "Suporte recem-setado" in core,
    "monsters can change positions through duel engine": "public bool TryChangePosition(" in core,
    "no position change after attacking": "if (monster.AttackedThisTurn)" in core,
    "no position change on summoning turn": "if (monster.TurnPlaced == Turn)" in core,
    "no two position changes in one turn": "if (monster.PositionChangedThisTurn)" in core,
    "set cards are revealed when battle resolves": "if (attacker.FaceDown) attacker.Reveal();" in core and
        "if (defender != null && defender.FaceDown) defender.Reveal();" in core,
    "fluent HUD is in separate partial": "public sealed partial class ArenaHud" in hud and
        "public sealed partial class ArenaHud" in flow,
    "card face selected before guardian controls": hud.find("Face: PARA BAIXO") <
        hud.find("Guardian: segunda"),
    "placing a card immediately enters battle": "hud.AfterHandPlacement();" in input_script and
        "match.TryBeginBattle(DuelSide.Player" in flow,
    "no need for attack button": '"Selecionar ataque"' not in hud and
        '"Iniciar combate"' not in hud,
    "last available attack can auto-pass": "hud.AfterPlayerAttack();" in attacks and
        "TryPassPlayerTurn();" in flow,
    "target selection is click attacker then enemy": "hud.SelectedAttacker = match.Phase == MatchPhase.Battle" in input_script and
        "hud.RequestPlayerAttack(hud.SelectedAttacker, target)" in input_script,
    "direct attacks accept enemy field": "PointerOverOpponentField(pointer)" in input_script and
        "match.Opponent.MonsterCount == 0 ? -1" in input_script,
    "hand fan has overlap/rotation and enlarged cards": "private CardView MakeHandCard(" in arena and
        "new Vector3(1.90f, 0.12f, 2.55f)" in arena and
        "view.SetFanRotation(" in arena,
    "shift fusion sequence numbered": "fusionSelection.Add(index)" in input_script and
        "view.SetFusionOrder(fusionSelection.IndexOf(view.HandIndex) + 1)" in input_script and
        "Badge_Number" in card,
    "face-down opponent cards do not reveal values in HUD": "match.Opponent.Monster(previewTarget).FaceDown" in hud,
    "face-down attack target name not shown before impact": "target.FaceDown" in attacks and
        "defending.FaceDown" in opponent,
    "opponent can flip old defense with legal rules": "brain.TryChoosePositionChange(game" in opponent and
        "game.TryChangePosition(DuelSide.Opponent" in opponent,
    "AI avoids inspecting concealed monster strengths": "if (enemy == null || enemy.FaceDown)" in brain and
        "if (hidden.Count > 0)" in brain,
    "duel opens automatically without sandbox click": "hud.SetDuelMode(true);" in game,
    "core regression scenarios added": "face-down defender is revealed on actual combat" in smoke and
        "AI switches previously set defense into attack" in smoke,
    "Unity editor tests authored": "SetMonsterFlipsOnLaterTurnAndCannotMoveAfterAttack" in editor,
}
for name, ok in checks.items():
    print(("OK " if ok else "FAIL ") + name)
if not all(checks.values()):
    raise SystemExit("Fluent duel source contract failed")
print(f"PASS: {len(checks)} fluent duel static checks (Unity runtime pending)")
