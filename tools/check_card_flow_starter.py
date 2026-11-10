#!/usr/bin/env python3
"""Source-only regression checks (not Unity Editor gameplay/visual tests)."""
from pathlib import Path
p=Path(__file__).resolve().parents[1]
def read(path):
    return (p/path).read_text(encoding="utf-8")
input_code=read("Assets/Scripts/Presentation/ArenaInteraction.cs")
wizard=read("Assets/Scripts/Presentation/ArenaHud.PlacementWizard.cs")
hud=read("Assets/Scripts/Presentation/ArenaHud.cs")
starter=read("Assets/Scripts/Core/StarterDeckCatalog.cs")
loader=read("Assets/Scripts/Presentation/StarterDeckLoader.cs")
picker=read("Assets/Scripts/Presentation/ArenaHud.StarterDeckChoice.cs")
game=read("Assets/Scripts/Presentation/RebornGame.cs")
importer=read("tools/import_fm_disc.py")
view=read("Assets/Scripts/Presentation/CardView.cs")
factory=read("Assets/Scripts/Presentation/ArenaFactory.cs")
checks={
    "clicking legal zone opens modal rather than committing":
        'hud.BeginPlacementChoices(outcome, ritual,' in input_code and
        '() => CommitMatchCard(zone)' in input_code,
    "Guardian before facing before post-placement position":
        'PlacementChoiceStep.Guardian' in wizard and
        'PlacementChoiceStep.Facing' in wizard and
        'PlacementChoiceStep.Position' in wizard,
    "wizard is presented before regular duel HUD":
        'if (PlacementWizardOpen)' in hud and 'DrawPlacementChoices();' in hud,
    "input locked while pending modal":
        'PlacementWizardOpen' in hud and
        'if (hud.PlacementWizardOpen)' in input_code,
    "cancel has no effect on duel before commit":
        'Posicionamento cancelado. Nenhuma carta foi utilizada' in wizard,
    "real placement happens only after face decision":
        'bool succeeded = callback != null && callback();' in wizard,
    "monster position follows committed field play":
        'hud.MarkPlacedMonster(zone.Index)' in input_code and
        'match.TryChangePosition(DuelSide.Player,' in wizard,
    "face-down ritual never activated as set":
        'PlaceFaceDown = !placementIsRitual && faceDown;' in wizard,
    "old side toggles replaced with contextual flow":
        '"1. Selecione cartas da mao' in hud and
        '"Guardian: segunda"' not in hud,
    "hand cards drawn in ordered cascade":
        'arrivalDelay' in view and
        '0.065f * (i - previousHandCount)' in factory,
    "fan geometry remains preserved":
        'HandFanGeometry.WithHoveredCard(' in factory,
    "starter uses seven original weighted pools":
        'public static readonly int[] DrawCounts = {16, 16, 4, 1, 1, 1, 1}' in starter,
    "starter enforces exactly 40 cards and max 3 copies":
        'copies[id] < 3' in starter and
        'if (result.Count != 40)' in starter,
    "four alternative selections, not four static fake deck lists":
        'FourChoices(Random rng)' in starter and
        'results.Count != 4' in starter,
    "starter pools reject unverified mismatched SHA":
        'content.SourceSha256' in loader and
        'starterPools.Length != 7' in loader,
    "real ISO importer exports Unity starter rows":
        '"starter_pools.json"' in importer and
        'normalize_starter_weights(row["weights"])' in importer,
    "starter picker communicates PS1 original has one random deck":
        'No jogo original existe apenas um sorteio' in picker,
    "missing starter pools never auto-selected as original":
        'starterMissing' in picker and
        'Continuar apenas em TREINO' in picker,
    "deck chosen persists over reset":
        'IReadOnlyList<CardDefinition> chosenStarter = null;' in game and
        'var playerSource = chosenStarter ??' in game,
    "starter UI requires explicit choice before battle":
        'StarterSelectionRequired' in hud and
        'ConfigureStarterDeckChoices(starterOffers' in game,
}
for k,ok in checks.items():
    print(('OK ' if ok else 'FAIL ')+k)
if not all(checks.values()):
    raise SystemExit('0.18 starter/placement contracts failed')
print(f'PASS: {len(checks)} 0.18 source checks; Unity video validation still pending')
