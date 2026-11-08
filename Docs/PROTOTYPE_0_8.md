# Prototype 0.8 — Weighted Opponent Decks

## What changed
- The local importer writes a private file alongside card data: Assets/StreamingAssets/RebornContent/opponent_decks.json.
- It has one 722-element weight array per duelist, indexed by original card ID from 1 to 722.
- The new C# core generates 40-card experimental opponent decks with weighted sampling and at most three copies per card.
- A fresh seed generates a different opponent deck every new duel. Seeded draws remain reproducible for tests.
- Unity refuses pools unless their source edition, SHA-256 and opponent deckKey match the card catalog.
- The duel HUD identifies the current opponent and provides a Next Opponent button when multiple pools are loaded.
- The player's deck samples 40 distinct cards for practice. It is NOT the authentic starter deck.

## How to import on your Windows PC
The supplied disc was named Yu-Gi-Oh! FM Original (Drop x15)(1).bin; because Drop x15 is a mod, it must not be presented as an unmodified retail disc.

From the Reborn project root, with Python 3 and a local checkout of the open-source FM Editor:

    py tools/import_fm_disc.py --disc "C:\Games\Yu-Gi-Oh! FM Original (Drop x15)(1).bin" --upstream "C:\Projects\Yu-Gi-Oh-Forbidden-Memories-Recompiled" --project . --edition original-ps1-drop-x15 --art --upscale 4

Adjust both paths. Install Pillow if using upscaled art. After import, check the two files in Assets/StreamingAssets/RebornContent:
- original_export.json: cards, IDs, types, stats, descriptions, Guardian Stars, fusions and duelist identifiers.
- opponent_decks.json: per-opponent deck weights, source edition and matching disc SHA.
Additional private tables and import diagnostics are stored under PrivateExtracts/reborn-disc.

Validate the first JSON:

    py tools/validate_import.py Assets/StreamingAssets/RebornContent/original_export.json

## Technical limitations
- This reproduces a basic weighted sample with three-copy cap, not the bit-exact retail PRNG or deck ordering.
- An opponent's deck weight pool is not a predeclared fixed deck; a new duel can draw a different selection.
- The opponent AI still uses heuristic action selection, not the original binary AI program.
- Real starter distribution, support effects, scripted dialogue and full story sequence remain pending verification.
- The PS1 artworks remain local. Upscaling by interpolation does not create missing visual detail.
- The project never embeds or uploads ROM/ISO files, extracted names, artwork or personal paths to GitHub.

## Automated tests
The GitHub Action checks the pure C# weighted sampler, importer weight normalization and acceptance of source edition original-ps1-drop-x15. Unity scene rendering and disk extraction need separate validation on the local PC.
