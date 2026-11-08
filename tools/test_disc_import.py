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
print("NOTE: no disc extraction attempted by CI.")
