#!/usr/bin/env python3
"""Fast source invariants; does NOT replace compiling/running Unity Editor."""
from pathlib import Path

root = Path(__file__).resolve().parents[1]
def source(path: str) -> str:
    return (root / path).read_text(encoding='utf-8')

core = source('Assets/Scripts/Core/DuelMatch.cs')
player = source('Assets/Scripts/Presentation/PlayerAttackDirector.cs')
opponent = source('Assets/Scripts/Presentation/OpponentTurnDirector.cs')
hud = source('Assets/Scripts/Presentation/ArenaHud.cs')
interaction = source('Assets/Scripts/Presentation/ArenaInteraction.cs')
boot = source('Assets/Scripts/Presentation/RebornGame.cs')
scene = source('Assets/Scripts/Presentation/ArenaFactory.cs')
impact = source('Assets/Scripts/Presentation/AttackProjectile.cs')

checks = {
    '5 monster/support zones in rules': 'public const int ZoneCount = 5;' in core,
    'battle preview exists': 'public bool CanAttack(' in core,
    'execution validates preview': 'if (!CanAttack(side, fromSlot, targetSlot, out reason)) return false;' in core,
    'player director validates before animating': player.find('match.CanAttack(') < player.find('arena.PlayMatchAttack(') < player.find('match.TryAttack('),
    'opponent director previews attack': 'game.CanAttack(' in opponent,
    'opponent VFX before damage': opponent.find('arena.PlayMatchAttack(') < opponent.find('game.TryAttack('),
    'HUD does not mutate combat': 'match.TryAttack(' not in hud,
    'input does not mutate combat': 'match.TryAttack(' not in interaction,
    'HUD forwards player attacks': 'PlayerAttackRequested?.Invoke(' in hud,
    'new duel cancels pending animation': 'playerDirector.Cancel();' in boot,
    'arena still generates five zone columns': 'for (int i = 0; i < 5; i++)' in scene,
    'particle impacts stop before configure': impact.find('particles.Stop(true,') < impact.find('var main = particles.main;'),
}
for label, ok in checks.items():
    print(('OK' if ok else 'FAIL'), label)
if not all(checks.values()):
    raise SystemExit('Presentation boundary checks failed')
print(f'PASS: {len(checks)} static presentation checks (no Unity compilation)')
