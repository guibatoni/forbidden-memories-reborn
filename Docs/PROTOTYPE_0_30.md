# Prototype 0.30 — Original fusion chain rules (development)

Base: `backup/prototype-0.29-unity-gate-verified-core` (6d76a21).

## Root cause of "fusions do not work correctly"

The recipe table, its ID mapping and the field-first order were already
correct. The defect was in **how an ordered chain resolves each pair**. The
engine used a single rule — "no recipe: drop the earlier card, keep the later
one" — and had no notion of Equip cards inside a chain.

The PS1 placement routine (decompiled `duel_scene_card_placement.c`, states
3/5/7 of `DuelScene_UpdateCardPlacement`, in the open Recompiled project used
by `tools/import_fm_disc.py`) resolves each `(current, next)` pair as:

1. `Duel_CheckFusion(current, next)` — verified recipe: fuse.
2. else `Duel_CheckEquip(next, current)` — `next` is an Equip compatible with
   the current monster: the Equip is consumed, the monster continues.
3. else `Duel_CheckEquip(current, next)` — `current` is an Equip compatible
   with the next monster: the Equip is consumed, that monster continues.
4. else failure (state 7): if the current card is a monster and the next is
   not, the **non-monster is thrown away and the monster continues**;
   otherwise the current card is thrown away.

Field chains put the existing own monster first (confirmed in
`duel_scene_hand_actions.c`, which places the occupied field card in slot 0
before the hand cards in pick order) — unchanged from 0.16.

Visible symptoms that are fixed:

| Selection | Before 0.30 | Now (PS1) |
|---|---|---|
| Monster + unmatched Spell/Trap | Spell result, monster lost / play rejected | Monster summoned, Spell to graveyard |
| Monster + compatible Equip (hand chain) | Equip result, monster lost | Monster summoned with Equip bonus |
| Fusion + Equip (3 materials) | Equip result | Fusion monster with Equip bonus |
| Field monster + unmatched Spell | Rejected | Field monster stays, Spell discarded |
| Field monster + compatible Equip in chain | Rejected | Field monster equipped, stays in place |

Equip compatibility and bonuses come only from `VerifiedEquipCatalog`
(imported, SHA-matched `equip_rules.json` or the two universal retail Equips).
Unverified Equips are discarded like any other unmatched non-monster; no
effect is ever invented.

## Technical changes

- `Core/FusionChainResolver.cs` — new pure resolver (no Unity, no state).
- `Core/DuelMatch.FusionChain.cs` — `PreviewHandChain`, `PreviewFieldChain`,
  event emission and Equip application shared by all paths.
- `PreviewResult`, `CanPlay`, `TryPlay`, `ResolveFieldSequence`,
  `CanPlayOnField`, `TryPlayOnField`, `CountHandFusionSteps`,
  `CountFieldFusionSteps`, `FinalFieldResultIsFusion` now use the resolver:
  the rules exist once.
- AI field fallback never throws a support card away on purpose and scores
  in-chain Equip gains.
- UI: field announcements distinguish fusion / equip / discard / replacement;
  keyboard auto-placement accepts Equip-only chains.

## Validation status

| Layer | Status |
|---|---|
| Static source contracts (`tools/check_*.py`) | Run locally: all pass |
| Python importer tests (`tools/test_*.py`) | Run locally: all pass |
| .NET Core smoke (`tools/Reborn.Core.Smoke`) | **Not run** here (no .NET SDK in the session). Runs in CI. |
| Unity Editor compilation | **Not run** — run `tools/TESTAR_UNITY_REBORN.ps1` |
| Unity EditMode (`FusionChainRulesTests`, 13 tests) | **Not run** — same script |
| PlayMode / visual check | **Not done** |

Open question kept conservative: whether the PS1 lets a chain Equip a
face-down field monster. The chain does not block it (the original routine has
no face check there); the dedicated single-Equip action still requires a
revealed target, as before.
