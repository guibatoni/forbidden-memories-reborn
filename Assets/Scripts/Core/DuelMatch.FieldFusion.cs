using System;
using System.Collections.Generic;

namespace Reborn.Core
{
    /// <summary>
    /// Ordered on-field combinations: existing monster first, then up to five
    /// selected hand cards. Valid recipes only; no invented Equip/Spell effects.
    /// This is the one mandatory hand placement of the turn.
    /// </summary>
    public sealed partial class DuelMatch
    {
        /// <summary>
        /// Preview does not change any card, event, LP, graveyard or turn state.
        /// Uses the shared PS1 chain rules (FusionChainResolver): a failed pair
        /// discards the earlier card, except that a monster survives a failed
        /// pair with a non-monster, and verified Equips are applied in-chain.
        /// </summary>
        public CardDefinition PreviewFieldResult(DuelSide side,
            IReadOnlyList<int> handIndices, int monsterSlot)
        {
            return ResolveFieldSequence(side, handIndices, monsterSlot,
                out var chain, out _) ? chain.Result : null;
        }

        private bool ResolveFieldSequence(DuelSide side,
            IReadOnlyList<int> indices, int monsterSlot,
            out FusionChainResult chain, out string reason)
        {
            chain = null;
            if (monsterSlot < 0 || monsterSlot >= ZoneCount)
                return Reject(out reason, "Zona de monstro invalida");
            var state = Side(side);
            var onField = state.Monster(monsterSlot);
            if (onField == null)
                return Reject(out reason, "Nao ha monstro proprio neste espaco");
            if (indices == null || indices.Count == 0 || indices.Count > HandLimit)
                return Reject(out reason, "Selecione de 1 a 5 cartas da mao");
            // The field card is always material #1 (PS1 placement order).
            chain = PreviewFieldChain(side, indices, monsterSlot);
            if (chain == null)
                return Reject(out reason, "Material repetido ou indice invalido");
            reason = "";
            return true;
        }

        public bool CanPlayOnField(DuelSide side, IReadOnlyList<int> handIndices,
            int monsterSlot, GuardianStar star,
            out CardDefinition result, out string reason)
        {
            result = null;
            if (!Active(side, MatchPhase.Main, out reason)) return false;
            if (hasPlayed)
                return Reject(out reason, "Jogada de mao ja utilizada neste turno");
            if (!ResolveFieldSequence(side, handIndices, monsterSlot,
                out var chain, out reason))
                return false;
            result = chain.Result;
            // An unsupported spell, equip, trap or ritual result cannot occupy
            // a monster zone: their original effects require validated data.
            if (result.Kind != CardKind.Monster)
                return Reject(out reason,
                    "Combinacao com suporte nao validada: efeito indisponivel");
            // A field monster that survives keeps its own Guardian Star.
            if (chain.SurvivorPosition != 0 &&
                !GuardianStarRules.IsAvailable(result, star))
                return Reject(out reason, "Guardian Star invalida para o resultado");
            var existing = chain.SurvivorPosition == 0 ?
                Side(side).Monster(monsterSlot) : null;
            if (!PendingEquipsFit(existing, result, chain))
                return Reject(out reason, "Bonus de equipamento fora do limite de atributos");
            reason = "";
            return true;
        }

