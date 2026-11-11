#!/usr/bin/env python3
"""Regression guard for the ArenaHud constructor / Unity PlayerPrefs error.

Static analysis only. The actual AddComponent NUnit regression is in
Assets/Tests/Editor/KeyboardInitializationTests.cs and must run in Unity.
"""
from pathlib import Path
root = Path(__file__).resolve().parents[1]
def read(name):
    return (root / name).read_text(encoding="utf-8")
hud = read("Assets/Scripts/Presentation/ArenaHud.KeyboardSettings.cs")
keys = read("Assets/Scripts/Presentation/RebornKeyBindings.cs")
game = read("Assets/Scripts/Presentation/RebornGame.cs")
editor = read("Assets/Tests/Editor/KeyboardInitializationTests.cs")
constructor = keys.split("public RebornKeyBindings()", 1)[1].split("public void LoadSaved()",1)[0]
awake = hud.split("private void Awake()",1)[1].split("public void ToggleKeyboardSettings()",1)[0]
checks = {
    "HUD does not construct keyboard controls in an instance field initializer":
        "private RebornKeyBindings keyBindings;" in hud and
        "private readonly RebornKeyBindings keyBindings = " not in hud,
    "key bindings constructor is pure, with no Unity PlayerPrefs APIs":
        "PlayerPrefs.Get" not in constructor and
        "PlayerPrefs.Set" not in constructor and
        "PlayerPrefs.Delete" not in constructor and
        "keys[action] = Default(action)" in constructor,
    "only Unity Awake constructs and loads saved mappings":
        "keyBindings = new RebornKeyBindings();" in awake and
        "keyBindings.LoadSaved();" in awake,
    "load accesses PlayerPrefs in an explicit lifecycle method":
        "public void LoadSaved()" in keys and
        "PlayerPrefs.GetString(" in keys.split("public void LoadSaved()",1)[1] and
        "private void Awake()" in hud,
    "normal GameObject.AddComponent / Initialize sequence stays intact":
        "gameObject.AddComponent<ArenaHud>()" in game and
        "hud.Initialize(session, arena," in game,
    "Unity editor test explicitly reproduces constructor path":
        "gameObject.AddComponent<ArenaHud>()" in editor and
        "Assert.DoesNotThrow" in editor and
        "Assert.IsNotNull(hud.KeyBindings" in editor,
    "remapping still saves selected user keys normally":
        "PlayerPrefs.SetString(" in keys and
        "PlayerPrefs.Save();" in keys and
        "PlayerPrefs.DeleteKey(" in keys,
    "reset and preview remain safe after initialization":
        "keyBindings.ResetDefaults();" in hud and
        "keyBindings.Get(action)" in hud,
}
for name, ok in checks.items():
    print(("OK " if ok else "FAIL ") + name)
if not all(checks.values()):
    raise SystemExit("FAIL: Unity 0.24.1 keyboard lifecycle regression")
print(f"PASS: {len(checks)} keyboard lifecycle contracts")
