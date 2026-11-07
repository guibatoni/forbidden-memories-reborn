using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Reborn.Core;

namespace Reborn.Tests
{
    public sealed class DuelMatchTests
    {
        private static CardDefinition Monster(string id, int atk, int def,
            GuardianStar first = GuardianStar.None, GuardianStar second = GuardianStar.None)
        {
            return new CardDefinition(id, "Test " + id, CardKind.Monster, atk, def,
                "Prototype test card", first, second);
        }

        private static CardDefinition Support(string id, CardKind kind = CardKind.Spell)
        {
            return new CardDefinition(id, "Test " + id, kind, 0, 0, "Placeholder");
        }

        private static IReadOnlyList<CardDefinition> Deck(params CardDefinition[] first)
        {
            var cards = new List<CardDefinition>(first);
            while (cards.Count < DuelMatch.DeckSize)
                cards.Add(Monster("extra-" + cards.Count, 1000, 1000));
            return cards;
        }

        private static DuelMatch Match(CardDefinition my, CardDefinition enemy)
        {
            return new DuelMatch(Deck(my), Deck(enemy));
        }

        private static void Play(DuelMatch match, DuelSide side, int slot = 0,
            BattlePosition position = BattlePosition.Attack, GuardianStar star = GuardianStar.None)
        {
            Assert.IsTrue(match.TryPlay(side, new [] { 0 }, FieldLane.Monster,
                slot, position, star, out var reason), reason);
        }

        private static void Begin(DuelMatch match, DuelSide side)
        {
            Assert.IsTrue(match.TryBeginBattle(side, out var error), error);
        }

        private static void End(DuelMatch match, DuelSide side)
        {
            Assert.IsTrue(match.TryEndTurn(side, out var error), error);
        }

        private static DuelMatch WithOpponentOnField(
            CardDefinition my, CardDefinition enemy,
            BattlePosition enemyPosition = BattlePosition.Attack,
            GuardianStar myStar = GuardianStar.None,
            GuardianStar opponentStar = GuardianStar.None)
        {
            var match = Match(my, enemy);
            Play(match, DuelSide.Player, 0, BattlePosition.Attack, myStar);
            End(match, DuelSide.Player);
            Play(match, DuelSide.Opponent, 0, enemyPosition, opponentStar);
            End(match, DuelSide.Opponent);
            return match;
        }

        [Test]
        public void AiPlanningDoesNotMutateStateAndRequiresOpponentTurn()
        {
            var match = new DuelMatch(Deck(Monster("mine",1200,900)),
                Deck(Monster("weak",1300,900),Monster("strong",2600,2000)));
            long count = match.Events.Count;
            Assert.IsFalse(SimpleOpponentAi.TryChoosePlay(match,out _,out _));
            Assert.AreEqual(count,match.Events.Count);
            End(match,DuelSide.Player);
            count = match.Events.Count;
            Assert.IsTrue(SimpleOpponentAi.TryChoosePlay(match,out int hand,out int slot));
            Assert.AreEqual("strong",match.Opponent.Hand[hand].Id);
            Assert.AreEqual(0,slot);
            Assert.AreEqual(count,match.Events.Count);
            Assert.IsNull(match.Opponent.Monster(0));
        }

        [Test]
        public void AiHasNoSummonWhenOpponentHandHasNoMonster()
        {
            var match = new DuelMatch(Deck(Monster("p",1000,900)),
                Deck(Support("s1"),Support("s2"),Support("s3"),
                    Support("s4"),Support("s5")));
            End(match,DuelSide.Player);
            Assert.IsFalse(SimpleOpponentAi.TryChoosePlay(match,out _,out _));
            Assert.IsFalse(match.PlayedThisTurn);
        }

