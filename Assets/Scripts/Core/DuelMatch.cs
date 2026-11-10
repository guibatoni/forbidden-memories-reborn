using System;
using System.Collections.Generic;

namespace Reborn.Core
{
    public enum MatchEventKind
    {
        Reset, Draw, Play, Fusion, FusionFailed, BattleStarted, PositionChanged, SupportActivated, Attack,
        Destroy, LifeChanged, TurnStarted, Graveyard, Finished
    }

    public sealed class MatchEvent
    {
        public long Sequence { get; }
        public MatchEventKind Kind { get; }
        public DuelSide Side { get; }
        public string CardId { get; }
        public FieldLane? Lane { get; }
        public int Slot { get; }
        public int Value { get; }
        public int Turn { get; }

        public MatchEvent(long sequence, MatchEventKind kind, DuelSide side,
            string cardId, FieldLane? lane, int slot, int value, int turn)
        {
            Sequence = sequence;
            Kind = kind;
            Side = side;
            CardId = cardId ?? "";
            Lane = lane;
            Slot = slot;
            Value = value;
            Turn = turn;
        }
    }

    public sealed class MonsterOnField
    {
        public CardDefinition Card { get; }
        public BattlePosition Position { get; private set; }
        public GuardianStar Star { get; }
        public bool AttackedThisTurn { get; internal set; }
        public bool PositionChangedThisTurn { get; internal set; }
        public bool FaceDown { get; private set; }
        public int TurnPlaced { get; }

        internal MonsterOnField(CardDefinition card, BattlePosition position, GuardianStar star,
            bool faceDown = false, int turnPlaced = -1)
        {
            Card = card; Position = position; Star = star;
            FaceDown = faceDown; TurnPlaced = turnPlaced;
        }
        internal void SetPosition(BattlePosition position)
        {
            Position = position;
            FaceDown = false;
            // The player may change positions repeatedly until this monster attacks.
            PositionChangedThisTurn = true;
        }
        internal void Reveal() { FaceDown = false; }
    }

    public sealed class MatchSide
    {
        private readonly Queue<CardDefinition> deck;
        private readonly List<CardDefinition> hand = new List<CardDefinition>(5);
        private readonly List<CardDefinition> graveyard = new List<CardDefinition>();
        private readonly MonsterOnField[] monsters = new MonsterOnField[5];
        private readonly CardDefinition[] supports = new CardDefinition[5];
        private readonly bool[] supportFaceDown = new bool[5];
        private readonly int[] supportSetTurn = new int[5];

        internal MatchSide(IReadOnlyList<CardDefinition> original)
        {
            deck = new Queue<CardDefinition>(original);
        }
        public int Life { get; internal set; } = 8000;
        public int DeckCount => deck.Count;
        public IReadOnlyList<CardDefinition> Hand => hand.AsReadOnly();
        public IReadOnlyList<CardDefinition> Graveyard => graveyard.AsReadOnly();
        public MonsterOnField Monster(int slot) => slot >= 0 && slot < 5 ? monsters[slot] : null;
        public CardDefinition Support(int slot) => slot >= 0 && slot < 5 ? supports[slot] : null;
        public bool SupportIsFaceDown(int slot) =>
            slot >= 0 && slot < 5 && supports[slot] != null && supportFaceDown[slot];
        public int SupportSetTurn(int slot) =>
            slot >= 0 && slot < 5 && supports[slot] != null ? supportSetTurn[slot] : -1;
        internal CardDefinition Draw() => deck.Count == 0 ? null : deck.Dequeue();
        internal void Give(CardDefinition card) => hand.Add(card);
        internal CardDefinition Take(int handIndex)
        {
            var card = hand[handIndex]; hand.RemoveAt(handIndex); return card;
        }
        internal void SetMonster(int index, MonsterOnField monster) => monsters[index] = monster;
        internal void SetSupport(int index, CardDefinition card,
            bool faceDown = false, int placedTurn = -1)
        {
            supports[index] = card;
            supportFaceDown[index] = card != null && faceDown;
            supportSetTurn[index] = card != null ? placedTurn : -1;
        }
        internal void Bury(CardDefinition card) { if (card != null) graveyard.Add(card); }
        internal void ReadyMonsters()
        {
            foreach (var m in monsters)
            {
                if (m == null) continue;
                m.AttackedThisTurn = false;
                m.PositionChangedThisTurn = false;
            }
        }
        /// <summary>Read-only count exposed to the separate Unity presentation assembly.</summary>
        public int MonsterCount
        {
            get { int n=0; foreach(var m in monsters) if(m!=null) n++; return n; }
        }
    }

