using System;
using Reborn.Core;
using UnityEngine;

namespace Reborn.Presentation
{
    /// <summary>
    /// Fully transactional PS1-style placement: Guardian -> Face ->
    /// rotated preview of ATK/DEF -> ONE confirmed hand action.
    /// No card is spent and no monster enters battle during the wizard.
    /// </summary>
    public sealed partial class ArenaHud
    {
        private enum PlacementChoiceStep { None, Guardian, Facing, Position }
        private PlacementChoiceStep placementStep;
        private CardDefinition placementPreview;
        private Func<bool> placementCommit;
        private bool placementIsRitual;
        private bool placementOverwritesField;
        private bool placementSecondaryChoice;

        public bool PlacementWizardOpen => placementStep != PlacementChoiceStep.None;
        public bool PlacementPositionPreviewOpen =>
            placementStep == PlacementChoiceStep.Position;

        public void BeginPlacementChoices(CardDefinition result, bool ritual,
            Func<bool> commit, bool overwritesField = false)
        {
            if (result == null || commit == null || PlacementWizardOpen ||
                match == null || match.Phase != MatchPhase.Main ||
                match.CurrentSide != DuelSide.Player || match.PlayedThisTurn)
                return;
            placementPreview = result;
            placementCommit = commit;
            placementIsRitual = ritual;
            placementOverwritesField = overwritesField;
            UseDefense = false;
            PlaceFaceDown = false;
            UseSecondaryStar = false;
            placementSecondaryChoice = false;
            bool twoStars = result.Kind == CardKind.Monster &&
                result.StarA != GuardianStar.None &&
                result.StarB != GuardianStar.None &&
                result.StarA != result.StarB;
            placementStep = twoStars ? PlacementChoiceStep.Guardian :
                PlacementChoiceStep.Facing;
            if (!twoStars && !CanPlaceFaceUp())
                placementSecondaryChoice = true;
        }

        public void CancelPlacementChoices()
        {
            if (!PlacementWizardOpen) return;
            placementStep = PlacementChoiceStep.None;
            placementCommit = null;
            placementPreview = null;
            placementIsRitual = false;
            placementOverwritesField = false;
            Announce("Selecao cancelada: nenhuma carta foi baixada.");
        }

        private bool CanPlaceFaceUp()
        {
            if (placementPreview == null) return false;
            return placementPreview.Kind == CardKind.Monster ||
                placementIsRitual ||
                (placementPreview.Kind == CardKind.Spell &&
                 match != null && match.HasVerifiedSpell(placementPreview));
        }

        public void ChoosePlacementOption(bool secondary)
        {
            if (!PlacementWizardOpen) return;
            placementSecondaryChoice = secondary;
        }

        public void RotatePlacementPreview(bool defense)
        {
            if (placementStep != PlacementChoiceStep.Position) return;
            UseDefense = defense;
        }

        public void ConfirmPlacementChoice()
        {
            ConfirmPlacementOption(placementSecondaryChoice);
        }

        private void ConfirmPlacementOption(bool secondary)
        {
            switch (placementStep)
            {
                case PlacementChoiceStep.Guardian:
                    UseSecondaryStar = secondary;
                    placementStep = PlacementChoiceStep.Facing;
                    placementSecondaryChoice = !CanPlaceFaceUp();
                    break;
                case PlacementChoiceStep.Facing:
                    if (!secondary && !CanPlaceFaceUp())
                    {
                        Announce("Este suporte nao possui ativacao Face Up verificada. Use Face Down.");
                        return;
                    }
                    PlaceFaceDown = !placementIsRitual && secondary;
                    if (placementPreview != null &&
                        placementPreview.Kind == CardKind.Monster)
                    {
                        // The monster is NOT committed until the visual card
                        // is rotated and confirmed with Z / two mouse clicks.
                        placementStep = PlacementChoiceStep.Position;
                        UseDefense = false;
                    }
                    else CommitPlacement();
                    break;
                case PlacementChoiceStep.Position:
                    CommitPlacement();
                    break;
            }
        }

        private void CommitPlacement()
        {
            var callback = placementCommit;
            // Unlock the HUD before the callback fires Match.Changed, but
            // keep the chosen Star, Face and Position values intact.
            placementStep = PlacementChoiceStep.None;
            placementCommit = null;
            placementPreview = null;
            placementIsRitual = false;
            placementOverwritesField = false;
            bool succeeded = callback != null && callback();
            if (!succeeded)
                Announce("Posicionamento recusado; sua carta continua na mao.");
        }

        // Compatibility for existing placement callers: now a pure post-commit
        // selection of the placed monster, never a second modal.
        public void MarkPlacedMonster(int slot)
        {
            SelectedFieldMonster = slot;
        }

