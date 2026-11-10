# Forbidden Memories Reborn — Prototype 0.15: Classic turn controls

## Scope
This builds on the unmerged 0.14.4 attack-aim feature branch, retaining the
approved five-card inverted fan and fixed game camera composition.

## Player sequence
1. At the start of a turn, draw up to five cards.
2. Select one hand card or an ordered Shift-fusion sequence. This consumes
   **one field placement action** and can place a Monster or a Support card.
3. Choose face-up/face-down, ATK/DEF, and Guardian Star on the HUD before
   placing the result in a legal field slot.
4. After placement, enter Battle automatically. Q or E toggles the selected
   player's monster between ATK and DEF as many times as desired until that
   monster has attacked. Newly set monsters may also toggle.
5. Attack with a face-up ATK monster (select attacker then opposing target).
   An attacker cannot later change to defense during the same turn.
6. Press **SPACE** or the HUD's "Passar turno" button to end the player's
   turn. Before the one mandatory placement, passing and beginning Battle
   are rejected by the rules engine. No automatic end-turn remains after a
   placement or after using the last available attacker.

## Card inspection
Right-click a visible card to open a large card artwork panel with name,
ID, type, ATK/DEF, both Guardian Stars where available, and imported
description/possible effect text. Right-click again, Escape or X closes it.
The panel does not execute effects and refuses hidden opponent card contents.
Previously right mouse drag rotated the arena; it now belongs exclusively
to card inspection. Existing wheel/R behavior is retained.

## Presentation
Newly drawn hand cards glide in from the deck; a selected card has a small
original tilt interpolation. This is inspired by the legibility of modern
digital card games but is an independent implementation. The approved fan
geometry, order, selected-fusion numbers, and hand focus modes remain intact.

## Limitations
The original disc's effect scripts, magic/ritual/equip execution, and many
campaign data tables are not yet implemented. No private disc assets are
published. Strict mandatory placement can expose a dead-end if all relevant
board slots are full and no legal fusion/field replacement action exists.
Do not silently allow passing in this state. The AI Lab records such
synthetic situations as unresolved; a legal no-slot resolution is future work.
Opponent turns should be reviewed in Unity when no valid placement exists.

## Verification
- Run .NET Core smoke checks, cross-assembly checks, the AI Lab, and
  `python3 tools/check_classic_turn_inspection.py` in GitHub Actions.
- Unity 6000.3.28f1: compile, test both right-click inspection and overlay
  closure, set/fuse cards, press SPACE before and after a mandatory play,
  press Q/E repeatedly with new/old monsters, attack, ensure post-attack
  Q/E fails; confirm one card placement per turn.
- Confirm neither the duel's hidden opponent cards nor the hand's approved
  fan are exposed/altered. Evaluate 1920x1080 and a smaller Game View size.
- Unity visual compilation, hit-testing and VFX remain unverified by
  GitHub's headless static checks.

## Branch strategy
This milestone is a dependent, draft PR against
`feature/prototype-0.14.4-attack-aim`; main and all backup branches are
untouched until approval.
