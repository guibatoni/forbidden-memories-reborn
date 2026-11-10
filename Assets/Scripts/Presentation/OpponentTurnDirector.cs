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
        private AdaptiveOpponentBrain brain;
        private Coroutine active;

        public void Initialize(ArenaFactory factory, ArenaHud interfaceHud,
            AdaptiveOpponentBrain aiBrain)
        {
            arena = factory;
            hud = interfaceHud;
            brain = aiBrain;
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

            if (brain != null && brain.TryChoosePlacement(game, out var plan))
            {
                var monster = plan.Result;
                int zone = plan.Slot;
                hud.Announce("Oponente escolheu uma carta da mao.");
                yield return Pause(0.90f);
                // A carta sai visualmente do lado adversario e vai para a zona fixa.
                arena.StageMatchPlacement(DuelSide.Opponent, FieldLane.Monster, zone,
                    new Vector3(0f, 0.46f, 6.9f), hud.AnimationsEnabled);
                if (game.TryPlay(DuelSide.Opponent, plan.HandIndices, FieldLane.Monster,
                    zone, plan.Position, plan.Star,
                    plan.Position == BattlePosition.Defense, out var reason))
                    hud.Announce(plan.Position == BattlePosition.Defense
                        ? "Oponente setou um monstro virado para baixo na zona " + (zone + 1) + "."
                        : "Oponente " + (plan.IsFusion ? "fundiu e invocou " : "invocou ") +
                            monster.Name + " na zona " + (zone + 1) +
                            " (" + plan.Position + ", " + plan.Star + ").");
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

            // Existing set monsters may switch to attack before the battle
            // phase. A newly summoned monster cannot switch this turn.
            for (int attempt = 0; attempt < DuelMatch.ZoneCount &&
                game.Phase != MatchPhase.Finished; attempt++)
            {
                if (brain == null || !brain.TryChoosePositionChange(game,
                    out int defenseSlot, out var nextPosition)) break;
                if (!game.TryChangePosition(DuelSide.Opponent, defenseSlot,
                    nextPosition, out var changeReason))
                {
                    hud.Announce("Posicao nao alterada: " + changeReason);
                    break;
                }
                hud.Announce("Oponente mudou a posicao do monstro da zona " +
                    (defenseSlot + 1) + " para ATK.");
                yield return Pause(0.50f);
            }

            if (game.Phase != MatchPhase.Finished && game.Phase == MatchPhase.Main)
            {
                hud.Announce("Oponente prepara a fase de combate.");
                yield return Pause(0.70f);
                game.TryBeginBattle(DuelSide.Opponent, out _);
                yield return Pause(0.50f);
            }

            bool announcedPlan = false;
            while (game.Phase == MatchPhase.Battle &&
                   brain != null && brain.TryChooseAttack(game, out int attackerSlot, out int targetSlot))
            {
                if (!announcedPlan && brain.LastAttackPlan != null &&
                    brain.LastAttackPlan.Steps.Count > 1)
                {
                    hud.Announce("IA avaliou uma sequencia de " +
                        brain.LastAttackPlan.Steps.Count + " ataques.");
                    announcedPlan = true;
                    yield return Pause(0.65f);
                }
                var monster = game.Opponent.Monster(attackerSlot);
                if (monster == null || !game.CanAttack(DuelSide.Opponent,attackerSlot,targetSlot,out _))
                    break;
                arena.HighlightAttacker(DuelSide.Opponent, attackerSlot);
                hud.Announce("IA selecionou: " + monster.Card.Name + " (zona " + (attackerSlot + 1) + ").");
                yield return Pause(1.00f);

                if (targetSlot >= 0 && game.Player.Monster(targetSlot) != null)
                {
                    var defending = game.Player.Monster(targetSlot);
                    hud.Announce(defending.FaceDown
                        ? "IA atacou uma carta virada para baixo."
                        : "IA declarou ataque a " + defending.Card.Name + ".");
                }
                else
                    hud.Announce("IA declarou ataque direto!");
                if (hud.AnimationsEnabled)
                    arena.PlayMatchAttack(DuelSide.Opponent, attackerSlot, targetSlot);
                // Nao aplica dano antes de o projetil terminar, mesmo no ritmo rapido.
                yield return new WaitForSeconds(hud.AnimationsEnabled
                    ? Mathf.Max(0.72f, 0.77f * hud.OpponentPace)
                    : Mathf.Max(0.15f, 0.45f * hud.OpponentPace));

                // The same read-only forecast is used to explain the final result.
                if (game.TryPreviewAttack(DuelSide.Opponent, attackerSlot, targetSlot,
                    out var preview,out var reason) &&
                    game.TryAttack(DuelSide.Opponent, attackerSlot, targetSlot,out reason))
                {
                    hud.Announce(CombatExplanation.Describe(preview));
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
