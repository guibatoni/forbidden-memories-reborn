using System;
using System.Collections.Generic;

namespace Reborn.Core
{
    /// <summary>
    /// A legal, currently executable two-material fusion. Only the player's
    /// own cards are inspected. A field fusion uses the existing face-up
    /// monster as the first material, then one hand card.
    /// </summary>
    public sealed class AvailableFusion
    {
        public int FirstHandIndex { get; }
        public int SecondHandIndex { get; }
        public int FieldSlot { get; }
        public CardDefinition Result { get; }
        public bool UsesField => FieldSlot >= 0;

        internal AvailableFusion(int firstHandIndex, int secondHandIndex,
            int fieldSlot, CardDefinition result)
        {
            FirstHandIndex = firstHandIndex;
            SecondHandIndex = secondHandIndex;
            FieldSlot = fieldSlot;
            Result = result ?? throw new ArgumentNullException(nameof(result));
        }
    }

    public sealed partial class DuelMatch
    {
        /// <summary>
        /// Returns only verified pair recipes that can be committed this turn.
        /// No unknown ISO rules, player-opponent hidden information, synthetic
        /// substitutions or preview mutations.
        /// </summary>
        public IReadOnlyList<AvailableFusion> FindAvailableFusions(DuelSide side)
        {
            var options = new List<AvailableFusion>();
            if (Phase != MatchPhase.Main || CurrentSide != side ||
                hasPlayed || fusions.Count == 0) return options;

            var owner = Side(side);
            for (int a = 0; a < owner.Hand.Count; a++)
            {
                for (int b = a + 1; b < owner.Hand.Count; b++)
                {
                    if (!fusions.TryFuse(owner.Hand[a], owner.Hand[b],
                        out var result) || result.Kind != CardKind.Monster)
                        continue;
                    for (int slot = 0; slot < ZoneCount; slot++)
                    {
                        if (owner.Monster(slot) != null) continue;
                        if (!CanPlay(side, new [] {a, b}, FieldLane.Monster,
                            slot, result.StarA, out _)) continue;
                        options.Add(new AvailableFusion(a, b, -1, result));
                        break;
                    }
                }
            }

            for (int slot = 0; slot < ZoneCount; slot++)
            {
                var field = owner.Monster(slot);
                if (field == null) continue;
                for (int hand = 0; hand < owner.Hand.Count; hand++)
                {
                    if (!fusions.TryFuse(field.Card, owner.Hand[hand],
                        out var result) || result.Kind != CardKind.Monster ||
                        !CanPlayOnField(side, new [] {hand}, slot,
                            result.StarA, out _, out _))
                        continue;
                    options.Add(new AvailableFusion(hand, -1, slot, result));
                }
            }
            return options;
        }
    }
}
