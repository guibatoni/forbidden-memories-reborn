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
        /// A failed fusion step replaces the intermediate result with the later
        /// hand card, exactly like the existing hand-only fusion algorithm.
        /// </summary>
        public CardDefinition PreviewFieldResult(DuelSide side,
            IReadOnlyList<int> handIndices, int monsterSlot)
        {
            return ResolveFieldSequence(side, handIndices, monsterSlot,
                out var result, out _, out _, out _) ? result : null;
        }

        private bool ResolveFieldSequence(DuelSide side,
            IReadOnlyList<int> indices, int monsterSlot,
            out CardDefinition result, out int survivingHandIndex,
            out int successfulFusions, out string reason)
        {
            result = null;
            survivingHandIndex = -1;
            successfulFusions = 0;
            if (monsterSlot < 0 || monsterSlot >= ZoneCount)
                return Reject(out reason, "Zona de monstro invalida");
            var state = Side(side);
            var onField = state.Monster(monsterSlot);
            if (onField == null)
                return Reject(out reason, "Nao ha monstro proprio neste espaco");
            if (indices == null || indices.Count == 0 || indices.Count > HandLimit)
                return Reject(out reason, "Selecione de 1 a 5 cartas da mao");
            var seen = new HashSet<int>();
            result = onField.Card; // The field card is always material #1.
            foreach (int index in indices)
            {
                if (index < 0 || index >= state.Hand.Count || !seen.Add(index))
                {
                    result = null;
                    return Reject(out reason, "Material repetido ou indice invalido");
                }
                var next = state.Hand[index];
                if (fusions.TryFuse(result, next, out var fused))
                {
                    result = fused;
                    survivingHandIndex = -1;
                    successfulFusions++;
                }
                else
                {
                    // A failed recipe discards the earlier intermediate result.
                    result = next;
                    survivingHandIndex = index;
                }
            }
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
                out result, out _, out _, out reason))
                return false;
            // An unsupported spell, equip, trap or ritual result cannot occupy
            // a monster zone: their original effects require validated data.
            if (result.Kind != CardKind.Monster)
                return Reject(out reason,
                    "Combinacao com suporte nao validada: efeito indisponivel");
            if (!GuardianStarRules.IsAvailable(result, star))
                return Reject(out reason, "Guardian Star invalida para o resultado");
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
                    var preview = PreviewFieldResult(side, new[] { hand }, slot);
                    if (preview == null || preview.Kind != CardKind.Monster) continue;
                    if (!CanPlayOnField(side, new[] { hand }, slot, preview.StarA,
                        out _, out _)) continue;
                    // Only facts from our own cards are considered.
                    int score = (recipe ? 100000 : 0) +
                        (preview.Attack - existing.Card.Attack) * 3 +
                        (preview.Defense - existing.Card.Defense);
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
            CardDefinition current = onField.Card;
            int survivor = -1;
            bool finalResultIsFusion = false;
            foreach (int index in handIndices)
            {
                var next = state.Hand[index];
                if (fusions.TryFuse(current, next, out var combined))
                {
                    current = combined;
                    survivor = -1;
                    fused = true;
                    finalResultIsFusion = true;
                    Emit(MatchEventKind.Fusion, side, current.Id,
                        FieldLane.Monster, monsterSlot);
                }
                else
                {
                    current = next;
                    survivor = index;
                    finalResultIsFusion = false;
                    Emit(MatchEventKind.FusionFailed, side, next.Id,
                        FieldLane.Monster, monsterSlot);
                }
            }

            var discarded = new List<CardDefinition>();
            foreach (int index in handIndices)
                if (index != survivor) discarded.Add(state.Hand[index]);
            // Preserve hand indexes until all consumed originals are captured.
            var ordered = new List<int>(handIndices);
            ordered.Sort((a, b) => b.CompareTo(a));
            foreach (int index in ordered) state.Take(index);

            state.Bury(onField.Card);
            Emit(MatchEventKind.Graveyard, side, onField.Card.Id);
            foreach (var material in discarded)
            {
                state.Bury(material);
                Emit(MatchEventKind.Graveyard, side, material.Id);
            }

            // An actual field fusion is face-up. In a failed combination where
            // the last hand monster survives, the requested face remains valid.
            state.SetMonster(monsterSlot,
                new MonsterOnField(current, position, star,
                    finalResultIsFusion ? false : faceDown, Turn));
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
