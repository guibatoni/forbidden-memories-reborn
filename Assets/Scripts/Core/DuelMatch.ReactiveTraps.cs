namespace Reborn.Core
{
    /// <summary>
    /// Resolves automatic PS1 reactions to verified direct-damage and
    /// recovery spells. A rejected/unrelated action never reveals a trap.
    /// </summary>
    public sealed partial class DuelMatch
    {
        private bool ConsumeReactiveTrap(DuelSide defendingSide,
            ReactiveTrapOperation trigger, int affectedValue)
        {
            if (reactiveTraps.Count == 0) return false;
            var owner = Side(defendingSide);
            for (int slot = 0; slot < ZoneCount; slot++)
            {
                var card = owner.Support(slot);
                if (card == null || !owner.SupportIsFaceDown(slot) ||
                    owner.SupportSetTurn(slot) == Turn ||
                    !reactiveTraps.Matches(card, trigger)) continue;
                // No card/LP changes before the rule and set timing match.
                owner.SetSupport(slot, null);
                owner.Bury(card);
                Emit(MatchEventKind.TrapTriggered, defendingSide, card.Id,
                    FieldLane.Support, slot, affectedValue);
                Emit(MatchEventKind.Graveyard, defendingSide, card.Id,
                    FieldLane.Support, slot);
                return true;
            }
            return false;
        }
    }
}
