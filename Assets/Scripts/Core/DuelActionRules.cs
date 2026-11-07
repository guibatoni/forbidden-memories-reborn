using System;
using System.Collections.Generic;

namespace Reborn.Core
{
    /// <summary>Valida somente colocacao no sandbox. Combate, fusao e efeitos NAO estao implementados.</summary>
    public static class DuelActionRules
    {
        public static FieldLane RequiredLane(CardKind kind)
        {
            return kind == CardKind.Monster ? FieldLane.Monster : FieldLane.Support;
        }

        public static bool CanPlace(CardDefinition card, FieldLane lane, int slot,
            Func<FieldLane, int, bool> occupied, out string reason)
        {
            if (card == null) { reason = "Carta invalida"; return false; }
            if (lane != FieldLane.Monster && lane != FieldLane.Support)
            { reason = "Tipo de zona invalido"; return false; }
            if (slot < 0 || slot >= DuelSession.ZoneCount)
            { reason = "Posicao invalida"; return false; }
            if (RequiredLane(card.Kind) != lane)
            { reason = "Tipo de zona incompativel"; return false; }
            if (occupied == null) throw new ArgumentNullException(nameof(occupied));
            if (occupied(lane, slot)) { reason = "Zona ocupada"; return false; }
            reason = "";
            return true;
        }

        public static IReadOnlyList<int> AvailableSlots(CardDefinition card, FieldLane lane,
            Func<FieldLane, int, bool> occupied)
        {
            var result = new List<int>();
            for (var i = 0; i < DuelSession.ZoneCount; ++i)
                if (CanPlace(card, lane, i, occupied, out _)) result.Add(i);
            return result;
        }
    }
}
