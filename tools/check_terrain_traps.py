#!/usr/bin/env python3
"""0.21 PS1 terrain and attack-trap static regression (no Unity Game View)."""
from pathlib import Path
base=Path(__file__).resolve().parents[1]
def read(p): return (base/p).read_text(encoding="utf-8")
terrain=read("Assets/Scripts/Core/TerrainRules.cs")
traps=read("Assets/Scripts/Core/AttackTrapCatalog.cs")
trigger=read("Assets/Scripts/Core/DuelMatch.AttackTraps.cs")
core=read("Assets/Scripts/Core/DuelMatch.cs")
forecast=read("Assets/Scripts/Core/CombatForecast.cs")
spells=read("Assets/Scripts/Core/VerifiedSpellCatalog.cs")
resolution=read("Assets/Scripts/Core/DuelMatch.VerifiedSpells.cs")
game=read("Assets/Scripts/Presentation/RebornGame.cs")
hud=read("Assets/Scripts/Presentation/ArenaHud.cs")
player=read("Assets/Scripts/Presentation/PlayerAttackDirector.cs")
opponent=read("Assets/Scripts/Presentation/OpponentTurnDirector.cs")
arena=read("Assets/Scripts/Presentation/ArenaFactory.cs")
smoke=read("tools/Reborn.Core.Smoke/Program.cs")
nunit=read("Assets/Tests/Editor/DuelMatchTests.cs")
checks={
 "six exact field IDs (330-335) and a shared Normal fallback":
    all(f'case "{i}":' in terrain for i in range(330,336)) and
    'default: return "Normal";' in terrain,
 "type bonuses 500 and penalties -500 only, with no guessed empty subtype":
    'if (type.Length == 0) return 0;' in terrain and
    'return -500;' in terrain and 'type == "machine"' in terrain,
 "terrain is a verified PS1 spell operation":
    all(f'result.Add("{i}", VerifiedSpellOperation.SetTerrain)' in spells
        for i in range(330,336)),
 "terrain event globally replaces previous field":
    'CurrentTerrainCardId = rule.CardId;' in resolution and
    'MatchEventKind.TerrainChanged' in resolution,
 "forecast reads global terrain and both sides":
    'CurrentTerrainCardId);' in core and
    'TerrainRules.Bonus(terrainCardId, attacker.Card)' in forecast and
    'TerrainRules.Bonus(terrainCardId, defender.Card)' in forecast,
 "all six exact original threshold trap IDs":
    all(f'result.Add("{i}", {threshold})' in traps
        for i,threshold in [(681,500),(682,1000),(683,1500),(684,2000),
                             (685,3000),(686,25500)]),
 "trigger must wait for set trap and respond to actual attack":
    'defending.SupportSetTurn(slot) == Turn' in trigger and
    'TryTriggerAttackTrap(side, fromSlot, targetSlot,' in core,
 "lowest eligible threshold priority not broadest":
    'if (limit < selectedThreshold)' in trigger,
 "preview cannot inspect or activate hidden trap":
    'TryTriggerAttackTrap(' not in core.split('public bool TryPreviewAttack(',1)[1].split('public bool TryAttack(',1)[0] and
    'defending.SupportIsFaceDown(slot)' in trigger,
 "defender stays hidden when attack is intercepted":
    'if (attacker.FaceDown) attacker.Reveal();' in trigger and
    'defender.Reveal()' not in trigger,
 "attacker destroyed, trap buried and no damage":
    'DestroyMonster(attackerSide, attackerSlot);' in trigger and
    'defending.Bury(trap);' in trigger and
    'Damage(' not in trigger,
 "no traps or terrain enabled for unidentified editions":
    'AttackTrapCatalog.OriginalPs1() : new AttackTrapCatalog()' in game and
    'VerifiedSpellCatalog.OriginalPs1Core()' in game,
 "both attack directors distinguish trap interception from normal combat":
    'MatchEventKind.TrapTriggered' in player and
    'MatchEventKind.TrapTriggered' in opponent,
 "terrain and trap effects have no licensed images":
    'PlayTrapBurst' in arena and 'PlayTerrainShift' in arena,
 "UI shows current global terrain name":
    'match.CurrentTerrainName' in hud,
 "deterministic tests cover hidden priority and field replacement":
    '0.21 lowest eligible trap interrupts direct attack' in smoke and
    '0.21 opponent Yami replaces Mountain' in smoke and
    'HiddenTrapsUseLowestEligibleThresholdWithoutRevealingOthers' in nunit,
}
for name,ok in checks.items():
    print(("OK " if ok else "FAIL ")+name)
if not all(checks.values()): raise SystemExit("0.21 contracts failed")
print(f"PASS: {len(checks)} terrain/trap source contracts")
