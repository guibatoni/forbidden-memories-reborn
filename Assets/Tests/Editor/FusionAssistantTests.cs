using System;
using System.Collections.Generic;
using NUnit.Framework;
using Reborn.Core;
using Reborn.Presentation;
using UnityEngine;

namespace Reborn.Tests
{
    public sealed class FusionAssistantTests
    {
        private static CardDefinition Monster(string id, int atk = 1000)
        {
            return new CardDefinition(id, id, CardKind.Monster,
                atk, 900, "synthetic unit test");
        }

        private static IReadOnlyList<CardDefinition> Deck(params CardDefinition[] first)
        {
            var cards = new List<CardDefinition>(first);
            while (cards.Count < DuelMatch.DeckSize)
                cards.Add(Monster("unit-filler-" + cards.Count));
            return cards;
        }

        [Test]
        public void FusionModalDoesNotSpendCardsAndUnblocksBeforeCallback()
        {
            var a = Monster("A");
            var b = Monster("B");
            var result = Monster("FUSED", 2200);
            var recipes = new FusionCatalog();
            recipes.Add(a.Id,b.Id,result);
            var duel = new DuelMatch(Deck(a,b),Deck(Monster("opp")),recipes);
            var gameObject = new GameObject("Test_Reborn_FusionAssistant");
            try
            {
                var hud = gameObject.AddComponent<ArenaHud>();
                hud.InitializeMatch(duel);
                hud.SetDuelMode(true);
                int initialEvents = duel.Events.Count;
                int initialHand = duel.Player.Hand.Count;
                int selected = 0;
                bool wasUnlockedInCallback = false;
                hud.FusionRequested += option =>
                {
                    selected++;
                    wasUnlockedInCallback = !hud.InputLocked &&
                        !hud.FusionAssistantOpen &&
                        option.Result.Id == result.Id &&
                        !option.UsesField;
                };

                hud.OpenFusionAssistant(null);
                Assert.IsTrue(hud.FusionAssistantOpen);
                Assert.IsTrue(hud.InputLocked,
                    "No world clicks during the selection modal");
                Assert.AreEqual(initialHand,duel.Player.Hand.Count);
                Assert.AreEqual(initialEvents,duel.Events.Count);

                hud.ConfirmFusionAssistantChoice();
                Assert.AreEqual(1,selected);
                Assert.IsTrue(wasUnlockedInCallback,
                    "UI must close before invoking the actual placement");
                Assert.AreEqual(initialHand,duel.Player.Hand.Count);
                Assert.AreEqual(initialEvents,duel.Events.Count);
                Assert.IsFalse(duel.PlayedThisTurn);

                hud.OpenFusionAssistant(null);
                Assert.IsTrue(hud.FusionAssistantOpen);
                hud.CloseFusionAssistant();
                Assert.IsFalse(hud.FusionAssistantOpen);
                Assert.AreEqual(1,selected);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void ChainedSelectionStaysReadOnlyAndKeepsFullOrderedMaterials()
        {
            var a = Monster("chain-a");
            var b = Monster("chain-b");
            var c = Monster("chain-c");
            var ab = Monster("chain-ab", 1800);
            var abc = Monster("chain-abc", 2500);
            var recipes = new FusionCatalog();
            recipes.Add(a.Id, b.Id, ab);
            recipes.Add(ab.Id, c.Id, abc);
            var duel = new DuelMatch(Deck(a,b,c),Deck(Monster("rival")),recipes);
            int initialEvents = duel.Events.Count;
            int initialHand = duel.Player.Hand.Count;
            var go = new GameObject("Test_Reborn_ChainedFusion");
            try
            {
                var hud = go.AddComponent<ArenaHud>();
                hud.InitializeMatch(duel);
                hud.SetDuelMode(true);
                AvailableFusion chosen = null;
                hud.FusionRequested += option => chosen = option;
                hud.OpenFusionAssistant(null);
                hud.MoveFusionAssistantChoice(1); // pair -> three materials
                hud.ConfirmFusionAssistantChoice();
                Assert.IsFalse(hud.FusionAssistantOpen);
                Assert.IsFalse(hud.InputLocked);
                Assert.IsNotNull(chosen);
                CollectionAssert.AreEqual(new[]{0,1,2}, chosen.HandIndices);
                Assert.AreEqual(abc.Id, chosen.Result.Id);
                Assert.AreEqual(2, chosen.FusionSteps);
                Assert.AreEqual(initialHand,duel.Player.Hand.Count);
                Assert.AreEqual(initialEvents,duel.Events.Count);
                Assert.IsFalse(duel.PlayedThisTurn);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void NoRecipeModalIsInformativeButNeverFabricatesOptions()
        {
            var duel = new DuelMatch(Deck(Monster("A"),Monster("B")),
                Deck(Monster("enemy")));
            Assert.AreEqual(0,duel.FusionRecipeCount);
            Assert.AreEqual(0,duel.FindAvailableFusions(DuelSide.Player).Count);
            var go = new GameObject("Test_Reborn_EmptyFusionAssistant");
            try
            {
                var hud = go.AddComponent<ArenaHud>();
                hud.InitializeMatch(duel);
                hud.SetDuelMode(true);
                hud.OpenFusionAssistant(null);
                Assert.IsTrue(hud.FusionAssistantOpen);
                int triggered = 0;
                hud.FusionRequested += _ => triggered++;
                hud.ConfirmFusionAssistantChoice();
                Assert.AreEqual(0,triggered);
                hud.CloseFusionAssistant();
                Assert.IsFalse(hud.InputLocked);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }
    }
}
