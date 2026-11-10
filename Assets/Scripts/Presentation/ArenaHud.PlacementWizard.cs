using System;
using Reborn.Core;
using UnityEngine;

namespace Reborn.Presentation
{
    /// <summary>
    /// Presentation-only, transactional placement wizard. Selecting a target
    /// does not mutate the duel: the validated commit happens after choosing
    /// Guardian Star and facing. ATK/DEF is chosen after the card lands.
    /// </summary>
    public sealed partial class ArenaHud
    {
        private enum PlacementChoiceStep { None, Guardian, Facing, Position }
        private PlacementChoiceStep placementStep;
        private CardDefinition placementPreview;
        private Func<bool> placementCommit;
        private int placedMonsterSlot = -1;
        private bool placementIsRitual;

        public bool PlacementWizardOpen => placementStep != PlacementChoiceStep.None;

        public void BeginPlacementChoices(CardDefinition result, bool ritual,
            Func<bool> commit)
        {
            if (result == null || commit == null || PlacementWizardOpen ||
                match == null || match.Phase != MatchPhase.Main ||
                match.CurrentSide != DuelSide.Player || match.PlayedThisTurn)
                return;
            placementPreview = result;
            placementCommit = commit;
            placementIsRitual = ritual;
            PlaceFaceDown = false;
            UseDefense = false;
            UseSecondaryStar = false;
            placedMonsterSlot = -1;
            bool twoStars = result.Kind == CardKind.Monster &&
                result.StarA != GuardianStar.None &&
                result.StarB != GuardianStar.None &&
                result.StarA != result.StarB;
            placementStep = twoStars ? PlacementChoiceStep.Guardian :
                ritual ? PlacementChoiceStep.Position : PlacementChoiceStep.Facing;
            // Ritual still commits only after the player confirms the selected
            // Guardian Star; this screen never invents a facing or second star.
            if (ritual && !twoStars) placementStep = PlacementChoiceStep.Facing;
        }

        public void CancelPlacementChoices()
        {
            if (placementStep == PlacementChoiceStep.Position)
            {
                // The main card action is already legal/committed: closing the
                // positioning dialog cannot undo or duplicate the placement.
                placementStep = PlacementChoiceStep.None;
                placementCommit = null;
                placementPreview = null;
                return;
            }
            placementStep = PlacementChoiceStep.None;
            placementCommit = null;
            placementPreview = null;
            placedMonsterSlot = -1;
            Announce("Posicionamento cancelado. Nenhuma carta foi utilizada.");
        }

        private void ConfirmFacing(bool faceDown)
        {
            if (placementStep != PlacementChoiceStep.Facing) return;
            // Ritual cards are activated from the hand, never set facedown.
            PlaceFaceDown = !placementIsRitual && faceDown;
            var callback = placementCommit;
            placementStep = PlacementChoiceStep.None;
            bool succeeded = callback != null && callback();
            placementCommit = null;
            if (!succeeded)
            {
                placementPreview = null;
                return;
            }
            if (placedMonsterSlot >= 0 && match != null &&
                match.Player.Monster(placedMonsterSlot) != null &&
                match.CurrentSide == DuelSide.Player)
            {
                placementStep = PlacementChoiceStep.Position;
            }
            else
            {
                placementPreview = null;
                Announce("Carta posicionada. Pressione Espaco para encerrar o turno.");
            }
        }

        public void MarkPlacedMonster(int slot)
        {
            placedMonsterSlot = slot;
        }

        private void SetPlacedPosition(BattlePosition position)
        {
            if (placementStep != PlacementChoiceStep.Position) return;
            var monster = match?.Player.Monster(placedMonsterSlot);
            if (monster != null && monster.Position != position)
            {
                if (!match.TryChangePosition(DuelSide.Player,
                    placedMonsterSlot, position, out var reason))
                {
                    Announce(reason);
                    return;
                }
            }
            // After an attack, the existing DuelMatch rule prevents changing
            // position. Q/E remains available throughout the remainder of turn.
            SelectedFieldMonster = placedMonsterSlot;
            placementStep = PlacementChoiceStep.None;
            placementPreview = null;
            placedMonsterSlot = -1;
            Announce("Posicao definida. Use Q/E ou Espaco para passar o turno.");
        }

