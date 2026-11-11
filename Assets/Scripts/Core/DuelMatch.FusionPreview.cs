using System.Collections.Generic;

namespace Reborn.Core
{
    /// <summary>
    /// Diagnostics for legal ordered fusion recipes. A failed pair consumes
    /// the prior intermediate in the real game; never pretend that the
    /// last card surviving an invalid pair was a successful fusion.
    /// </summary>
    public sealed partial class DuelMatch
    {
        public int FusionRecipeCount => fusions.Count;

        public int CountHandFusionSteps(DuelSide side, IReadOnlyList<int> indices)
        {
            if (indices == null || indices.Count < 2 ||
                indices.Count > HandLimit) return 0;
            var owner = Side(side);
            var used = new HashSet<int>();
            CardDefinition last = null;
            int successful = 0;
            foreach (int index in indices)
            {
                if (index < 0 || index >= owner.Hand.Count || !used.Add(index))
                    return 0;
                var next = owner.Hand[index];
                if (last == null) last = next;
                else if (fusions.TryFuse(last, next, out var combined))
                {
                    last = combined;
                    successful++;
                }
                else last = next;
            }
            return successful;
        }

        public int CountFieldFusionSteps(DuelSide side,
            IReadOnlyList<int> handIndices, int monsterSlot)
        {
            if (!ResolveFieldSequence(side, handIndices, monsterSlot,
                out _, out _, out int successful, out _))
                return 0;
            return successful;
        }

        public bool FinalFieldResultIsFusion(DuelSide side,
            IReadOnlyList<int> handIndices, int monsterSlot)
        {
            return ResolveFieldSequence(side, handIndices, monsterSlot,
                out _, out int survivor, out int successful, out _) &&
                successful > 0 && survivor < 0;
        }

        public bool HasRecipe(CardDefinition a, CardDefinition b) =>
            fusions.TryFuse(a,b,out _);
    }
}
