using System.Collections;
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
            var opponentDirector = gameObject.AddComponent<OpponentTurnDirector>();
            opponentDirector.Initialize(arena,hud);
            hud.OpponentTurnRequested += () => opponentDirector.PlayTurn(hud.Match);
            void BindMatch(DuelMatch duel)
            {
                hud.InitializeMatch(duel);
                interaction.InitializeMatch(duel);
                DuelSide lastAttackingSide = DuelSide.Player;
                duel.EventEmitted += evt =>
                {
                    if (!hud.DuelModeActive || !hud.AnimationsEnabled || hud.Match != duel) return;
                    if (evt.Kind == MatchEventKind.Attack)
                    {
                        lastAttackingSide = evt.Side;
                        // A IA ja reproduziu o ataque ANTES de aplicar dano no motor.
                        if (evt.Side == DuelSide.Player)
                            arena.PlayMatchAttack(evt.Side,evt.Slot,evt.Value);
                    }
                    else if (evt.Kind == MatchEventKind.Destroy)
                    {
                        if (lastAttackingSide == DuelSide.Player)
                            StartCoroutine(DelayedDestruction(arena,hud,duel,evt.Side,evt.Slot));
                        else arena.PlayMatchDestruction(evt.Side,evt.Slot);
                    }
                };
            }
            BindMatch(CreateExperimentalMatch());
            hud.NewMatchRequested += () =>
            {
                opponentDirector.Cancel();
                StopAllCoroutines(); // descarta efeitos adiados do duelo anterior
                var newDuel = CreateExperimentalMatch();
                BindMatch(newDuel);
                hud.ResetMatch(newDuel);
            };
        }

        private static IEnumerator DelayedDestruction(ArenaFactory arena, ArenaHud hud,
            DuelMatch duel, DuelSide side, int slot)
        {
            yield return new WaitForSeconds(0.66f);
            if (hud != null && hud.Match == duel && hud.DuelModeActive && hud.AnimationsEnabled)
                arena.PlayMatchDestruction(side,slot);
        }
    }
}