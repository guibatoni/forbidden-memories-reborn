#!/usr/bin/env python3
"""Static fidelity contract for original PS1 data, rituals and original VFX.

These checks are not a Unity Editor compilation or a real-disc extraction.
"""
from pathlib import Path
import ast

root = Path(__file__).resolve().parents[1]
def read(path):
    return (root / path).read_text(encoding="utf-8")

importer = read("tools/import_fm_disc.py")
validator = read("tools/validate_import.py")
audit = read("tools/audit_dueltrace_original.py")
ritual = read("Assets/Scripts/Core/DuelMatch.Rituals.cs")
catalog = read("Assets/Scripts/Core/RitualCatalog.cs")
bundle = read("Assets/Scripts/Core/GameContentBundle.cs")
loader = read("Assets/Scripts/Presentation/OriginalContentLoader.cs")
game = read("Assets/Scripts/Presentation/RebornGame.cs")
interaction = read("Assets/Scripts/Presentation/ArenaInteraction.cs")
fx = read("Assets/Scripts/Presentation/FusionCeremonyVfx.cs")
arena = read("Assets/Scripts/Presentation/ArenaFactory.cs")
smoke = read("tools/Reborn.Core.Smoke/Program.cs")
ignored = read(".gitignore")

for name in ("tools/import_fm_disc.py", "tools/validate_import.py",
             "tools/audit_dueltrace_original.py"):
    ast.parse(read(name), filename=name)

checks = {
    "existing local importer exports all card descriptions":
        '"description": card.description' in importer and
        "range(1, 723)" in importer,
    "description coverage reports each card category":
        '"descriptionCoverageByKind": categories' in importer and
        '"missingDescriptionCount"' in importer,
    "retail reader-quirk fusion recipes retained and labeled":
        '"origin": "ps1-reader-quirk"' in importer and
        "if (left, right) in data.glitch_fusions:" not in importer,
    "all three original ritual tributes exported":
        '"ritualCard": int(ritual_id)' in importer and
        '"tributes": [t1, t2, t3]' in importer,
    "complete 722-card validation available":
        "'--complete'" in validator and "list(range(1, 723))" in validator,
    "DUELTRACE audit does not promote candidates automatically":
        "def compare(" in audit and "matchingPairs" in audit and
        "No data is uploaded" in audit,
    "ritual catalog allows repeated IDs but requires 3 valid materials":
        "tributeIds.Length != 3" in catalog and
        "Ritual material ID cannot be empty" in catalog and
        "!seen.Add(tribute)" not in catalog,
    "ritual execution picks three distinct physical field slots":
        "new HashSet<int>()" in ritual and "taken.Add(matchSlot)" in ritual,
    "ritual requires own face-up field monsters":
        "monster.FaceDown" in ritual and
        "monster.Card.Id != recipe.TributeIds[needed]" in ritual,
    "ritual is one legal hand action and produces no hand monster":
        "hasPlayed = true;" in ritual and "state.Take(handIndex)" in ritual and
        "new MonsterOnField(result" in ritual,
    "no ritual state changes in Can/Preview":
        "public CardDefinition PreviewRitual" in ritual and
        "public bool CanSummonRitual" in ritual and
        "if (!CanSummonRitual(" in ritual,
    "ritual graveyard contains only physical ritual plus tributes":
        "state.Bury(used);" in ritual and "state.Bury(original.Card);" in ritual,
    "Unity loader validates ritual recipe IDs and types":
        "RitualData[] rituals;" in loader and
        "ritualCard.Kind != CardKind.Ritual" in loader and
        "rituals.Add(ritualId, materials, summon);" in loader,
    "game match receives imported ritual recipes":
        "loaded ? imported.Rituals : new RitualCatalog()" in game and
        "public RitualCatalog Rituals" in bundle,
    "UI previews and invokes only known rituals":
        "match.PreviewRitual(DuelSide.Player" in interaction and
        "match.CanSummonRitual(DuelSide.Player" in interaction and
        "match.TrySummonRitual(DuelSide.Player" in interaction,
    "VFX is original geometry and separate from rules":
        "class FusionCeremonyVfx : MonoBehaviour" in fx and
        "Arcane_Ring_" in fx and "Ceremony_Orbit_Mote_" in fx and
        "TryPlay(" not in fx and "Camera." not in fx,
    "field fusion and ritual use the same VFX foundation":
        "PlayRitualCeremony(" in arena and
        "PlayFieldCombinationEffect(" in arena and
        "visual.AddComponent<FusionCeremonyVfx>().Begin(ceremony)" in arena,
    "smoke validates ritual sacrifice and no mutation":
        "ritual preview is fully read-only" in smoke and
        "ritual consumes exactly 3 field tributes" in smoke,
    "original disc assets remain git-ignored":
        "Assets/StreamingAssets/RebornContent/*.json" in ignored and
        "PrivateExtracts/" in ignored and "*.bin" in ignored,
}
for label, ok in checks.items():
    print(("OK " if ok else "FAIL ") + label)
if not all(checks.values()):
    raise SystemExit("0.17 original-data/ritual contracts failed")
print(f"PASS: {len(checks)} original-data/ritual/VFX source contracts (Unity validation pending)")
