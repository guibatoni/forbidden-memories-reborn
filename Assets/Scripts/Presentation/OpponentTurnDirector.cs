using System.Collections;
using Reborn.Core;
using UnityEngine;

namespace Reborn.Presentation
{
    /// <summary>
    /// Diretor de apresentacao. A IA decide a proxima jogada somente quando a
    /// anterior acabou. Cada etapa mostra uma mensagem e uma animacao separada.
    /// </summary>
    public sealed class OpponentTurnDirector : MonoBehaviour
    {
        private DuelMatch match;
        private ArenaFactory arena;
        private ArenaHud hud;
        private Coroutine active;

        public void Initialize(ArenaFactory factory, ArenaHud interfaceHud)
        {
            arena = factory;
            hud = interfaceHud;
        }

        public void Cancel()
        {
            if (active != null) StopCoroutine(active);
            active = null;
            if (arena != null) arena.HighlightAttacker(DuelSide.Opponent, -1);
            if (hud != null) hud.SetOpponentBusy(false);
        }

        public void PlayTurn(DuelMatch game)
        {
            Cancel();
            if (game == null || game.CurrentSide != DuelSide.Opponent ||
                game.Phase == MatchPhase.Finished) return;
            match = game;
            hud.SetOpponentBusy(true);
            active = StartCoroutine(PerformTurn(game));
        }

        private IEnumerator Pause(float seconds)
        {
            // Ritmo altera somente a apresentacao, nunca a logica de duelo.
            yield return new WaitForSeconds(Mathf.Max(0.10f, seconds * hud.OpponentPace));
        }

        private IEnumerator PerformTurn(DuelMatch game)
        {
            hud.Announce("Turno da IA: analisando as cartas...");
            yield return Pause(0.85f);

            if (SimpleOpponentAi.TryChoosePlay(game, out int hand, out int zone))
            {
                var monster = game.Opponent.Hand[hand];
                hud.Announce("Oponente escolheu uma carta da mao.");
                yield return Pause(0.90f);
                // A carta sai visualmente do lado adversario e vai para a zona fixa.
                arena.StageMatchPlacement(DuelSide.Opponent, FieldLane.Monster, zone,
                    new Vector3(0f, 0.46f, 6.9f), hud.AnimationsEnabled);
                if (game.TryPlay(DuelSide.Opponent, new [] { hand }, FieldLane.Monster,
                    zone, BattlePosition.Attack, monster.StarA, out var reason))
                    hud.Announce("Oponente invocou " + monster.Name + " na zona " + (zone + 1) + ".");
                else
                {
                    arena.ClearStagedPlacement();
                    hud.Announce("Jogada recusada: " + reason);
                }
                yield return Pause(1.20f);
            }
            else
            {
                hud.Announce("Oponente nao possui uma invocacao valida.");
                yield return Pause(0.85f);
            }

            if (game.Phase != MatchPhase.Finished && game.Phase == MatchPhase.Main)
            {
                hud.Announce("Oponente prepara a fase de combate.");
                yield return Pause(0.70f);
                game.TryBeginBattle(DuelSide.Opponent, out _);
                yield return Pause(0.50f);
            }

            while (game.Phase == MatchPhase.Battle &&
                   SimpleOpponentAi.TryChooseAttack(game, out int attackerSlot, out int targetSlot))
            {
                var monster = game.Opponent.Monster(attackerSlot);
                if (monster == null || !game.CanAttack(DuelSide.Opponent,attackerSlot,targetSlot,out _))
                    break;
                arena.HighlightAttacker(DuelSide.Opponent, attackerSlot);
                hud.Announce("IA selecionou: " + monster.Card.Name + " (zona " + (attackerSlot + 1) + ").");
                yield return Pause(1.00f);

                if (targetSlot >= 0 && game.Player.Monster(targetSlot) != null)
                    hud.Announce("IA declarou ataque a " + game.Player.Monster(targetSlot).Card.Name + ".");
                else
                    hud.Announce("IA declarou ataque direto!");
                if (hud.AnimationsEnabled)
                    arena.PlayMatchAttack(DuelSide.Opponent, attackerSlot, targetSlot);
                // Nao aplica dano antes de o projetil terminar, mesmo no ritmo rapido.
                yield return new WaitForSeconds(hud.AnimationsEnabled
                    ? Mathf.Max(0.72f, 0.77f * hud.OpponentPace)
                    : Mathf.Max(0.15f, 0.45f * hud.OpponentPace));

                int lifeBefore = game.Player.Life;
                if (game.TryAttack(DuelSide.Opponent, attackerSlot, targetSlot, out var reason))
                {
                    int difference = lifeBefore - game.Player.Life;
                    hud.Announce(difference > 0
                        ? "Ataque resolvido. Seus LP: -" + difference + " (" + game.Player.Life + ")."
                        : "Ataque resolvido. Confira o campo e os cemiterios.");
                }
                else hud.Announce("Ataque nao executado: " + reason);
                arena.HighlightAttacker(DuelSide.Opponent, -1);
                yield return Pause(1.05f);
            }

            arena.HighlightAttacker(DuelSide.Opponent, -1);
            if (game.Phase != MatchPhase.Finished && game.CurrentSide == DuelSide.Opponent)
            {
                hud.Announce("Oponente encerra seu turno.");
                yield return Pause(0.85f);
                if (game.TryEndTurn(DuelSide.Opponent, out var reason))
                    hud.Announce(game.Phase == MatchPhase.Finished ?
                        "Duelo encerrado." : "Seu turno! Escolha uma carta.");
                else hud.Announce("Nao foi possivel encerrar o turno: " + reason);
            }
            else hud.Announce("Duelo encerrado.");

            active = null;
            hud.SetOpponentBusy(false);
        }

        private void OnDisable() { Cancel(); }
    }
}
