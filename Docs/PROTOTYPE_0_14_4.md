# Forbidden Memories Reborn — Prototype 0.14.4 (Attack Aim)

## Goal
Provide clear pointer-to-target feedback during the battle phase, while keeping
the approved fixed camera, the original rules engine, and Prototype 0.14.3
summon / hand animations unchanged.

## Behavior
- With a legal attacker selected, hovering a legal enemy monster shows a
  thin gold arcing line. A direct attack shows the arc toward the opponent
  when the enemy has no monsters.
- Hovering an empty enemy monster slot while the opponent has monsters,
  hovering HUD, or selecting an illegal target hides the arc.
- The arc disappears during AI turns, attack animations, match redraws,
  mode changes, or once attacker selection is cleared.
- The arc has no collider, does not alter LP or attack rules, and does not
  expose hidden cards.
- Battle preview uses the same CanAttack gate as the cursor feedback.

## Verification
Run `python3 tools/check_attack_aim.py`. GitHub CI runs this static check;
it does not compile/render the Unity scene.

In Unity 6000.3.28f1:
1. Confirm no compiler errors.
2. Place a monster, select it in battle, hover a valid enemy target:
   a subtle gold curved line should appear.
3. Hover an empty enemy slot while enemy monsters remain:
   the line must disappear.
4. With opponent monsters cleared, hover opponent area:
   the direct attack line should appear.
5. Move over the HUD, pass turn, complete an attack, reset the duel,
   and toggle Sandbox: the line must disappear.
6. Repeat with a face-down defender. No hidden name or statistics may appear.
7. Confirm camera, Shift-fusion hand and 0.14.3 summon arc are unchanged.

## Safety and scope
This change does not alter core rules, AI, original-disc import, card effects,
assets, or camera. Never include private disc images, extracted card artwork
or user-specific AI files in public packages.