        [Test]
        public void AiChoosesAttackerAndWeakestValidTargetWithoutMutation()
        {
            var match = Match(Monster("defender",1100,1000), Monster("attacker",1800,1200));
            Play(match,DuelSide.Player,2);
            End(match,DuelSide.Player);
            Play(match,DuelSide.Opponent,4);
            Begin(match,DuelSide.Opponent);
            int oldLife = match.Player.Life;
            Assert.IsTrue(SimpleOpponentAi.TryChooseAttack(match,out int attacker,out int target));
            Assert.AreEqual(4,attacker);
            Assert.AreEqual(2,target);
            Assert.AreEqual(oldLife,match.Player.Life);
            Assert.IsFalse(match.Opponent.Monster(4).AttackedThisTurn);
            Assert.IsTrue(match.TryAttack(DuelSide.Opponent,attacker,target,out _));
            Assert.IsFalse(SimpleOpponentAi.TryChooseAttack(match,out _,out _));
        }

        [Test]
        public void AiSelectsDirectAttackOnlyWhenEnemyHasNoMonster()
        {
            var match = Match(Monster("p",1200,1000), Monster("o",1800,1000));
            End(match,DuelSide.Player);
            Play(match,DuelSide.Opponent,0);
            Begin(match,DuelSide.Opponent);
            Assert.IsTrue(SimpleOpponentAi.TryChooseAttack(match,out int from,out int to));
            Assert.AreEqual(0,from);
            Assert.AreEqual(-1,to);
        }

        [Test]
        public void StartsWithTwoDistinctHandsAndFixedField()
        {
            var match = Match(Monster("p",1500,1000), Monster("o",1300,900));
            Assert.AreEqual(DuelSide.Player, match.CurrentSide);
            Assert.AreEqual(5,match.Player.Hand.Count);
            Assert.AreEqual(5,match.Opponent.Hand.Count);
            Assert.AreEqual(35,match.Player.DeckCount);
            Assert.AreEqual(35,match.Opponent.DeckCount);
            Assert.AreEqual(8000,match.Player.Life);
            Assert.AreEqual(8000,match.Opponent.Life);
            Assert.IsNull(match.Player.Monster(4));
            Assert.IsNull(match.Opponent.Support(4));
        }

        [Test]
        public void RejectsDeckOtherThan40Cards()
        {
            Assert.Throws<ArgumentException>(() =>
                new DuelMatch(new [] { Monster("p",1000,1000) }, Deck(Monster("o",1000,1000))));
        }

        [Test]
        public void RejectsNullCardsInDeck()
        {
            var deck = Deck(Monster("p",1000,1000)).ToArray();
            deck[12] = null;
            Assert.Throws<ArgumentException>(() => new DuelMatch(deck,Deck(Monster("o",1000,1000))));
        }

        [Test]
        public void RejectsOpponentPlayingOnPlayersTurnWithoutMutation()
        {
            var match = Match(Monster("p",1000,1000),Monster("o",1000,1000));
            Assert.IsFalse(match.TryPlay(DuelSide.Opponent,new [] { 0 },FieldLane.Monster,0,
                BattlePosition.Attack,GuardianStar.None,out _));
            Assert.AreEqual(5,match.Opponent.Hand.Count);
            Assert.IsNull(match.Opponent.Monster(0));
        }

        [Test]
        public void OnePlayPerTurnIsEnforced()
        {
            var match = Match(Monster("p",1500,1000),Monster("o",1000,1000));
            Play(match,DuelSide.Player);
            Assert.IsFalse(match.TryPlay(DuelSide.Player,new [] { 0 },FieldLane.Monster,1,
                BattlePosition.Attack,GuardianStar.None,out _));
            Assert.AreEqual(4,match.Player.Hand.Count);
        }

        [Test]
        public void RejectsOutOfRangeSlotsWithoutConsumingHand()
        {
            var match = Match(Monster("p",1500,1000),Monster("o",1000,1000));
            foreach (var i in new [] { -1, 5, 99 })
                Assert.IsFalse(match.TryPlay(DuelSide.Player,new [] { 0 },FieldLane.Monster,i,
                    BattlePosition.Attack,GuardianStar.None,out _));
            Assert.AreEqual(5,match.Player.Hand.Count);
        }

        [Test]
        public void RejectsCardInWrongLane()
        {
            var match = Match(Monster("p",1500,1000),Monster("o",1000,1000));
            Assert.IsFalse(match.TryPlay(DuelSide.Player,new [] { 0 },FieldLane.Support,0,
                BattlePosition.Attack,GuardianStar.None,out _));
            Assert.IsNull(match.Player.Support(0));
        }

