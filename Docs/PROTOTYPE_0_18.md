# Prototype 0.18 — Hand entry, fusion-choice sequence, four starter options

## Flow implemented
- Draw up to five: incoming cards enter from the deck in a cascading,
  independently interpolated motion; the approved inverted fan, spacing,
  hover and hand focus remain unchanged.
- Select one card or Shift-select 2–5 cards for fusion in exact numbered order.
  The player may point to an empty legal zone or their own occupied monster.
- Clicking or dragging to a legal zone triggers a full-size choice wizard.
  **The action is not committed yet**.
- Wizard: choose Guardian Star (only when two verified distinct stars are
  present), then face-up/face-down, then the duel engine commits exactly one
  legal placement/fusion/ritual.
- After the card lands, a full-size ATK/DEF screen appears for monsters.
  Existing Q/E toggling is still allowed repeatedly until an attack.
- ESC before committing cancels without discarding a card. ESC on the
  final ATK/DEF screen closes it without undoing an already committed play.
- Spell/trap/equip placements do not prompt for monster position; a ritual
  cannot be facedown on activation.
- All rule validation still passes through CanPlay/CanPlayOnField/
  CanSummonRitual and actual Try* commands. No HUD preview executes effects.

## Four initial deck candidates
Yu-Gi-Oh! Forbidden Memories retail does **not** have four fixed starter
deck types. It deals a single randomized 40-card deck from seven weighted
pools with draw counts 16/16/4/1/1/1/1.

Reborn adds a deliberate quality-of-life option: four independent proposals,
each dealt from the **same real imported seven pools**. The player selects
one; its 40 physical cards remain constant across duel restarts rather than
randomizing a brand-new roster on every new duel.

The seven distributions are:
1. Monster ATK+DEF below 1100: 16
2. Monster ATK+DEF 1100..1599: 16
3. Monster ATK+DEF 1600..2099: 4
4. Monster ATK+DEF 2100+: 1
5. Pure Magic: 1
6. Field Magic: 1
7. Equip: 1

The weights are extracted from the user's own ISO via
`tools/IMPORTAR_ORIGINAL.ps1`, now outputting
`Assets/StreamingAssets/RebornContent/starter_pools.json`.
Both the 722-card catalog and starter rows must match the same SHA-256.
No approximate deck is labeled original. When weights are missing (a
DUELTRACE-only card profile does not carry them), the modal explains how
to import the ISO and offers an explicitly labeled **training** fallback.

The weighted draws enforce a limit of three copies and shuffle each
40-card option. This does **not** reproduce the exact PS1 MIPS random
number stream or guarantee matching decks for the same original seed.

## Verification
CI:
- dotnet core smoke builds/executes and checks six categories, draw counts,
  40-card compositions, four distinct proposals and invalid weights
- original importer contract and seven-pool export source static check
- prior core, fusion, ritual, HUD and AI regressions

Unity 6000.3.28f1 — to verify in a real Play session:
1. Start with a correct imported Original catalog AND seven starter pools.
   Confirm four 40-card choices appear before any battle interaction.
2. Pick deck 2; restart a duel and verify the same 40 card IDs are retained.
3. Draw into five-card hand. Confirm new cards glide sequentially without
   flipping/inverting or changing the proven hand fan.
4. Shift-select 2–5 cards, pick a target, verify Guardian choices,
   facing and resulting card all agree.
5. Cancel during Guardian/Face modal: no card spent, no battle transition.
6. Confirm face-up/down: exactly one card play and one fusion result.
7. Choose ATK/DEF after placing a monster, then Q/E repeatedly, attack,
   ensure defender position cannot be changed by Q/E after attacking.
8. Test support cards and rituals: no irrelevant monster position screen;
   ritual never sets face down.
9. Without `starter_pools.json`, confirm the UI **does not** silently
   label an arbitrary practice deck a retail PS1 initial deck.
10. Test 1920x1080 and smaller Game View resolutions.

The provided gameplay video could not be automatically frame-inspected in
the current environment. These changes were based on the existing code,
user-specified behavior and verified PS1 starting-deck data; visual review
on the target machine remains essential.
