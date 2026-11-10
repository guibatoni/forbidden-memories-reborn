# Forbidden Memories Reborn — Prototype 0.14: Fluid Duel & Fanned Hand

## Player goal
Make the tabletop flow closer to Forbidden Memories for PS1, while using
larger, overlapping five-card hand presentation inspired by MTG Arena.
The arena camera stays fixed and the field retains five monster slots
and five support slots per side.

## Hand interaction
- Each card in hand is enlarged to a 1.90 x 2.55 tabletop dimension and
  arranged in an overlapping, gently rotated fan.
- Hover or selection raises and scales the card.
- Regular click selects a card; Shift+click adds materials to the current
  fusion sequence in the EXACT order selected. The card gets a small
  number in the upper-right corner, from 1 through 5.
- Fusion resolution uses the existing sequential core algorithm; there
  is no fabricated outcome or extra fusion rule.
- The right panel presents **face up/down first**, then
  **ATK/DEF position**, then the card's Guardian Star, then click
  or drag to the destination zone.

## Simplified battle flow
- Only one placement from the hand per turn is allowed, including ordered
  materials consumed in one fusion action.
- After a successful hand placement, battle starts automatically.
  No separate "Start Battle" or "Select Attack" button is needed.
- During battle, click your eligible attacker, then the opponent's monster
  to attack. If the opponent's monster row is empty, click any opponent
  field zone or valid opposing arena region for a direct attack.
- When no monster can attack or legally change position, the player turn
  can pass automatically. Otherwise a simple **Pass Turn** control remains.
- Newly set monsters cannot change position during their own placement turn.
  Existing monsters may switch position once per turn if legal.
  After a monster attacked, it cannot switch into defense in that turn.
- The AI now may set defense face-down and, on a later turn, legally flip
  a previous defense into attack. It performs these changes sequentially.

## Face-down security
- A hidden defender's card art, name and forecast statistics are not
  revealed in the HUD before impact. An actual attack reveals it.
- The AI does not use concealed player monster ATK/DEF for its attack
  selection or immediate placement evaluation. It performs a blind
  challenge when a hidden defender is present.
- The card remains face-down during combat preview and only flips when
  DuelMatch actually resolves the attack.

## Magics, traps, equipment, rituals
- Face-down support cards are kept on the table, with a recorded set turn.
- Clicking an already set support in battle selects its activation option.
- Freshly set supports cannot be activated in the same turn by this
  prototype. This is a prototype timing choice pending authentic rule
  validation and is not claimed to reproduce every original edge case.
- **Actual card effects are not yet implemented**. The button safely
  explains "efeito nao validado" rather than making up an ATK bonus,
  trap trigger, ritual result or equip compatibility.
- Once verified effect data is extracted, the same flow can activate
  equipment during battle and resolve other supported cards. Never
  publish private ISO content or copied card assets to public GitHub.

## Verification
- Core rules smoke tests cover face-down placements, flips, support
  set timing, anti-double-placement, post-attack position lock, true
  combat reveal and AI defense-to-attack behavior.
- Static source contracts check fan UI, Shift sequence badges, click
  targets, and camera/board continuity.
- GitHub Actions runs the C# core, cross-assembly access smoke test and
  the offline AI Lab. Static checks do NOT compile the Unity presentation.
- Open Unity 6000.3.28f1, verify no compile errors, test mouse raycasting
  between overlapping hand cards, then click/drag and play multiple turns.

## Backup
GitHub branch: backup/prototype-0.14
ChatGPT Library: Forbidden Memories Reborn/Backups/Prototype 0.14
Source archives do not include disc images, extracted copyrighted art,
private deck data or personal adaptive AI profiles.
