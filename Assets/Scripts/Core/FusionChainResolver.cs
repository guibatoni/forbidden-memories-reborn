using System;
using System.Collections.Generic;

namespace Reborn.Core
{
    /// <summary>What happened when the next material met the current card.</summary>
    public enum FusionStepKind
    {
        /// <summary>A verified recipe replaced both cards with its result.</summary>
        Fused,
        /// <summary>A verified Equip was consumed; the monster keeps going.</summary>
        Equipped,
        /// <summary>No recipe: the current card was discarded, the next continues.</summary>
        DiscardedCurrent,
        /// <summary>No recipe: a non-monster next card was discarded, the monster continues.</summary>
        DiscardedNext
    }

    /// <summary>One resolved pair. MaterialPosition is the incoming material's
    /// position in the sequence (1..n-1); CardId is the card produced (Fused),
    /// the Equip consumed (Equipped) or the card thrown away (Discarded*).</summary>
    public sealed class FusionStep
    {
        public FusionStepKind Kind { get; }
        public int MaterialPosition { get; }
        public string CardId { get; }
        public int AttackGain { get; }
        public int DefenseGain { get; }

        internal FusionStep(FusionStepKind kind, int position, string cardId,
            int attackGain = 0, int defenseGain = 0)
        {
            Kind = kind;
            MaterialPosition = position;
            CardId = cardId ?? "";
            AttackGain = attackGain;
            DefenseGain = defenseGain;
        }
    }

    /// <summary>Immutable outcome of an ordered material chain.</summary>
    public sealed class FusionChainResult
    {
        public CardDefinition Result { get; }
        /// <summary>Position (in the material sequence) of the physical card that
        /// ends the chain, or -1 when the result is a virtual fusion product.</summary>
        public int SurvivorPosition { get; }
        public IReadOnlyList<FusionStep> Steps { get; }
        public int SuccessfulFusions { get; }
        public int EquipsApplied { get; }
        /// <summary>True when at least one pair fused or equipped, i.e. the
        /// chain is not merely a sequence of discards.</summary>
        public bool HasProductiveStep => SuccessfulFusions > 0 || EquipsApplied > 0;
        public bool FinalIsFusion => SurvivorPosition < 0;
        /// <summary>Equip gains still attached to Result, in application order.
        /// Gains applied to a card that was later fused or discarded are gone.</summary>
        public IReadOnlyList<FusionStep> PendingEquips { get; }
        public int PendingAttackGain { get; }
        public int PendingDefenseGain { get; }

        internal FusionChainResult(CardDefinition result, int survivor,
            List<FusionStep> steps, int fusions, List<FusionStep> pending)
        {
            Result = result;
            SurvivorPosition = survivor;
            Steps = steps.AsReadOnly();
            SuccessfulFusions = fusions;
            PendingEquips = pending.AsReadOnly();
            foreach (var step in steps)
                if (step.Kind == FusionStepKind.Equipped) EquipsApplied++;
            foreach (var equip in pending)
            {
                PendingAttackGain += equip.AttackGain;
                PendingDefenseGain += equip.DefenseGain;
            }
        }
    }

    /// <summary>
    /// The single rules implementation for ordered fusion material chains
    /// (hand-only and field-first). It follows the PS1 placement routine
    /// (Duel scene card placement, state 3/5/7 in the decompiled source):
    ///   1. a verified fusion recipe for (current, next) fuses;
    ///   2. otherwise, if next is an Equip compatible with the current
    ///      monster, the Equip is consumed and the monster continues;
    ///   3. otherwise, if current is an Equip compatible with the next
    ///      monster, the Equip is consumed and that monster continues;
    ///   4. otherwise the pair fails: when the current card is a monster and
    ///      the next is not, the next card is discarded and the monster
    ///      continues; in every other case the current card is discarded.
    /// Pure and read-only: no Unity, no duel state, no randomness.
    /// </summary>
    public static class FusionChainResolver
    {
        public static FusionChainResult Resolve(IReadOnlyList<CardDefinition> materials,
            FusionCatalog fusions, VerifiedEquipCatalog equips)
        {
            if (materials == null || materials.Count == 0)
                throw new ArgumentException("Missing fusion materials", nameof(materials));
            foreach (var material in materials)
                if (material == null)
                    throw new ArgumentException("Null fusion material", nameof(materials));

            var steps = new List<FusionStep>();
            var pending = new List<FusionStep>();
            CardDefinition current = materials[0];
            int survivor = 0;
            int fused = 0;

            for (int position = 1; position < materials.Count; position++)
            {
                var next = materials[position];
                if (fusions != null && fusions.TryFuse(current, next, out var product))
                {
                    current = product;
                    survivor = -1;
                    fused++;
                    pending.Clear(); // A new card: earlier equip gains are lost.
                    steps.Add(new FusionStep(FusionStepKind.Fused, position, product.Id));
                    continue;
                }

                if (equips != null && current.Kind == CardKind.Monster &&
                    equips.CanApply(next, current, out var onCurrent))
                {
                    var step = new FusionStep(FusionStepKind.Equipped, position,
                        next.Id, onCurrent.AttackGain, onCurrent.DefenseGain);
                    steps.Add(step);
                    pending.Add(step);
                    continue;
                }

                if (equips != null && next.Kind == CardKind.Monster &&
                    equips.CanApply(current, next, out var onNext))
                {
                    var step = new FusionStep(FusionStepKind.Equipped, position,
                        current.Id, onNext.AttackGain, onNext.DefenseGain);
                    current = next;
                    survivor = position;
                    pending.Clear();
                    steps.Add(step);
                    pending.Add(step);
                    continue;
                }

                if (current.Kind == CardKind.Monster && next.Kind != CardKind.Monster)
                {
                    steps.Add(new FusionStep(FusionStepKind.DiscardedNext, position, next.Id));
                    continue;
                }

                steps.Add(new FusionStep(FusionStepKind.DiscardedCurrent, position, current.Id));
                current = next;
                survivor = position;
                pending.Clear();
            }
            return new FusionChainResult(current, survivor, steps, fused, pending);
        }
    }
}
