using System;
using System.Collections.Generic;
using Reborn.Core;
using UnityEngine;

namespace Reborn.Presentation
{
    [DefaultExecutionOrder(-500)]
    public sealed class RebornGame : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureEntryPoint()
        {
            if (FindFirstObjectByType<RebornGame>() == null)
                new GameObject("REBORN_Prototype").AddComponent<RebornGame>();
        }

        private void Start()
        {
            if (FindObjectsByType<RebornGame>(FindObjectsSortMode.None).Length > 1)
            {
                Destroy(gameObject);
                return;
            }
            GameContentBundle imported;
            string importStatus;
            bool loaded = OriginalContentLoader.TryLoad(out imported, out importStatus);
            OpponentDeckCatalog pools = null;
            if (loaded)
            {
                if (OpponentDeckLoader.TryLoad(imported, out pools, out string info))
                    Debug.Log("REBORN: " + info);
                else Debug.LogWarning("REBORN: " + info);
            }
            int selectedPoolIndex = 0;
            var initialCards = loaded
                ? DemoDeck.RandomPracticeDeck(imported.Cards, new Random(Guid.NewGuid().GetHashCode()))
                : DemoDeck.Create();
            var session = new DuelSession(DeckRandomizer.ShuffleNewMatch(initialCards));
            var arena = new ArenaFactory(transform);
            arena.Build();
            var hud = gameObject.AddComponent<ArenaHud>();
            hud.Initialize(session, arena, loaded ? imported : null, importStatus);
            var interaction = gameObject.AddComponent<ArenaInteraction>();
            interaction.Initialize(session, arena, hud);

            DuelMatch CreateExperimentalMatch()
            {
                // The player's 40-card practice deck is NOT the original starter pool.
                var playerSource = loaded
                    ? DemoDeck.RandomPracticeDeck(imported.Cards, new Random(Guid.NewGuid().GetHashCode()))
                    : DemoDeck.Create();
                var playerDeck = DeckRandomizer.ShuffleNewMatch(playerSource);
                IReadOnlyList<CardDefinition> opponentDeck;
                if (pools != null)
                {
                    var pool = pools.Pools[selectedPoolIndex];
                    opponentDeck = pool.Deal(imported.Cards, new Random(Guid.NewGuid().GetHashCode()));
                    hud.SetOpponentSource(pool.Name + " (#" + pool.Id + ")", pools.Pools.Count > 1);
                }
                else
                {
                    var source = loaded
                        ? DemoDeck.RandomPracticeDeck(imported.Cards, new Random(Guid.NewGuid().GetHashCode()))
                        : DemoDeck.Create();
                    opponentDeck = DeckRandomizer.ShuffleNewMatch(source);
                    hud.SetOpponentSource("IA demonstrativa", false);
                }
                return new DuelMatch(playerDeck, opponentDeck,
                    loaded ? imported.Fusions : DemoDeck.CreateSampleFusions());
            }

            var opponentDirector = gameObject.AddComponent<OpponentTurnDirector>();
            opponentDirector.Initialize(arena, hud);
            var playerDirector = gameObject.AddComponent<PlayerAttackDirector>();
            playerDirector.Initialize(arena, hud);
            var feedback = gameObject.AddComponent<CombatFeedbackOverlay>();
            feedback.Initialize(hud);
            hud.OpponentTurnRequested += () => opponentDirector.PlayTurn(hud.Match);
            hud.PlayerAttackRequested += (fromSlot, targetSlot) =>
                playerDirector.RequestAttack(hud.Match, fromSlot, targetSlot);
            hud.ModeChanged += active => { if (!active) playerDirector.Cancel(); };

            void BindMatch(DuelMatch duel)
            {
                hud.InitializeMatch(duel);
                interaction.InitializeMatch(duel);
                feedback.Bind(duel);
                duel.EventEmitted += evt =>
                {
                    if (!hud.DuelModeActive || !hud.AnimationsEnabled || hud.Match != duel) return;
                    if (evt.Kind == MatchEventKind.Destroy)
                        arena.PlayMatchDestruction(evt.Side, evt.Slot);
                };
            }
            void ResetDuel()
            {
                opponentDirector.Cancel();
                playerDirector.Cancel();
                var duel = CreateExperimentalMatch();
                BindMatch(duel);
                hud.ResetMatch(duel);
            }

            BindMatch(CreateExperimentalMatch());
            hud.NewMatchRequested += ResetDuel;
            hud.NextOpponentRequested += () =>
            {
                if (pools == null || pools.Pools.Count < 2) return;
                selectedPoolIndex = (selectedPoolIndex + 1) % pools.Pools.Count;
                ResetDuel();
            };
        }
    }
}
