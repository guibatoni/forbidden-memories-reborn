namespace Reborn.Core
{
    /// <summary>
    /// Automatic trap reaction in the actual attack transaction. The preview
    /// does not reveal hidden traps or mutate events. Trap priority chooses
    /// the narrowest eligible retail ATK threshold, then the lowest slot.
    /// </summary>
    public sealed partial class DuelMatch
    {
        private bool TryTriggerAttackTrap(DuelSide attackerSide,
            int attackerSlot, int originalTargetSlot, int attackPower)
        {
            if (attackTraps.Count == 0) return false;
            DuelSide defenderSide = Other(attackerSide);
            MatchSide defending = Side(defenderSide);
            var selectedSlot = -1;
            int selectedThreshold = int.MaxValue;
            for (int slot = 0; slot < ZoneCount; slot++)
            {
                var card = defending.Support(slot);
                if (card == null || !defending.SupportIsFaceDown(slot) ||
                    defending.SupportSetTurn(slot) == Turn) continue;
                if (!attackTraps.TryMatch(card, attackPower, out int limit))
                    continue;
                if (limit < selectedThreshold)
                {
                    selectedThreshold = limit;
                    selectedSlot = slot;
                }
            }
            if (selectedSlot < 0) return false;

            var attacker = Side(attackerSide).Monster(attackerSlot);
            if (attacker == null) return false;
            CardDefinition trap = defending.Support(selectedSlot);
            // The defender is NOT revealed, because combat never resolves.
            if (attacker.FaceDown) attacker.Reveal();
            attacker.AttackedThisTurn = true;
            Emit(MatchEventKind.Attack, attackerSide, attacker.Card.Id,
                FieldLane.Monster, attackerSlot, originalTargetSlot);

            defending.SetSupport(selectedSlot, null);
            defending.Bury(trap);
            Emit(MatchEventKind.TrapTriggered, defenderSide, trap.Id,
                FieldLane.Support, selectedSlot, attackPower);
            Emit(MatchEventKind.Graveyard, defenderSide, trap.Id,
                FieldLane.Support, selectedSlot);

            DestroyMonster(attackerSide, attackerSlot);
            SignalChanged();
            return true;
        }
    }
}
