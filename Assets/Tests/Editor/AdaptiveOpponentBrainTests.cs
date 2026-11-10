using System;
using System.Collections.Generic;
using NUnit.Framework;
using Reborn.Core;

namespace Reborn.Tests
{
    public sealed class AdaptiveOpponentBrainTests
    {
        private static CardDefinition M(string id, int attack)
            => new CardDefinition(id, id, CardKind.Monster, attack, attack, "");
        private static IReadOnlyList<CardDefinition> Deck(params CardDefinition[] first)
        {
            var cards = new List<CardDefinition>(first);
            for (int i = 1; i < 40; i++) cards.Add(M("f" + i, 900));
            return cards;
        }

        [Test]
        public void PlanningUsesOnlyLegalOpponentActions()
        {
            var duel = new DuelMatch(Deck(M("player", 1200)), Deck(M("cpu", 2500)));
            var brain = new AdaptiveOpponentBrain(new Random(123));
            brain.StartDuel(new AiLearningState("demo"));
            Assert.IsFalse(brain.TryChoosePlacement(duel, out _));
            Assert.IsTrue(duel.TryEndTurn(DuelSide.Player, out _));
            int oldEvents = duel.Events.Count;
            Assert.IsTrue(brain.TryChoosePlacement(duel, out var plan));
            Assert.IsTrue(duel.CanPlay(DuelSide.Opponent, plan.HandIndices,
                FieldLane.Monster, plan.Slot, plan.Star, out _));
            Assert.AreEqual(oldEvents, duel.Events.Count);
        }

        [Test]
        public void LearnsOnlyAfterFinishAndDoesNotTrainTwice()
        {
            var duel = new DuelMatch(Deck(M("player", 9000)), Deck(M("cpu", 500)));
            var state = new AiLearningState("demo");
            var brain = new AdaptiveOpponentBrain(new Random(8));
            brain.StartDuel(state);
            Assert.IsFalse(brain.RecordCompletedDuel(duel));
            Assert.IsTrue(duel.TryPlay(DuelSide.Player, new[] {0},
                FieldLane.Monster, 0, BattlePosition.Attack, GuardianStar.None, out _));
            Assert.IsTrue(duel.TryBeginBattle(DuelSide.Player, out _));
            Assert.IsTrue(duel.TryAttack(DuelSide.Player, 0, -1, out _));
            Assert.IsTrue(brain.RecordCompletedDuel(duel));
            Assert.AreEqual(1, state.duels);
            Assert.AreEqual(1, state.playerAttacks);
            Assert.IsFalse(brain.RecordCompletedDuel(duel));
            Assert.AreEqual(1, state.duels);
        }

        [Test]
        public void MultistepSearchClearsWallWithWeakerMonsterBeforeDirectAttack()
        {
            var match = new DuelMatch(
                Deck(new CardDefinition("defender", "defender", CardKind.Monster, 200, 1800, "")),
                Deck(M("low", 2100), M("high", 3200)));
            Assert.IsTrue(match.TryPlay(DuelSide.Player, new[] { 0 },
                FieldLane.Monster, 0, BattlePosition.Defense,
                GuardianStar.None, out _));
            Assert.IsTrue(match.TryEndTurn(DuelSide.Player, out _));
            Assert.IsTrue(match.TryPlay(DuelSide.Opponent, new[] { 0 },
                FieldLane.Monster, 0, BattlePosition.Attack,
                GuardianStar.None, out _));
            Assert.IsTrue(match.TryEndTurn(DuelSide.Opponent, out _));
            Assert.IsTrue(match.TryEndTurn(DuelSide.Player, out _));
            Assert.IsTrue(match.TryPlay(DuelSide.Opponent, new[] { 0 },
                FieldLane.Monster, 1, BattlePosition.Attack,
                GuardianStar.None, out _));
            Assert.IsTrue(match.TryBeginBattle(DuelSide.Opponent, out _));
            long previousEvents = match.Events.Count;
            Assert.IsTrue(MultiAttackPlanner.TryPlan(match, AiTactic.Balanced,
                new Random(9), out var plan));
            Assert.AreEqual(2, plan.Steps.Count);
            Assert.AreEqual(0, plan.Steps[0].AttackerSlot);
            Assert.AreEqual(0, plan.Steps[0].TargetSlot);
            Assert.AreEqual(1, plan.Steps[1].AttackerSlot);
            Assert.AreEqual(-1, plan.Steps[1].TargetSlot);
            Assert.AreEqual(previousEvents, match.Events.Count);
            Assert.AreEqual(8000, match.Player.Life);
        }

        [Test]
        public void CorruptScoresAreSanitizedAndRandomStylesRemainAvailable()
        {
            var state = new AiLearningState("demo");
            state.tacticValues = new[] { float.NaN, float.PositiveInfinity };
            state.duels = -100;
            state.Sanitize();
            Assert.AreEqual(5, state.tacticValues.Length);
            Assert.AreEqual(0, state.duels);
            var brain = new AdaptiveOpponentBrain(new Random(29));
            var seen = new HashSet<AiTactic>();
            for (int i = 0; i < 60; i++)
            {
                brain.StartDuel(state);
                seen.Add(brain.Tactic);
            }
            Assert.GreaterOrEqual(seen.Count, 3);
        }
    }
}
