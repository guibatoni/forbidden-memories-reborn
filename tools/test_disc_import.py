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
    print("PASS: local parser discovery and SHA-256 helpers")
print("NOTE: no disc extraction attempted by CI.")
