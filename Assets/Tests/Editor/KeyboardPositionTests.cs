using System.Collections.Generic;
using NUnit.Framework;
using Reborn.Core;
using Reborn.Presentation;
using UnityEngine;

namespace Reborn.Tests
{
    /// <summary>
    /// The post-placement keyboard must set explicit ATK/DEF, not toggle
    /// for both keys; mouse mode retains its existing toggle button.
    /// </summary>
    public sealed class KeyboardPositionTests
    {
        private static IReadOnlyList<CardDefinition> Deck(string id)
        {
            var result = new List<CardDefinition>();
            result.Add(new CardDefinition(id, id, CardKind.Monster, 1700, 1300,
                "synthetic regression test"));
            for (int i = 1; i < DuelMatch.DeckSize; i++)
                result.Add(new CardDefinition(id + "-filler-" + i, "Filler",
                    CardKind.Monster, 800, 800, "synthetic"));
            return result;
        }

        [Test]
        public void ExplicitPositionsAreIdempotentAndDoNotRevealFaceDownCards()
        {
            var duel = new DuelMatch(Deck("my-card"), Deck("enemy-card"));
            Assert.IsTrue(duel.TryPlay(DuelSide.Player, new[] {0},
                FieldLane.Monster, 0, BattlePosition.Attack,
                GuardianStar.None, true, out var reason), reason);
            var go = new GameObject("Test_Reborn_Hud_QE");
            try
            {
                var hud = go.AddComponent<ArenaHud>();
                hud.InitializeMatch(duel);
                hud.SetDuelMode(true);
                hud.SelectedFieldMonster = 0;

                int events = duel.Events.Count;
                hud.TrySetSelectedMonsterPosition(BattlePosition.Attack);
                Assert.AreEqual(events, duel.Events.Count,
                    "Q when already in ATK must not toggle to DEF");
                Assert.AreEqual(BattlePosition.Attack, duel.Player.Monster(0).Position);

                hud.TrySetSelectedMonsterPosition(BattlePosition.Defense);
                Assert.AreEqual(BattlePosition.Defense, duel.Player.Monster(0).Position);
                Assert.IsTrue(duel.Player.Monster(0).FaceDown);

                events = duel.Events.Count;
                hud.TrySetSelectedMonsterPosition(BattlePosition.Defense);
                Assert.AreEqual(events, duel.Events.Count,
                    "E when already in DEF must not toggle to ATK");

                hud.TrySetSelectedMonsterPosition(BattlePosition.Attack);
                Assert.AreEqual(BattlePosition.Attack, duel.Player.Monster(0).Position);
                Assert.IsTrue(duel.Player.Monster(0).FaceDown,
                    "Changing stance cannot reveal a hidden monster");

                hud.TryChangeSelectedMonster(); // Existing mouse toggle
                Assert.AreEqual(BattlePosition.Defense, duel.Player.Monster(0).Position);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
