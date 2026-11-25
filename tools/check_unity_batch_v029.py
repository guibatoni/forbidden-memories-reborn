#!/usr/bin/env python3
"""Static guard for the Windows Unity EditMode runner; not a Unity execution."""
from pathlib import Path

root = Path(__file__).resolve().parents[1]
script = (root / "tools/TESTAR_UNITY_REBORN.ps1").read_text(encoding="utf-8")
local = (root / "tools/VALIDAR_REBORN.ps1").read_text(encoding="utf-8")
manifest = (root / "ProjectSettings/ProjectVersion.txt").read_text(encoding="utf-8")
asmdef = (root / "Assets/Tests/Editor/Reborn.Core.Tests.asmdef").read_text(encoding="utf-8")
checks = {
    "Target actual project Unity editor version":
        "m_EditorVersion:" in manifest and
        "ProjectVersion.txt" in script and "$expectedVersion" in script,
    "Run actual Unity EditMode Test Runner in batchmode":
        all(flag in script for flag in (
            '"-batchmode"', '"-runTests"', '"-projectPath"',
            '"-testPlatform", "EditMode"', '"-testResults"',
            '"-logFile"', '"-assemblyNames"', '"Reborn.Core.Tests"'
        )) and '"name": "Reborn.Core.Tests"' in asmdef,
    "Do not pass unsupported quit flag while running Unity tests":
        '"-quit"' not in script,
    "Prevent false success using old NUnit XML":
        'Remove-Item -LiteralPath $results -Force' in script and
        '$testRun.GetAttribute("failed")' in script and
        '$testRun.GetAttribute("result")' in script and
        '$exitCode -ne 0' in script and
        '$total -le 0' in script,
    "Missing XML displays compiler errors from Unity log":
        "'error CS[0-9]+" in script and
        "Unity nao produziu XML de testes" in script,
    "Never put generated XML in tracked source":
        'Library\\RebornValidation' in script,
    "Local core check remains separate and does not auto-start Unity":
        '"check_unity_batch_v029.py"' in local and
        'TESTAR_UNITY_REBORN.ps1' not in local,
}
for label, passed in checks.items():
    print(("OK " if passed else "FAIL ") + label)
if not all(checks.values()):
    raise SystemExit("v0.29 Unity batch verification contract failed")
print(f"PASS: {len(checks)} static runner checks; run actual Unity locally")