        [Test]
        public void SupportCardsUseSupportLaneWithoutFakeEffect()
        {
            var match = Match(Support("magic"),Monster("o",1000,1000));
            Assert.IsTrue(match.TryPlay(DuelSide.Player,new [] { 0 },FieldLane.Support,2,
                BattlePosition.Attack,GuardianStar.None,out _));
            Assert.AreEqual("magic",match.Player.Support(2).Id);
            Assert.AreEqual(8000,match.Opponent.Life);
        }

        [Test]
        public void InvalidIndicesAndDuplicatesAreAtomic()
        {
            var match = Match(Monster("p",1800,1200),Monster("o",1000,1000));
            Assert.IsFalse(match.TryPlay(DuelSide.Player,new [] { 0,0 },FieldLane.Monster,0,
                BattlePosition.Attack,GuardianStar.None,out _));
            Assert.IsFalse(match.TryPlay(DuelSide.Player,new [] { 0,5 },FieldLane.Monster,0,
                BattlePosition.Attack,GuardianStar.None,out _));
            Assert.IsFalse(match.TryPlay(DuelSide.Player,new int[0],FieldLane.Monster,0,
                BattlePosition.Attack,GuardianStar.None,out _));
            Assert.AreEqual(5,match.Player.Hand.Count);
            Assert.IsNull(match.Player.Monster(0));
        }

        [Test]
        public void GuardianStarHasTwoIndependentCycles()
        {
            var ring = new [] { GuardianStar.Mercury,GuardianStar.Sun,GuardianStar.Moon,
                GuardianStar.Venus };
            for (int i=0;i<ring.Length;i++)
                Assert.IsTrue(GuardianStarRules.HasAdvantage(ring[i],ring[(i+1)%ring.Length]));
            var other = new [] { GuardianStar.Mars,GuardianStar.Jupiter,GuardianStar.Saturn,
                GuardianStar.Uranus,GuardianStar.Pluto,GuardianStar.Neptune };
            for (int i=0;i<other.Length;i++)
                Assert.IsTrue(GuardianStarRules.HasAdvantage(other[i],other[(i+1)%other.Length]));
            Assert.IsFalse(GuardianStarRules.HasAdvantage(GuardianStar.Mars,GuardianStar.Sun));
            Assert.IsFalse(GuardianStarRules.HasAdvantage(GuardianStar.None,GuardianStar.Sun));
            Assert.AreEqual(500,GuardianStarRules.Bonus(GuardianStar.Sun,GuardianStar.Moon));
        }

        [Test]
        public void RefusesGuardianStarOutsideTheCardsTwoChoices()
        {
            var match = Match(Monster("p",1800,1300,GuardianStar.Mercury,GuardianStar.Mars),
                Monster("o",1000,1000));
            Assert.IsFalse(match.TryPlay(DuelSide.Player,new [] { 0 },FieldLane.Monster,0,
                BattlePosition.Attack,GuardianStar.Moon,out _));
            Assert.IsFalse(match.TryPlay(DuelSide.Player,new [] { 0 },FieldLane.Monster,0,
                BattlePosition.Attack,GuardianStar.None,out _));
            Assert.AreEqual(5,match.Player.Hand.Count);
        }

        [Test]
        public void UnknownGuardianStarsGiveNoUnverifiedBonus()
        {
            Assert.AreEqual(0,GuardianStarRules.Bonus(GuardianStar.None,GuardianStar.Moon));
            Assert.IsTrue(GuardianStarRules.IsAvailable(Monster("x",1000,1000),GuardianStar.None));
        }

        [Test]
        public void DirectAttackDeductsLifeWhenEnemyHasNoMonsters()
        {
            var match = Match(Monster("p",1700,1000),Monster("o",1000,1000));
            Play(match,DuelSide.Player);
            Begin(match,DuelSide.Player);
            Assert.IsTrue(match.TryAttack(DuelSide.Player,0,-1,out _));
            Assert.AreEqual(6300,match.Opponent.Life);
            Assert.IsNotNull(match.Player.Monster(0));
        }