    /// <summary>
    /// Prototype rules kernel. Two sides, five monster and five support zones per side.
    /// No Unity references. Does NOT claim full binary-accurate Forbidden Memories rules:
    /// effect scripts, terrain, trap triggers, rituals, ranks, duel rewards and opponent
    /// decks remain pending verified ISO extraction. Guarding these unknowns prevents
    /// fake mechanics or placeholder data from entering the production engine.
    /// </summary>
    public sealed partial class DuelMatch
    {
        public const int DeckSize = 40;
        public const int ZoneCount = 5;
        public const int HandLimit = 5;
        private readonly MatchSide player;
        private readonly MatchSide opponent;
        private readonly FusionCatalog fusions;
        private readonly List<MatchEvent> history = new List<MatchEvent>();
        private long sequence;
        private bool hasPlayed;

        public event Action Changed;
        public event Action<MatchEvent> EventEmitted;
        public MatchSide Player => player;
        public MatchSide Opponent => opponent;
        public DuelSide CurrentSide { get; private set; } = DuelSide.Player;
        public MatchPhase Phase { get; private set; } = MatchPhase.Main;
        public DuelSide? Winner { get; private set; }
        public int Turn { get; private set; } = 1;
        public IReadOnlyList<MatchEvent> Events => history.AsReadOnly();
        public bool PlayedThisTurn => hasPlayed;

        public DuelMatch(IReadOnlyList<CardDefinition> playerDeck,
            IReadOnlyList<CardDefinition> opponentDeck, FusionCatalog catalog = null)
        {
            CheckDeck(playerDeck, nameof(playerDeck));
            CheckDeck(opponentDeck, nameof(opponentDeck));
            player = new MatchSide(playerDeck);
            opponent = new MatchSide(opponentDeck);
            fusions = catalog ?? new FusionCatalog();
            Emit(MatchEventKind.Reset, DuelSide.Player);
            DrawToHand(player, DuelSide.Player, false);
            DrawToHand(opponent, DuelSide.Opponent, false);
            SignalChanged();
        }

        private static void CheckDeck(IReadOnlyList<CardDefinition> cards, string param)
        {
            if (cards == null || cards.Count != DeckSize)
                throw new ArgumentException("Deck must contain exactly 40 cards", param);
            foreach (var card in cards)
                if (card == null) throw new ArgumentException("Null card in deck", param);
        }

        public MatchSide Side(DuelSide side) => side == DuelSide.Player ? player : opponent;
        public static DuelSide Other(DuelSide side) => side == DuelSide.Player ? DuelSide.Opponent : DuelSide.Player;

        private void Emit(MatchEventKind kind, DuelSide side, string id = "",
            FieldLane? lane = null, int slot = -1, int value = 0)
        {
            var evt = new MatchEvent(++sequence, kind, side, id, lane, slot, value, Turn);
            history.Add(evt);
            EventEmitted?.Invoke(evt);
        }
        private void SignalChanged() { Changed?.Invoke(); }
        private bool Reject(out string reason, string message) { reason = message; return false; }
        private bool Active(DuelSide side, MatchPhase phase, out string error)
        {
            if (Phase == MatchPhase.Finished) return Reject(out error, "Duelo terminado");
            if (CurrentSide != side) return Reject(out error, "Nao e o turno deste jogador");
            if (Phase != phase) return Reject(out error, "Fase incorreta");
            error = ""; return true;
        }

        private void DrawToHand(MatchSide state, DuelSide side, bool deckOut)
        {
            while (state.Hand.Count < HandLimit)
            {
                var card = state.Draw();
                if (card == null)
                {
                    if (deckOut) Finish(Other(side));
                    return;
                }
                state.Give(card);
                Emit(MatchEventKind.Draw, side, card.Id);
            }
        }

