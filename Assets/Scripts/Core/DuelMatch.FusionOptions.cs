using System;
using System.Collections.Generic;

namespace Reborn.Core
{
    /// <summary>
    /// An immutable, presently legal chain made exclusively of verified
    /// recipes. A field chain starts with the own monster in FieldSlot,
    /// followed by HandIndices in their exact execution order.
    /// </summary>
    public sealed class AvailableFusion
    {
        public IReadOnlyList<int> HandIndices { get; }
        public int FirstHandIndex => HandIndices[0];
        // Compatibility with pair-only callers: field combinations have no
        // second hand index, even when more materials are involved.
        public int SecondHandIndex => UsesField || HandIndices.Count < 2
            ? -1 : HandIndices[1];
        public int FieldSlot { get; }
        public CardDefinition Result { get; }
        public bool UsesField => FieldSlot >= 0;
        public int MaterialCount => HandIndices.Count + (UsesField ? 1 : 0);
        public int FusionSteps => MaterialCount - 1;

        internal AvailableFusion(IReadOnlyList<int> indices, int fieldSlot,
            CardDefinition result)
        {
            if (indices == null || indices.Count == 0)
                throw new ArgumentException("Missing fusion materials", nameof(indices));
            var copy = new int[indices.Count];
            for (int i = 0; i < indices.Count; ++i) copy[i] = indices[i];
            HandIndices = Array.AsReadOnly(copy);
            FieldSlot = fieldSlot;
            Result = result ?? throw new ArgumentNullException(nameof(result));
        }
    }

    public sealed partial class DuelMatch
    {
        /// <summary>
        /// Enumerate strictly verified hand chains of 2-5 materials and
        /// field chains (own monster followed by 1-5 hand materials).
        /// Every intermediate step must fuse successfully. Failed steps
        /// remain possible manually, but never appear as a suggestion.
        /// Search is bounded by the five-card hand and is read-only.
        /// </summary>
        public IReadOnlyList<AvailableFusion> FindAvailableFusions(DuelSide side)
        {
            var options = new List<AvailableFusion>();
            if (Phase != MatchPhase.Main || CurrentSide != side ||
                hasPlayed || fusions.Count == 0) return options;

            var owner = Side(side);
            int freeSlot = -1;
            for (int slot = 0; slot < ZoneCount; ++slot)
                if (owner.Monster(slot) == null)
                {
                    freeSlot = slot;
                    break;
                }

            // First pair is canonical: A+B and B+A produce the same
            // verified result. All later steps preserve their own order.
            if (freeSlot >= 0)
                for (int a = 0; a < owner.Hand.Count; ++a)
                    for (int b = a + 1; b < owner.Hand.Count; ++b)
                    {
                        if (!fusions.TryFuse(owner.Hand[a], owner.Hand[b],
                            out var result) || result.Kind != CardKind.Monster)
                            continue;
                        var used = new bool[owner.Hand.Count];
                        used[a] = used[b] = true;
                        var path = new List<int> { a, b };
                        ExploreHandChains(side, owner, freeSlot, path, used,
                            result, options);
                    }

            for (int slot = 0; slot < ZoneCount; ++slot)
            {
                var field = owner.Monster(slot);
                if (field == null) continue;
                for (int hand = 0; hand < owner.Hand.Count; ++hand)
                {
                    if (!fusions.TryFuse(field.Card, owner.Hand[hand],
                        out var result) || result.Kind != CardKind.Monster)
                        continue;
                    var used = new bool[owner.Hand.Count];
                    used[hand] = true;
                    var path = new List<int> { hand };
                    ExploreFieldChains(side, owner, slot, path, used,
                        result, options);
                }
            }

            options.Sort(CompareAvailableFusions);
            return options;
        }

        private void ExploreHandChains(DuelSide side, MatchSide owner,
            int freeSlot, List<int> path, bool[] used,
            CardDefinition current, List<AvailableFusion> options)
        {
            if (CanPlay(side, path, FieldLane.Monster, freeSlot,
                current.StarA, out _))
                options.Add(new AvailableFusion(path, -1, current));
            if (path.Count >= HandLimit) return;

            for (int next = 0; next < owner.Hand.Count; ++next)
            {
                if (used[next] ||
                    !fusions.TryFuse(current, owner.Hand[next], out var combined) ||
                    combined.Kind != CardKind.Monster) continue;
                used[next] = true;
                path.Add(next);
                ExploreHandChains(side, owner, freeSlot, path, used,
                    combined, options);
                path.RemoveAt(path.Count - 1);
                used[next] = false;
            }
        }

        private void ExploreFieldChains(DuelSide side, MatchSide owner,
            int slot, List<int> path, bool[] used,
            CardDefinition current, List<AvailableFusion> options)
        {
            if (CanPlayOnField(side, path, slot, current.StarA,
                out _, out _))
                options.Add(new AvailableFusion(path, slot, current));
            if (path.Count >= HandLimit) return;

            for (int next = 0; next < owner.Hand.Count; ++next)
            {
                if (used[next] ||
                    !fusions.TryFuse(current, owner.Hand[next], out var combined) ||
                    combined.Kind != CardKind.Monster) continue;
                used[next] = true;
                path.Add(next);
                ExploreFieldChains(side, owner, slot, path, used,
                    combined, options);
                path.RemoveAt(path.Count - 1);
                used[next] = false;
            }
        }

        private static int CompareAvailableFusions(AvailableFusion a,
            AvailableFusion b)
        {
            int compare = a.MaterialCount.CompareTo(b.MaterialCount);
            if (compare != 0) return compare;
            compare = b.Result.Attack.CompareTo(a.Result.Attack);
            if (compare != 0) return compare;
            compare = a.FieldSlot.CompareTo(b.FieldSlot);
            if (compare != 0) return compare;
            for (int i = 0; i < a.HandIndices.Count && i < b.HandIndices.Count; ++i)
            {
                compare = a.HandIndices[i].CompareTo(b.HandIndices[i]);
                if (compare != 0) return compare;
            }
            return a.HandIndices.Count.CompareTo(b.HandIndices.Count);
        }
    }
}
