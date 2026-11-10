# Forbidden Memories Reborn — Prototype 0.14.1: Responsive hand

## Based on local Unity visual feedback
The existing 0.14 hand had an inverted fan and crowded cards. This patch:
- Reverses the fan's Y rotation and depth curvature while keeping card
  index 0 on the left and increasing indices to the right.
- Makes the fan open when the cursor enters the hand area, including
  the enlarged/raised cards, and compact when attention returns to the arena.
- Expanded center-to-center spacing is **1.95 tabletop units** — an
  increase of **0.90** over the old 1.05 setting. These are Unity scene
  units (not real-world millimeters).
- Compact center spacing is 1.15 units, with Y=0.44 and Z=-7.52.
  Expanded mode uses Y=0.58 and Z=-7.28.
- The outer cards now curve toward the player instead of the battlefield;
  the card face itself remains upright and readable.

## Animation and interaction
- HandFanGeometry is a Unity-independent, tested calculation of per-card
  X/Y/Z and yaw, for up to five cards.
- ArenaFactory.SetHandFocus updates target poses; it never clears or
  recreates the hand when the mouse crosses between zones.
- CardView.Update retains its existing Lerp and Slerp timing and
  its hover/selection lift. It does not override a dragged card's position.
- ArenaInteraction expands the hand when the pointer targets a hand
  card or the combined projected compact/expanded hand region. The
  two-pose region prevents flicker when animated cards leave the cursor.
- HUD hover and opponent input-lock suppress hand expansion.
- Fusion selections, Shift-click numbering, original card IDs, gameplay
  controls and the fixed camera remain unchanged.

## How to verify in Unity 6000.3.28f1
1. Back up the 0.14 project and update files from the clean source.
2. Open the Unity Editor and confirm zero compiler errors.
3. Move the mouse onto an empty arena slot: the hand should settle
   lower, with tighter overlapping cards.
4. Move the mouse onto a hand card: the hand should expand smoothly.
   Check the reversed curve, increased spacing and that left-to-right
   numbers remain stable across opening and closing.
5. Select a card with a click; Shift-click several more, checking
   sequence badges and that the fan does not reset or flicker.
6. Drag a card from the hand to a zone, releasing normally. The
   dragged card must not jump or get swallowed by the fan animation.
7. Repeat in both Sandbox and Duel modes at your normal Game view size.

GitHub Actions checks pure C# fan geometry assertions and static Unity
integration contracts. It does not render the scene or verify the
cursor/drag feeling in the Unity Editor; these require local testing.

## Backup
- GitHub recovery branch: backup/prototype-0.14.1
- CI artifact: Reborn-0.14.1-Clean-Source
- ChatGPT Library: Forbidden Memories Reborn/Backups/Prototype 0.14.1
- No ISO, licensed card artwork or adaptive AI save data are included.