        [Test]
        public void DirectAttackIsBlockedByEnemyMonster()
        {
            var match = WithOpponentOnField(Monster("p",1800,1000),Monster("o",1100,1000));
            Begin(match,DuelSide.Player);
            Assert.IsFalse(match.TryAttack(DuelSide.Player,0,-1,out _));
            Assert.IsFalse(match.TryAttack(DuelSide.Player,0,4,out _));
            Assert.AreEqual(8000,match.Opponent.Life);
        }

        [Test]
        public void GuardianBonusDeterminesAttackVsAttackOutcome()
        {
            var match = WithOpponentOnField(
                Monster("p",2000,1000,GuardianStar.Mercury),
                Monster("o",2200,1000,GuardianStar.Sun),
                BattlePosition.Attack,GuardianStar.Mercury,GuardianStar.Sun);
            Begin(match,DuelSide.Player);
            Assert.IsTrue(match.TryAttack(DuelSide.Player,0,0,out _));
            Assert.AreEqual(7700,match.Opponent.Life);
            Assert.IsNull(match.Opponent.Monster(0));
            Assert.AreEqual("o",match.Opponent.Graveyard.Last().Id);
            Assert.AreEqual(2000,match.Player.Monster(0).Card.Attack); // Non-permanent bonus
        }

        [Test]
        public void GuardianDisadvantageCanDestroyAttacker()
        {
            var match = WithOpponentOnField(
                Monster("p",2000,1000,GuardianStar.Sun),
                Monster("o",1800,1000,GuardianStar.Mercury),
                BattlePosition.Attack,GuardianStar.Sun,GuardianStar.Mercury);
            Begin(match,DuelSide.Player);
            Assert.IsTrue(match.TryAttack(DuelSide.Player,0,0,out _));
            Assert.IsNull(match.Player.Monster(0));
            Assert.AreEqual(7700,match.Player.Life);
        }

        [Test]
        public void AttackBeatsDefenseWithoutLifeLoss()
        {
            var match = WithOpponentOnField(Monster("p",2300,1000),
                Monster("o",1200,1700),BattlePosition.Defense);
            Begin(match,DuelSide.Player);
            Assert.IsTrue(match.TryAttack(DuelSide.Player,0,0,out _));
            Assert.IsNull(match.Opponent.Monster(0));
            Assert.AreEqual(8000,match.Opponent.Life);
        }

        [Test]
        public void DefenseWithHigherPointsDamagesAttackerButDoesNotDestroyIt()
        {
            var match = WithOpponentOnField(Monster("p",1500,1000),
                Monster("o",500,2000),BattlePosition.Defense);
            Begin(match,DuelSide.Player);
            Assert.IsTrue(match.TryAttack(DuelSide.Player,0,0,out _));
            Assert.IsNotNull(match.Player.Monster(0));
            Assert.AreEqual(7500,match.Player.Life);
        }

        [Test]
        public void EqualDefenseDoesNotDestroyAnyMonster()
        {
            var match = WithOpponentOnField(Monster("p",2000,1000),
                Monster("o",1200,2000),BattlePosition.Defense);
            Begin(match,DuelSide.Player);
            Assert.IsTrue(match.TryAttack(DuelSide.Player,0,0,out _));
            Assert.IsNotNull(match.Player.Monster(0));
            Assert.IsNotNull(match.Opponent.Monster(0));
            Assert.AreEqual(8000,match.Player.Life);
            Assert.AreEqual(8000,match.Opponent.Life);
        }

        [Test]
        public void EqualAttackDestroysBothMonsters()
        {
            var match = WithOpponentOnField(Monster("p",1800,1000),Monster("o",1800,1000));
            Begin(match,DuelSide.Player);
            Assert.IsTrue(match.TryAttack(DuelSide.Player,0,0,out _));
            Assert.IsNull(match.Player.Monster(0));
            Assert.IsNull(match.Opponent.Monster(0));
            Assert.AreEqual(8000,match.Player.Life);
            Assert.AreEqual(8000,match.Opponent.Life);
        }

