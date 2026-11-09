#!/usr/bin/env python3
"""Non-runtime source contracts; not a substitute for compiling Unity Editor."""
from pathlib import Path
root = Path(__file__).resolve().parents[1]
def source(path):
    return (root / path).read_text(encoding="utf-8")

brain = source("Assets/Scripts/Core/AdaptiveOpponentBrain.cs")
game = source("Assets/Scripts/Presentation/RebornGame.cs")
director = source("Assets/Scripts/Presentation/OpponentTurnDirector.cs")
storage = source("Assets/Scripts/Presentation/AdaptiveAiStorage.cs")
hud = source("Assets/Scripts/Presentation/ArenaHud.cs")
checks = {
    "only legal placements": "match.CanPlay(DuelSide.Opponent" in brain,
    "only legal attacks": "match.CanAttack(DuelSide.Opponent" in brain,
    "no peeking at player hand": "match.Player.Hand" not in brain,
    "five adaptive tactics": "Counterplay" in brain and "FusionFocused" in brain,
    "learning requires completed matches": "match.Phase != MatchPhase.Finished" in brain,
    "duplicate learning refused": "ReferenceEquals(match, recordedDuel)" in brain,
    "learning uses match outcome": "match.Winner.Value == DuelSide.Opponent" in brain,
    "directors use brain instead of static AI": "brain.TryChoosePlacement(" in director and "brain.TryChooseAttack(" in director,
    "player attack animation order unchanged": "game.TryAttack(" in director and director.index("arena.PlayMatchAttack(") < director.index("game.TryAttack("),
    "per-opponent profile": "storage.Get(identity)" in game,
    "completed duels persisted": "storage.Save();" in game,
    "no network learning storage": "Application.persistentDataPath" in storage and "HttpClient" not in storage,
    "UI displays tactic and completed duels": "SetAiInsight" in hud,
    "System.Random explicitly resolved in Unity": "new Random(" not in game and "new System.Random(" in game,
    "fixed scene/camera not changed": "arena.Build();" in game and "Orbit.ResetView();" in source("Assets/Scripts/Presentation/ArenaFactory.cs"),
}
for label, ok in checks.items():
    print(("OK " if ok else "FAIL ") + label)
if not all(checks.values()):
    raise SystemExit("Adaptive AI source checks failed")
print(f"PASS: {len(checks)} static adaptive AI checks (Unity execution pending)")
