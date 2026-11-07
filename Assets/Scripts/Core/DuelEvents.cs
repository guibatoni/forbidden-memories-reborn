using System;

namespace Reborn.Core
{
    // Eventos descrevem o que aconteceu, nunca como deve ser animado.
    public enum DuelEventKind { Reset, CardDrawn, CardPlaced, CardRemoved, SandboxTurnAdvanced }

    public sealed class DuelEvent
    {
        public long Sequence { get; }
        public DuelEventKind Kind { get; }
        public string CardId { get; }
        public FieldLane? Lane { get; }
        public int Slot { get; }
        public int Turn { get; }

        public DuelEvent(long sequence, DuelEventKind kind, string cardId,
            FieldLane? lane, int slot, int turn)
        {
            if (sequence <= 0) throw new ArgumentOutOfRangeException(nameof(sequence));
            Sequence = sequence;
            Kind = kind;
            CardId = cardId ?? "";
            Lane = lane;
            Slot = slot;
            Turn = turn;
        }
    }
}
