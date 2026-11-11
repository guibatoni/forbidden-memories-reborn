#!/usr/bin/env python3
"""PS1 reactive-trap source invariants; Unity visuals still need a Play test."""
from pathlib import Path
root=Path(__file__).resolve().parents[1]
def read(p): return (root/p).read_text(encoding="utf-8")
catalog=read("Assets/Scripts/Core/ReactiveTrapCatalog.cs")
traps=read("Assets/Scripts/Core/DuelMatch.ReactiveTraps.cs")
spells=read("Assets/Scripts/Core/DuelMatch.VerifiedSpells.cs")
core=read("Assets/Scripts/Core/DuelMatch.cs")
game=read("Assets/Scripts/Presentation/RebornGame.cs")
ai=read("Assets/Scripts/Presentation/OpponentTurnDirector.cs")
smoke=read("tools/Reborn.Core.Smoke/Program.cs")
nunit=read("Assets/Tests/Editor/DuelMatchTests.cs")
checks={
    "the reactive list is ONLY two verified trap IDs":
        'result.Add("687", ReactiveTrapOperation.ReflectDirectSpellDamage)' in catalog and
        'result.Add("688", ReactiveTrapOperation.ReverseSpellHealing)' in catalog and
        'result.Add("689"' not in catalog and 'result.Add("690"' not in catalog,
    "reactive traps must already be face-down and set in earlier turn":
        'owner.SupportIsFaceDown(slot)' in traps and
        'owner.SupportSetTurn(slot) == Turn' in traps,
    "trap is buried once and emits public activation event":
        'owner.SetSupport(slot, null);' in traps and 'owner.Bury(card);' in traps and
        'MatchEventKind.TrapTriggered' in traps and
        'MatchEventKind.Graveyard' in traps,
    "burn reflection targets original caster without recursion":
        'ReactiveTrapOperation.ReflectDirectSpellDamage' in spells and
        'Damage(user, rule.Amount);' in spells and
        'Damage(Other(user), rule.Amount);' in spells,
    "Simochi converts intended recovery into damage":
        'ReactiveTrapOperation.ReverseSpellHealing' in spells and
        'Damage(user, rule.Amount);' in spells,
    "traps are enabled only for original-PS1 editions":
        'ReactiveTrapCatalog.OriginalPs1() : new ReactiveTrapCatalog()' in game and
        'reactiveTraps = reactionCatalog ?? new ReactiveTrapCatalog()' in core,
    "original user/UI text is not executed as trap script":
        'EffectText' not in traps and 'EffectText' not in catalog,
    "AI can activate later-set known spells and equips sequentially":
        'game.TryActivateSupport(DuelSide.Opponent, support' in ai and
        'game.TryEquipFromSupport(DuelSide.Opponent' in ai and
        'yield return Pause(0.40f);' in ai,
    "AI never eagerly activates traps or unknown support":
        'card.Kind != CardKind.Spell' in ai and
        'game.CanActivateSupport(DuelSide.Opponent' in ai,
    "deterministic and NUnit tests cover both reactions":
        '0.22 Goblin Fan redirects Sparks burn' in smoke and
        '0.22 Simochi converts Red Medicine' in smoke and
        'GoblinFanReflectsBurnAndSimochiChangesHealingToDamage' in nunit,
}
for name,ok in checks.items():
    print(("OK " if ok else "FAIL ")+name)
if not all(checks.values()): raise SystemExit("0.22 reactive trap contracts failed")
print(f"PASS: {len(checks)} reactive trap source contracts")
