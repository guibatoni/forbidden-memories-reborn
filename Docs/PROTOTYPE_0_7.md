# Reborn 0.7 — Disc data, tactical AI and random decks

## Current status
- New matches shuffle both decks independently (40 cards), with deterministic seed support in tests.
- The experimental AI evaluates placements, Guardian Stars, defensive positions, two-card *verified* fusions and profitable attacks. It avoids known losing attacks.
- Imported card names/stats, fusions, equipment targets, rituals, opponent weighted deck/drop pools, starter weighted pools and optional card art can be extracted **locally** using the disc owned by the user and an upstream open-source parser.
- The 0.7 parser does **not** claim to extract opponent story scripts, full card effect logic, scripted conditions or exactly replicate the original duelist AI. Imported weighted pools are source data, not fully simulated deck generation rules.

## Required local extraction (Windows)
The importer depends on the independently developed [YFM Re-Decomp/FM Editor](https://github.com/Unchiga/Yu-Gi-Oh-Forbidden-Memories-Recompiled). No game bytes or upstream parser are bundled in our public repository.

1. Install Python 3 and optionally `py -m pip install Pillow` for 4x PNG resizing.
2. In a separate folder, run `git clone https://github.com/Unchiga/Yu-Gi-Oh-Forbidden-Memories-Recompiled.git`.
3. From the Reborn project root:
```powershell
py tools/import_fm_disc.py --disc "C:\Games\Yu-Gi-Oh! FM Original (Drop x15)(1).bin" --upstream "C:\Projects\Yu-Gi-Oh-Forbidden-Memories-Recompiled" --project . --edition original-ps1-drop-x15 --art --upscale 4
```
Replace the Windows paths with real paths.

## Outputs (private, gitignored)
- `Assets/StreamingAssets/RebornContent/original_export.json` — 722 cards, IDs, types, stats, names, descriptions, Guardian Stars, opponents and parsed fusion pairs. The Unity 0.6 JSON loader reads these.
- `PrivateExtracts/reborn-disc/disc_tables.json` — all opponent weighted decks/drops, starter pools, equip compatibility, ritual recipes and AI parameter bytes; **not yet wired to actual campaign AI**.
- `Assets/Resources/RebornCardArt/001.png` … `722.png` — optional PNG assets for local Unity display, upscaled from PS1 pixel sources with simple sharpening. They contain no new artistic detail and are **never committed**.
- `PrivateExtracts/reborn-disc/art/portraits` and source image manifest — optional opponent portraits.

## Validation
Run `py tools/validate_import.py Assets/StreamingAssets/RebornContent/original_export.json` and inspect `PrivateExtracts/reborn-disc/import-report.json`.

**Critical:** An edition called *Original (Drop x15)* is a modded disc, not a verified retail image. Do not label it a byte-identical retail extraction. Do not upload disc bytes, images, original text, private exports, or copyrighted character assets to this public repository or to Vercel.

## AI approach
First ship legal-action planning with deterministic evaluators and simulation. Next: optional minimax/beam search with configurable time budget; optional Unity ML-Agents for learned policies; optional Ollama local or Vercel AI SDK as development/debug helpers, **not** as the authoritative rules engine.

## Vercel
Suitable for a separate web dashboard, release notes, telemetry **with opt-in**, and account/licensing backend if one is ever needed. Unity remains the Windows game executable, and its turn resolution is local/offline. Do not send raw ISO or protected art to cloud APIs.