        public CardDefinition PreviewResult(DuelSide side, IReadOnlyList<int> indices)
        {
            if (indices == null || indices.Count == 0 || indices.Count > HandLimit) return null;
            var hand = Side(side).Hand;
            var seen = new HashSet<int>();
            CardDefinition result = null;
            foreach (var index in indices)
            {
                if (index < 0 || index >= hand.Count || !seen.Add(index)) return null;
                if (result == null) result = hand[index];
                else if (!fusions.TryFuse(result, hand[index], out var fused)) result = hand[index];
                else result = fused;
            }
            return result;
        }

        public bool CanPlay(DuelSide side, IReadOnlyList<int> handIndices,
            FieldLane lane, int slot, GuardianStar star, out string reason)
        {
            if (!Active(side, MatchPhase.Main, out reason)) return false;
            if (hasPlayed) return Reject(out reason, "Acao de colocacao ja utilizada neste turno");
            if (handIndices == null || handIndices.Count == 0 || handIndices.Count > HandLimit)
                return Reject(out reason, "Selecione entre 1 e 5 cartas");
            var state = Side(side);
            var selected = new HashSet<int>();
            CardDefinition outcome = null;
            foreach (int index in handIndices)
            {
                if (index < 0 || index >= state.Hand.Count || !selected.Add(index))
                    return Reject(out reason, "Indices duplicados ou invalidos");
                var card = state.Hand[index];
                if (outcome == null) outcome = card;
                else if (!fusions.TryFuse(outcome, card, out var fused)) outcome = card;
                else outcome = fused;
            }
            var expectedLane = DuelActionRules.RequiredLane(outcome.Kind);
            if (expectedLane != lane) return Reject(out reason, "Tipo de zona incompativel");
            if (slot < 0 || slot >= ZoneCount) return Reject(out reason, "Zona invalida");
            if (lane == FieldLane.Monster && state.Monster(slot) != null ||
                lane == FieldLane.Support && state.Support(slot) != null)
                return Reject(out reason, "Zona ocupada");
            if (lane == FieldLane.Monster && !GuardianStarRules.IsAvailable(outcome, star))
                return Reject(out reason, "Guardian Star invalida para esta carta");
            reason = ""; return true;
        }

        public bool TryPlay(DuelSide side, IReadOnlyList<int> indices, FieldLane lane, int slot,
            BattlePosition position, GuardianStar star, out string reason)
        {
            return TryPlay(side, indices, lane, slot, position, star, false, out reason);
        }

        /// <summary>One hand placement, with chosen face orientation.</summary>
        public bool TryPlay(DuelSide side, IReadOnlyList<int> indices, FieldLane lane, int slot,
            BattlePosition position, GuardianStar star, bool faceDown, out string reason)
        {
            if (!CanPlay(side, indices, lane, slot, star, out reason)) return false;
            var state = Side(side);
            // Only physical hand cards can go to the graveyard. Intermediate
            // fusion results are virtual and must never be buried as materials.
            CardDefinition result = null;
            int survivingOriginalIndex = -1;
            foreach (var index in indices)
            {
                var card = state.Hand[index];
                if (result == null)
                {
                    result = card; survivingOriginalIndex = index;
                    continue;
                }
                if (fusions.TryFuse(result, card, out var fused))
                {
                    result = fused;
                    survivingOriginalIndex = -1;
                    Emit(MatchEventKind.Fusion, side, result.Id, lane, slot);
                }
                else
                {
                    result = card;
                    survivingOriginalIndex = index;
                    Emit(MatchEventKind.FusionFailed, side, card.Id, lane, slot);
                }
            }
            var spent = new List<CardDefinition>();
            foreach (var index in indices)
                if (index != survivingOriginalIndex) spent.Add(state.Hand[index]);
            // Remove from highest index to avoid moving the remaining selections.
            var sorted = new List<int>(indices);
            sorted.Sort((a, b) => b.CompareTo(a));
            foreach (var index in sorted) state.Take(index);
            foreach (var material in spent)
            {
                state.Bury(material);
                Emit(MatchEventKind.Graveyard, side, material.Id);
            }
            if (lane == FieldLane.Monster)
                state.SetMonster(slot, new MonsterOnField(result, position, star, faceDown, Turn));
            else state.SetSupport(slot, result, faceDown, Turn);
            hasPlayed = true;
            Emit(MatchEventKind.Play, side, result.Id, lane, slot);
            SignalChanged();
            return true;
        }