        /// <summary>
        /// Read-only one-card fallback for AI. Prefer a verified field recipe;
        /// a replacement is allowed only as a last legal mandatory play.
        /// </summary>
        public bool TryChooseFieldCombination(DuelSide side, bool requireRecipe,
            out int handIndex, out int monsterSlot, out CardDefinition result)
        {
            handIndex = -1;
            monsterSlot = -1;
            result = null;
            if (Phase != MatchPhase.Main || CurrentSide != side || hasPlayed)
                return false;
            var owner = Side(side);
            int bestScore = int.MinValue;
            for (int slot = 0; slot < ZoneCount; slot++)
            {
                var existing = owner.Monster(slot);
                if (existing == null) continue;
                for (int hand = 0; hand < owner.Hand.Count; hand++)
                {
                    var material = owner.Hand[hand];
                    bool recipe = fusions.TryFuse(existing.Card, material, out _);
                    if (requireRecipe && !recipe) continue;
                    var chain = PreviewFieldChain(side, new[] { hand }, slot);
                    var preview = chain?.Result;
                    if (preview == null || preview.Kind != CardKind.Monster) continue;
                    // Throwing a non-monster away is legal but never a useful
                    // AI choice; a verified in-chain Equip is.
                    if (chain.Steps.Count == 1 &&
                        chain.Steps[0].Kind == FusionStepKind.DiscardedNext) continue;
                    if (!CanPlayOnField(side, new[] { hand }, slot, preview.StarA,
                        out _, out _)) continue;
                    // Only facts from our own cards are considered.
                    int score = (recipe ? 100000 : 0) +
                        (preview.Attack + chain.PendingAttackGain -
                            existing.Card.Attack) * 3 +
                        (preview.Defense + chain.PendingDefenseGain -
                            existing.Card.Defense);
                    if (score <= bestScore) continue;
                    bestScore = score;
                    handIndex = hand;
                    monsterSlot = slot;
                    result = preview;
                }
            }
            return handIndex >= 0;
        }

        /// <summary>
        /// Executes a validated on-field combination. All original physical
        /// losers enter the graveyard; virtual intermediate fusion products do
        /// not. No mutation occurs when the preview rejects a command.
        /// </summary>
        public bool TryPlayOnField(DuelSide side, IReadOnlyList<int> handIndices,
            int monsterSlot, BattlePosition position, GuardianStar star, bool faceDown,
            out CardDefinition result, out bool fused, out string reason)
        {
            fused = false;
            if (!CanPlayOnField(side, handIndices, monsterSlot, star,
                out result, out reason)) return false;
            var state = Side(side);
            var onField = state.Monster(monsterSlot);
            var chain = PreviewFieldChain(side, handIndices, monsterSlot);
            var current = chain.Result;
            bool fieldSurvives = chain.SurvivorPosition == 0;
            int survivor = HandIndexAt(handIndices, chain.SurvivorPosition, 1);
            EmitChainSteps(side, chain, FieldLane.Monster, monsterSlot);

            // Report a "fusion" to the UI only when the *final* on-field
            // result is a fusion. Earlier successful intermediate steps still
            // have their own Fusion events in the log.
            bool finalResultIsFusion = chain.FinalIsFusion;
            fused = finalResultIsFusion;

            var discarded = new List<CardDefinition>();
            foreach (int index in handIndices)
                if (index != survivor) discarded.Add(state.Hand[index]);
            // Preserve hand indexes until all consumed originals are captured.
            var ordered = new List<int>(handIndices);
            ordered.Sort((a, b) => b.CompareTo(a));
            foreach (int index in ordered) state.Take(index);

            if (!fieldSurvives)
            {
                state.Bury(onField.Card);
                Emit(MatchEventKind.Graveyard, side, onField.Card.Id);
            }
            foreach (var material in discarded)
            {
                state.Bury(material);
                Emit(MatchEventKind.Graveyard, side, material.Id);
            }

            if (fieldSurvives)
            {
                // Only Equips were consumed or non-monsters thrown away: the
                // same monster stays, keeping position, star, face and bonuses.
                ApplyPendingEquips(onField, chain);
            }
            else
            {
                // An actual field fusion is face-up. In a failed combination where
                // the last hand monster survives, the requested face remains valid.
                var placed = new MonsterOnField(current, position, star,
                    finalResultIsFusion ? false : faceDown, Turn);
                ApplyPendingEquips(placed, chain);
                state.SetMonster(monsterSlot, placed);
            }
            hasPlayed = true;
            result = current;
            Emit(MatchEventKind.Play, side, current.Id,
                FieldLane.Monster, monsterSlot);
            SignalChanged();
            reason = "";
            return true;
        }
    }
}
