using System;

namespace Reborn.Core
{
    /// <summary>
    /// PS1 spell resolution for an explicit catalog of audited card IDs.
    /// Does not use arbitrary DUELTRACE descriptions as executable rules.
    /// </summary>
    public sealed partial class DuelMatch
    {
        public bool HasVerifiedSpell(CardDefinition card) =>
            verifiedSpells.TryGet(card, out _);

        /// <summary>
        /// A prepared face-down spell may be activated later during its owner's
        /// turn. Trap cards are triggered by event conditions and are deliberately
        /// not handled by this action without verified scripts.
        /// </summary>
        public bool TryActivateSupport(DuelSide side, int slot, out string reason)
        {
            if (!CanActivateSupport(side, slot, out reason)) return false;
            var owner = Side(side);
            var card = owner.Support(slot);
            if (!verifiedSpells.TryGet(card, out var rule))
                return Reject(out reason, "Efeito da carta sem validacao");
            owner.SetSupport(slot, null);
            owner.Bury(card);
            Emit(MatchEventKind.SupportActivated, side, card.Id, FieldLane.Support, slot);
            Emit(MatchEventKind.Graveyard, side, card.Id, FieldLane.Support, slot);
            ApplyVerifiedSpell(side, rule);
            SignalChanged();
            reason = "";
            return true;
        }

        private void ApplyVerifiedSpell(DuelSide user, VerifiedSpellRule rule)
        {
            switch (rule.Operation)
            {
                case VerifiedSpellOperation.SetTerrain:
                    if (!TerrainRules.IsKnown(rule.CardId))
                        throw new InvalidOperationException("Campo fora da lista original");
                    CurrentTerrainCardId = rule.CardId;
                    Emit(MatchEventKind.TerrainChanged, user, rule.CardId,
                        FieldLane.Support, -1);
                    break;
                case VerifiedSpellOperation.RestoreLife:
                {
                    if (ConsumeReactiveTrap(Other(user),
                        ReactiveTrapOperation.ReverseSpellHealing, rule.Amount))
                    {
                        Damage(user, rule.Amount);
                        break;
                    }
                    var side = Side(user);
                    int healed = (int)Math.Min((long)rule.Amount,
                        (long)int.MaxValue - side.Life);
                    if (healed > 0)
                    {
                        side.Life += healed;
                        Emit(MatchEventKind.LifeChanged, user, rule.CardId,
                            FieldLane.Support, -1, healed);
                    }
                    break;
                }
                case VerifiedSpellOperation.DirectDamage:
                    if (ConsumeReactiveTrap(Other(user),
                        ReactiveTrapOperation.ReflectDirectSpellDamage, rule.Amount))
                        Damage(user, rule.Amount);
                    else
                        Damage(Other(user), rule.Amount);
                    break;
                case VerifiedSpellOperation.DestroyEnemyMonsters:
                    for (int i = 0; i < ZoneCount; i++)
                        DestroyMonster(Other(user), i);
                    break;
                case VerifiedSpellOperation.DestroyAllFieldCards:
                    foreach (DuelSide side in new[] {DuelSide.Player, DuelSide.Opponent})
                    {
                        for (int i = 0; i < ZoneCount; i++)
                        {
                            DestroyMonster(side, i);
                            DestroySupport(side, i);
                        }
                    }
                    break;
                default:
                    throw new InvalidOperationException(
                        "Operacao verificada nao implementada: " + rule.Operation);
            }
        }

        private void DestroySupport(DuelSide side, int slot)
        {
            var owner = Side(side);
            var card = owner.Support(slot);
            if (card == null) return;
            owner.SetSupport(slot, null);
            owner.Bury(card);
            Emit(MatchEventKind.Destroy, side, card.Id, FieldLane.Support, slot);
            Emit(MatchEventKind.Graveyard, side, card.Id, FieldLane.Support, slot);
        }
    }
}