        /// <summary>
        /// Read-only fallback for opponents with only support cards available.
        /// The caller must still explicitly TryPlay exactly one card.
        /// Unverified effects are never activated.
        /// </summary>
        public bool TryChooseSupportSet(DuelSide side, out int handIndex, out int slot)
        {
            handIndex = -1;
            slot = -1;
            if (Phase != MatchPhase.Main || CurrentSide != side || hasPlayed)
                return false;
            var hand = Side(side).Hand;
            for (int h = 0; h < hand.Count; h++)
            {
                if (hand[h].Kind == CardKind.Monster) continue;
                for (int field = 0; field < ZoneCount; field++)
                {
                    if (!CanPlay(side, new[] { h }, FieldLane.Support,
                        field, GuardianStar.None, out _)) continue;
                    handIndex = h;
                    slot = field;
                    return true;
                }
            }
            return false;
        }

        public bool CanChangePosition(DuelSide side, int slot,
            BattlePosition newPosition, out string reason)
        {
            if (Phase == MatchPhase.Finished)
                return Reject(out reason, "Duelo encerrado");
            if (CurrentSide != side)
                return Reject(out reason, "Nao e o seu turno");
            if (slot < 0 || slot >= ZoneCount)
                return Reject(out reason, "Zona invalida");
            var monster = Side(side).Monster(slot);
            if (monster == null) return Reject(out reason, "Nao ha monstro nesta zona");
            if (monster.AttackedThisTurn)
                return Reject(out reason, "Monstro que ja atacou nao pode mudar de posicao");
             if (monster.Position == newPosition && !monster.FaceDown)
                return Reject(out reason, "Posicao ja selecionada");
            reason = "";
            return true;
        }

        public bool TryChangePosition(DuelSide side, int slot,
            BattlePosition newPosition, out string reason)
        {
            if (!CanChangePosition(side, slot, newPosition, out reason))
                return false;
            var monster = Side(side).Monster(slot);
            monster.SetPosition(newPosition);
            Emit(MatchEventKind.PositionChanged, side, monster.Card.Id,
                FieldLane.Monster, slot, (int)newPosition);
            SignalChanged();
            return true;
        }

        /// <summary>
        /// Support cards can be prepared face-down. We do not simulate magic/
        /// trap/equip effects without a validated resolver from the game data.
        /// </summary>
        public bool CanActivateSupport(DuelSide side, int slot, out string reason)
        {
            if (Phase == MatchPhase.Finished || CurrentSide != side)
                return Reject(out reason, "Fora do turno");
            if (slot < 0 || slot >= ZoneCount || Side(side).Support(slot) == null)
                return Reject(out reason, "Nao ha suporte nesta zona");
            if (!Side(side).SupportIsFaceDown(slot))
                return Reject(out reason, "Somente suporte setado virado para baixo pode ser ativado");
            if (Side(side).SupportSetTurn(slot) == Turn)
                return Reject(out reason, "Suporte recem-setado: aguarde seu proximo turno");
            return Reject(out reason, "Efeito nao validado: ativacao indisponivel nesta versao");
        }

        public bool TryBeginBattle(DuelSide side, out string reason)
        {
            if (!Active(side, MatchPhase.Main, out reason)) return false;
            if (!hasPlayed)
                return Reject(out reason, "Baixe uma carta antes de entrar em batalha");
            Phase = MatchPhase.Battle;
            Emit(MatchEventKind.BattleStarted, side);
            SignalChanged();
            return true;
        }

        /// <summary>
        /// Read-only validation used by the presentation directors before playing
        /// their animations. Must not emit events, mark monsters or alter LP.
        /// </summary>
        public bool CanAttack(DuelSide side, int fromSlot, int targetSlot, out string reason)
        {
            if (!Active(side, MatchPhase.Battle, out reason)) return false;
            if (fromSlot < 0 || fromSlot >= ZoneCount) return Reject(out reason, "Atacante invalido");
            if (targetSlot < -1 || targetSlot >= ZoneCount) return Reject(out reason, "Alvo invalido");
            var owner = Side(side); var enemy = Side(Other(side));
            var attacker = owner.Monster(fromSlot);
            if (attacker == null) return Reject(out reason, "Nao ha monstro atacante");
            if (attacker.Position != BattlePosition.Attack)
                return Reject(out reason, "Monstro em defesa nao pode atacar");
            if (attacker.AttackedThisTurn) return Reject(out reason, "Monstro ja atacou neste turno");
            var defender = targetSlot == -1 ? null : enemy.Monster(targetSlot);
            if (targetSlot >= 0 && defender == null)
                return Reject(out reason, "Alvo vazio; use ataque direto quando o campo estiver livre");
            if (targetSlot == -1 && enemy.MonsterCount != 0)
                return Reject(out reason, "Ataque direto bloqueado por monstros");
            reason = "";
            return true;
        }

