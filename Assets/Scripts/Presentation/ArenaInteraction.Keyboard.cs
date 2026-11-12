using System;
using System.Collections.Generic;
using Reborn.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Reborn.Presentation
{
    /// <summary>
    /// Full keyboard navigation shares the exact same DuelMatch/GUI actions
    /// as mouse selection. No second rules engine and no synthetic fusion data.
    /// </summary>
    public sealed partial class ArenaInteraction
    {
        private enum KeyboardLocation
        {
            Hand, OwnMonster, OwnSupport, EnemyMonster, EnemySupport
        }

        private KeyboardLocation keyboardLocation = KeyboardLocation.Hand;
        private int keyboardIndex;
        private bool keyboardActive;
        private bool keyboardHandArmed;
        private int lastClickHandIndex = -1;
        private float lastHandClickTime = -10f;
        private bool mousePositionKnown;
        private Vector2 previousMousePosition;

        private void TrackMouseActivity(Vector2 pointer)
        {
            var mouse = Mouse.current;
            if (mousePositionKnown &&
                (pointer-previousMousePosition).sqrMagnitude > 25f)
                keyboardActive = false;
            if (mouse != null &&
                (mouse.leftButton.wasPressedThisFrame ||
                 mouse.rightButton.wasPressedThisFrame))
                keyboardActive = false;
            mousePositionKnown = true;
            previousMousePosition = pointer;
        }

        private bool Shortcut(Keyboard keyboard, RebornKeyAction action) =>
            hud != null && hud.KeyBindings.Pressed(keyboard, action);

        private ZoneView KeyboardZone(bool opponent, FieldLane lane, int slot)
        {
            foreach (var zone in arena.Zones)
                if (zone != null && zone.Opponent == opponent &&
                    zone.Lane == lane && zone.Index == slot)
                    return zone;
            return null;
        }

        private void ResetKeyboardFocus()
        {
            keyboardLocation = KeyboardLocation.Hand;
            keyboardIndex = 0;
            keyboardHandArmed = false;
            keyboardActive = false;
            lastClickHandIndex = -1;
            lastHandClickTime = -10f;
        }

        private void ClearPlayerSelection()
        {
            fusionSelection.Clear();
            hud.FusionCount = 0;
            hud.SelectedHandIndex = -1;
            hud.FieldFusionPreview = null;
            hud.SelectedAttacker = -1;
            hud.SelectedFieldMonster = -1;
            hud.SelectedSupportSlot = -1;
            hud.PreviewTargetSlot = -1;
            keyboardHandArmed = false;
            pressedHand = -1;
            dragging = false;
            UpdateLegalHighlights();
        }

        private bool SelectHandMaterial(int index, bool append)
        {
            if (match == null || match.Phase != MatchPhase.Main ||
                match.PlayedThisTurn || match.CurrentSide != DuelSide.Player ||
                index < 0 || index >= match.Player.Hand.Count)
                return false;
            CardDefinition card = match.Player.Hand[index];
            if (!append)
                fusionSelection.Clear();
            if (fusionSelection.Count == 0)
                hud.ConfigurePlacement(card);
            if (!fusionSelection.Contains(index))
            {
                if (fusionSelection.Count >= DuelMatch.HandLimit)
                {
                    hud.Message = "Maximo de cinco materiais em uma fusao.";
                    return false;
                }
                fusionSelection.Add(index);
            }
            hud.SelectedHandIndex = index;
            hud.FusionCount = fusionSelection.Count;
            int fusions = match.CountHandFusionSteps(DuelSide.Player, fusionSelection);
            var result = match.PreviewResult(DuelSide.Player, fusionSelection);
            hud.FieldFusionPreview = fusions > 0 ? result : null;
            hud.Message = match.FusionRecipeCount == 0 &&
                fusionSelection.Count > 1 ?
                "Sem receitas de fusao neste perfil. Importe a ISO Original; " +
                "nenhuma fusao sera inventada." :
                (fusionSelection.Count == 1 ? "Carta selecionada: " :
                "Materiais " + fusionSelection.Count + "/5: ") +
                card.Name + (fusions > 0 && result != null
                    ? " | FUSOES " + fusions + " -> " + result.Name
                    : fusionSelection.Count > 1 ? " | sem receita nesta ordem" : "") +
                ". Duplo clique ou Z novamente joga no primeiro slot livre.";
            return true;
        }

        private void MarkHandForFusion(int index)
        {
            if (!SelectHandMaterial(index, true)) return;
            keyboardHandArmed = true;
            hud.Message = "Material " + fusionSelection.Count + " marcado: " +
                match.Player.Hand[index].Name +
                ". Setas navegam; CIMA marca e BAIXO desmarca.";
        }

        private void UnmarkHandFusion(int index)
        {
            fusionSelection.Remove(index);
            hud.FusionCount = fusionSelection.Count;
            hud.SelectedHandIndex = index;
            hud.FieldFusionPreview = null;
            if (fusionSelection.Count > 1 &&
                match.CountHandFusionSteps(DuelSide.Player, fusionSelection) > 0)
                hud.FieldFusionPreview =
                    match.PreviewResult(DuelSide.Player, fusionSelection);
            keyboardHandArmed = fusionSelection.Count > 0;
            hud.Message = "Material desmarcado. " +
                fusionSelection.Count + " materiais restantes.";
            UpdateLegalHighlights();
        }

        /// <summary>
        /// One exact, non-destructive auto-slot policy for keyboard and
        /// mouse double-click. Never replaces an occupied monster/support.
        /// If all five slots are occupied the player must explicitly choose
        /// a field fusion/equip target.
        /// </summary>
        private bool TryAutoPlaceSelected()
        {
            if (match == null || hud.InputLocked || match.PlayedThisTurn ||
                match.Phase != MatchPhase.Main ||
                fusionSelection.Count == 0) return false;
            var order = (IReadOnlyList<int>)new List<int>(fusionSelection);
            var result = match.PreviewResult(DuelSide.Player, order);
            if (result == null) return false;
            if (order.Count > 1 &&
                match.CountHandFusionSteps(DuelSide.Player, order) == 0)
            {
                hud.Message = "As cartas marcadas nao possuem fusao nesta ordem. " +
                    "Revise a receita ou jogue manualmente para uma troca " +
                    "intencional, sem descartar materiais por engano.";
                return false;
            }
            var lane = result.Kind == CardKind.Monster ?
                FieldLane.Monster : FieldLane.Support;
            for (int slot = 0; slot < DuelMatch.ZoneCount; slot++)
            {
                var empty = lane == FieldLane.Monster ?
                    match.Player.Monster(slot) == null :
                    match.Player.Support(slot) == null;
                if (!empty) continue;
                var zone = KeyboardZone(false, lane, slot);
                if (zone == null) continue;
                if (!match.CanPlay(DuelSide.Player, order, lane, slot,
                    result.StarA, out var reason))
                {
                    hud.Message = reason;
                    return false;
                }
                hud.Message = "Slot " + (slot + 1) + " selecionado automaticamente.";
                PlaceMatchCard(zone);
                return hud.PlacementWizardOpen;
            }
            hud.Message = lane == FieldLane.Monster ?
                "Monstros 1-5 ocupados: escolha um monstro para fusao/equip " +
                "ou passe o turno se ja tiver baixado uma carta." :
                "Suportes 1-5 ocupados: escolha uma zona livre ou um alvo de Equip.";
            return false;
        }

        private void ChooseFusionOption(AvailableFusion selected)
        {
            if (selected == null || match == null || hud.InputLocked ||
                match.Phase != MatchPhase.Main ||
                match.CurrentSide != DuelSide.Player || match.PlayedThisTurn)
                return;
            // A previously displayed result may become stale after a duel
            // event. Compare it with a fresh, read-only legal option set.
            AvailableFusion legal = null;
            foreach (var choice in match.FindAvailableFusions(DuelSide.Player))
            {
                if (choice.FieldSlot == selected.FieldSlot &&
                    choice.FirstHandIndex == selected.FirstHandIndex &&
                    choice.Result.Id == selected.Result.Id &&
                    SameHandOrder(choice.HandIndices,selected.HandIndices))
                {
                    legal = choice;
                    break;
                }
            }
            if (legal == null)
            {
                hud.Message = "O campo mudou: revise as fusoes disponiveis.";
                return;
            }
            fusionSelection.Clear();
            fusionSelection.AddRange(legal.HandIndices);
            hud.ConfigurePlacement(match.Player.Hand[legal.FirstHandIndex]);
            hud.SelectedHandIndex = legal.FirstHandIndex;
            hud.FusionCount = fusionSelection.Count;
            hud.FieldFusionPreview = legal.Result;
            keyboardHandArmed = true;
            lastClickHandIndex = -1;
            if (legal.UsesField)
            {
                var zone = KeyboardZone(false, FieldLane.Monster, legal.FieldSlot);
                if (zone != null)
                {
                    SetKeyboardFocus(KeyboardLocation.OwnMonster,legal.FieldSlot);
                    PlaceMatchCard(zone);
                }
            }
            else
            {
                SetKeyboardFocus(KeyboardLocation.Hand,legal.FirstHandIndex);
                TryAutoPlaceSelected();
            }
            if (!hud.PlacementWizardOpen)
                hud.Message = "Receita selecionada. Escolha uma zona legal para jogar.";
        }

        // Always recheck the full ordered chain, not only its first pair.
        private static bool SameHandOrder(IReadOnlyList<int> a,
            IReadOnlyList<int> b)
        {
            if (a == null || b == null || a.Count != b.Count) return false;
            for (int i = 0; i < a.Count; ++i)
                if (a[i] != b[i]) return false;
            return true;
        }

        private void SetKeyboardFocus(KeyboardLocation location, int index)
        {
            keyboardActive = true;
            keyboardLocation = location;
            keyboardIndex = Mathf.Clamp(index, 0, DuelMatch.ZoneCount - 1);
            ApplyKeyboardFocus();
        }

        private void ApplyKeyboardFocus()
        {
            if (!keyboardActive || hud == null || arena == null || !hud.DuelModeActive)
                return;
            if (keyboardLocation == KeyboardLocation.Hand)
            {
                int total = match == null ? 0 : match.Player.Hand.Count;
                keyboardIndex = Mathf.Clamp(keyboardIndex, 0,Mathf.Max(0,total-1));
                arena.SetHandFocus(true);
                arena.SetHandHover(total > 0 ? keyboardIndex : -1);
                if (total > 0 && hud.SelectedHandIndex < 0)
                    hud.HoveredCard = match.Player.Hand[keyboardIndex];
            }
            else
            {
                arena.SetHandHover(-1);
                var lane = keyboardLocation == KeyboardLocation.OwnMonster ||
                    keyboardLocation == KeyboardLocation.EnemyMonster ?
                        FieldLane.Monster : FieldLane.Support;
                bool opponent = keyboardLocation == KeyboardLocation.EnemyMonster ||
                    keyboardLocation == KeyboardLocation.EnemySupport;
                foreach (var zone in arena.Zones)
                {
                    if (zone == null) continue;
                    bool focus = zone.Lane == lane &&
                        zone.Opponent == opponent && zone.Index == keyboardIndex;
                    zone.SetHighlight(focus);
                }
            }
        }

        private void MoveKeyboardVertical(bool up)
        {
            switch (keyboardLocation)
            {
                case KeyboardLocation.Hand:
                    if (match.Player.Hand.Count == 0) return;
                    if (up) MarkHandForFusion(keyboardIndex);
                    else UnmarkHandFusion(keyboardIndex);
                    break;
                case KeyboardLocation.EnemySupport:
                    SetKeyboardFocus(up ? KeyboardLocation.EnemySupport :
                        KeyboardLocation.EnemyMonster, keyboardIndex);
                    break;
                case KeyboardLocation.EnemyMonster:
                    SetKeyboardFocus(up ? KeyboardLocation.EnemySupport :
                        KeyboardLocation.OwnMonster, keyboardIndex);
                    break;
                case KeyboardLocation.OwnMonster:
                    SetKeyboardFocus(up ? KeyboardLocation.EnemyMonster :
                        KeyboardLocation.OwnSupport, keyboardIndex);
                    break;
                case KeyboardLocation.OwnSupport:
                    SetKeyboardFocus(up ? KeyboardLocation.OwnMonster :
                        KeyboardLocation.Hand, keyboardIndex);
                    break;
            }
        }

        private CardDefinition KeyboardFocusedCard()
        {
            if (match == null) return null;
            switch (keyboardLocation)
            {
                case KeyboardLocation.Hand:
                    return keyboardIndex < match.Player.Hand.Count ?
                        match.Player.Hand[keyboardIndex] : null;
                case KeyboardLocation.OwnMonster:
                    return match.Player.Monster(keyboardIndex)?.Card;
                case KeyboardLocation.OwnSupport:
                    return match.Player.Support(keyboardIndex);
                case KeyboardLocation.EnemyMonster:
                    if (match.Opponent.Monster(keyboardIndex)?.FaceDown == true)
                        return null;
                    return match.Opponent.Monster(keyboardIndex)?.Card;
                case KeyboardLocation.EnemySupport:
                    if (match.Opponent.SupportIsFaceDown(keyboardIndex))
                        return null;
                    return match.Opponent.Support(keyboardIndex);
                default: return null;
            }
        }

        private void SelectKeyboardZone()
        {
            bool enemy = keyboardLocation == KeyboardLocation.EnemyMonster ||
                keyboardLocation == KeyboardLocation.EnemySupport;
            var lane = keyboardLocation == KeyboardLocation.OwnSupport ||
                keyboardLocation == KeyboardLocation.EnemySupport ?
                    FieldLane.Support : FieldLane.Monster;
            var zone = KeyboardZone(enemy, lane, keyboardIndex);
            if (zone == null) return;
            if (enemy)
            {
                if (match.Phase == MatchPhase.Battle && hud.SelectedAttacker >= 0)
                {
                    int target = match.Opponent.MonsterCount == 0 ? -1 :
                        lane == FieldLane.Monster ? keyboardIndex : -2;
                    if (target >= -1)
                        hud.RequestPlayerAttack(hud.SelectedAttacker,target);
                    else hud.Message = "Escolha o monstro adversario para atacar.";
                }
                else hud.Message = "Campo adversario: escolha um atacante primeiro.";
                return;
            }

            if (lane == FieldLane.Monster &&
                match.Player.Monster(keyboardIndex) != null &&
                hud.SelectedSupportSlot >= 0)
            {
                int selected = hud.SelectedSupportSlot;
                var card = match.Player.Support(selected);
                if (card != null && card.Kind == CardKind.Equip)
                {
                    if (match.CanEquipFromSupport(DuelSide.Player,selected,
                        keyboardIndex,out var rule,out var reason))
                        hud.BeginEquipChoice(card,match.Player.Monster(keyboardIndex),
                            rule,() => CommitSetEquip(selected,keyboardIndex),
                            null,true);
                    else hud.Message = reason;
                    return;
                }
            }

            if (match.Phase == MatchPhase.Main && !match.PlayedThisTurn &&
                fusionSelection.Count > 0)
            {
                PlaceMatchCard(zone);
                return;
            }
            if (lane == FieldLane.Monster &&
                match.Player.Monster(keyboardIndex) != null)
            {
                var monster = match.Player.Monster(keyboardIndex);
                hud.SelectedFieldMonster = keyboardIndex;
                hud.SelectedAttacker = match.Phase == MatchPhase.Battle &&
                    monster.Position == BattlePosition.Attack &&
                    !monster.AttackedThisTurn ? keyboardIndex : -1;
                hud.Message = hud.SelectedAttacker >= 0 ?
                    "Atacante selecionado. W vai para o oponente; Z confirma alvo." :
                    "Monstro selecionado. Q=ATK e E=DEF.";
                return;
            }
            if (lane == FieldLane.Support &&
                match.Player.Support(keyboardIndex) != null)
            {
                if (hud.SelectedSupportSlot == keyboardIndex &&
                    match.CanActivateSupport(DuelSide.Player,keyboardIndex,out _))
                {
                    hud.TryActivateSelectedSupport();
                    return;
                }
                hud.SelectedSupportSlot = keyboardIndex;
                hud.SelectedFieldMonster = -1;
                hud.Message = "Suporte selecionado; Z novamente ativa magia " +
                    "verificada ou navegue ate monstro para equipar.";
            }
        }

        private bool ProcessMatchKeyboard()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return false;
            if (hud.KeyboardSettingsOpen)
            {
                hud.CaptureKeyboardSetting(keyboard);
                return true;
            }
            if (hud.FusionAssistantOpen)
            {
                if (Shortcut(keyboard,RebornKeyAction.Cancel) ||
                    Shortcut(keyboard,RebornKeyAction.FusionAssistant) ||
                    keyboard.escapeKey.wasPressedThisFrame)
                    hud.CloseFusionAssistant();
                else if (Shortcut(keyboard,RebornKeyAction.FusionMarkUp) ||
                    Shortcut(keyboard,RebornKeyAction.NavigateLeft))
                    hud.MoveFusionAssistantChoice(-1);
                else if (Shortcut(keyboard,RebornKeyAction.FusionUnmarkDown) ||
                    Shortcut(keyboard,RebornKeyAction.NavigateRight))
                    hud.MoveFusionAssistantChoice(1);
                else if (Shortcut(keyboard,RebornKeyAction.Select))
                    hud.ConfirmFusionAssistantChoice();
                return true;
            }
            // F1 is a dedicated rescue shortcut even after the user rebinds
            // the configured keys. Mouse settings button stays available.
            if (keyboard.f1Key.wasPressedThisFrame)
            {
                hud.ToggleKeyboardSettings();
                return true;
            }
            if (hud.StarterSelectionRequired)
            {
                if (Shortcut(keyboard,RebornKeyAction.NavigateLeft) ||
                    Shortcut(keyboard,RebornKeyAction.FusionUnmarkDown))
                    hud.MoveStarterChoice(-1);
                else if (Shortcut(keyboard,RebornKeyAction.NavigateRight) ||
                    Shortcut(keyboard,RebornKeyAction.FusionMarkUp))
                    hud.MoveStarterChoice(1);
                else if (Shortcut(keyboard,RebornKeyAction.Select))
                    hud.ConfirmStarterChoice();
                return true;
            }
            if (hud.PlacementWizardOpen)
            {
                if (Shortcut(keyboard,RebornKeyAction.Cancel) ||
                    keyboard.escapeKey.wasPressedThisFrame)
                    hud.CancelPlacementChoices();
                else if (Shortcut(keyboard,RebornKeyAction.AttackPosition))
                    hud.RotatePlacementPreview(false);
                else if (Shortcut(keyboard,RebornKeyAction.DefensePosition))
                    hud.RotatePlacementPreview(true);
                else if (Shortcut(keyboard,RebornKeyAction.NavigateLeft))
                    hud.ChoosePlacementOption(false);
                else if (Shortcut(keyboard,RebornKeyAction.NavigateRight))
                    hud.ChoosePlacementOption(true);
                else if (Shortcut(keyboard,RebornKeyAction.Select))
                    hud.ConfirmPlacementChoice();
                return true;
            }
            if (hud.EquipChoiceOpen)
            {
                if (Shortcut(keyboard,RebornKeyAction.Cancel) ||
                    keyboard.escapeKey.wasPressedThisFrame)
                    hud.CancelEquipChoice();
                else if (Shortcut(keyboard,RebornKeyAction.DefensePosition))
                    hud.ConfirmEquipSetChoice();
                else if (Shortcut(keyboard,RebornKeyAction.Select))
                    hud.ConfirmEquipChoice();
                return true;
            }
            if (hud.CardInspectionOpen)
            {
                if ((Mouse.current != null &&
                     Mouse.current.rightButton.wasPressedThisFrame) ||
                    Shortcut(keyboard,RebornKeyAction.Cancel) ||
                    Shortcut(keyboard,RebornKeyAction.InspectCard) ||
                    keyboard.escapeKey.wasPressedThisFrame)
                    hud.CloseCardInspection();
                return true;
            }
            if (hud.InputLocked || match.CurrentSide != DuelSide.Player ||
                match.Phase == MatchPhase.Finished)
                return false;

            if (Shortcut(keyboard,RebornKeyAction.FusionAssistant))
            {
                hud.OpenFusionAssistant(null);
                return true;
            }
            if (Shortcut(keyboard,RebornKeyAction.PassTurn))
            {
                hud.TryPassPlayerTurn();
                return true;
            }
            if (Shortcut(keyboard,RebornKeyAction.Cancel) ||
                keyboard.escapeKey.wasPressedThisFrame)
            {
                ClearPlayerSelection();
                hud.Message = "Selecao cancelada (X).";
                keyboardActive = true;
                ApplyKeyboardFocus();
                return true;
            }
            if (Shortcut(keyboard,RebornKeyAction.InspectCard))
            {
                if (!keyboardActive && hoveredCard != null)
                    InspectKnownCard(hoveredCard);
                else
                {
                    var card = KeyboardFocusedCard();
                    if (card != null) hud.ToggleCardInspection(card);
                    else hud.Message = "Carta nao revelada ou espaco vazio.";
                }
                keyboardActive = true;
                return true;
            }
            bool requestAttack = Shortcut(keyboard,RebornKeyAction.AttackPosition);
            bool requestDefense = Shortcut(keyboard,RebornKeyAction.DefensePosition);
            if (requestAttack || requestDefense)
            {
                if (keyboardLocation == KeyboardLocation.OwnMonster &&
                    match.Player.Monster(keyboardIndex) != null)
                    hud.SelectedFieldMonster = keyboardIndex;
                hud.TrySetSelectedMonsterPosition(requestAttack ?
                    BattlePosition.Attack : BattlePosition.Defense);
                keyboardActive = true;
                ApplyKeyboardFocus();
                return true;
            }
            if (Shortcut(keyboard,RebornKeyAction.OpponentBoard))
            {
                SetKeyboardFocus(keyboardLocation == KeyboardLocation.EnemyMonster ||
                    keyboardLocation == KeyboardLocation.EnemySupport ?
                    KeyboardLocation.Hand : KeyboardLocation.EnemyMonster,
                    keyboardIndex);
                hud.Message = keyboardLocation == KeyboardLocation.Hand ?
                    "Seletor na mao." :
                    "Mesa do oponente. Setas navegam; BAIXO volta ao seu campo.";
                return true;
            }
            if (Shortcut(keyboard,RebornKeyAction.NavigateLeft) ||
                Shortcut(keyboard,RebornKeyAction.NavigateRight))
            {
                int direction = Shortcut(keyboard,RebornKeyAction.NavigateLeft) ?
                    -1 : +1;
                int maxIndex = keyboardLocation == KeyboardLocation.Hand ?
                    Math.Max(0, match.Player.Hand.Count-1) : DuelMatch.ZoneCount-1;
                keyboardIndex = Mathf.Clamp(keyboardIndex+direction,0,maxIndex);
                // Navigation does NOT cancel the ordered materials already
                // marked with Up. Z may confirm after moving the cursor.
                SetKeyboardFocus(keyboardLocation,keyboardIndex);
                return true;
            }
            if (Shortcut(keyboard,RebornKeyAction.FusionMarkUp) ||
                Shortcut(keyboard,RebornKeyAction.FusionUnmarkDown))
            {
                MoveKeyboardVertical(Shortcut(keyboard,RebornKeyAction.FusionMarkUp));
                ApplyKeyboardFocus();
                return true;
            }
            if (Shortcut(keyboard,RebornKeyAction.Select))
            {
                if (keyboardLocation == KeyboardLocation.Hand)
                {
                    if (fusionSelection.Count > 0)
                        TryAutoPlaceSelected();
                    else if (SelectHandMaterial(keyboardIndex,false))
                        keyboardHandArmed = true;
                }
                else SelectKeyboardZone();
                keyboardActive = true;
                ApplyKeyboardFocus();
                return true;
            }
            return false;
        }
    }
}