        private void DrawPlacementChoices()
        {
            if (!PlacementWizardOpen || placementPreview == null) return;
            Panel(new Rect(0, 0, Screen.width, Screen.height),
                new Color(0.007f, 0.011f, 0.025f, 0.94f));
            float w = Mathf.Min(690f, Screen.width - 32f);
            float h = Mathf.Min(350f, Screen.height - 28f);
            float left = (Screen.width - w) * 0.5f;
            float top = (Screen.height - h) * 0.5f;
            Panel(new Rect(left, top, w, h), new Color(0.025f, 0.045f, 0.090f, 1f));
            Panel(new Rect(left + 14f, top + 13f, w - 28f, 4f),
                new Color(0.87f, 0.65f, 0.29f, 1f));
            string phase = placementStep == PlacementChoiceStep.Guardian ?
                "ESCOLHA A GUARDIAN STAR" :
                placementStep == PlacementChoiceStep.Facing ?
                "CARTA VIRADA PARA CIMA OU PARA BAIXO?" :
                "ESCOLHA A POSICAO APOS COLOCAR";
            GUI.Label(new Rect(left + 25f, top + 30f, w - 50f, 43f), phase, heading);
            GUI.Label(new Rect(left + 25f, top + 83f, w - 50f, 46f),
                placementPreview.Name + "  #" + placementPreview.Id +
                "    ATK " + placementPreview.Attack +
                " / DEF " + placementPreview.Defense, body);
            string first = "", second = "";
            if (placementStep == PlacementChoiceStep.Guardian)
            {
                first = placementPreview.StarA.ToString();
                second = placementPreview.StarB.ToString();
            }
            else if (placementStep == PlacementChoiceStep.Facing)
            {
                first = "FACE PARA CIMA";
                second = "FACE PARA BAIXO";
            }
            else
            {
                first = "ATAQUE";
                second = "DEFESA";
            }
            float optionWidth = (w - 75f) * 0.5f;
            float optionTop = top + 147f;
            if (GUI.Button(new Rect(left + 25f, optionTop, optionWidth, 94f), first, button))
            {
                if (placementStep == PlacementChoiceStep.Guardian)
                {
                    UseSecondaryStar = false;
                    placementStep = PlacementChoiceStep.Facing;
                }
                else if (placementStep == PlacementChoiceStep.Facing) ConfirmFacing(false);
                else SetPlacedPosition(BattlePosition.Attack);
            }
            if (GUI.Button(new Rect(left + 50f + optionWidth, optionTop,
                optionWidth, 94f),
                placementIsRitual && placementStep == PlacementChoiceStep.Facing ?
                    "RITUAL: FACE PARA CIMA" : second, button))
            {
                if (placementStep == PlacementChoiceStep.Guardian)
                {
                    UseSecondaryStar = true;
                    placementStep = PlacementChoiceStep.Facing;
                }
                else if (placementStep == PlacementChoiceStep.Facing)
                    ConfirmFacing(!placementIsRitual);
                else SetPlacedPosition(BattlePosition.Defense);
            }
            GUI.Label(new Rect(left + 25f, top + h - 88f, w - 50f, 43f),
                placementStep == PlacementChoiceStep.Position ?
                "A carta ja foi baixada; Q/E permite mudar ATK/DEF antes de atacar." :
                "Escolha uma opcao. ESC cancela sem gastar sua jogada.",small);
            if (GUI.Button(new Rect(left + 25f, top + h - 45f, w - 50f, 30f),
                placementStep == PlacementChoiceStep.Position ?
                "Manter posicao atual" : "Cancelar sem colocar", button))
                CancelPlacementChoices();
        }
    }
}