        /// <summary>
        /// Read-only authoritative calculation for animation and diagnostics.
        /// It performs the same eligibility checks as executing an attack,
        /// without emitting events or consuming the attacker's action.
        /// </summary>
        public bool TryPreviewAttack(DuelSide side, int fromSlot, int targetSlot,
            out CombatForecast forecast, out string reason)
        {
            forecast = null;
            if (!CanAttack(side, fromSlot, targetSlot, out reason)) return false;
            var own = Side(side);
            var enemy = Side(Other(side));
            forecast = new CombatForecast(own.Monster(fromSlot),
                targetSlot < 0 ? null : enemy.Monster(targetSlot));
            return true;
        }

        public bool TryAttack(DuelSide side, int fromSlot, int targetSlot, out string reason)
        {
            if (!TryPreviewAttack(side, fromSlot, targetSlot, out var forecast, out reason))
                return false;
            var attacker = Side(side).Monster(fromSlot);
            var defender = targetSlot >= 0 ? Side(Other(side)).Monster(targetSlot) : null;
            // Set monsters are revealed only when they actually participate in combat.
            if (defender != null && defender.FaceDown) defender.Reveal();
            if (attacker.FaceDown) attacker.Reveal();
            attacker.AttackedThisTurn = true;
            Emit(MatchEventKind.Attack, side, attacker.Card.Id,
                FieldLane.Monster, fromSlot, targetSlot);

            // Execute the same resolution calculated by TryPreviewAttack.
            // In particular, 1800 DEF + 500 winning Guardian Star = 2300 DEF;
            // a 2100 ATK monster cannot destroy that defender.
            if (forecast.DefenderDestroyed)
                DestroyMonster(Other(side), targetSlot);
            if (forecast.AttackerDestroyed)
                DestroyMonster(side, fromSlot);
            if (forecast.DamageToDefender > 0)
                Damage(Other(side), forecast.DamageToDefender);
            if (forecast.DamageToAttacker > 0)
                Damage(side, forecast.DamageToAttacker);
            SignalChanged();
            return true;
        }

        private void Damage(DuelSide side, int amount)
        {
            if (amount <= 0) return;
            var owner = Side(side);
            owner.Life = Math.Max(0, owner.Life - amount);
            Emit(MatchEventKind.LifeChanged, side, "", null, -1, -amount);
            if (owner.Life == 0) Finish(Other(side));
        }

        private void DestroyMonster(DuelSide side, int slot)
        {
            var owner = Side(side);
            var monster = owner.Monster(slot);
            if (monster == null) return;
            owner.SetMonster(slot, null);
            owner.Bury(monster.Card);
            Emit(MatchEventKind.Destroy, side, monster.Card.Id, FieldLane.Monster, slot);
            Emit(MatchEventKind.Graveyard, side, monster.Card.Id);
        }

        private void Finish(DuelSide winner)
        {
            if (Phase == MatchPhase.Finished) return;
            Winner = winner;
            Phase = MatchPhase.Finished;
            Emit(MatchEventKind.Finished, winner);
            SignalChanged();
        }

        public bool TryEndTurn(DuelSide side, out string reason)
        {
            if (Phase == MatchPhase.Finished) return Reject(out reason, "Duelo terminado");
            if (CurrentSide != side) return Reject(out reason, "Nao e o turno deste jogador");
            if (!hasPlayed)
                return Reject(out reason, "Baixe ao menos uma carta antes de passar o turno");
            CurrentSide = Other(side);
            Turn++;
            hasPlayed = false;
            Phase = MatchPhase.Main;
            var owner = Side(CurrentSide);
            owner.ReadyMonsters();
            Emit(MatchEventKind.TurnStarted, CurrentSide);
            // Draw is mandatory when the hand is not full; deck-out is terminal.
            DrawToHand(owner, CurrentSide, true);
            SignalChanged();
            reason = ""; return true;
        }
    }
}
