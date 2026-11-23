using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Reborn.Core;

namespace Reborn.Tests
{
    /// <summary>
    /// 0.30 regression tests for the ordered material chain, following the PS1
    /// placement routine (fusion, then in-chain Equip, then the failure rule
    /// that keeps a monster when it meets an unmatched non-monster).
    /// Synthetic IDs test the RULES only; no fusion recipe is claimed to be an
    /// original one. "657" is Megamorph, the verified universal Equip in
    /// VerifiedEquipCatalog.OriginalPs1Foundation (+1000/+1000).
    /// </summary>
    public sealed class FusionChainRulesTests
    {
        private static CardDefinition Monster(string id, int atk, int def,
            GuardianStar first = GuardianStar.None, GuardianStar second = GuardianStar.None)
        {
            return new CardDefinition(id, "Test " + id, CardKind.Monster, atk, def,
                "Chain test card", first, second);
        }

        private static CardDefinition Card(string id, CardKind kind)
        {
            return new CardDefinition(id, "Test " + id, kind, 0, 0, "Chain test card");
        }

        private static CardDefinition Megamorph()
        {
            return new CardDefinition("657", "Megamorph", CardKind.Equip, 0, 0, "");
        }

        private static IReadOnlyList<CardDefinition> Deck(params CardDefinition[] first)
        {
            var cards = new List<CardDefinition>(first);
            while (cards.Count < DuelMatch.DeckSize)
                cards.Add(Monster("extra-" + cards.Count, 1000, 1000));
            return cards;
        }

        private static DuelMatch Duel(FusionCatalog recipes,
            params CardDefinition[] playerFirst)
        {
            return new DuelMatch(Deck(playerFirst), Deck(Monster("enemy", 900, 900)),
                recipes, null, null, VerifiedEquipCatalog.OriginalPs1Foundation());
        }

        private static void PassFirstRound(DuelMatch match)
        {
            Assert.IsTrue(match.TryPlay(DuelSide.Player, new[] { 0 }, FieldLane.Monster,
                0, BattlePosition.Attack, GuardianStar.None, out var reason), reason);
            Assert.IsTrue(match.TryEndTurn(DuelSide.Player, out reason), reason);
            Assert.IsTrue(match.TryPlay(DuelSide.Opponent, new[] { 0 }, FieldLane.Monster,
                0, BattlePosition.Attack, GuardianStar.None, out reason), reason);
            Assert.IsTrue(match.TryEndTurn(DuelSide.Opponent, out reason), reason);
        }

        // ---------- Pure resolver ----------

        [Test]
        public void ResolverFusesVerifiedPairInEitherOrder()
        {
            var a = Monster("A", 1000, 1000);
            var b = Monster("B", 1100, 900);
            var f = Monster("F", 2500, 2000);
            var recipes = new FusionCatalog();
            recipes.Add("A", "B", f);
            var forward = FusionChainResolver.Resolve(new[] { a, b }, recipes, null);
            var backward = FusionChainResolver.Resolve(new[] { b, a }, recipes, null);
            Assert.AreSame(f, forward.Result);
            Assert.AreSame(f, backward.Result);
            Assert.IsTrue(forward.FinalIsFusion);
            Assert.AreEqual(1, forward.SuccessfulFusions);
        }

        [Test]
        public void ResolverTwoUnmatchedMonstersKeepTheLaterOne()
        {
            var a = Monster("A", 1000, 1000);
            var b = Monster("B", 1100, 900);
            var chain = FusionChainResolver.Resolve(new[] { a, b }, new FusionCatalog(), null);
            Assert.AreSame(b, chain.Result);
            Assert.AreEqual(1, chain.SurvivorPosition);
            Assert.AreEqual(FusionStepKind.DiscardedCurrent, chain.Steps[0].Kind);
            Assert.AreEqual("A", chain.Steps[0].CardId);
        }

        [Test]
        public void ResolverMonsterSurvivesUnmatchedNonMonster()
        {
            var a = Monster("A", 1000, 1000);
            foreach (var kind in new[] { CardKind.Spell, CardKind.Trap,
                CardKind.Equip, CardKind.Ritual })
            {
                var support = Card("S-" + kind, kind);
                var chain = FusionChainResolver.Resolve(new[] { a, support },
                    new FusionCatalog(), new VerifiedEquipCatalog());
                Assert.AreSame(a, chain.Result, kind.ToString());
                Assert.AreEqual(0, chain.SurvivorPosition, kind.ToString());
                Assert.AreEqual(FusionStepKind.DiscardedNext, chain.Steps[0].Kind);
            }
        }

