#!/usr/bin/env python3
"""Static contracts for local source-only Equip export, not Unity execution."""
from pathlib import Path
root=Path(__file__).resolve().parents[1]
def read(p):return (root/p).read_text(encoding="utf-8")
converter=read("tools/export_equip_rules.py")
importer=read("tools/import_fm_disc.py")
loader=read("Assets/Scripts/Presentation/OriginalEquipLoader.cs")
win=read("tools/EXPORTAR_EQUIPS.ps1")
tests=read("tools/test_equip_export.py")
terrain=read("Assets/Scripts/Core/TerrainRules.cs")
checks={
 "same disc SHA mandatory":
    'if original_sha != catalog_sha:' in converter and
    'content.SourceSha256' in loader,
 "all seven original tables must be non-divergent":
    '"equip table differs"' in converter and
    '"terrain copy"' in converter,
 "no guessed equip targets or card kinds":
    'raw_targets' in converter and
    'str(lookup[raw_target].get("kind")).lower() != "monster"' in converter,
 "original universal IDs are not registered twice":
    'if equip_id in (657, 668):' in converter,
 "structural validation not misrepresented as gameplay confirmed":
    '"individualDuelTesting": False' in converter and
    '"structuralValidation"' in converter,
 "original disc importer now saves card type label":
    '"typeLabel": (' in importer and
    'gamedata.TYPE_NAMES[int(card.type)]' in importer,
 "complete imported types can drive terrain bonuses":
    'monster.TypeLabel' in terrain and
    '"typeLabel": (' in importer,
 "importer automatically writes equip manifest when data available":
    'from export_equip_rules import build as build_equip_rules' in importer and
    '"equip_rules.json"' in importer,
 "failed import removes stale cross-ROM equip compatibility":
    '(target / "equip_rules.json").unlink(missing_ok=True)' in importer,
 "optional same-sha reexport without rescanning ISO":
    'disc_tables.json' in converter and
    'export_equip_rules.py' in win,
 "source JSON remains out of repo (private path only)":
    '"PrivateExtracts"' in converter and
    'json.dumps(report' in converter,
 "strict tests cover wrong SHA and divergent equip banks":
    'Different ISO pairing tables were accepted' in tests and
    'Divergent terrain banks accepted' in tests,
}
for name,ok in checks.items():print(("OK " if ok else "FAIL ")+name)
if not all(checks.values()):raise SystemExit("Equip export static contract failed")
print(f"PASS: {len(checks)} PS1 equip export checks")
