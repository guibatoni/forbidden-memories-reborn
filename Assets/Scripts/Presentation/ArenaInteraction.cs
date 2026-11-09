using System;
using System.Collections.Generic;
using Reborn.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Reborn.Presentation
{
    /// <summary>Traduz gestos para comandos; somente DuelSession modifica o estado do campo.</summary>
    public sealed class ArenaInteraction : MonoBehaviour
    {
        private DuelSession session;
        private DuelMatch match;
        private readonly List<int> fusionSelection = new List<int>();
        private ArenaFactory arena;
        private ArenaHud hud;
        private int pressedHand = -1;
        private int lastSelectedHand = -2;
        private bool dragging;
        private Vector2 pressStart;
        private ZoneView hoveredZone;
        private CardView hoveredCard;

        public void Initialize(DuelSession model, ArenaFactory factory, ArenaHud ui)
        {
            session = model;
            arena = factory;
            hud = ui;
            session.Changed += () =>
            {
                hud.SelectedHandIndex = -1;
                lastSelectedHand = -2;
                arena.Refresh(session);
                UpdateLegalHighlights();
            };
            arena.Refresh(session);
            hud.ModeChanged += active =>
            {
                pressedHand = -1; dragging = false;
                fusionSelection.Clear(); hud.FusionCount = 0;
                hud.SelectedHandIndex = -1;
                if (active && match != null) arena.RefreshMatch(match);
                else arena.Refresh(session);
                UpdateLegalHighlights();
            };
            UpdateLegalHighlights();
        }

        public void InitializeMatch(DuelMatch model)
        {
            if (match != null) match.Changed -= OnMatchChanged;
            match = model;
            match.Changed += OnMatchChanged;
            fusionSelection.Clear();
            hud.FusionCount = 0;
        }

        private void OnMatchChanged()
        {
            if (!hud.DuelModeActive) return;
            hud.SelectedHandIndex = -1;
            hud.HoveredCard = null;
            hud.SelectedAttacker = -1;
            hud.PreviewTargetSlot = -1;
            fusionSelection.Clear();
            hud.FusionCount = 0;
            arena.RefreshMatch(match);
            UpdateLegalHighlights();
        }

        private void Update()
        {
            if (session == null || arena.Camera == null || hud == null) return;
            if (hud.DuelModeActive) { UpdateMatchInput(); return; }
            var mouse = Mouse.current;
            if (mouse == null) return;
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                CancelSelection();
                hud.Message = "Selecao cancelada.";
            }

            Vector2 pointer = mouse.position.ReadValue();
            bool overHud = hud.IsPointerOverHud(pointer);
            FindPointerTargets(pointer, dragging ? pressedHand : -1,
                out CardView card, out ZoneView zone);
            if (overHud) { card = null; zone = null; }

            if (hoveredCard != card)
            {
                if (hoveredCard != null) hoveredCard.SetHovered(false);
                hoveredCard = card;
                if (hoveredCard != null && hoveredCard.HandIndex >= 0)
                    hoveredCard.SetHovered(true);
                hud.HoveredCard = hoveredCard != null && !hoveredCard.Facedown
                    ? hoveredCard.Card : null;
            }
            if (hoveredZone != zone)
            {
                if (hoveredZone != null) hoveredZone.SetHighlight(false);
                hoveredZone = zone;
                if (hoveredZone != null && !hoveredZone.Opponent)
                    hoveredZone.SetHighlight(true);
            }

            if (mouse.leftButton.wasPressedThisFrame && !overHud)
            {
                if (hud.SandboxRemovalMode && zone != null && !zone.Opponent)
                {
                    if (session.TryRemoveSandbox(zone.Lane, zone.Index, out var error))
                    {
                        arena.PlaySandboxRemoval(zone.Lane, zone.Index);
                        hud.Message = "Remocao demonstrativa: nao representa combate original.";
                    }
                    else hud.Message = error;
                    return;
                }
                if (card != null && card.HandIndex >= 0)
                {
                    pressedHand = card.HandIndex;
                    pressStart = pointer;
                    hud.SelectedHandIndex = pressedHand;
                    hud.Message = $"Selecionada: {card.Card.Name}. Escolha uma zona compativel.";
                }
                else if (zone != null && hud.SelectedHandIndex >= 0)
                {
                    TryPlace(zone, hud.SelectedHandIndex);
                }
            }

            if (pressedHand >= 0 && mouse.leftButton.isPressed &&
                (dragging || (pointer - pressStart).sqrMagnitude >= 144f))
            {
                dragging = true;
                var selectedView = FindHandView(pressedHand);
                if (selectedView != null && !overHud)
                {
                    var plane = new Plane(Vector3.up, new Vector3(0,0.8f,0));
                    if (plane.Raycast(arena.Camera.ScreenPointToRay(pointer), out float distance))
                        selectedView.SetDragWorldPosition(
                            arena.Camera.ScreenPointToRay(pointer).GetPoint(distance) + Vector3.up * 0.35f);
                }
                FindPointerTargets(pointer, pressedHand, out _, out zone);
            }

            if (mouse.leftButton.wasReleasedThisFrame && pressedHand >= 0)
            {
                var pressedView = FindHandView(pressedHand);
                // Busca a zona sob a carta arrastada, ignorando seus colliders.
                FindPointerTargets(pointer, pressedHand, out _, out var releasedZone);
                if (!overHud && releasedZone != null)
                    TryPlace(releasedZone, pressedHand);
                if (pressedView != null) pressedView.StopDragging();
                pressedHand = -1;
                dragging = false;
            }

            if (hud.SelectedHandIndex != lastSelectedHand)
                UpdateLegalHighlights();
            foreach (var view in arena.HandViews)
                view.SetSelected(view.HandIndex == hud.SelectedHandIndex);
        }

        private void UpdateMatchInput()
        {
            if (match == null || arena.Camera == null) return;
            if (match.Phase == MatchPhase.Finished || hud.InputLocked ||
                match.CurrentSide != DuelSide.Player)
            {
                // Bloqueia cliques na vez da IA e na tela final.
                if (hoveredCard != null) hoveredCard.SetHovered(false);
                if (hoveredZone != null) hoveredZone.SetHighlight(false);
                hoveredCard = null;
                hoveredZone = null;
                hud.HoveredCard = null;
                hud.PreviewTargetSlot = -1;
                return;
            }
            var mouse = Mouse.current;
            if (mouse == null) return;
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                hud.SelectedHandIndex = -1;
                hud.SelectedAttacker = -1;
                hud.PreviewTargetSlot = -1;
                fusionSelection.Clear(); hud.FusionCount = 0;
                pressedHand = -1; dragging = false;
                hud.Message = "Selecao cancelada.";
            }
            Vector2 pointer = mouse.position.ReadValue();
            bool overHud = hud.IsPointerOverHud(pointer);
            FindPointerTargets(pointer,dragging ? pressedHand : -1,
                out CardView card,out ZoneView zone);
            if (overHud) { card = null; zone = null; }
            hud.PreviewTargetSlot = hud.AttackMode && hud.SelectedAttacker >= 0 &&
                zone != null && zone.Opponent && zone.Lane == FieldLane.Monster
                ? zone.Index : -1;
            if (hoveredCard != card)
            {
                if (hoveredCard != null) hoveredCard.SetHovered(false);
                hoveredCard = card;
                if (hoveredCard != null && hoveredCard.HandIndex >= 0)
                    hoveredCard.SetHovered(true);
                hud.HoveredCard = hoveredCard != null && !hoveredCard.Facedown
                    ? hoveredCard.Card : null;
            }
            if (mouse.leftButton.wasPressedThisFrame && !overHud)
            {
                if (card != null && card.HandIndex >= 0 && !hud.AttackMode)
                {
                    int index = card.HandIndex;
                    bool shift = Keyboard.current != null &&
                        (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed);
                    if (!shift) fusionSelection.Clear();
                    if (!fusionSelection.Contains(index)) fusionSelection.Add(index);
                    hud.FusionCount = fusionSelection.Count;
                    hud.SelectedHandIndex = index;
                    pressedHand = index;
                    pressStart = pointer;
                    var outcome = match.PreviewResult(DuelSide.Player,fusionSelection);
                    hud.Message = outcome == null ? "Selecao invalida." :
                        "Selecionadas " + fusionSelection.Count + " carta(s). Resultado previsto: " + outcome.Name;
                    UpdateLegalHighlights();
                }
                else if (zone != null && zone.Lane == FieldLane.Monster && hud.AttackMode)
                {
                    if (!zone.Opponent)
                    {
                        var monster = match.Player.Monster(zone.Index);
                        if (monster == null) hud.Message = "Escolha um monstro seu.";
                        else
                        {
                            hud.SelectedAttacker = zone.Index;
                            hud.Message = "Atacante selecionado: " + monster.Card.Name + ". Clique no alvo.";
                            UpdateLegalHighlights();
                        }
                    }
                    else if (hud.SelectedAttacker >= 0)
                    {
                        hud.RequestPlayerAttack(hud.SelectedAttacker,zone.Index);
                    }
                }
                else if (zone != null && !zone.Opponent && hud.SelectedHandIndex >= 0)
                    PlaceMatchCard(zone);
            }
            if (pressedHand >= 0 && mouse.leftButton.isPressed &&
                (dragging || (pointer-pressStart).sqrMagnitude >= 144f))
            {
                dragging = true;
                var view = FindHandView(pressedHand);
                if (view != null && !overHud)
                {
                    var plane = new Plane(Vector3.up,new Vector3(0,0.8f,0));
                    var ray = arena.Camera.ScreenPointToRay(pointer);
                    if (plane.Raycast(ray,out float distance))
                        view.SetDragWorldPosition(ray.GetPoint(distance)+Vector3.up*0.35f);
                }
            }
            if (mouse.leftButton.wasReleasedThisFrame && pressedHand >= 0)
            {
                var view = FindHandView(pressedHand);
                FindPointerTargets(pointer,pressedHand,out _,out var releasedZone);
                if (!overHud && dragging && releasedZone != null && !releasedZone.Opponent)
                    PlaceMatchCard(releasedZone);
                if (view != null) view.StopDragging();
                pressedHand = -1;
                dragging = false;
            }
            UpdateLegalHighlights();
            foreach (var view in arena.HandViews)
                view.SetSelected(fusionSelection.Contains(view.HandIndex));
        }

        private void PlaceMatchCard(ZoneView zone)
        {
            var selected = fusionSelection.Count > 0 ?
                (IReadOnlyList<int>)fusionSelection : new [] { hud.SelectedHandIndex };
            var result = match.PreviewResult(DuelSide.Player,selected);
            if (result == null) { hud.Message = "Selecao invalida."; return; }
            var star = hud.UseSecondaryStar ? result.StarB : result.StarA;
            var position = hud.UseDefense ? BattlePosition.Defense : BattlePosition.Attack;
            var source = FindHandView(hud.SelectedHandIndex);
            var origin = source != null ? source.transform.position : zone.transform.position;
            arena.StagePlacement(zone.Lane,zone.Index,origin,hud.AnimationsEnabled);
            if (match.TryPlay(DuelSide.Player,selected,zone.Lane,zone.Index,position,star,out var reason))
                hud.Message = "Carta colocada: " + result.Name + ".";
            else
            {
                arena.ClearStagedPlacement();
                hud.Message = reason;
            }
            UpdateLegalHighlights();
        }

        private CardView FindHandView(int handIndex)
        {
            foreach (var view in arena.HandViews)
                if (view != null && view.HandIndex == handIndex) return view;
            return null;
        }

        private void FindPointerTargets(Vector2 pointer, int ignoredHand,
            out CardView card, out ZoneView zone)
        {
            card = null;
            zone = null;
            var hits = Physics.RaycastAll(arena.Camera.ScreenPointToRay(pointer), 150f);
            Array.Sort(hits, (a,b) => a.distance.CompareTo(b.distance));
            foreach (var hit in hits)
            {
                var foundCard = hit.collider.GetComponentInParent<CardView>();
                if (foundCard != null && foundCard.HandIndex == ignoredHand && ignoredHand >= 0)
                    continue;
                if (card == null && foundCard != null) card = foundCard;
                if (zone == null)
                {
                    var foundZone = hit.collider.GetComponentInParent<ZoneView>();
                    if (foundZone != null) zone = foundZone;
                }
                if (card != null && zone != null) return;
            }
        }

        private void TryPlace(ZoneView zone, int handIndex)
        {
            if (zone.Opponent)
            { hud.Message = "O campo adversario nao aceita comandos."; return; }
            var source = FindHandView(handIndex);
            var origin = source != null ? source.transform.position : zone.transform.position;
            arena.StagePlacement(zone.Lane,zone.Index,origin,hud.AnimationsEnabled);
            if (session.TryPlace(handIndex,zone.Lane,zone.Index,out var reason))
                hud.Message = $"Carta colocada em {zone.Lane}, posicao {zone.Index+1}.";
            else
            {
                arena.ClearStagedPlacement();
                hud.Message = reason;
            }
        }

        private void CancelSelection()
        {
            var pressedView = FindHandView(pressedHand);
            if (pressedView != null) pressedView.StopDragging();
            hud.SelectedHandIndex = -1;
            pressedHand = -1;
            dragging = false;
        }

        private void UpdateLegalHighlights()
        {
            if (session == null || arena == null || hud == null) return;
            lastSelectedHand = hud.SelectedHandIndex;
            foreach (var zone in arena.Zones)
            {
                if (hud.DuelModeActive && match != null)
                {
                    if (hud.AttackMode && match.Phase == MatchPhase.Battle)
                    {
                        bool eligible = zone.Lane == FieldLane.Monster &&
                            (zone.Opponent
                                ? hud.SelectedAttacker >= 0 && match.Opponent.Monster(zone.Index) != null
                                : match.Player.Monster(zone.Index) != null &&
                                  !match.Player.Monster(zone.Index).AttackedThisTurn &&
                                  match.Player.Monster(zone.Index).Position == BattlePosition.Attack);
                        zone.SetCombatTarget(eligible);
                        continue;
                    }
                    var selection = fusionSelection.Count > 0 ?
                        (IReadOnlyList<int>)fusionSelection : new [] { lastSelectedHand };
                    var card = match.PreviewResult(DuelSide.Player,selection);
                    var star = card == null ? GuardianStar.None :
                        (hud.UseSecondaryStar ? card.StarB : card.StarA);
                    zone.SetLegal(!zone.Opponent &&
                        match.CanPlay(DuelSide.Player,selection,zone.Lane,zone.Index,star,out _));
                }
                else zone.SetLegal(!zone.Opponent &&
                    session.CanPlace(lastSelectedHand, zone.Lane, zone.Index, out _));
            }
        }
    }
}
