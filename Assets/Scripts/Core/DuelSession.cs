using System;
using System.Collections.Generic;

namespace Reborn.Core
{
    /// <summary>
    /// Sandbox de regras basicas. Nao representa turnos, fusoes, efeitos e combate originais.
    /// O protocolo de eventos permite testar a interface sem acoplar o motor ao Unity.
    /// </summary>
    public sealed class DuelSession
    {
        public const int ZoneCount = 5;
        public const int MaxHand = 5;
        private readonly List<CardDefinition> initialDeck;
        private Queue<CardDefinition> drawPile;
        private readonly List<CardDefinition> hand = new List<CardDefinition>();
        private readonly CardDefinition[] monsters = new CardDefinition[ZoneCount];
        private readonly CardDefinition[] supports = new CardDefinition[ZoneCount];
        private readonly List<DuelEvent> events = new List<DuelEvent>();
        private long sequence;

        public event Action Changed;
        public event Action<DuelEvent> EventEmitted;

        public int PlayerLife { get; private set; } = 8000;
        public int OpponentLife { get; private set; } = 8000;
        public int Turn { get; private set; } = 1;
        public int DeckCount => drawPile.Count;
        public IReadOnlyList<CardDefinition> Hand => hand;
        public IReadOnlyList<DuelEvent> EventLog => events.AsReadOnly();

        public DuelSession(IReadOnlyList<CardDefinition> deck)
        {
            if (deck == null || deck.Count < MaxHand)
                throw new ArgumentException("Deck insuficiente", nameof(deck));
            initialDeck = new List<CardDefinition>(deck);
            Reset();
        }

        public CardDefinition At(FieldLane lane, int slot)
        {
            if (slot < 0 || slot >= ZoneCount ||
                (lane != FieldLane.Monster && lane != FieldLane.Support)) return null;
            return (lane == FieldLane.Monster ? monsters : supports)[slot];
        }

        private bool IsOccupied(FieldLane lane, int slot) => At(lane, slot) != null;

        public bool CanPlace(int handIndex, FieldLane lane, int slot, out string reason)
        {
            if (handIndex < 0 || handIndex >= hand.Count)
            { reason = "Carta invalida"; return false; }
            return DuelActionRules.CanPlace(hand[handIndex], lane, slot, IsOccupied, out reason);
        }

        public IReadOnlyList<int> AvailableSlots(int handIndex, FieldLane lane)
        {
            if (handIndex < 0 || handIndex >= hand.Count) return Array.Empty<int>();
            return DuelActionRules.AvailableSlots(hand[handIndex], lane, IsOccupied);
        }

        public bool TryPlace(int handIndex, FieldLane lane, int slot, out string reason)
        {
            if (!CanPlace(handIndex, lane, slot, out reason)) return false;
            var card = hand[handIndex];
            var row = lane == FieldLane.Monster ? monsters : supports;
            row[slot] = card;
            hand.RemoveAt(handIndex);
            Publish(DuelEventKind.CardPlaced, card.Id, lane, slot);
            Changed?.Invoke();
            return true;
        }

        /// <summary>Ferramenta de demonstracao grafica. NAO e uma acao de duelo real.</summary>
        public bool TryRemoveSandbox(FieldLane lane, int slot, out string reason)
        {
            if ((lane != FieldLane.Monster && lane != FieldLane.Support) ||
                slot < 0 || slot >= ZoneCount)
            { reason = "Posicao invalida"; return false; }
            var row = lane == FieldLane.Monster ? monsters : supports;
            if (row[slot] == null) { reason = "Zona vazia"; return false; }
            var card = row[slot];
            row[slot] = null;
            Publish(DuelEventKind.CardRemoved, card.Id, lane, slot);
            Changed?.Invoke();
            reason = "";
            return true;
        }

        public int DrawUpToFive()
        {
            var drawn = 0;
            while (hand.Count < MaxHand && drawPile.Count > 0)
            {
                var card = drawPile.Dequeue();
                hand.Add(card);
                Publish(DuelEventKind.CardDrawn, card.Id, null, -1);
                drawn++;
            }
            if (drawn > 0) Changed?.Invoke();
            return drawn;
        }

        public void NextSandboxTurn()
        {
            Turn++;
            Publish(DuelEventKind.SandboxTurnAdvanced, "", null, -1);
            DrawUpToFive();
            Changed?.Invoke();
        }

        public void Reset()
        {
            drawPile = new Queue<CardDefinition>(initialDeck);
            hand.Clear();
            Array.Clear(monsters, 0, monsters.Length);
            Array.Clear(supports, 0, supports.Length);
            PlayerLife = 8000;
            OpponentLife = 8000;
            Turn = 1;
            events.Clear();
            sequence = 0;
            Publish(DuelEventKind.Reset, "", null, -1);
            DrawUpToFive();
            Changed?.Invoke();
        }

        private void Publish(DuelEventKind kind, string cardId, FieldLane? lane, int slot)
        {
            var evt = new DuelEvent(++sequence, kind, cardId, lane, slot, Turn);
            events.Add(evt);
            EventEmitted?.Invoke(evt);
        }
    }
}
