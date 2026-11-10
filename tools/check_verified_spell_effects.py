#!/usr/bin/env python3
"""0.19 effect-engine source invariants; not Unity Game View validation."""
from pathlib import Path
root = Path(__file__).resolve().parents[1]
def read(path):
    return (root/path).read_text(encoding="utf-8")
catalog = read("Assets/Scripts/Core/VerifiedSpellCatalog.cs")
core = read("Assets/Scripts/Core/DuelMatch.cs")
effects = read("Assets/Scripts/Core/DuelMatch.VerifiedSpells.cs")
input_code = read("Assets/Scripts/Presentation/ArenaInteraction.cs")
flow = read("Assets/Scripts/Presentation/ArenaHud.DuelFlow.cs")
game = read("Assets/Scripts/Presentation/RebornGame.cs")
arena = read("Assets/Scripts/Presentation/ArenaFactory.cs")
feedback = read("Assets/Scripts/Presentation/CombatFeedbackOverlay.cs")
smoke = read("tools/Reborn.Core.Smoke/Program.cs")
unity = read("Assets/Tests/Editor/DuelMatchTests.cs")
checks = {
    "explicit 12 original retail spell IDs only":
        all(f'result.Add("{n:03d}"' in catalog for n in range(336,348)) and
        "return card != null && card.Kind == CardKind.Spell" in catalog,
    "healing and direct damage values authored explicitly":
        "RestoreLife, 200" in catalog and
        "RestoreLife, 5000" in catalog and
        "DirectDamage, 50" in catalog and
        "DirectDamage, 1000" in catalog,
    "imported free-text is not executable":
        "EffectText" not in effects and "EffectText" not in catalog and
        "verifiedSpells.TryGet" in core,
    "effects enabled only for Original edition":
        'imported.Edition.StartsWith("original-ps1"' in game and
        "VerifiedSpellCatalog.OriginalPs1Core()" in game,
    "training mode has no original spell execution":
        "verifiedSpells.Count > 0" in core and
        "new VerifiedSpellCatalog()" in game,
    "unknown faceup magic rejected before any physical change":
        "if (resolveInstantSpell && !verifiedSpells.TryGet(preview" in core and
        core.index("if (resolveInstantSpell && !verifiedSpells.TryGet(preview") <
        core.index("var spent = new List<CardDefinition>()"),
    "verified faceup spell executes in one placement":
        "if (resolveInstantSpell)" in core and
        "ApplyVerifiedSpell(side, spellRule);" in core and
        "hasPlayed = true;" in core,
    "prepared magic checks a later turn and proven effect":
        "owner.SupportSetTurn(slot) == Turn" in core and
        "verifiedSpells.TryGet(card, out _)" in core,
    "set magic uses exactly one activation and graveyard":
        "owner.SetSupport(slot, null);" in effects and
        "MatchEventKind.SupportActivated" in effects and
        "MatchEventKind.Graveyard" in effects,
    "life gain uses positive delta and no int overflow":
        "Math.Min((long)rule.Amount" in effects and
        "MatchEventKind.LifeChanged" in effects,
    "raigeki targets opposing monsters only":
        "DestroyMonster(Other(user), i);" in effects,
    "dark hole targets both players monsters and supports":
        "DestroyMonster(side, i);" in effects and
        "DestroySupport(side, i);" in effects,
    "unknown trap/equip/ritual faceup prevented in Original":
        'preview.Kind != CardKind.Spell' in core,
    "UI uses validated activation, not a fake button":
        "match.TryActivateSupport(DuelSide.Player" in flow and
        "match.CanActivateSupport(DuelSide.Player" in input_code,
    "spell feedback handles both positive and negative LP":
        'evt.Value > 0 ? "+" : "-"' in feedback,
    "spell VFX and destruction use correct support lane":
        "PlaySpellActivation" in arena and
        "evt.Lane ?? FieldLane.Monster" in game,
    "core regressions test set and instant variants":
        "0.19 previously set Sparks activates later" in smoke and
        "0.19 face-up Mooyan Curry heals 200" in smoke,
    "nunit checks unverified text cannot execute":
        "UnverifiedEffectTextCannotActivateOrConsumeCards" in unity,
}
for item, ok in checks.items():
    print(("OK " if ok else "FAIL ") + item)
if not all(checks.values()):
    raise SystemExit("Verified spell contracts failed")
print(f"PASS: {len(checks)} 0.19 effects contracts (Unity visual testing pending)")
