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
            var storage = new AdaptiveAiStorage();
            var brain = new AdaptiveOpponentBrain(
                new System.Random(Guid.NewGuid().GetHashCode()));
            int selectedPoolIndex = 0;
            var initialCards = loaded
                ? DemoDeck.RandomPracticeDeck(imported.Cards, new System.Random(Guid.NewGuid().GetHashCode()))
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
                var playerSource = loaded
                    ? DemoDeck.RandomPracticeDeck(imported.Cards, new System.Random(Guid.NewGuid().GetHashCode()))
                    : DemoDeck.Create();
                var playerDeck = DeckRandomizer.ShuffleNewMatch(playerSource);
                IReadOnlyList<CardDefinition> opponentDeck;
                string identity;
                if (pools != null)
                {
                    var pool = pools.Pools[selectedPoolIndex];
                    opponentDeck = pool.Deal(imported.Cards, new System.Random(Guid.NewGuid().GetHashCode()));
                    hud.SetOpponentSource(pool.Name + " (#" + pool.Id + ")", pools.Pools.Count > 1);
                    identity = imported.Edition + ":" +
                        imported.SourceSha256.Substring(0, 8) + ":" + pool.Id;
                }
                else
                {
                    var source = loaded
                        ? DemoDeck.RandomPracticeDeck(imported.Cards, new System.Random(Guid.NewGuid().GetHashCode()))
                        : DemoDeck.Create();
                    opponentDeck = DeckRandomizer.ShuffleNewMatch(source);
                    hud.SetOpponentSource("IA de treino", false);
                    identity = loaded
                        ? imported.Edition + ":sandbox" : "demo:training";
                }
                brain.StartDuel(storage.Get(identity));
                hud.SetAiInsight(brain.TacticLabel, brain.State.duels);
                return new DuelMatch(playerDeck, opponentDeck,
                    loaded ? imported.Fusions : DemoDeck.CreateSampleFusions());
            }

            var opponentDirector = gameObject.AddComponent<OpponentTurnDirector>();
            opponentDirector.Initialize(arena, hud, brain);
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
                // Capture the current brain state for this duel; don't mix opponents.
                var profile = brain.State;
                var tactic = brain.Tactic;
                bool learned = false;
                duel.EventEmitted += evt =>
                {
                    if (evt.Kind == MatchEventKind.Finished && !learned)
                    {
                        learned = true;
                        if (ReferenceEquals(hud.Match, duel) &&
                            ReferenceEquals(brain.State, profile) &&
                            brain.Tactic == tactic && brain.RecordCompletedDuel(duel))
                        {
                            storage.Save();
                            hud.SetAiInsight(brain.TacticLabel, profile.duels);
                        }
                    }
                    if (!hud.DuelModeActive || !hud.AnimationsEnabled || hud.Match != duel) return;
                    if (evt.Kind == MatchEventKind.Destroy)
                        arena.PlayMatchDestruction(evt.Side, evt.Slot);
                };
            }

            void ResetDuel()
            {
                opponentDirector.Cancel();
                playerDirector.Cancel();
                var newMatch = CreateExperimentalMatch();
                BindMatch(newMatch);
                hud.ResetMatch(newMatch);
            }
            BindMatch(CreateExperimentalMatch());
            // Launch straight into the playable duel; Sandbox stays available
            // through the optional HUD button rather than interrupting gameplay.
            hud.SetDuelMode(true);
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
