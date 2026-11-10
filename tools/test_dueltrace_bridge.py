#!/usr/bin/env python3
"""Synthetic DUELTRACE integration tests. No retail ROM/cards/assets involved."""
import copy
import json
import tempfile
from pathlib import Path

from import_dueltrace_profile import (build_export, choose_profile,
                                      mirror_report_matches)
from validate_import import validate

SHA = "b" * 64


def profile():
    rows = []
    for cid in range(1, 723):
        type_id = {20: 20, 21: 21, 22: 22, 23: 23}.get(cid, 0)
        rows.append({"id": cid, "name": "Synthetic Card #" + str(cid),
                     "type_id": type_id, "atk": 1000, "def": 800,
                     "effect_text": None, "effect_source": None,
                     "effect_status": "nao_identificado"})
    rows[19]["effect_text"] = "Synthetic effect data only"
    rows[19]["effect_source"] = "manifesto_local_vinculado_sha256"
    rows[19]["effect_status"] = "fornecido_externamente_nao_verificado_na_iso"
    return {"schema": 3, "rom_sha256": SHA,
            "verified": {"cartas": "verificado"},
            "cards": rows, "opponents": [],
            "fusion_recipes": [{"a":1, "b":2, "result":3,
                                "validation":"observada_manualmente_no_jogo"}]}


def baseline():
    p = profile()
    data, _ = build_export(p)
    data["source"]["edition"] = "original-ps1-synthetic-test"
    data["cards"][0]["description"] = "Real original text, synthetic placeholder"
    data["cards"][0]["guardianStarA"] = "Mercury"
    data["cards"][0]["guardianStarB"] = "Sun"
    data["rituals"] = [{"ritualCard":22, "tributes":[1,1,1],
                        "result":3,"verified":True}]
    return data


p = profile()
data, stats = build_export(p)
errors, _ = validate(data)
assert not errors, errors
assert stats["cards"] == 722 and stats["effectTexts"] == 1
assert stats["missingDescriptions"] == 722
assert data["cards"][19]["kind"] == "Spell"
assert data["cards"][20]["kind"] == "Trap"
assert data["cards"][21]["kind"] == "Ritual"
assert data["cards"][22]["kind"] == "Equip"
assert data["cards"][19]["effectStatus"] == \
       "fornecido_externamente_nao_verificado_na_iso"
assert len(data["fusions"]) == 1 and data["fusions"][0]["result"] == 3
assert stats["effectsExecutable"] is False
assert not data["rituals"]
print("PASS: DUELTRACE 722 IDs/types/names/stats, honest missing descriptions, read-only effects")

merged, details = build_export(p, baseline())
assert merged["cards"][0]["description"] == \
    "Real original text, synthetic placeholder"
assert merged["cards"][0]["guardianStarA"] == "Mercury"
assert merged["cards"][0]["guardianStarB"] == "Sun"
assert merged["rituals"][0]["tributes"] == [1,1,1]
assert details["descriptions"] == 1 and details["rituals"] == 1
print("PASS: same-SHA disc descriptions, Guardian Stars and rituals preserved")

bad = copy.deepcopy(p)
bad["rom_sha256"] = "c" * 64
try:
    build_export(bad, baseline())
except ValueError as err:
    assert "SHA" in str(err)
else:
    raise AssertionError("Mixed ROM profiles were accepted")
bad = copy.deepcopy(p)
bad["cards"][0]["type_id"] = 999
try:
    build_export(bad)
except ValueError:
    pass
else:
    raise AssertionError("Unknown card type was accepted")
bad = copy.deepcopy(p)
bad["cards"][1]["id"] = 1
try:
    build_export(bad)
except ValueError:
    pass
else:
    raise AssertionError("Duplicate card ID was accepted")
bad = copy.deepcopy(p)
bad["cards"][0]["effect_text"] = "No provenance"
try:
    build_export(bad)
except ValueError:
    pass
else:
    raise AssertionError("Effect text without provenance was accepted")
print("PASS: wrong mod, invalid type, duplicate ID and unproven effect text rejected")

review = copy.deepcopy(p)
review["fusion_recipes"].append(
    {"a": 3, "b": 4, "result": 5,
     "validation": "revisada_usuario_em_lote",
     "fusion_candidate_fingerprint": "d"*64})
review["fusion_review"] = {
    "status": "revisado_usuario_nao_testado_individualmente",
    "rom_sha256": SHA, "candidate_fingerprint": "d"*64,
    "recipe_count": 2}
report = {"rom_sha256": SHA, "candidate_fingerprint": "d"*64,
          "valid_banks":7, "matching_banks":7,
          "divergent_banks":False, "bank_conflicts":0,
          "banks":[{"status":"decodificado",
                    "recipelist_sha256":"d"*64,
                    "conflict_count":0} for _ in range(7)]}
assert mirror_report_matches(review,report)
no_batch, counts = build_export(review)
assert len(no_batch["fusions"]) == 1 and counts["ignoredUnreviewed"] == 1
with_batch, counts = build_export(review,reviewed=True,fusion_report=report)
assert len(with_batch["fusions"]) == 2
assert counts["dueltraceReviewed"] == 1
bad_report = copy.deepcopy(report)
bad_report["matching_banks"] = 6
try:
    build_export(review, reviewed=True, fusion_report=bad_report)
except ValueError:
    pass
else:
    raise AssertionError("Divergent 7-bank fusion report was accepted")
print("PASS: individually verified recipes default; reviewed batches require explicit 7-bank proof")

with tempfile.TemporaryDirectory() as base:
    directory = Path(base)
    home = directory / "ForbiddenMemoriesMultiMod"
    home.mkdir()
    file = home / "profile.json"
    file.write_text(json.dumps(p),encoding="utf-8")
    (home / "registro_mods.json").write_text(json.dumps(
        {"entries":[{"file_name":"Yu-Gi-Oh! FM Original (Drop x15).bin",
                     "profile_path":str(file)}]}),encoding="utf-8")
    assert choose_profile(None,home) == file.resolve()
print("PASS: discovers the local DUELTRACE Original profile without copying ROM bytes")
