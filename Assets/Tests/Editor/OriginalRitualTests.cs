using System;
using System.Collections.Generic;
using NUnit.Framework;
using Reborn.Core;

namespace Reborn.Tests
{
    public sealed class OriginalRitualTests
    {
        private static CardDefinition M(string id)
            => new CardDefinition(id,id,CardKind.Monster,1500,1300,"");
        private static IReadOnlyList<CardDefinition> Deck(params CardDefinition[] prefix)
        {
            var cards = new List<CardDefinition>(prefix);
            while (cards.Count < DuelMatch.DeckSize)
                cards.Add(M("filler" + cards.Count));
            return cards;
        }
        private static DuelMatch ReadyForRitual(out CardDefinition summoned,
            bool firstFaceDown = false)
        {
            summoned = new CardDefinition("summoned","Test result",CardKind.Monster,
                2800,2300,"");
            var book = new RitualCatalog();
            book.Add("ritual",new[] {"a","b","c"},summoned);
            var game = new DuelMatch(
                Deck(M("a"),M("b"),M("c"),
                    new CardDefinition("ritual","Test ritual",CardKind.Ritual,0,0,"")),
                Deck(M("enemy")),new FusionCatalog(),book);
            for (int slot = 0; slot < 3; slot++)
            {
                Assert.IsTrue(game.TryPlay(DuelSide.Player,new[] {0},
                    FieldLane.Monster,slot,BattlePosition.Attack,
                    GuardianStar.None,slot == 0 && firstFaceDown,out _));
                Assert.IsTrue(game.TryEndTurn(DuelSide.Player,out _));
                Assert.IsTrue(game.TryPlay(DuelSide.Opponent,new[] {0},
                    FieldLane.Monster,slot,BattlePosition.Defense,
                    GuardianStar.None,out _));
                Assert.IsTrue(game.TryEndTurn(DuelSide.Opponent,out _));
            }
            return game;
        }

        [Test]
        public void RitualConsumesThreeSpecificFieldCardsAndAHandRitualOnly()
        {
            var game = ReadyForRitual(out var expected);
            Assert.AreEqual(expected,game.PreviewRitual(DuelSide.Player,0));
            int before = game.Events.Count;
            Assert.IsFalse(game.CanSummonRitual(DuelSide.Player,0,4,
                GuardianStar.None,out _,out _));
            Assert.AreEqual(before,game.Events.Count);
            Assert.IsTrue(game.TrySummonRitual(DuelSide.Player,0,1,
                BattlePosition.Defense,GuardianStar.None,
                out var result,out _));
            Assert.AreEqual(expected,result);
            Assert.IsNull(game.Player.Monster(0));
            Assert.AreEqual(result,game.Player.Monster(1).Card);
            Assert.IsNull(game.Player.Monster(2));
            Assert.IsFalse(game.Player.Monster(1).FaceDown);
            Assert.AreEqual(4,game.Player.Graveyard.Count);
            Assert.IsTrue(game.PlayedThisTurn);
            Assert.IsFalse(game.TrySummonRitual(DuelSide.Player,0,1,
                BattlePosition.Attack,GuardianStar.None,out _,out _));
        }

        [Test]
        public void FaceDownTributeMustBeRevealedBeforeRitual()
        {
            var game = ReadyForRitual(out _, true);
            int eventsBefore = game.Events.Count;
            Assert.IsNull(game.PreviewRitual(DuelSide.Player,0));
            Assert.IsFalse(game.TrySummonRitual(DuelSide.Player,0,1,
                BattlePosition.Attack,GuardianStar.None,out _,out _));
            Assert.AreEqual(eventsBefore,game.Events.Count);
            Assert.IsTrue(game.Player.Monster(0).FaceDown);
            Assert.IsTrue(game.TryChangePosition(DuelSide.Player,0,
                BattlePosition.Defense,out _));
            // Face-up defense is acceptable, as only facedown is disallowed.
            Assert.IsTrue(game.CanSummonRitual(DuelSide.Player,0,1,
                GuardianStar.None,out _,out _));
        }

        [Test]
        public void ThreeCopiesOfSameCardInDifferentSlotsSatisfyRitual()
        {
            var book = new RitualCatalog();
            book.Add("ritual",new[] {"blue","blue","blue"},M("ultimate"));
            var game = new DuelMatch(
                Deck(M("blue"),M("blue"),M("blue"),
                    new CardDefinition("ritual","Ritual",CardKind.Ritual,0,0,"")),
                Deck(M("enemy")),new FusionCatalog(),book);
            for (int slot=0;slot<3;slot++)
            {
                Assert.IsTrue(game.TryPlay(DuelSide.Player,new[] {0},
                    FieldLane.Monster,slot,BattlePosition.Attack,GuardianStar.None,out _));
                Assert.IsTrue(game.TryEndTurn(DuelSide.Player,out _));
                Assert.IsTrue(game.TryPlay(DuelSide.Opponent,new[] {0},
                    FieldLane.Monster,slot,BattlePosition.Defense,GuardianStar.None,out _));
                Assert.IsTrue(game.TryEndTurn(DuelSide.Opponent,out _));
            }
            Assert.IsTrue(game.TrySummonRitual(DuelSide.Player,0,2,
                BattlePosition.Attack,GuardianStar.None,out var result,out _));
            Assert.AreEqual("ultimate",result.Id);
            Assert.AreEqual(4,game.Player.Graveyard.Count);
            Assert.IsNull(game.Player.Monster(0));
            Assert.IsNull(game.Player.Monster(1));
            Assert.AreEqual("ultimate",game.Player.Monster(2).Card.Id);
        }

        [Test]
        public void RitualCatalogRejectsNonOriginalTributeDefinitions()
        {
            var book = new RitualCatalog();
            Assert.Throws<ArgumentException>(() => book.Add("invalid",
                new[] {"a","", "a"},M("monster")));
            Assert.Throws<ArgumentException>(() => book.Add("short",
                new[] {"a","b"},M("monster")));
            book.Add("r",new[] {"a","b","c"},M("monster"));
            book.Add("three-copies",new[] {"blue","blue","blue"},M("ultimate"));
            Assert.AreEqual(2,book.Count);
            Assert.IsTrue(book.TryGet("three-copies",out var threeCopies));
            Assert.AreEqual("blue",threeCopies.TributeIds[0]);
            Assert.AreEqual("blue",threeCopies.TributeIds[1]);
            Assert.AreEqual("blue",threeCopies.TributeIds[2]);
            Assert.Throws<ArgumentException>(() => book.Add("r",
                new[] {"a","b","d"},M("other")));
        }
    }
}
