# Forbidden Memories Reborn — Prototype 0.16: In-field fusion and occupied-slot play

## Player controls
1. Draw to five cards and select one to five hand materials. Use Shift-click
   to number the hand materials in the exact desired order (1–5).
2. Instead of dropping onto an empty monster slot, click or drag the selected
   materials onto an **existing monster in your own row**.
3. The field monster is material #0, always processed first. Remaining hand
   materials resolve pairwise in their numbered order. Up to six physical
   starting cards take part (one field + five hand).
4. Hovering your occupied monster while hand materials are selected shows
   the resulting monster, ATK/DEF and its two Guardian Stars. The arena
   highlights only positions allowed by the duel rules.
5. Choose final position and Guardian Star, then confirm placement.
   This counts as your mandatory one hand play. After placement the duel
   enters Battle; SPACE ends the turn explicitly, and Q/E switches ATK/DEF
   until the monster attacks (as in 0.15).
6. A verified fusion creates a face-up monster in the **same field slot**.
   If a pair has no recipe, its earlier intermediate is discarded and the
   later card becomes the next combination material. A failed sequence
   ending in a monster replaces the previous field monster.
7. No recipe or edit will conjure a fake card. Only pairs present in
   `FusionCatalog` participate in successful fusion.

## Gameplay and integrity
- `DuelMatch.PreviewFieldResult` and `CanPlayOnField` are read-only.
  Rejected actions never consume hand cards, turn actions, or LP.
- `TryPlayOnField` validates before mutation, preserves the chosen hand
  sequence and sends *physical* losing originals to the graveyard.
  Intermediate fusion results are virtual, not physically buried.
- Field combinations cannot be initiated after the player's mandatory
  placement or during Battle; opponent field positions are never targets.
- An unsupported Spell, Trap, Equip or Ritual **result** is rejected.
  Spell/equip/ritual *effects* (including their special activation,
  attribute boosts, rank changes and trap triggers) still require real
  source data and are not fabricated.
- A card that survived an invalid field recipe can be set face-down.
  A successful field fusion is face-up.
- The opponent prefers an ordinary placement, then verified field recipe,
  then a support set, then a legal field-monster replacement when no other
  option exists. Every decision is committed by the same rules engine.
- If no legal play is possible at all (for example only unverified support
  effects with all slots occupied), do not silently skip the mandatory
  placement. This remaining exceptional state will require verified effects.

## Visual implementation
- The approved 0.15 hand fan, hover, selection, drawing and fixed camera
  are preserved.
- The actual summon reuses the existing animation from hand to slot.
  An original cyan procedural burst identifies a successful fusion;
  an amber one identifies a failed-recipe replacement.
- The UI previews only combinations with the player's own monsters and
  never reveals an opponent's hidden card.

## Validation
Automated:
- `dotnet run --project tools/Reborn.Core.Smoke/Reborn.Core.Smoke.csproj -c Release`
- `dotnet run --project tools/Reborn.CrossAssembly.Smoke/Reborn.CrossAssembly.Smoke.csproj -c Release`
- `python3 tools/check_field_fusions.py` and all preexisting static checks
- `dotnet run --project tools/Reborn.AiLab/Reborn.AiLab.csproj -c Release -- --self-test`

Within Unity 6000.3.28f1 (not tested by GitHub CI):
1. Open the clean project and verify zero compiler errors.
2. Hover a selected card over an occupied player monster; confirm accurate
   result preview and highlight. Empty slots still work as before.
3. Click and drag one hand card onto the player monster. Check both a
   validated recipe and a **failed recipe** (replacement, lost materials).
4. Shift-select 2–5 hand cards and play onto one field monster. Verify
   sequence ordering, returned Guardian Star, and resulting face orientation.
5. Fill five monster zones on your side; ensure hand play still works by
   replacing a monster or performing a validated field fusion.
6. Confirm a failed Equip/Spell result does not silently invent a boost.
7. Check the face-down opponent remains private; field-target collisions and
   hand drag stable at 1920x1080 and reduced Game View.
8. Confirm the AI can perform a legal combo/replacement when its field is full
   and that SPACE, Q/E, attacks and the camera behave as before.

## Scope and safety
No retail ISO, original card images, opponent decks, licensed audio,
private adaptive AI saves or extracted assets are committed. Verify
fusion tables against the user's local game export before claiming complete
PS1 fidelity. Field-vs-hand fusion rank accounting is deferred until
the precise game rules/drops scoring are validated.
