# Forbidden Memories Reborn — Prototype 0.14.2: Hand hover spacing

## What changed
Building on Prototype 0.14.1's corrected fan and dual compact/expanded
focus modes, card hover now gently moves adjacent hand cards outwards.
This makes the focused card easier to inspect without changing camera
settings, its position in hand or the established fusion-selection order.

- The focused card keeps its own baseline target; neighboring cards are
  displaced by 0.30 scene units (immediate neighbor), 0.14 (second
  neighbor) or 0.06 (more distant cards), away from the focus.
- On arena focus, no additional neighbor gaps are applied. The compact
  fan still drops to its lower previous position.
- The hovered card rises to 0.80 scene units and scales to 1.18;
  a selected but unhovered card is highlighted more subtly at 0.22
  elevation and 1.065 scale.
- Scene units are not physical millimeters. A stable screen-space
  hand region includes raised and displaced cards, reducing flicker.
- The original 0.14.1 expanded spread remains 1.95 units with its
  corrected curve and rotation, in original left-to-right card order.

## Technical boundaries
- A pure-C# HandFanGeometry.WithHoveredCard method calculates offsets
  for between 1 and 5 hand cards; the 3D renderer consumes its results.
- ArenaFactory.SetHandHover and SetHandFocus update per-card targets
  without rebuilding GameObjects or changing current hand indices.
- ArenaInteraction determines the cursor's current card; while left
  mouse is held, hover targeting is pinned to the pressed card so a
  moving neighbor cannot hijack a drag.
- CardView continues animating position, rotation and scale with
  the previous smoothing. Hand focus effects never affect field cards.
- Core duel rules, Guardian Stars, AI strategy, card set mechanics,
  five monster slots and five support slots remain unchanged.
- No proprietary PS1 disc data or artwork is added to public builds.

## Verification
The independent .NET 8 core smoke test checks geometry for all hand
sizes (1–5), focused indices, neighbor directions, missing focus,
compact mode and invalid indices. A source contract checker confirms
input wiring, drag stability and UI lift, without claiming that Unity
has compiled or displayed the scene.

To verify in Unity 6000.3.28f1:
1. Back up the previous project. Update files to 0.14.2.
2. Confirm the Console has no compilation errors.
3. Hover across all five cards; observe the center card lifting and
   immediate neighbors separating with an animated transition.
4. Move off the hand into the arena: the fan should compact and lower.
5. Select a card, Shift-click 2-3 more, then move across the hand.
   The 1–5 fusion numbers must not change order.
6. Drag a selected card to a monster/support slot and verify input
   remains attached to the pressed card throughout the animation.
7. Repeat with fewer than five cards and with the cursor outside the
   game window/HUD to check for jitter.

## Backup
GitHub branch: backup/prototype-0.14.2
GitHub Actions artifact: Reborn-0.14.2-Clean-Source
ChatGPT Library: Forbidden Memories Reborn/Backups/Prototype 0.14.2
