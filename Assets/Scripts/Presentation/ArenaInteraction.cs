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
                hud.SelectedFieldMonster = -1;
                hud.SelectedSupportSlot = -1;
                arena.SetAttackAim(-1, -1);
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
            hud.SelectedFieldMonster = -1;
            hud.SelectedSupportSlot = -1;
            hud.PreviewTargetSlot = -1;
            fusionSelection.Clear();
            hud.FusionCount = 0;
            hud.FieldFusionPreview = null;
            hud.CloseCardInspection();
            arena.SetAttackAim(-1, -1);
            arena.RefreshMatch(match);
            UpdateLegalHighlights();
        }

        private void Update()
        {
            if (session == null || arena.Camera == null || hud == null) return;
            if (hud.DuelModeActive) { UpdateMatchInput(); return; }
            var mouse = Mouse.current;
            if (mouse == null) return;
            if (hud.CardInspectionOpen)
            {
                if (mouse.rightButton.wasPressedThisFrame ||
                    (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame))
                    hud.CloseCardInspection();
                return;
            }
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
            if (mouse.rightButton.wasPressedThisFrame && !overHud)
            {
                InspectKnownCard(card);
                return;
            }
            // Expand on hand hover (or its stable projected screen region).
            // Dragging holds the fan open until the user releases the card.
            arena.SetHandFocus(!overHud && (dragging ||
                (card != null && card.HandIndex >= 0) ||
                arena.IsPointerNearHand(pointer)));
            // Pin the current card while holding the mouse button, so the
            // neighbor animation cannot steal the target mid-drag.
            int handHover = pressedHand >= 0 && mouse.leftButton.isPressed
                ? pressedHand : card != null && card.HandIndex >= 0
                    ? card.HandIndex : -1;
            arena.SetHandHover(!overHud ? handHover : -1);

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
                hud.FieldFusionPreview = null;
                arena.SetAttackAim(-1, -1);
                arena.SetHandFocus(false);
                arena.SetHandHover(-1);
                return;
            }
            var mouse = Mouse.current;
            if (mouse == null) return;
            if (hud.CardInspectionOpen)
            {
                if (mouse.rightButton.wasPressedThisFrame ||
                    (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame))
                    hud.CloseCardInspection();
                return;
            }
            var keys = Keyboard.current;
            if (keys != null && keys.spaceKey.wasPressedThisFrame)
            {
                hud.TryPassPlayerTurn();
                return;
            }
            if (keys != null && (keys.qKey.wasPressedThisFrame ||
                keys.eKey.wasPressedThisFrame))
            {
                hud.TryChangeSelectedMonster();
                UpdateLegalHighlights();
                return;
            }
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                hud.SelectedHandIndex = -1;
                hud.SelectedAttacker = -1;
                hud.SelectedFieldMonster = -1;
                hud.SelectedSupportSlot = -1;
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
            hud.FieldFusionPreview = null;
            if (zone != null && !zone.Opponent &&
                zone.Lane == FieldLane.Monster &&
                match.Phase == MatchPhase.Main &&
                fusionSelection.Count > 0 &&
                match.Player.Monster(zone.Index) != null)
            {
                // Pure preview. No material or turn is consumed while hovering.
                var selectedCard = hud.SelectedHandIndex >= 0 &&
                    hud.SelectedHandIndex < match.Player.Hand.Count
                    ? match.Player.Hand[hud.SelectedHandIndex] : null;
                bool ritualPreview = fusionSelection.Count == 1 &&
                    selectedCard != null && selectedCard.Kind == CardKind.Ritual;
                var prediction = ritualPreview ?
                    match.PreviewRitual(DuelSide.Player, hud.SelectedHandIndex) :
                    match.PreviewFieldResult(DuelSide.Player, fusionSelection, zone.Index);
                if (prediction != null && prediction.Kind == CardKind.Monster)
                    hud.FieldFusionPreview = prediction;
            }
            if (mouse.rightButton.wasPressedThisFrame && !overHud)
            {
                InspectKnownCard(card);
                return;
            }
            arena.SetHandFocus(!overHud && (dragging ||
                (card != null && card.HandIndex >= 0) ||
                arena.IsPointerNearHand(pointer)));
            // Pin the current card while holding the mouse button, so the
            // neighbor animation cannot steal the target mid-drag.
            int handHover = pressedHand >= 0 && mouse.leftButton.isPressed
                ? pressedHand : card != null && card.HandIndex >= 0
                    ? card.HandIndex : -1;
            arena.SetHandHover(!overHud ? handHover : -1);
            // The same authoritative rule gate controls preview and aim.
            // A face-down defender never exposes its contents via the arc.
            int aimTarget = -2;
            if (!overHud && hud.AttackMode && hud.SelectedAttacker >= 0 &&
                match.Phase == MatchPhase.Battle)
            {
                if (zone != null && zone.Opponent)
                {
                    if (match.Opponent.MonsterCount == 0)
                        aimTarget = -1;
                    else if (zone.Lane == FieldLane.Monster &&
                        match.Opponent.Monster(zone.Index) != null)
                        aimTarget = zone.Index;
                }
                else if (zone == null && match.Opponent.MonsterCount == 0 &&
                    PointerOverOpponentField(pointer))
                    aimTarget = -1;
            }
            bool legalAim = aimTarget >= -1 &&
                match.CanAttack(DuelSide.Player, hud.SelectedAttacker, aimTarget, out _);
            arena.SetAttackAim(legalAim ? hud.SelectedAttacker : -1,
                legalAim ? aimTarget : -1);
            hud.PreviewTargetSlot = legalAim && aimTarget >= 0 ? aimTarget : -1;
            if (hoveredZone != zone)
            {
                if (hoveredZone != null) hoveredZone.SetHighlight(false);
                hoveredZone = zone;
                if (hoveredZone != null) hoveredZone.SetHighlight(true);
            }
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
                if (card != null && card.HandIndex >= 0)
                {
                    if (match.Phase != MatchPhase.Main || match.PlayedThisTurn)
                        hud.Message = "Sua jogada ja foi usada. Ataque ou passe o turno.";
                    else
                    {
                        int index = card.HandIndex;
                        bool shift = Keyboard.current != null &&
                            (Keyboard.current.leftShiftKey.isPressed ||
                             Keyboard.current.rightShiftKey.isPressed);
                        if (!shift) fusionSelection.Clear();
                        if (fusionSelection.Count == 0) hud.ConfigurePlacement(card.Card);
                        if (!fusionSelection.Contains(index) &&
                            fusionSelection.Count < DuelMatch.HandLimit)
                            fusionSelection.Add(index);
                        hud.FusionCount = fusionSelection.Count;
                        hud.SelectedHandIndex = index;
                        pressedHand = index;
                        pressStart = pointer;
                        var outcome = match.PreviewResult(DuelSide.Player,fusionSelection);
                        var ritual = match.PreviewRitual(DuelSide.Player, index);
                        hud.Message = card.Card.Kind == CardKind.Ritual ?
                            (ritual == null ?
                                "Ritual: exige 3 monstros especificos revelados no campo." :
                                "Ritual pronto: escolha uma zona de tributo para invocar " +
                                ritual.Name + ".") :
                            outcome == null ? "Selecao invalida." :
                            "Materiais: " + fusionSelection.Count + "/5. " + outcome.Name +
                            " | clique uma zona vazia ou coloque sobre seu monstro para fundir.";
                    }
                }
                else if (zone != null && !zone.Opponent &&
                    zone.Lane == FieldLane.Monster &&
                    match.Player.Monster(zone.Index) != null &&
                    match.Phase == MatchPhase.Main &&
                    hud.SelectedHandIndex >= 0 && fusionSelection.Count > 0)
                {
                    // Existing field monster is first, followed by selected
                    // hand cards in the exact Shift-click sequence.
                    PlaceMatchCard(zone);
                }
                else if (zone != null && !zone.Opponent &&
                    zone.Lane == FieldLane.Monster &&
                    match.Player.Monster(zone.Index) != null)
                {
                    var monster = match.Player.Monster(zone.Index);
                    hud.SelectedFieldMonster = zone.Index;
                    hud.SelectedSupportSlot = -1;
                    hud.SelectedAttacker = match.Phase == MatchPhase.Battle &&
                        monster.Position == BattlePosition.Attack &&
                        !monster.AttackedThisTurn ? zone.Index : -1;
                    hud.Message = hud.SelectedAttacker >= 0
                        ? "Atacante " + monster.Card.Name + ": clique no alvo adversario."
                        : "Monstro selecionado. Alterar ATK/DEF no painel, se permitido.";
                }
                else if (zone != null && zone.Opponent &&
                    match.Phase == MatchPhase.Battle && hud.SelectedAttacker >= 0)
                {
                    // Any enemy zone works for direct attack only when the
                    // opponent's monster row is actually empty.
                    int target = match.Opponent.MonsterCount == 0 ? -1 :
                        zone.Lane == FieldLane.Monster ? zone.Index : -2;
                    if (target == -2) hud.Message = "Ataque o monstro adversario primeiro.";
                    else hud.RequestPlayerAttack(hud.SelectedAttacker, target);
                }
                else if (zone != null && !zone.Opponent &&
                    zone.Lane == FieldLane.Support &&
                    match.Player.Support(zone.Index) != null)
                {
                    hud.SelectedSupportSlot = zone.Index;
                    hud.SelectedFieldMonster = -1;
                    hud.SelectedAttacker = -1;
                    hud.Message = "Suporte selecionado. Efeitos aguardam validacao.";
                }
                else if (zone != null && !zone.Opponent &&
                    match.Phase == MatchPhase.Main && hud.SelectedHandIndex >= 0)
                    PlaceMatchCard(zone);
                else if (hud.SelectedAttacker >= 0 &&
                    match.Phase == MatchPhase.Battle &&
                    match.Opponent.MonsterCount == 0 &&
                    PointerOverOpponentField(pointer))
                    hud.RequestPlayerAttack(hud.SelectedAttacker, -1);
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
            {
                view.SetSelected(fusionSelection.Contains(view.HandIndex));
                view.SetFusionOrder(fusionSelection.IndexOf(view.HandIndex) + 1);
            }
        }

        private void InspectKnownCard(CardView card)
        {
            if (card == null || card.Card == null) return;
            // The player can inspect their own set cards, but never hidden
            // opponent identities until the duel engine reveals them.
            bool owned = card.HandIndex >= 0 ||
                card.gameObject.name.StartsWith("Player_") ||
                card.gameObject.name.StartsWith("Placed_");
            if (card.Facedown && !owned)
            {
                hud.Message = "Carta adversaria oculta ate ser revelada.";
                return;
            }
            hud.ToggleCardInspection(card.Card);
        }

        private void PlaceMatchCard(ZoneView zone)
        {
            if (zone == null || zone.Opponent ||
                match.Phase != MatchPhase.Main || match.PlayedThisTurn) return;
            var selected = fusionSelection.Count > 0 ?
                (IReadOnlyList<int>)fusionSelection : new [] { hud.SelectedHandIndex };
            bool onField = zone.Lane == FieldLane.Monster &&
                match.Player.Monster(zone.Index) != null;
            bool ritualPlay = onField && selected.Count == 1 &&
                selected[0] >= 0 && selected[0] < match.Player.Hand.Count &&
                match.Player.Hand[selected[0]].Kind == CardKind.Ritual;
            if (ritualPlay)
            {
                var expected = match.PreviewRitual(DuelSide.Player, selected[0]);
                if (expected == null)
                {
                    hud.Message = "Ritual requer tres tributos especificos virados para cima.";
                    return;
                }
                var ritualStar = hud.UseSecondaryStar ?
                    expected.StarB : expected.StarA;
                if (!match.CanSummonRitual(DuelSide.Player, selected[0],
                    zone.Index, ritualStar, out _, out var previewReason))
                {
                    hud.Message = previewReason;
                    return;
                }
                var sourceView = FindHandView(selected[0]);
                var from = sourceView != null ?
                    sourceView.transform.position : zone.transform.position;
                arena.StagePlacement(FieldLane.Monster, zone.Index, from,
                    hud.AnimationsEnabled);
                if (match.TrySummonRitual(DuelSide.Player, selected[0], zone.Index,
                    hud.UseDefense ? BattlePosition.Defense : BattlePosition.Attack,
                    ritualStar, out var summoned, out var ritualReason))
                {
                    arena.PlayRitualCeremony(DuelSide.Player, zone.Index,
                        hud.AnimationsEnabled);
                    hud.Announce("Ritual invocado: " + summoned.Name + "!");
                    hud.AfterHandPlacement();
                    hud.SelectedFieldMonster = zone.Index;
                }
                else
                {
                    arena.ClearStagedPlacement();
                    hud.Message = ritualReason;
                }
                UpdateLegalHighlights();
                return;
            }
            // Unlike a hand-only combination, field material is FIRST.
            var result = onField ?
                match.PreviewFieldResult(DuelSide.Player, selected, zone.Index) :
                match.PreviewResult(DuelSide.Player, selected);
            if (result == null) { hud.Message = "Selecao invalida."; return; }
            var star = hud.UseSecondaryStar ? result.StarB : result.StarA;
            var position = hud.UseDefense ? BattlePosition.Defense : BattlePosition.Attack;
            var source = FindHandView(hud.SelectedHandIndex);
            var origin = source != null ? source.transform.position : zone.transform.position;

            if (onField)
            {
                if (!match.CanPlayOnField(DuelSide.Player, selected, zone.Index,
                    star, out _, out var previewReason))
                {
                    hud.Message = previewReason;
                    return; // Never animate or consume cards for invalid effects.
                }
                arena.StagePlacement(zone.Lane, zone.Index, origin, hud.AnimationsEnabled);
                if (match.TryPlayOnField(DuelSide.Player, selected, zone.Index,
                    position, star, hud.PlaceFaceDown,
                    out var fieldResult, out bool fused, out var reason))
                {
                    arena.PlayFieldCombinationEffect(DuelSide.Player, zone.Index,
                        fused, hud.AnimationsEnabled);
                    hud.Announce(fused ? "Fusao no campo: " + fieldResult.Name :
                        "Sem receita de fusao: " + fieldResult.Name +
                        " substituiu o monstro anterior.");
                    hud.AfterHandPlacement();
                    hud.SelectedFieldMonster = zone.Index;
                }
                else
                {
                    arena.ClearStagedPlacement();
                    hud.Message = reason;
                }
            }
            else
            {
                arena.StagePlacement(zone.Lane, zone.Index, origin, hud.AnimationsEnabled);
                int eventOffset = match.Events.Count;
                if (match.TryPlay(DuelSide.Player, selected, zone.Lane, zone.Index,
                    position, star, hud.PlaceFaceDown, out var reason))
                {
                    bool actuallyFused = false;
                    for (int evt = eventOffset; evt < match.Events.Count; evt++)
                        if (match.Events[evt].Kind == MatchEventKind.Fusion &&
                            match.Events[evt].Side == DuelSide.Player)
                            actuallyFused = true;
                    if (actuallyFused && zone.Lane == FieldLane.Monster)
                        arena.PlayFieldCombinationEffect(DuelSide.Player,
                            zone.Index, true, hud.AnimationsEnabled);
                    hud.Announce("Carta colocada: " + result.Name +
                        (hud.PlaceFaceDown ? " virada para baixo." : " virada para cima."));
                    hud.AfterHandPlacement();
                    hud.SelectedFieldMonster = zone.Lane == FieldLane.Monster ?
                        zone.Index : -1;
                }
                else
                {
                    arena.ClearStagedPlacement();
                    hud.Message = reason;
                }
            }
            UpdateLegalHighlights();
        }

        /// <summary>
        /// World-space direct attack region, based on a horizontal plane.
        /// Never allows direct combat while an enemy monster is present.
        /// </summary>
        private bool PointerOverOpponentField(Vector2 pointer)
        {
            var plane = new Plane(Vector3.up, new Vector3(0f, 0.52f, 0f));
            var ray = arena.Camera.ScreenPointToRay(pointer);
            if (!plane.Raycast(ray, out float distance)) return false;
            Vector3 point = ray.GetPoint(distance);
            return point.z > 0.45f && point.z < 6.7f && Mathf.Abs(point.x) < 6.7f;
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
                    if (match.Phase == MatchPhase.Battle &&
                        match.CurrentSide == DuelSide.Player && !hud.InputLocked)
                    {
                        bool eligible = false;
                        if (zone.Opponent)
                        {
                            if (hud.SelectedAttacker >= 0)
                            {
                                if (match.Opponent.MonsterCount == 0)
                                    eligible = match.CanAttack(DuelSide.Player,
                                        hud.SelectedAttacker, -1, out _);
                                else if (zone.Lane == FieldLane.Monster &&
                                    match.Opponent.Monster(zone.Index) != null)
                                    eligible = match.CanAttack(DuelSide.Player,
                                        hud.SelectedAttacker, zone.Index, out _);
                            }
                        }
                        else if (zone.Lane == FieldLane.Monster &&
                            match.Player.Monster(zone.Index) != null)
                        {
                            // Available attack sources stay highlighted until
                            // selected. Already-attacked/defending monsters
                            // do not misleadingly look attack-ready.
                            if (match.Opponent.MonsterCount == 0)
                                eligible = match.CanAttack(DuelSide.Player,
                                    zone.Index, -1, out _);
                            else for (int target = 0; target < DuelMatch.ZoneCount; target++)
                            {
                                if (match.Opponent.Monster(target) == null) continue;
                                if (!match.CanAttack(DuelSide.Player,
                                    zone.Index, target, out _)) continue;
                                eligible = true;
                                break;
                            }
                        }
                        zone.SetCombatTarget(eligible);
                        zone.SetCombatSource(!zone.Opponent &&
                            zone.Lane == FieldLane.Monster &&
                            zone.Index == hud.SelectedAttacker &&
                            eligible);
                        continue;
                    }
                    var selection = fusionSelection.Count > 0 ?
                        (IReadOnlyList<int>)fusionSelection : new [] { lastSelectedHand };
                    bool occupiedMonster = !zone.Opponent &&
                        zone.Lane == FieldLane.Monster &&
                        match.Player.Monster(zone.Index) != null;
                    bool ritualSelected = selection.Count == 1 &&
                        selection[0] >= 0 &&
                        selection[0] < match.Player.Hand.Count &&
                        match.Player.Hand[selection[0]].Kind == CardKind.Ritual;
                    var card = ritualSelected && occupiedMonster ?
                        match.PreviewRitual(DuelSide.Player, selection[0]) :
                        occupiedMonster ?
                        match.PreviewFieldResult(DuelSide.Player, selection, zone.Index) :
                        match.PreviewResult(DuelSide.Player, selection);
                    var star = card == null ? GuardianStar.None :
                        (hud.UseSecondaryStar ? card.StarB : card.StarA);
                    bool legal = !zone.Opponent && card != null &&
                        (ritualSelected && occupiedMonster ?
                            match.CanSummonRitual(DuelSide.Player, selection[0],
                                zone.Index, star, out _, out _) :
                        occupiedMonster ?
                            match.CanPlayOnField(DuelSide.Player, selection,
                                zone.Index, star, out _, out _) :
                            match.CanPlay(DuelSide.Player, selection,
                                zone.Lane, zone.Index, star, out _));
                    zone.SetLegal(legal);
                }
                else zone.SetLegal(!zone.Opponent &&
                    session.CanPlace(lastSelectedHand, zone.Lane, zone.Index, out _));
            }
        }
    }
}