        [Test]
        public void CannotAttackBeforeBattlePhase()
        {
            var match = Match(Monster("p",1800,1000),Monster("o",1000,1000));
            Play(match,DuelSide.Player);
            Assert.IsFalse(match.TryAttack(DuelSide.Player,0,-1,out _));
            Assert.AreEqual(8000,match.Opponent.Life);
        }

        [Test]
        public void DefensePositionCannotAttack()
        {
            var match = Match(Monster("p",1800,1000),Monster("o",1000,1000));
            Play(match,DuelSide.Player,0,BattlePosition.Defense);
            Begin(match,DuelSide.Player);
            Assert.IsFalse(match.TryAttack(DuelSide.Player,0,-1,out _));
            Assert.AreEqual(8000,match.Opponent.Life);
        }

        [Test]
        public void SameMonsterCannotAttackTwice()
        {
            var match = Match(Monster("p",1800,1000),Monster("o",1000,1000));
            Play(match,DuelSide.Player); Begin(match,DuelSide.Player);
            Assert.IsTrue(match.TryAttack(DuelSide.Player,0,-1,out _));
            Assert.IsFalse(match.TryAttack(DuelSide.Player,0,-1,out _));
            Assert.AreEqual(6200,match.Opponent.Life);
        }

        [Test]
        public void TurnChangesSideAndRefillsOnlyWhenPlayerStartsTheirTurn()
        {
            var match = Match(Monster("p",1800,1000),Monster("o",1000,1000));
            Play(match,DuelSide.Player);
            Assert.AreEqual(4,match.Player.Hand.Count);
            End(match,DuelSide.Player);
            Assert.AreEqual(DuelSide.Opponent,match.CurrentSide);
            Assert.AreEqual(4,match.Player.Hand.Count);
            End(match,DuelSide.Opponent);
            Assert.AreEqual(DuelSide.Player,match.CurrentSide);
            Assert.AreEqual(5,match.Player.Hand.Count);
            Assert.AreEqual(34,match.Player.DeckCount);
            Assert.AreEqual(3,match.Turn);
        }

        [Test]
        public void CannotPlayAfterBeginningBattle()
        {
            var match = Match(Monster("p",1800,1000),Monster("o",1000,1000));
            Begin(match,DuelSide.Player);
            Assert.IsFalse(match.TryPlay(DuelSide.Player,new [] {0},FieldLane.Monster,0,
                BattlePosition.Attack,GuardianStar.None,out _));
        }

        [Test]
        public void OnlyImportedFusionRecipesAreExecuted()
        {
            var a=Monster("A",1000,1000);var b=Monster("B",1200,1200);
            var fusion=Monster("F",3000,2000,GuardianStar.Sun);
            var catalog=new FusionCatalog(); catalog.Add("A","B",fusion);
            var match=new DuelMatch(Deck(a,b),Deck(Monster("o",1000,1000)),catalog);
            Assert.AreEqual("F",match.PreviewResult(DuelSide.Player,new [] {0,1}).Id);
            Assert.IsTrue(match.TryPlay(DuelSide.Player,new [] {0,1},FieldLane.Monster,1,
                BattlePosition.Attack,GuardianStar.Sun,out _));
            Assert.AreEqual("F",match.Player.Monster(1).Card.Id);
            Assert.AreEqual(3,match.Player.Hand.Count);
            Assert.AreEqual(2,match.Player.Graveyard.Count);
            Assert.AreEqual(1,match.Events.Count(e=>e.Kind==MatchEventKind.Fusion));
        }

        [Test]
        public void UnknownFusionRetainsLastCardAndBuriesPreviousPhysicalMaterial()
        {
            var a=Monster("A",1000,1000);var b=Monster("B",1200,1200);
            var match=new DuelMatch(Deck(a,b),Deck(Monster("o",1000,1000)));
            Assert.AreEqual("B",match.PreviewResult(DuelSide.Player,new [] {0,1}).Id);
            Assert.IsTrue(match.TryPlay(DuelSide.Player,new [] {0,1},FieldLane.Monster,1,
                BattlePosition.Attack,GuardianStar.None,out _));
            Assert.AreEqual("B",match.Player.Monster(1).Card.Id);
            Assert.AreEqual(1,match.Player.Graveyard.Count);
            Assert.AreEqual("A",match.Player.Graveyard[0].Id);
            Assert.AreEqual(1,match.Events.Count(e=>e.Kind==MatchEventKind.FusionFailed));
        }

