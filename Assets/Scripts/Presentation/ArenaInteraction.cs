using System;
using Reborn.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Reborn.Presentation
{
    /// <summary>Traduz gestos para comandos; somente DuelSession modifica o estado do campo.</summary>
    public sealed class ArenaInteraction : MonoBehaviour
    {
        private DuelSession session;
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
            UpdateLegalHighlights();
        }

        private void Update()
        {
            if (session == null || arena.Camera == null || hud == null) return;
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
                zone.SetLegal(!zone.Opponent &&
                    session.CanPlace(lastSelectedHand, zone.Lane, zone.Index, out _));
        }
    }
}