        [Test]
        public void ResolverNonMonsterFirstIsDiscardedForTheMonster()
        {
            var spell = Card("S", CardKind.Spell);
            var a = Monster("A", 1000, 1000);
            var chain = FusionChainResolver.Resolve(new[] { spell, a },
                new FusionCatalog(), null);
            Assert.AreSame(a, chain.Result);
            Assert.AreEqual(1, chain.SurvivorPosition);
        }

        [Test]
        public void ResolverAppliesVerifiedEquipInBothOrders()
        {
            var a = Monster("A", 1200, 1000);
            var equips = VerifiedEquipCatalog.OriginalPs1Foundation();
            var after = FusionChainResolver.Resolve(new[] { a, Megamorph() }, null, equips);
            var before = FusionChainResolver.Resolve(new[] { Megamorph(), a }, null, equips);
            Assert.AreSame(a, after.Result);
            Assert.AreEqual(0, after.SurvivorPosition);
            Assert.AreEqual(1000, after.PendingAttackGain);
            Assert.AreEqual(1000, after.PendingDefenseGain);
            Assert.AreSame(a, before.Result);
            Assert.AreEqual(1, before.SurvivorPosition);
            Assert.AreEqual(1000, before.PendingAttackGain);
            Assert.IsTrue(after.HasProductiveStep);
            Assert.AreEqual(0, after.SuccessfulFusions);
        }

        [Test]
        public void ResolverUnverifiedEquipIsDiscardedNotApplied()
        {
            var a = Monster("A", 1200, 1000);
            var unknown = Card("999", CardKind.Equip);
            var chain = FusionChainResolver.Resolve(new[] { a, unknown }, null,
                VerifiedEquipCatalog.OriginalPs1Foundation());
            Assert.AreSame(a, chain.Result);
            Assert.AreEqual(0, chain.PendingAttackGain);
            Assert.AreEqual(FusionStepKind.DiscardedNext, chain.Steps[0].Kind);
            Assert.IsFalse(chain.HasProductiveStep);
        }

        [Test]
        public void ResolverFusionResetsEarlierEquipButLaterEquipStays()
        {
            var a = Monster("A", 1000, 1000);
            var b = Monster("B", 1100, 900);
            var f = Monster("F", 2500, 2000);
            var recipes = new FusionCatalog();
            recipes.Add("A", "B", f);
            var equips = VerifiedEquipCatalog.OriginalPs1Foundation();
            var lost = FusionChainResolver.Resolve(new[] { a, Megamorph(), b }, recipes, equips);
            Assert.AreSame(f, lost.Result);
            Assert.AreEqual(0, lost.PendingAttackGain);
            var kept = FusionChainResolver.Resolve(new[] { a, b, Megamorph() }, recipes, equips);
            Assert.AreSame(f, kept.Result);
            Assert.IsTrue(kept.FinalIsFusion);
            Assert.AreEqual(1000, kept.PendingAttackGain);
        }

        // ---------- Duel integration: hand ----------

        [Test]
        public void HandMonsterPlusUnmatchedSpellSummonsMonsterAndBuriesSpell()
        {
            var a = Monster("A", 1000, 1000);
            var spell = Card("S", CardKind.Spell);
            var match = Duel(new FusionCatalog(), a, spell);
            Assert.AreSame(a, match.PreviewResult(DuelSide.Player, new[] { 0, 1 }));
            Assert.IsTrue(match.TryPlay(DuelSide.Player, new[] { 0, 1 }, FieldLane.Monster,
                0, BattlePosition.Attack, GuardianStar.None, out var reason), reason);
            Assert.AreEqual("A", match.Player.Monster(0).Card.Id);
            Assert.AreEqual(1, match.Player.Graveyard.Count);
            Assert.AreEqual("S", match.Player.Graveyard[0].Id);
            Assert.AreEqual(3, match.Player.Hand.Count);
        }

        [Test]
        public void HandMonsterPlusMegamorphSummonsEquippedMonster()
        {
            var a = Monster("A", 1200, 1000);
            var match = Duel(new FusionCatalog(), a, Megamorph());
            Assert.IsTrue(match.TryPlay(DuelSide.Player, new[] { 0, 1 }, FieldLane.Monster,
                2, BattlePosition.Attack, GuardianStar.None, out var reason), reason);
            var placed = match.Player.Monster(2);
            Assert.AreEqual("A", placed.Card.Id);
            Assert.AreEqual(2200, placed.EffectiveAttack);
            Assert.AreEqual(2000, placed.EffectiveDefense);
            Assert.AreEqual(1, placed.EquipCount);
            Assert.AreEqual("657", match.Player.Graveyard.Single().Id);
            Assert.AreEqual(1, match.Events.Count(e => e.Kind == MatchEventKind.Equipped));
            Assert.AreEqual(0, match.Events.Count(e => e.Kind == MatchEventKind.FusionFailed));
        }

