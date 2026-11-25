using System.Collections.Generic;

namespace Reborn.Core
{
    /// <summary>
    /// Bridges the pure FusionChainResolver to duel state. Every hand chain,
    /// field chain, preview and diagnostic goes through these helpers so the
    /// rules exist exactly once.
    /// </summary>
    public sealed partial class DuelMatch
    {
        /// <summary>Read-only resolution of a hand-only chain; null when the
        /// indices are invalid (empty, &gt;5, repeated or out of range).</summary>
        public FusionChainResult PreviewHandChain(DuelSide side, IReadOnlyList<int> indices)
        {
            var materials = CollectHandMaterials(Side(side), null, indices);
            return materials == null ? null :
                FusionChainResolver.Resolve(materials, fusions, verifiedEquips);
        }

        /// <summary>Read-only resolution of a field chain: own monster in
        /// monsterSlot is material #1, then the hand cards in order. Survivor
        /// position 0 means the existing field monster stays on the field.</summary>
        public FusionChainResult PreviewFieldChain(DuelSide side,
            IReadOnlyList<int> indices, int monsterSlot)
        {
            if (monsterSlot < 0 || monsterSlot >= ZoneCount) return null;
            var onField = Side(side).Monster(monsterSlot);
            if (onField == null) return null;
            var materials = CollectHandMaterials(Side(side), onField.Card, indices);
            return materials == null ? null :
                FusionChainResolver.Resolve(materials, fusions, verifiedEquips);
        }

        /// <summary>Hand index of the physical card at a chain position.
        /// fieldOffset is 1 for field chains (position 0 is the field card).</summary>
        private static int HandIndexAt(IReadOnlyList<int> indices, int position,
            int fieldOffset)
        {
            int at = position - fieldOffset;
            return at >= 0 && at < indices.Count ? indices[at] : -1;
        }

        private static List<CardDefinition> CollectHandMaterials(MatchSide owner,
            CardDefinition first, IReadOnlyList<int> indices)
        {
            if (indices == null || indices.Count == 0 || indices.Count > HandLimit)
                return null;
            var seen = new HashSet<int>();
            var materials = new List<CardDefinition>(indices.Count + 1);
            if (first != null) materials.Add(first);
            foreach (int index in indices)
            {
                if (index < 0 || index >= owner.Hand.Count || !seen.Add(index))
                    return null;
                materials.Add(owner.Hand[index]);
            }
            return materials;
        }

        /// <summary>Log every resolved pair, in order, for UI and AI.</summary>
        private void EmitChainSteps(DuelSide side, FusionChainResult chain,
            FieldLane lane, int slot)
        {
            foreach (var step in chain.Steps)
            {
                switch (step.Kind)
                {
                    case FusionStepKind.Fused:
                        Emit(MatchEventKind.Fusion, side, step.CardId, lane, slot);
                        break;
                    case FusionStepKind.Equipped:
                        Emit(MatchEventKind.Equipped, side, step.CardId, lane, slot,
                            step.AttackGain);
                        break;
                    default:
                        Emit(MatchEventKind.FusionFailed, side, step.CardId, lane, slot);
                        break;
                }
            }
        }

        /// <summary>Apply equip gains still attached to the chain result.</summary>
        private static void ApplyPendingEquips(MonsterOnField monster,
            FusionChainResult chain)
        {
            foreach (var equip in chain.PendingEquips)
                if (monster.CanApplyEquip(equip.AttackGain, equip.DefenseGain))
                    monster.ApplyEquip(equip.AttackGain, equip.DefenseGain);
        }

        private static bool PendingEquipsFit(MonsterOnField monster,
            CardDefinition card, FusionChainResult chain)
        {
            long attack = (long)card.Attack + chain.PendingAttackGain +
                (monster != null ? monster.EquipAttackBonus : 0);
            long defense = (long)card.Defense + chain.PendingDefenseGain +
                (monster != null ? monster.EquipDefenseBonus : 0);
            return attack <= int.MaxValue && defense <= int.MaxValue;
        }
    }
}
