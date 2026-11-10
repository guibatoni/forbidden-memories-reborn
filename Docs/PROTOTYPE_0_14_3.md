# Forbidden Memories Reborn — Prototype 0.14.3

## Field Interaction & Summon Motion
This update improves the existing fixed-camera tabletop UX without
changing card rules, learning profiles, deck composition or duel outcomes.

### Field feedback
- Each zone displays an available status ONLY when the active selected
  card/attack can legally use it, based on DuelMatch.CanPlay or
  DuelMatch.CanAttack.
- A legal zone under the pointer uses a brighter border and thicker
  trim. Hovering an illegal target does not suggest a valid play.
- The selected monster attacker uses a warm gold outline; enemy attack
  targets use warmer red/orange cues. Player placement zones use cyan.
- The prior 0.14.2 hand focus, Shift+click fusion numbering and drag
  selection remain intact.
- Enemy zones are highlighted only for legal attacks. If the enemy
  monster row is empty, a direct attack may target the opponent area.
- Monster already committed to attacking / in defense is not shown as
  available attack source.

### Card placement motion
- Invocations move from their source toward their slot along a curved
  path, starting gently tilted, easing toward the destination and
  settling with a small controlled bounce.
- Animation ends at the EXACT field position, rotation and scale.
- The duel engine accepts/refuses a play independently; animation is
  purely visual and cannot modify LP, Guardian Star effects, attack
  counters or zone occupancy.
- The camera remains fixed; no zooms or camera travel are added.

### Known limitations
- There is no Unity Editor rendering in GitHub CI. Mouse hitboxes,
  Game view resolution and visual feel require local testing.
- This milestone does NOT add magic, trap, equip or ritual effects;
  unsupported effects remain unimplemented, avoiding invented rules.
- All file assets from PS1 discs and personal learning profiles stay
  outside the public repository.

### Unity verification (6000.3.28f1)
1. Preserve a copy of your existing project and apply this 0.14.3 update.
2. Confirm that the Unity Console has no red compiler errors.
3. Select a monster in the hand, hover over valid/invalid monster zones
   and confirm only available zones brighten.
4. Repeat for support cards and numbered Shift+click fusion materials.
5. Place a card by clicking or dragging, and watch the arcing summon
   end aligned with the field slot and correct face/position.
6. In battle, select an available attacker. Hover a real opposing
   monster: its border brightens. Hover an illegal/empty monster slot:
   no false valid-target indication should appear.
7. If opponent has no monsters, verify direct attack targeting.
8. Use a previously attacked monster to confirm the attack source
   no longer appears active.

### Backup
- Recovery branch: backup/prototype-0.14.3
- GitHub Actions clean source: Reborn-0.14.3-Clean-Source
- Library: Forbidden Memories Reborn/Backups/Prototype 0.14.3
