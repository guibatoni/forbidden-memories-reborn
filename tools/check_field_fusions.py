#!/usr/bin/env python3
"""0.16 field-combination contracts; does not compile/render the Unity Editor."""
from pathlib import Path

root = Path(__file__).resolve().parents[1]
def read(path):
    return (root / path).read_text(encoding="utf-8")

core = read("Assets/Scripts/Core/DuelMatch.cs")
field = read("Assets/Scripts/Core/DuelMatch.FieldFusion.cs")
input_code = read("Assets/Scripts/Presentation/ArenaInteraction.cs")
hud = read("Assets/Scripts/Presentation/ArenaHud.cs")
factory = read("Assets/Scripts/Presentation/ArenaFactory.cs")
opponent = read("Assets/Scripts/Presentation/OpponentTurnDirector.cs")
simple = read("Assets/Scripts/Core/SimpleOpponentAi.cs")
smoke = read("tools/Reborn.Core.Smoke/Program.cs")
chain = read("Assets/Scripts/Core/DuelMatch.FusionChain.cs")
resolver = read("Assets/Scripts/Core/FusionChainResolver.cs")

checks = {
    "core split preserves cross-assembly API": "public sealed partial class DuelMatch" in core and
        "public sealed partial class DuelMatch" in field,
    # 0.30: field and hand chains share FusionChainResolver (PS1 placement
    # routine); the field card is material #1 of the resolved sequence.
    "field material is the first fusion operand": "CollectHandMaterials(Side(side), onField.Card, indices)" in chain and
        "chain = PreviewFieldChain(side, indices, monsterSlot);" in field,
    "five hand materials permitted": "indices.Count > HandLimit" in field,
    "field preview is pure": "public CardDefinition PreviewFieldResult(" in field and
        "return ResolveFieldSequence(" in field,
    "occupied own monster required": "state.Monster(monsterSlot)" in field and
        "Nao ha monstro proprio neste espaco" in field,
    "one mandatory play enforced by core": "if (hasPlayed)" in field and
        "hasPlayed = true;" in field,
    "no invented equipment effects": "result.Kind != CardKind.Monster" in field and
        "Combinacao com suporte nao validada" in field,
    "wrong Guardian Star rejected": "GuardianStarRules.IsAvailable(result, star)" in field,
    "invalid sequence rejects repeated hand indexes": "new HashSet<int>()" in chain and
        "!seen.Add(index)" in chain,
    "originals are buried but virtual results are not": "state.Bury(onField.Card);" in field and
        "if (index != survivor) discarded.Add(" in field and
        "if (!fieldSurvives)" in field,
    "final verified fusion face-up, later failed survivor preserves requested face":
        "finalResultIsFusion ? false : faceDown" in field and
        "bool finalResultIsFusion = chain.FinalIsFusion;" in field,
    "failure replaces earlier result": "current = next;" in resolver and
        "FusionStepKind.DiscardedCurrent" in resolver and
        "MatchEventKind.FusionFailed" in chain,
    "monster survives a failed pair with a non-monster (PS1 state 7)":
        "current.Kind == CardKind.Monster && next.Kind != CardKind.Monster" in resolver and
        "FusionStepKind.DiscardedNext" in resolver,
    "verified equips resolve inside the chain (PS1 state 5)":
        "equips.CanApply(next, current, out var onCurrent)" in resolver and
        "equips.CanApply(current, next, out var onNext)" in resolver,
    "real fusion uses validated recipes only": "fusions.TryFuse(current, next, out var product)" in resolver,
    "player can click occupied own field slot": "match.Player.Monster(zone.Index) != null" in input_code and
        "PlaceMatchCard(zone);" in input_code,
    "player can drag to occupied own field slot": "PlaceMatchCard(releasedZone);" in input_code,
    "rendered preview is non-mutating": "match.PreviewFieldResult(" in input_code and
        "FieldFusionPreview = prediction" in input_code,
    "highlight uses authoritative legal gate": "match.CanPlayOnField(DuelSide.Player, selection," in input_code,
    "field fusion and failed replacement are visually distinct": "PlayFieldCombinationEffect(" in factory and
        "Field_Fusion_Spark" in factory and "Field_Replacement_Spark" in factory,
    "original hand layout untouched": "HandFanGeometry.WithHoveredCard(" in factory,
    "preview includes resulting guardian stars": "FieldFusionPreview.StarA" in hud and
        "FieldFusionPreview.StarB" in hud,
    "AI tries validated field recipe first": "game.TryChooseFieldCombination(DuelSide.Opponent, true," in opponent,
    "AI replaces when all other placements fail": "game.TryChooseFieldCombination(DuelSide.Opponent, false," in opponent,
    "headless AI also uses field combination": "match.TryPlayOnField(" in simple,
    "smoke covers 6-material sequence": "six-material field fusion" in smoke,
    "smoke covers unverified equip rollback": "unverified equip on occupied monster" in smoke,
}
for title, ok in checks.items():
    print(("OK " if ok else "FAIL ") + title)
if not all(checks.values()):
    raise SystemExit("Field-fusion source contracts failed")
print(f"PASS: {len(checks)} field-fusion source checks (Unity visual validation pending)")
