#!/usr/bin/env python3
"""Disc-free verification of argument safety and local path discovery."""
import importlib.util
import tempfile
from pathlib import Path

import_path = Path(__file__).with_name("import_fm_disc.py")
spec = importlib.util.spec_from_file_location("reborn_local_importer", import_path)
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)

with tempfile.TemporaryDirectory() as base:
    root = Path(base)
    pc = root / "tools" / "pc"
    (pc / "fm_editor").mkdir(parents=True)
    (pc / "fm_editor" / "gamedata.py").write_text("# test\n", encoding="utf-8")
    (pc / "extract_images.py").write_text("# test\n", encoding="utf-8")
    assert module.find_upstream(root) == pc
    assert module.find_upstream(pc) == pc
    fake = root / "file.bin"
    fake.write_bytes(b"hello binary")
    import hashlib
    assert module.sha256(fake) == hashlib.sha256(b"hello binary").hexdigest()

    raw = {str(i): i for i in range(1, 21)}
    normalized = module.normalize_deck_weights(raw)
    assert len(normalized) == 722 and normalized[0] == 1 and normalized[19] == 20
    assert module.normalize_deck_weights(list(normalized)) == normalized
    assert module.normalize_deck_weights([0] + normalized) == normalized
    for malformed in ({"0": 9, **raw}, {"800": 1, **raw}, {str(i): 1 for i in range(1, 10)}, [1] * 4):
        try:
            module.normalize_deck_weights(malformed)
        except ValueError:
            pass
        else:
            raise AssertionError("Malformed pool accepted")
    # Starter field/magic pools have fewer than fourteen cards.
    starter = module.normalize_starter_weights({"101": 1024, "102": 1024})
    assert len(starter) == 722 and sum(starter) == 2048
    assert starter[100] == 1024 and starter[101] == 1024
    try:
        module.normalize_starter_weights({"101": 100})
    except ValueError:
        pass
    else:
        raise AssertionError("Broken starter weight total accepted")
    print("PASS: sparse two-card starter magic pool and corrupt weights")
    print("PASS: normalized opponent weights and malformed pool rejection")

    validator_file = Path(__file__).with_name("validate_import.py")
    check_spec = importlib.util.spec_from_file_location("reborn_local_validation", validator_file)
    check_module = importlib.util.module_from_spec(check_spec)
    check_spec.loader.exec_module(check_module)
    sample = {"schema": 1, "source": {"edition": "original-ps1-drop-x15",
        "discSha256": "a" * 64}, "cards": [{"id": 1, "name": "TEST",
        "kind": "Monster", "attack": 100, "defense": 100,
        "guardianStarA": "", "guardianStarB": ""}]}
    errors, _ = check_module.validate(sample)
    assert not errors, errors
    sample["source"]["edition"] = "unrelated"
    errors, _ = check_module.validate(sample)
    assert any("source.edition" in issue for issue in errors)
    print("PASS: original-ps1-* editions validate without accepting unrelated discs")
    print("PASS: local parser discovery and SHA-256 helpers")
# A fully synthetic 722-card disc tests the export format without copying
# retail names/text or including any PS1 ISO in the test runner.
from types import SimpleNamespace

with tempfile.TemporaryDirectory() as sandbox:
    root = Path(sandbox)
    (root / "Assets").mkdir()
    disc = root / "synthetic.bin"
    disc.write_bytes(b"not-a-real-disc")
    cards = {}
    for index in range(1, 723):
        kind = 22 if index in (666, 667) else (20 if index == 710 else
                21 if index == 711 else 23 if index == 712 else 0)
        cards[index] = SimpleNamespace(id=index, name=f"Synthetic {index:03d}",
            description=f"Synthetic-only description {index}", attack=500,
            defense=700, type=kind, star1=0, star2=0)
    fake_data = SimpleNamespace(cards=cards,
        fusions={(1,2):3, (2,3):4},
        glitch_fusions={(2,3)},
        rituals={666:(1,2,3,4),667:(1,1,1,4)},
        pools=[], equips={}, notes=[])
    game = SimpleNamespace(
        read_game=lambda slus,wa: fake_data,
        TYPE_MAGIC=20, TYPE_TRAP=21, TYPE_RITUAL=22, TYPE_EQUIP=23,
        TYPE_NAMES=["Dragon"]*20 + ["Magic","Trap","Ritual","Equip"],
        STAR_NAMES=[""], STARTER_BASE=0)
    old_load_game = module.load_game
    try:
        module.load_game = lambda disc_path,pc: (
            SimpleNamespace(slus=b"",wa=b""),game)
        report = module.export(disc, root, root, "original-ps1-test",
                               upscale=1, extract_art=False)
    finally:
        module.load_game = old_load_game
    generated = (root/"Assets"/"StreamingAssets"/"RebornContent"/"original_export.json")
    data = check_module.validate(
        __import__("json").loads(generated.read_text(encoding="utf-8")))
    errors, details = data
    assert errors == [], errors
    assert details["cards"] == 722 and details["rituals"] == 2
    assert details["missingDescriptions"] == 0
    assert report["ps1ReaderQuirkFusionsIncluded"] == 1
    assert report["fusions"] == 2 and report["descriptionCount"] == 722
    assert report["descriptionCoverageByKind"]["Ritual"]["withDescription"] == 2
    extracted = __import__("json").loads(generated.read_text(encoding="utf-8"))
    assert extracted["cards"][0]["typeLabel"] == "Dragon"
    assert extracted["cards"][711]["typeLabel"] == "Equip"
    assert any(recipe["tributes"] == [1,1,1]
               for recipe in extracted["rituals"])
    assert report["descriptionCoverageByKind"]["Spell"]["withDescription"] == 1
    assert report["descriptionCoverageByKind"]["Trap"]["withDescription"] == 1
    assert report["descriptionCoverageByKind"]["Equip"]["withDescription"] == 1
    print("PASS: synthetic 722-card export, PS1 reader quirks, all types and rituals")
print("NOTE: CI does not possess the user's original disc and cannot validate retail hashes.")

