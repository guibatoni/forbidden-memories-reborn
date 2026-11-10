using System;
using System.Collections.Generic;

namespace Reborn.Core
{
    public sealed partial class DuelMatch
    {
        private bool FindRitualTributes(DuelSide side, RitualRecipe recipe,
            out int[] slots)
        {
            slots = new int[3];
            var taken = new HashSet<int>();
            var owner = Side(side);
            for (int needed = 0; needed < 3; needed++)
            {
                int matchSlot = -1;
                for (int slot = 0; slot < ZoneCount; slot++)
                {
                    var monster = owner.Monster(slot);
                    if (taken.Contains(slot) || monster == null || monster.FaceDown ||
                        monster.Card.Id != recipe.TributeIds[needed]) continue;
                    matchSlot = slot;
                    break;
                }
                if (matchSlot < 0) return false;
                slots[needed] = matchSlot;
                taken.Add(matchSlot);
            }
            return true;
        }

        /// <summary>
        /// The PS1 requires all three named tributes already face up on the
        /// player's own board. The ritual result is generated, NOT played from
        /// the hand or selected through a level-sum tribute calculation.
        /// The selected result slot must be one of the three tribute slots.
        /// Read-only: no turn, hand, LP or event changes.
        /// </summary>
        public bool CanSummonRitual(DuelSide side, int handIndex, int resultSlot,
            GuardianStar star, out CardDefinition result, out string reason)
        {
            result = null;
            if (!Active(side, MatchPhase.Main, out reason)) return false;
            if (hasPlayed)
                return Reject(out reason, "Uma carta ja foi baixada neste turno");
            var owner = Side(side);
            if (handIndex < 0 || handIndex >= owner.Hand.Count)
                return Reject(out reason, "Carta ritual da mao invalida");
            var ritualCard = owner.Hand[handIndex];
            if (ritualCard.Kind != CardKind.Ritual ||
                !rituals.TryGet(ritualCard.Id, out var recipe))
                return Reject(out reason, "Ritual sem receita extraida e validada");
            if (!FindRitualTributes(side, recipe, out var slots))
                return Reject(out reason,
                    "Ritual requer tres monstros especificos virados para cima no campo");
            bool isSacrificeZone = false;
            foreach (int slot in slots) if (slot == resultSlot) isSacrificeZone = true;
            if (!isSacrificeZone)
                return Reject(out reason,
                    "Escolha uma zona ocupada por um dos tres tributos");
            if (!GuardianStarRules.IsAvailable(recipe.Result, star))
                return Reject(out reason, "Guardian Star invalida para o ritual");
            result = recipe.Result;
            reason = "";
            return true;
        }

        public bool TrySummonRitual(DuelSide side, int handIndex, int resultSlot,
            BattlePosition position, GuardianStar star,
            out CardDefinition result, out string reason)
        {
            if (!CanSummonRitual(side, handIndex, resultSlot, star,
                out result, out reason)) return false;

            var state = Side(side);
            var recipe = rituals.TryGet(state.Hand[handIndex].Id, out var entry)
                ? entry : null;
            if (recipe == null || !FindRitualTributes(side, recipe, out var slots))
                return Reject(out reason, "Tributos mudaram antes do ritual");

            // All checks completed. Only physical ritual card and physical
            // tribute monsters go to the graveyard, in deterministic order.
            var used = state.Take(handIndex);
            state.Bury(used);
            Emit(MatchEventKind.Graveyard, side, used.Id, FieldLane.Support);
            foreach (int slot in slots)
            {
                var original = state.Monster(slot);
                state.Bury(original.Card);
                state.SetMonster(slot, null);
                Emit(MatchEventKind.Graveyard, side, original.Card.Id,
                    FieldLane.Monster, slot);
            }
            state.SetMonster(resultSlot, new MonsterOnField(result, position,
                star, false, Turn));
            hasPlayed = true;
            Emit(MatchEventKind.RitualSummoned, side, result.Id,
                FieldLane.Monster, resultSlot);
            Emit(MatchEventKind.Play, side, result.Id,
                FieldLane.Monster, resultSlot);
            SignalChanged();
            reason = "";
            return true;
        }

        /// <summary>Hover preview helper without changing any game state.</summary>
        public CardDefinition PreviewRitual(DuelSide side, int handIndex)
        {
            var owner = Side(side);
            if (handIndex < 0 || handIndex >= owner.Hand.Count ||
                owner.Hand[handIndex].Kind != CardKind.Ritual)
                return null;
            if (!rituals.TryGet(owner.Hand[handIndex].Id, out var recipe) ||
                !FindRitualTributes(side, recipe, out _))
                return null;
            return recipe.Result;
        }
    }
}
