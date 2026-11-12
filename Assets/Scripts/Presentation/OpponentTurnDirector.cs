using System.Collections;
using System.Collections.Generic;
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

        private IEnumerator PauseForPlacement()
        {
            // Staggered fusion trails and the ceremony must finish before the
            // next action. Fast pace shortens deliberation, not VFX duration.
            float seconds = hud.AnimationsEnabled
                ? Mathf.Max(1.12f, 1.20f * hud.OpponentPace)
                : Mathf.Max(0.10f, 0.45f * hud.OpponentPace);
            yield return new WaitForSeconds(seconds);
        }

        private IEnumerator PlayFieldCombination(DuelMatch game,
            int handIndex, int zone, CardDefinition preview)
        {
            if (preview == null) yield break;
            hud.Announce("Oponente combina uma carta com o monstro da zona " +
                (zone + 1) + ".");
            yield return Pause(0.90f);
            arena.StageMatchPlacement(DuelSide.Opponent, FieldLane.Monster, zone,
                new Vector3(0f, 0.46f, 6.9f), hud.AnimationsEnabled);
            var position = preview.Defense > preview.Attack + 500
                ? BattlePosition.Defense : BattlePosition.Attack;
            if (game.TryPlayOnField(DuelSide.Opponent, new[] { handIndex }, zone,
                position, preview.StarA, position == BattlePosition.Defense,
                out var outcome, out bool fused, out var reason))
            {
                arena.PlayFieldCombinationEffect(DuelSide.Opponent, zone, fused,
                    hud.AnimationsEnabled);
                if (fused)
                {
                    // Pure light streams; no concealed card identity is shown.
                    var sources = new List<Vector3>
                    {
                        new Vector3((zone - 2) * 2.4f, 0.80f, 2f),
                        new Vector3(0f, 0.55f, 6.9f)
                    };
                    arena.PlayFusionMaterialTrails(sources,
                        FieldLane.Monster, zone, hud.AnimationsEnabled,
                        DuelSide.Opponent);
                }
                hud.Announce(fused ? "Fusao da IA: " + outcome.Name :
                    "IA substituiu seu monstro por " + outcome.Name + ".");
            }
            else
            {
                arena.ClearStagedPlacement();
                hud.Announce("Combinacao recusada: " + reason);
            }
            yield return PauseForPlacement();
        }

        private IEnumerator PerformTurn(DuelMatch game)
        {
            hud.Announce("Turno da IA: analisando as cartas...");
            yield return Pause(0.85f);

            // The PS1 CPU can use prepared cards on its own later turn.
            // No trap or unverified effect is activated proactively.
            for (int support = 0; support < DuelMatch.ZoneCount &&
                 game.Phase == MatchPhase.Main &&
                 game.CurrentSide == DuelSide.Opponent; support++)
            {
                var card = game.Opponent.Support(support);
                if (card == null || card.Kind != CardKind.Spell ||
                    !game.CanActivateSupport(DuelSide.Opponent, support, out _))
                    continue;
                hud.Announce("IA ativa magia previamente preparada: " + card.Name);
                yield return Pause(0.40f);
                if (game.TryActivateSupport(DuelSide.Opponent, support, out var reason))
                    hud.Announce("Magia da IA ativada: " + card.Name + ".");
                else
                    hud.Announce("Magia da IA recusada: " + reason);
                yield return Pause(0.35f);
            }
            for (int support = 0; support < DuelMatch.ZoneCount &&
                 game.Phase == MatchPhase.Main &&
                 game.CurrentSide == DuelSide.Opponent; support++)
            {
                var card = game.Opponent.Support(support);
                if (card == null || card.Kind != CardKind.Equip) continue;
                for (int target = 0; target < DuelMatch.ZoneCount; target++)
                {
                    if (!game.CanEquipFromSupport(DuelSide.Opponent,
                        support, target, out _, out _)) continue;
                    hud.Announce("IA seleciona um monstro para " + card.Name + ".");
                    yield return Pause(0.40f);
                    if (game.TryEquipFromSupport(DuelSide.Opponent,
                        support, target, out var reason))
                        hud.Announce("Equip da IA aplicado: " + card.Name + ".");
                    else
                        hud.Announce("Equip da IA recusado: " + reason);
                    yield return Pause(0.35f);
                    break;
                }
            }
            if (game.Phase == MatchPhase.Finished)
            {
                active = null;
                hud.SetOpponentBusy(false);
                yield break;
            }

            if (brain != null && brain.TryChoosePlacement(game, out var plan))
            {
                var monster = plan.Result;
                int zone = plan.Slot;
                hud.Announce("Oponente escolheu uma carta da mao.");
                yield return Pause(0.90f);
                // A carta sai visualmente do lado adversario e vai para a zona fixa.
                arena.StageMatchPlacement(DuelSide.Opponent, FieldLane.Monster, zone,
                    new Vector3(0f, 0.46f, 6.9f), hud.AnimationsEnabled);
                int beforePlacementEvents = game.Events.Count;
                if (game.TryPlay(DuelSide.Opponent, plan.HandIndices, FieldLane.Monster,
                    zone, plan.Position, plan.Star,
                    plan.Position == BattlePosition.Defense, out var reason))
                {
                    int verifiedFusionSteps = 0;
                    for (int e = beforePlacementEvents; e < game.Events.Count; ++e)
                        if (game.Events[e].Side == DuelSide.Opponent &&
                            game.Events[e].Kind == MatchEventKind.Fusion)
                            verifiedFusionSteps++;
                    // Only animate real engine-confirmed fusions. Never use
                    // the number of selected cards to invent an effect.
                    if (verifiedFusionSteps > 0)
                    {
                        arena.PlayFieldCombinationEffect(DuelSide.Opponent,
                            zone, true, hud.AnimationsEnabled);
                        var origins = new List<Vector3>();
                        for (int i = 0; i < plan.HandIndices.Count; ++i)
                            origins.Add(new Vector3(
                                (i - (plan.HandIndices.Count - 1) * 0.5f) * 0.72f,
                                0.55f, 6.9f));
                        arena.PlayFusionMaterialTrails(origins,
                            FieldLane.Monster, zone, hud.AnimationsEnabled,
                            DuelSide.Opponent);
                    }
                    hud.Announce(plan.Position == BattlePosition.Defense
                        ? "Oponente setou um monstro virado para baixo na zona " +
                            (zone + 1) + "."
                        : "Oponente " + (verifiedFusionSteps > 0 ? "fundiu e invocou " : "invocou ") +
                            monster.Name + " na zona " + (zone + 1) +
                            " (" + plan.Position + ", " + plan.Star + ").");
                }
                else
                {
                    arena.ClearStagedPlacement();
                    hud.Announce("Jogada recusada: " + reason);
                }
                yield return PauseForPlacement();
            }
            else if (game.TryChooseFieldCombination(DuelSide.Opponent, true,
                out int fusionHand, out int fusionSlot, out var fusionPreview))
            {
                // The field monster is the first material, hand card the second.
                yield return StartCoroutine(PlayFieldCombination(game,
                    fusionHand, fusionSlot, fusionPreview));
            }
            else if (game.TryChooseSupportSet(DuelSide.Opponent,
                out int supportHand, out int supportSlot))
            {
                // A facedown support card is a legal compulsory placement.
                // Its unverified effect is never executed or exposed.
                hud.Announce("Oponente prepara um suporte virado para baixo.");
                yield return Pause(0.70f);
                arena.StageMatchPlacement(DuelSide.Opponent, FieldLane.Support,
                    supportSlot, new Vector3(0f, 0.46f, 6.9f),
                    hud.AnimationsEnabled);
                if (game.TryPlay(DuelSide.Opponent, new[] { supportHand },
                    FieldLane.Support, supportSlot, BattlePosition.Defense,
                    GuardianStar.None, true, out var supportReason))
                    hud.Announce("Oponente setou um suporte na zona " +
                        (supportSlot + 1) + ".");
                else
                {
                    arena.ClearStagedPlacement();
                    hud.Announce("Suporte recusado: " + supportReason);
                }
                yield return PauseForPlacement();
            }
            else if (game.TryChooseFieldCombination(DuelSide.Opponent, false,
                out int replacementHand, out int replacementSlot, out var replacementPreview))
            {
                // When no free slot/verified fusion/support set is possible,
                // replacing one's own monster still counts as the required play.
                yield return StartCoroutine(PlayFieldCombination(game,
                    replacementHand, replacementSlot, replacementPreview));
            }
            else
            {
                hud.Announce("Nenhuma colocacao legal disponivel; turno nao sera pulado.");
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

            if (game.Phase != MatchPhase.Finished &&
                game.Phase == MatchPhase.Main && game.PlayedThisTurn)
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
                int eventCount = game.Events.Count;
                if (game.TryPreviewAttack(DuelSide.Opponent, attackerSlot, targetSlot,
                    out var preview,out var reason) &&
                    game.TryAttack(DuelSide.Opponent, attackerSlot, targetSlot,out reason))
                {
                    string trap = "";
                    for (int i = eventCount; i < game.Events.Count; i++)
                        if (game.Events[i].Kind == MatchEventKind.TrapTriggered)
                            trap = game.Events[i].CardId;
                    hud.Announce(trap.Length > 0 ?
                        "Armadilha #" + trap + " interceptou a IA!" :
                        CombatExplanation.Describe(preview));
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