        [Test]
        public void ThreeCardChainDoesNotBuryVirtualFusionCard()
        {
            var a=Monster("A",1000,1000);var b=Monster("B",1100,1000);var c=Monster("C",500,1000);
            var f=Monster("F",2200,2000);
            var catalog=new FusionCatalog();catalog.Add("A","B",f);
            var match=new DuelMatch(Deck(a,b,c),Deck(Monster("o",1000,1000)),catalog);
            Assert.IsTrue(match.TryPlay(DuelSide.Player,new [] {0,1,2},FieldLane.Monster,1,
                BattlePosition.Attack,GuardianStar.None,out _));
            Assert.AreEqual("C",match.Player.Monster(1).Card.Id);
            Assert.AreEqual(2,match.Player.Graveyard.Count);
            Assert.IsFalse(match.Player.Graveyard.Any(x=>x.Id=="F"));
        }

        [Test]
        public void DuplicateFusionRecipeIsRejected()
        {
            var catalog=new FusionCatalog();var c=Monster("c",1000,1000);
            catalog.Add("a","b",c);
            Assert.Throws<ArgumentException>(()=>catalog.Add("b","a",c));
        }

        [Test]
        public void SimpleOpponentAiCompletesAnOpponentTurn()
        {
            var match=Match(Monster("p",500,500),Monster("o",1900,900));
            End(match,DuelSide.Player);
            SimpleOpponentAi.TakeTurn(match);
            Assert.AreEqual(DuelSide.Player,match.CurrentSide);
            Assert.AreEqual(6100,match.Player.Life);
            Assert.AreEqual(3,match.Turn);
            Assert.IsNotNull(match.Opponent.Monster(0));
        }

        [Test]
        public void SimpleOpponentAiDoesNotActOnWrongTurn()
        {
            var match=Match(Monster("p",500,500),Monster("o",1900,900));
            SimpleOpponentAi.TakeTurn(match);
            Assert.AreEqual(DuelSide.Player,match.CurrentSide);
            Assert.IsNull(match.Opponent.Monster(0));
        }

        [Test]
        public void DirectDamageCanFinishMatchAndBlockFurtherChanges()
        {
            var match=Match(Monster("p",8500,500),Monster("o",500,500));
            Play(match,DuelSide.Player);
            Begin(match,DuelSide.Player);
            Assert.IsTrue(match.TryAttack(DuelSide.Player,0,-1,out _));
            Assert.AreEqual(MatchPhase.Finished,match.Phase);
            Assert.AreEqual(DuelSide.Player,match.Winner);
            Assert.AreEqual(0,match.Opponent.Life);
            Assert.IsFalse(match.TryEndTurn(DuelSide.Player,out _));
        }

        [Test]
        public void MatchEventsHaveStrictlyIncreasingSequenceNumbers()
        {
            var match=Match(Monster("p",1800,1000),Monster("o",1000,1000));
            Play(match,DuelSide.Player); Begin(match,DuelSide.Player);
            match.TryAttack(DuelSide.Player,0,-1,out _);
            var sequences=match.Events.Select(e=>e.Sequence).ToArray();
            Assert.AreEqual(sequences.Distinct().Count(),sequences.Length);
            Assert.IsTrue(sequences.SequenceEqual(sequences.OrderBy(x=>x)));
        }

        [Test]
        public void MatchStateDoesNotDependOnRendering()
        {
            var match=Match(Monster("p",1800,1000),Monster("o",1000,1000));
            int changes=0;
            match.Changed += ()=>changes++;
            Play(match,DuelSide.Player); Begin(match,DuelSide.Player);
            match.TryAttack(DuelSide.Player,0,-1,out _);
            Assert.GreaterOrEqual(changes,3);
            Assert.AreEqual(6200,match.Opponent.Life);
        }
    }
}