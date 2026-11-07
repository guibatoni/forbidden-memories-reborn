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
            var deck = loaded ? DemoDeck.FromCatalog(imported.Cards) : DemoDeck.Create();
            var session = new DuelSession(deck);
            var arena = new ArenaFactory(transform);
            arena.Build();
            var hud = gameObject.AddComponent<ArenaHud>();
            hud.Initialize(session,arena,loaded ? imported : null,importStatus);
            var interaction = gameObject.AddComponent<ArenaInteraction>();
            interaction.Initialize(session,arena,hud);
            DuelMatch CreateExperimentalMatch()
            {
                // Placeholder decks/recipes ONLY; never claim these are original opponents.
                var opponentDeck = loaded ? DemoDeck.FromCatalog(imported.Cards) : DemoDeck.Create();
                return new DuelMatch(deck,opponentDeck,
                    loaded ? imported.Fusions : DemoDeck.CreateSampleFusions());
            }
            void BindMatch(DuelMatch duel)
            {
                hud.InitializeMatch(duel);
                interaction.InitializeMatch(duel);
                duel.EventEmitted += evt =>
                {
                    if (evt.Kind == MatchEventKind.Destroy && hud.AnimationsEnabled && hud.DuelModeActive)
                        arena.PlayMatchDestruction(evt.Side,evt.Slot);
                };
            }
            BindMatch(CreateExperimentalMatch());
            hud.NewMatchRequested += () =>
            {
                var newDuel = CreateExperimentalMatch();
                BindMatch(newDuel);
                hud.ResetMatch(newDuel);
            };
        }
    }
}