        private void DrawRotatingCard(Rect canvas)
        {
            if (placementPreview == null) return;
            float cardW = Mathf.Min(161f, canvas.height * 0.62f);
            float cardH = cardW * 1.32f;
            var rect = new Rect(canvas.center.x - cardW * 0.5f,
                canvas.center.y - cardH * 0.5f, cardW, cardH);
            Matrix4x4 original = GUI.matrix;
            GUIUtility.RotateAroundPivot(UseDefense ? 90f : 0f, canvas.center);
            Panel(new Rect(rect.x - 7f, rect.y - 7f,
                rect.width + 14f, rect.height + 14f),
                new Color(0.83f,0.61f,0.31f));
            if (PlaceFaceDown)
            {
                Panel(rect, new Color(0.18f,0.115f,0.275f));
                Panel(new Rect(rect.x+12f,rect.y+12f,rect.width-24f,
                    rect.height-24f), new Color(0.32f,0.20f,0.47f));
                GUI.Label(rect,"REBORN\nFACE DOWN",
                    Style(18,Color.white,FontStyle.Bold));
            }
            else
                GUI.DrawTexture(rect, CardArtCatalog.Get(placementPreview),
                    ScaleMode.ScaleToFit);
            GUI.matrix = original;
        }

        private void DrawPlacementChoices()
        {
            if (!PlacementWizardOpen || placementPreview == null) return;
            Panel(new Rect(0,0,Screen.width,Screen.height),
                new Color(0.006f,0.012f,0.028f,0.95f));
            float w = Mathf.Min(710f, Screen.width-26f);
            float h = Mathf.Min(510f, Screen.height-24f);
            float left = (Screen.width-w)*0.5f;
            float top = (Screen.height-h)*0.5f;
            Panel(new Rect(left,top,w,h), new Color(0.025f,0.043f,0.090f));
            Panel(new Rect(left+14f,top+13f,w-28f,4f),
                new Color(0.88f,0.65f,0.29f));
            string title = placementStep == PlacementChoiceStep.Guardian ?
                "ESCOLHA A GUARDIAN STAR" :
                placementStep == PlacementChoiceStep.Facing ?
                "FACE UP OU FACE DOWN?" : "GIRE A CARTA ANTES DE JOGAR";
            GUI.Label(new Rect(left+22f,top+25f,w-44f,43f),
                placementOverwritesField ? "TROCA SEM FUSAO - " + title : title,
                heading);
            GUI.Label(new Rect(left+22f,top+73f,w-44f,37f),
                (placementOverwritesField ?
                    "SEM RECEITA: o monstro atual sera substituido!  " : "") +
                placementPreview.Name+"  #"+placementPreview.Id,body);

            if (placementStep == PlacementChoiceStep.Position)
            {
                float midY = top + Mathf.Min(h*0.53f,260f);
                DrawRotatingCard(new Rect(left+w*0.5f-125f,top+97f,250f,
                    Mathf.Min(260f,h-216f)));
                GUI.Label(new Rect(left+22f,top+h-124f,w-44f,32f),
                    (UseDefense ? "DEFESA  |  CARTA HORIZONTAL" :
                        "ATAQUE  |  CARTA VERTICAL") +
                    "   |   Q = ATK   E = DEF",body);
                GUI.Label(new Rect(left+22f,top+h-95f,w-44f,24f),
                    "Z confirma. X cancela sem gastar carta. Clique na carta para girar.",small);
                if (GUI.Button(new Rect(left+22f,top+h-60f,w*0.54f-27f,39f),
                    "CONFIRMAR A CARTA [Z]",button))
                    CommitPlacement();
                if (GUI.Button(new Rect(left+w*0.54f+5f,top+h-60f,
                    w*0.46f-27f,39f),"CANCELAR [X]",button))
                    CancelPlacementChoices();
                // Clicking the card twice is not required inside this modal;
                // one click rotates, keyboard Z or button commits.
                if (Event.current.type == EventType.MouseDown &&
                    new Rect(left+w*0.5f-130f,top+100f,260f,
                        Mathf.Min(260f,h-220f)).Contains(Event.current.mousePosition))
                {
                    UseDefense = !UseDefense;
                    Event.current.Use();
                }
                return;
            }

            string first = placementStep == PlacementChoiceStep.Guardian ?
                placementPreview.StarA.ToString() : "FACE PARA CIMA";
            string second = placementStep == PlacementChoiceStep.Guardian ?
                placementPreview.StarB.ToString() :
                (placementIsRitual ? "RITUAL FACE UP" : "FACE PARA BAIXO");
            float optionWidth=(w-74f)*0.5f;
            float optionY=top+Mathf.Min(160f,h*0.37f);
            GUI.enabled = placementStep != PlacementChoiceStep.Facing ||
                CanPlaceFaceUp();
            if (GUI.Button(new Rect(left+22f,optionY,optionWidth,109f),
                (placementSecondaryChoice ? "" : "► ") + first,button))
                ConfirmPlacementOption(false);
            GUI.enabled = true;
            if (GUI.Button(new Rect(left+50f+optionWidth,optionY,
                optionWidth,109f),
                (placementSecondaryChoice ? "► " : "") + second,button))
                ConfirmPlacementOption(true);
            GUI.Label(new Rect(left+22f,top+h-111f,w-44f,47f),
                "Use setas esquerda/direita e Z. Clique tambem funciona.\n" +
                "Nenhuma carta e gasta antes da confirmacao final.",small);
            if (GUI.Button(new Rect(left+22f,top+h-55f,w-44f,34f),
                "CANCELAR [X]",button)) CancelPlacementChoices();
        }
    }
}
