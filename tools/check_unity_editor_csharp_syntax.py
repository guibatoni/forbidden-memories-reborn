#!/usr/bin/env python3
"""Fail CI if Unity EditMode test scripts repeat the C# CS0819 pattern.

Not a replacement for compiling Unity Editor tests. This check targets the
specific regression: 'var a = ..., b = ...;' is illegal C#.
"""
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[1]
tests_dir = ROOT / "Assets" / "Tests" / "Editor"
pattern = re.compile(r"\bvar\s+[A-Za-z_]\w*\s*=\s*[^;\n]*?,\s*[A-Za-z_]\w*\s*=")
issues = []
tested = 0

for path in sorted(tests_dir.rglob("*.cs")):
    tested += 1
    for number, line in enumerate(path.read_text(encoding="utf-8").splitlines(), 1):
        if pattern.search(line) and not line.lstrip().startswith("//"):
            issues.append(f"{path.relative_to(ROOT)}:{number}: {line.strip()}")

for issue in issues:
    print("FAIL: CS0819: " + issue)
if issues:
    raise SystemExit(f"Found {len(issues)} invalid implicit-typed multi-declarators")

fusion_test = (tests_dir / "FusionAssistantTests.cs").read_text(encoding="utf-8")
assert 'var a = Monster("A");' in fusion_test
assert 'var b = Monster("B");' in fusion_test
print(f"PASS: {tested} Unity Editor C# test files checked against CS0819 pattern")
print("Note: Unity Editor compilation remains a separate local validation gate.")
