#!/usr/bin/env python3
"""0.20 PS1 Equip engine: static source contracts; not Unity Game View."""
from pathlib import Path
root=Path(__file__).resolve().parents[1]
def read(p): return (root/p).read_text(encoding="utf-8")
catalog=read("Assets/Scripts/Core/VerifiedEquipCatalog.cs")
core=read("Assets/Scripts/Core/DuelMatch.cs")
equips=read("Assets/Scripts/Core/DuelMatch.Equips.cs")
combat=read("Assets/Scripts/Core/CombatForecast.cs")
loader=read("Assets/Scripts/Presentation/OriginalEquipLoader.cs")
game=read("Assets/Scripts/Presentation/RebornGame.cs")
input_code=read("Assets/Scripts/Presentation/ArenaInteraction.cs")
hud=read("Assets/Scripts/Presentation/ArenaHud.cs")
modal=read("Assets/Scripts/Presentation/ArenaHud.EquipChoice.cs")
arena=read("Assets/Scripts/Presentation/ArenaFactory.cs")
smoke=read("tools/Reborn.Core.Smoke/Program.cs")
checks={
  "real retail universal equipment IDs and exact +500/+1000 bonuses":
     'AddUniversal("657", 1000, 1000)' in catalog and
     'AddUniversal("668", 500, 500)' in catalog and
     "CanApply(CardDefinition equip" in catalog,
  "verified IDs only, text never interpreted":
     "EffectText" not in catalog and "EffectText" not in equips and
     "TryGet(equip, out rule)" in equips,
  "compatibility by exact monster IDs, not guessed type names":
     "HashSet<string>" in catalog and "eligible.Contains(monster.Id)" in catalog,
  "face-down monster never auto-revealed during equip":
     "if (target.FaceDown)" in equips and
     "target.ApplyEquip" in equips and
     "Reveal()" not in equips,
  "bad equip never mutates cards or events":
     equips.index("if (!CanEquipFromHand(") <
     equips.index("var original = owner.Take(handIndex);"),
  "equip uses one mandatory hand placement":
     "hasPlayed = true;" in equips and
     "owner.Bury(original);" in equips,
  "equips accumulate separately on field monster":
     "EquipAttackBonus" in core and "EquipDefenseBonus" in core and
     "EquipCount++" in core,
  "combat prediction and direct attacks use buffed values":
     "attacker.EffectiveAttack" in combat and
     "defender.EffectiveAttack" in combat and
     "defender.EffectiveDefense" in combat,
  "previously set equips activate later only once":
     "owner.SupportSetTurn(supportSlot) == Turn" in equips and
     "owner.SetSupport(supportSlot, null);" in equips and
     "TryEquipFromSupport" in equips,
  "import rules require local SHA, verified and known cards":
     "content.SourceSha256" in loader and
     "equip.verified" in loader and
     "monster.Kind != CardKind.Monster" in loader,
  "a malicious or mismatched manifest falls back to safe foundation":
     "Equip manifest invalido" in loader and
     "return baseline;" in loader,
  "player picks an own occupied monster and confirms equip":
     "BeginEquipChoice" in input_code and
     "CanEquipFromHand" in input_code and
     "CommitEquipFromHand" in input_code,
  "cancelled modal does not consume a physical card":
     "CancelEquipChoice();" in modal and
     "if (!commit())" in modal and
     "match.TryEquipFromHand" not in modal,
  "set equip interaction has independent activation path":
     "CommitSetEquip(" in input_code and
     "CanEquipFromSupport(" in input_code and
     "TryEquipFromSupport(" in input_code,
  "duel cannot interact with world while equip confirmation is open":
     "EquipChoiceOpen" in hud and
     "if (hud.EquipChoiceOpen)" in input_code,
  "original procedural buff feedback does not move the original monster":
     "PlayEquipActivation" in arena and
     "MatchEventKind.Equipped" in game,
  "buffed attributes visible only on face-up monsters":
     "if (!monster.FaceDown && monster.EquipCount > 0)" in arena and
     "monster.EffectiveAttack" in arena,
  "core smoke includes compatibility, stacking, hidden, set equip cases":
     "0.20 stacking two retail equips" in smoke and
     "0.20 next-turn set equip activates exactly once" in smoke,
}
for label, ok in checks.items():
    print(("OK " if ok else "FAIL ") + label)
if not all(checks.values()):
    raise SystemExit("Prototype 0.20 source contracts failed")
print(f"PASS: {len(checks)} verified PS1 Equip source contracts (Unity visual test pending)")