        [Test]
        public void HandFusionThenMegamorphKeepsBonusOnVirtualResult()
        {
            var a = Monster("A", 1000, 1000);
            var b = Monster("B", 1100, 900);
            var f = Monster("F", 2500, 2000);
            var recipes = new FusionCatalog();
            recipes.Add("A", "B", f);
            var match = Duel(recipes, a, b, Megamorph());
            Assert.AreEqual(1, match.CountHandFusionSteps(DuelSide.Player, new[] { 0, 1, 2 }));
            Assert.IsTrue(match.TryPlay(DuelSide.Player, new[] { 0, 1, 2 }, FieldLane.Monster,
                0, BattlePosition.Attack, GuardianStar.None, out var reason), reason);
            Assert.AreEqual("F", match.Player.Monster(0).Card.Id);
            Assert.AreEqual(3500, match.Player.Monster(0).EffectiveAttack);
            CollectionAssert.AreEquivalent(new[] { "A", "B", "657" },
                match.Player.Graveyard.Select(c => c.Id).ToArray());
        }

        // ---------- Duel integration: field ----------

        [Test]
        public void FieldMonsterSurvivesUnmatchedSpellAndStaysInPlace()
        {
            var a = Monster("A", 1000, 1000);
            var spell = Card("S", CardKind.Spell);
            var match = Duel(new FusionCatalog(), a, spell);
            PassFirstRound(match);
            var before = match.Player.Monster(0);
            Assert.IsFalse(match.FinalFieldResultIsFusion(DuelSide.Player, new[] { 0 }, 0));
            Assert.AreEqual(0, match.PreviewFieldChain(DuelSide.Player, new[] { 0 }, 0)
                .SurvivorPosition);
            Assert.IsTrue(match.TryPlayOnField(DuelSide.Player, new[] { 0 }, 0,
                BattlePosition.Defense, GuardianStar.None, true,
                out var result, out bool fused, out var reason), reason);
            Assert.IsFalse(fused);
            Assert.AreSame(a, result);
            Assert.AreSame(before, match.Player.Monster(0));
            Assert.AreEqual(BattlePosition.Attack, match.Player.Monster(0).Position);
            Assert.AreEqual("S", match.Player.Graveyard.Single().Id);
            Assert.IsTrue(match.PlayedThisTurn);
        }

        [Test]
        public void FieldMonsterReceivesMegamorphSelectedAsChainMaterial()
        {
            var a = Monster("A", 1200, 1000);
            var match = Duel(new FusionCatalog(), a, Megamorph());
            PassFirstRound(match);
            Assert.IsTrue(match.TryPlayOnField(DuelSide.Player, new[] { 0 }, 0,
                BattlePosition.Attack, GuardianStar.None, false,
                out _, out bool fused, out var reason), reason);
            Assert.IsFalse(fused);
            Assert.AreEqual("A", match.Player.Monster(0).Card.Id);
            Assert.AreEqual(2200, match.Player.Monster(0).EffectiveAttack);
            Assert.AreEqual("657", match.Player.Graveyard.Single().Id);
        }

        [Test]
        public void FieldFusionStillReplacesAndBuriesBothMaterials()
        {
            var a = Monster("A", 1000, 1000);
            var b = Monster("B", 1100, 900);
            var f = Monster("F", 2500, 2000);
            var recipes = new FusionCatalog();
            recipes.Add("A", "B", f);
            var match = Duel(recipes, a, b);
            PassFirstRound(match);
            Assert.IsTrue(match.TryPlayOnField(DuelSide.Player, new[] { 0 }, 0,
                BattlePosition.Attack, GuardianStar.None, true,
                out var result, out bool fused, out var reason), reason);
            Assert.IsTrue(fused);
            Assert.AreSame(f, result);
            Assert.IsFalse(match.Player.Monster(0).FaceDown);
            CollectionAssert.AreEquivalent(new[] { "A", "B" },
                match.Player.Graveyard.Select(c => c.Id).ToArray());
        }
    }
}
