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
            if (indices == null || indices.Count < 2) return 0;
            var chain = PreviewHandChain(side, indices);
            return chain == null ? 0 : chain.SuccessfulFusions;
        }

        public int CountFieldFusionSteps(DuelSide side,
            IReadOnlyList<int> handIndices, int monsterSlot)
        {
            if (!ResolveFieldSequence(side, handIndices, monsterSlot,
                out var chain, out _))
                return 0;
            return chain.SuccessfulFusions;
        }

        public bool FinalFieldResultIsFusion(DuelSide side,
            IReadOnlyList<int> handIndices, int monsterSlot)
        {
            return ResolveFieldSequence(side, handIndices, monsterSlot,
                out var chain, out _) && chain.FinalIsFusion;
        }

        public bool HasRecipe(CardDefinition a, CardDefinition b) =>
            fusions.TryFuse(a,b,out _);
    }
}
