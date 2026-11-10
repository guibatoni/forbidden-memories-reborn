#!/usr/bin/env python3
"""Source contracts for visible Original-card loading. Not Unity Editor compilation."""
from pathlib import Path
root=Path(__file__).resolve().parents[1]
def read(path):
    return (root/path).read_text(encoding="utf-8")
game=read("Assets/Scripts/Presentation/RebornGame.cs")
local=read("Assets/Scripts/Presentation/DueltraceLocalProfileLoader.cs")
loader=read("Assets/Scripts/Presentation/OriginalContentLoader.cs")
hud=read("Assets/Scripts/Presentation/ArenaHud.cs")
deck=read("Assets/Scripts/Core/DemoDeck.cs")
art=read("Assets/Scripts/Presentation/CardArtCatalog.cs")
checks={
    "fictional p04 card is recognized as a demo fixture":
        '"p04", "Fera do Nilo"' in deck,
    "full original export has precedence over DUELTRACE runtime fallback":
        game.index("OriginalContentLoader.TryLoad(") <
        game.index("DueltraceLocalProfileLoader.TryLoad("),
    "a malformed existing export is never silently overridden":
        '!System.IO.File.Exists(OriginalContentLoader.ExpectedPath)' in game,
    "DUELTRACE local registry selected without copies":
        'registro_mods.json' in local and 'File.ReadAllText(' in local,
    "only one Original profile is accepted":
        'candidates.Count != 1' in local,
    "profile paths stay inside DUELTRACE data home":
        'resolved.StartsWith(homePrefix' in local,
    "profile and registry SHA must match":
        'selected.sha256' in local and 'ValidSha(profile.rom_sha256)' in local,
    "722 verified records with unique real numeric IDs are mandatory":
        'profile.cards.Length != 722' in local and
        'cards[c.id - 1] != null' in local and
        'profile.verified.cartas != "verificado"' in local,
    "card identity uses original three-digit IDs":
        'c.id.ToString("000")' in local,
    "all special card kinds supported":
        all(s in local for s in ('CardKind.Spell','CardKind.Trap',
                                 'CardKind.Equip','CardKind.Ritual')),
    "no Guardian Stars fabricated":
        'GuardianStar.None, GuardianStar.None' in local,
    "only individually observed fusion recipes loaded directly":
        'f.validation != "observada_manualmente_no_jogo"' in local,
    "full validated export rejects partial or duplicate cards":
        'parsed.cards.Length != 722' in loader and
        '!seenCardIds.Add(entry.id)' in loader,
    "HUD explicitly marks unavailable content":
        'CATALOGO ORIGINAL AINDA NAO CARREGADO' in hud and
        'ConfigureContentAvailability(loaded)' in game,
    "cannot play until explicit demo acknowledgment":
        'ContentImportRequired' in hud and
        'demoAcknowledged = true;' in hud and
        'InputLocked => OpponentBusy || PlayerAttackBusy || ContentImportRequired' in hud,
    "art source preserves optional local PNG loader":
        'Resources.Load<Texture2D>("RebornCardArt/" + card.Id)' in art,
}
for k,ok in checks.items():
    print(('OK ' if ok else 'FAIL ')+k)
if not all(checks.values()):
    raise SystemExit('Original auto-loader source regression failed')
print(f'PASS: {len(checks)} original content autoload source contracts; Unity runtime testing pending')
