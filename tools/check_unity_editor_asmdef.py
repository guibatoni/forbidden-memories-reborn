#!/usr/bin/env python3
"""Prevent Unity EditMode test assembly reference regressions.

GitHub's .NET Core smoke compiles only Reborn.Core, not Unity assemblies.
Check the dependency graph explicitly so newly introduced Editor tests don't
break Unity with CS0234 (Reborn.Presentation / UnityEngine.InputSystem).
"""
from __future__ import annotations

import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


def load_json(relative: str) -> dict:
    path = ROOT / relative
    data = json.loads(path.read_text(encoding="utf-8"))
    if not isinstance(data, dict):
        raise AssertionError(f"{relative} must be a JSON object")
    return data


test_asm = load_json("Assets/Tests/Editor/Reborn.Core.Tests.asmdef")
core_asm = load_json("Assets/Scripts/Core/Reborn.Core.asmdef")
presentation_asm = load_json(
    "Assets/Scripts/Presentation/Reborn.Presentation.asmdef")
packages = load_json("Packages/manifest.json")

test_refs = set(test_asm.get("references", []))
production_refs = set(presentation_asm.get("references", []))
core_refs = set(core_asm.get("references", []))
deps = packages.get("dependencies", {})
tests = list((ROOT / "Assets/Tests/Editor").rglob("*.cs"))

checks = {
    "test assembly explicitly references Reborn.Core":
        "Reborn.Core" in test_refs,
    "test assembly explicitly references Reborn.Presentation":
        "Reborn.Presentation" in test_refs,
    "test assembly explicitly references Unity.InputSystem":
        "Unity.InputSystem" in test_refs,
    "source Reborn.Presentation assembly has exact registered name":
        presentation_asm.get("name") == "Reborn.Presentation",
    "source Reborn.Core assembly has exact registered name":
        core_asm.get("name") == "Reborn.Core",
    "production presentation itself references Core and Input System":
        {"Reborn.Core", "Unity.InputSystem"}.issubset(production_refs),
    "Core stays independent of Unity engine and presentation":
        core_asm.get("noEngineReferences") is True and
        "Reborn.Presentation" not in core_refs,
    "Unity Input System package is installed in manifest":
        bool(deps.get("com.unity.inputsystem")),
    "Unity Test Framework package is installed in manifest":
        bool(deps.get("com.unity.test-framework")),
    "Editor test assembly is Editor only":
        test_asm.get("includePlatforms") == ["Editor"],
    "Unity marks the assembly as NUnit TestAssemblies":
        "TestAssemblies" in test_asm.get("optionalUnityReferences", []),
    "Editor tests using the presentation namespace have explicit reference":
        not any("using Reborn.Presentation;" in path.read_text(encoding="utf-8")
                for path in tests) or "Reborn.Presentation" in test_refs,
    "Editor tests using Unity Input System have explicit reference":
        not any("using UnityEngine.InputSystem;" in path.read_text(encoding="utf-8")
                for path in tests) or "Unity.InputSystem" in test_refs,
    "regressions for keyboard initialization and keyboard position exist":
        (ROOT / "Assets/Tests/Editor/KeyboardInitializationTests.cs").is_file()
        and (ROOT / "Assets/Tests/Editor/KeyboardPositionTests.cs").is_file(),
}

for label, ok in checks.items():
    print(("OK " if ok else "FAIL ") + label)

if not all(checks.values()):
    raise SystemExit("Unity Editor test assembly references are invalid")

print(f"PASS: {len(checks)} Unity Editor asmdef/package contracts checked")
print("Unity Editor compilation still requires Unity 6000.3 and local Package Manager.")
