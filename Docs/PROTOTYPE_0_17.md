# Forbidden Memories Reborn — Prototype 0.17
## PS1 fusion fidelity, 722-card texts, original rituals and original ceremonies

This milestone is stacked on 0.16 and must not be merged into the public
`main` branch until Unity and a **real locally owned disc** have been tested.

### Source of truth (local and private)

The project already contains `tools/import_fm_disc.py`, which uses the open-source
[FM Editor parser](https://github.com/Unchiga/Yu-Gi-Oh-Forbidden-Memories-Recompiled)
from a separate checkout. The ROM data are **never committed**. Its parsed
`data.cards[1..722]` includes card names, description texts, card kinds,
ATK/DEF and Guardian Stars. It also reads the disc's `data.fusions`,
`data.rituals`, equipped targets and duelist tables.

Prototype 0.17 extends that existing importer to:
- export all 722 cards, preserving descriptions **as supplied by the disc**
  without inventing text or effects;
- report totals and non-empty descriptions for Monster, Spell, Trap, Equip
  and Ritual separately; missing entries are reported, never guessed;
- export **all PS1-compatible fusion recipes returned by the parser**,
  including the original reader's 15 odd-record-overread recipes when present;
  such rows have `origin: ps1-reader-quirk`, the other rows
  `origin: disc-fusion-table`;
- export `rituals[]` as `{ritualCard, tributes:[t1,t2,t3], result, verified}`;
- write both structured and private reports (with ROM SHA-256) locally;
- allow optional read-only comparison with a DUELTRACE `*.fusoes.json`
  candidate report using `tools/audit_dueltrace_original.py`.

The user-owned file **FM Original (Drop x15).bin** may have modified drop
rates or other tables. Its filename is **not proof** of bitwise identity
with retail. Always check its actual SHA-256 and compare extracted recipes
with an authoritative original-data reference before claiming full fidelity.
The `verified` field means an accepted binary table record, not
that each row has been individually reproduced in a duel.

### One-command import on Windows

From a clone of this Reborn 0.17 project, with Python 3 and Git installed:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\IMPORTAR_ORIGINAL.ps1 -Disc "C:\Games\Yu-Gi-Oh! FM Original (Drop x15).bin"
```

The helper obtains the open-source parser into `PrivateExtracts/tools/`
(if not already present), reads your local disc, exports data, runs strict
card-ID validation and prints the description coverage report. No ROM or
description data are uploaded.

For an existing DUELTRACE candidate report:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\IMPORTAR_ORIGINAL.ps1 -Disc "C:\Games\Yu-Gi-Oh! FM Original (Drop x15).bin" -DueltraceReport "C:\Users\YOU\AppData\Local\ForbiddenMemoriesMultiMod\investigacoes\YOURHASH.fusoes.json"
```

Alternatively run directly:

```text
py tools/import_fm_disc.py --disc "C:\Games\Yu-Gi-Oh! FM Original (Drop x15).bin" --upstream "C:\Sources\Yu-Gi-Oh-Forbidden-Memories-Recompiled" --project . --edition original-ps1-drop-x15
py tools/validate_import.py Assets/StreamingAssets/RebornContent/original_export.json --complete
```

Local generated files:
- `Assets/StreamingAssets/RebornContent/original_export.json` — cards,
  descriptions, fusions and rituals used by Unity
- `Assets/StreamingAssets/RebornContent/opponent_decks.json` — synthetic
  deck-pool support based on the supplied edition
- `PrivateExtracts/reborn-disc/disc_tables.json` — auxiliary game tables
- `PrivateExtracts/reborn-disc/import-report.json` — extraction coverage

All paths are ignored by Git and are not included in public releases.
Never distribute those private texts/images or a full ISO as a public asset
without permission from the rightsholder.

### Original PS1 ritual mechanic — NOT modern Yu-Gi-Oh!

The ritual spell is activated using **three specific, face-up monster copies
already placed in three separate slots of your own field**. Some recipes
require repeated card IDs (e.g. three identical monsters); the field copies
must be physically distinct. These monsters are sacrificed and the
ritual result appears in their place. You do **not** need the ritual monster
in your hand, an Extra Deck, an exact Level sum, or generic tributes.

Example: a three-copy ritual can ask for three separate Blue-Eyes White
Dragon cards rather than three different monster IDs. The actual required
IDs and result are always taken from the local disc export, never assumed.

In the 0.17 prototype:
1. Have the three necessary monsters face-up in separate field slots.
2. Select the ritual card in hand. The status explains whether the
   required three tributes are ready.
3. Choose one of the three highlighted tribute slots. This becomes
   the result slot after the three monsters are sacrificed.
4. Choose result position and Guardian Star. A successful ritual places
   the result face-up and consumes the **one mandatory card play** of the
   current turn; SPACE remains the explicit turn-end button.
5. A special purple/gold procedural ceremony appears without zooming the
   fixed camera.

Important distinction: the retail game may consume a ritual card when
activated without its three materials and simply do nothing. This
prototype intentionally *rejects* invalid activation before consuming
cards until the exact failure flow is validated on the user's disc.
That difference must be resolved before calling ritual behavior identical
to retail. The optional set-face-down support behavior is also provisional.

### Original fusion visual language (independent artwork and motion)

Visual-only `FusionCeremonyVfx` uses:
- three animated concentric orbit rings made from procedurally positioned
  LineRenderer vertices, alternating direction and elevation;
- six tiny orbiting light motes moving through a synchronized helix;
- a brief, timed center reveal pillar with a distinct cadence;
- a cyan-gold palette for fusion, warm gold for replacement, and
  purple-gold for ritual;
- no copyrighted animation, prerendered textures, external assets, sound
  clips, or camera movement.

It plays for **real successful hand fusion**, field fusion and ritual,
not for unverified attempts. It never changes LP, fusion outcomes or
attacker eligibility.

### Verification and limitations

CI:
- .NET Core deterministic duel smoke, including three exact tributes
  and no-mutation invalid ritual checks;
- separate-assembly API compilation;
- complete 722-*synthetic*-card export, text counts per kind, test
  reader-quirk fusion and ritual encoding;
- static original-data/ritual/VFX source contracts;
- pre-existing AI Lab and regression tests.

Still required:
- actual original disc import and report review (not performed in CI);
- compare all fusion IDs/results and ritual recipes with known retail
  reference, flag modified Drop x15 differences;
- Unity 6000.3.28f1 Editor compilation and mouse/zone hit testing;
- play a successful ritual and one unsuccessful ritual in the real PS1;
- verify geometry, depth, UI, hand fan and visual comfort at multiple
  window sizes;
- profile scene performance with optional animations toggled on/off.

**Status:** Source and tests prepared. Real-disc extraction, completeness
figures and Unity visuals must not be claimed as already validated.
