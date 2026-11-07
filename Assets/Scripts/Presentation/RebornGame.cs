using Reborn.Core;
using UnityEngine;

namespace Reborn.Presentation
{
    /// <summary>Bootstrap da demo: uma cena vazia basta para gerar o prototipo.</summary>
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
            // Nunca instancia duas arenas, mesmo se a cena contiver o bootstrap.
            if (FindObjectsByType<RebornGame>(FindObjectsSortMode.None).Length > 1)
            {
                Destroy(gameObject); return;
            }
            GameContentBundle imported;
            string importStatus;
            bool loaded = OriginalContentLoader.TryLoad(out imported,out importStatus);
            var cardPool = loaded ? DemoDeck.FromCatalog(imported.Cards) : DemoDeck.Create();
            var session = new DuelSession(DeckRandomizer.ShuffleNewMatch(cardPool));
            var arena = new ArenaFactory(transform);
            arena.Build();
            var hud = gameObject.AddComponent<ArenaHud>();
            hud.Initialize(session,arena,loaded ? imported : null,importStatus);
            var interaction = gameObject.AddComponent<ArenaInteraction>();
            interaction.Initialize(session,arena,hud);
            DuelMatch CreateExperimentalMatch()
            {
                // Placeholder decks/recipes ONLY; never claim these are original opponents.
                var playerDeck = DeckRandomizer.ShuffleNewMatch(cardPool);
                var opponentDeck = DeckRandomizer.ShuffleNewMatch(
                    loaded ? DemoDeck.FromCatalog(imported.Cards) : DemoDeck.Create());
                return new DuelMatch(playerDeck,opponentDeck,
                    loaded ? imported.Fusions : DemoDeck.CreateSampleFusions());
            }
            var opponentDirector = gameObject.AddComponent<OpponentTurnDirector>();
            opponentDirector.Initialize(arena,hud);
            var playerDirector = gameObject.AddComponent<PlayerAttackDirector>();
            playerDirector.Initialize(arena,hud);
            var feedback = gameObject.AddComponent<CombatFeedbackOverlay>();
            feedback.Initialize(hud);
            hud.OpponentTurnRequested += () => opponentDirector.PlayTurn(hud.Match);
            hud.PlayerAttackRequested += (fromSlot,targetSlot) =>
                playerDirector.RequestAttack(hud.Match,fromSlot,targetSlot);
            hud.ModeChanged += active => { if (!active) playerDirector.Cancel(); };
            void BindMatch(DuelMatch duel)
            {
                hud.InitializeMatch(duel);
                interaction.InitializeMatch(duel);
                feedback.Bind(duel);
                duel.EventEmitted += evt =>
                {
                    if (!hud.DuelModeActive || !hud.AnimationsEnabled || hud.Match != duel) return;
                    // Both directors launch their projectiles BEFORE TryAttack; once
                    // damage is committed, destruction VFX can play immediately.
                    if (evt.Kind == MatchEventKind.Destroy)
                        arena.PlayMatchDestruction(evt.Side,evt.Slot);
                };
            }
            BindMatch(CreateExperimentalMatch());
            hud.NewMatchRequested += () =>
            {
                opponentDirector.Cancel();
                playerDirector.Cancel();
                var newDuel = CreateExperimentalMatch();
                BindMatch(newDuel);
                hud.ResetMatch(newDuel);
            };
        }

    }
}
