using System;
using System.Collections.Generic;
using Reborn.Core;
using UnityEngine;

namespace Reborn.Presentation
{
    /// <summary>
    /// An in-duel browser for ONLY the user's own playable, verified fusions.
    /// This UI never guesses formulas and never spends a card during preview.
    /// </summary>
    public sealed partial class ArenaHud
    {
        private IReadOnlyList<AvailableFusion> fusionOptions;
        private Action<AvailableFusion> onFusionChosen;
        public event Action<AvailableFusion> FusionRequested;
        private int selectedFusionOption;
        private Vector2 fusionScroll;

        public bool FusionAssistantOpen => fusionOptions != null;

        public void OpenFusionAssistant(Action<AvailableFusion> choose)
        {
            if (match == null || !DuelModeActive ||
                match.CurrentSide != DuelSide.Player ||
                match.Phase != MatchPhase.Main || InputLocked) return;
            fusionOptions = match.FindAvailableFusions(DuelSide.Player);
            onFusionChosen = choose;
            selectedFusionOption = 0;
            fusionScroll = Vector2.zero;
        }

        public void CloseFusionAssistant()
        {
            fusionOptions = null;
            onFusionChosen = null;
            selectedFusionOption = 0;
        }

        public void MoveFusionAssistantChoice(int delta)
        {
            if (!FusionAssistantOpen || fusionOptions.Count == 0) return;
            selectedFusionOption =
                (selectedFusionOption + fusionOptions.Count +
                    (delta < 0 ? -1 : 1)) % fusionOptions.Count;
        }

        public void ConfirmFusionAssistantChoice()
        {
            if (!FusionAssistantOpen || selectedFusionOption < 0 ||
                selectedFusionOption >= fusionOptions.Count) return;
            var chosen = fusionOptions[selectedFusionOption];
            var callback = onFusionChosen;
            CloseFusionAssistant(); // Unblock the placement wizard first.
            if (callback != null) callback(chosen);
            else FusionRequested?.Invoke(chosen);
        }

        private string FusionOptionLabel(AvailableFusion option)
        {
            if (match == null) return "Fusao indisponivel";
            var hand = match.Player.Hand;
            if (option.FirstHandIndex < 0 ||
                option.FirstHandIndex >= hand.Count) return "Carta da mao invalida";
            var first = hand[option.FirstHandIndex];
            if (option.UsesField)
            {
                var field = match.Player.Monster(option.FieldSlot);
                if (field == null) return "Monstro do campo nao encontrado";
                return "CAMPO " + (option.FieldSlot + 1) + ": " +
                    field.Card.Name + " + MAO " +
                    (option.FirstHandIndex + 1) + ": " + first.Name +
                    "  =  " + option.Result.Name;
            }
            if (option.SecondHandIndex < 0 ||
                option.SecondHandIndex >= hand.Count) return "Material invalido";
            return "MAO " + (option.FirstHandIndex + 1) + ": " + first.Name +
                " + MAO " + (option.SecondHandIndex + 1) +
                ": " + hand[option.SecondHandIndex].Name +
                "  =  " + option.Result.Name;
        }

        private void DrawFusionAssistantLauncher()
        {
            if (match == null || !DuelModeActive ||
                match.CurrentSide != DuelSide.Player ||
                match.Phase != MatchPhase.Main ||
                OpponentBusy || PlayerAttackBusy || ContentImportRequired ||
                StarterSelectionRequired || KeyboardSettingsOpen ||
                PlacementWizardOpen || EquipChoiceOpen) return;
            var y = Screen.height - 111f;
            if (y < 540f) return;
            if (GUI.Button(new Rect(12f, y, 251f, 38f),
                "FUSOES DISPONIVEIS [F]", button))
                OpenFusionAssistant(null);
        }

        private void DrawFusionAssistant()
        {
            if (!FusionAssistantOpen) return;
            Panel(new Rect(0f, 0f, Screen.width, Screen.height),
                new Color(0.007f,0.014f,0.033f,0.96f));
            float width = Mathf.Min(840f, Screen.width - 24f);
            float height = Mathf.Min(665f, Screen.height - 25f);
            float x = (Screen.width-width)*0.5f;
            float y = (Screen.height-height)*0.5f;
            Panel(new Rect(x,y,width,height),
                new Color(0.026f,0.043f,0.089f,1f));
            Panel(new Rect(x+13f,y+12f,width-26f,4f),
                new Color(0.83f,0.65f,0.31f));
            GUI.Label(new Rect(x+20f,y+28f,width-40f,38f),
                "FUSOES REAIS DISPONIVEIS",heading);
            int count = fusionOptions.Count;
            GUI.Label(new Rect(x+20f,y+75f,width-40f,58f),
                "Receitas do perfil: " + match.FusionRecipeCount +
                "   |   Pares jogaveis agora: " + count +
                "\nSelecione uma receita. Nao ha fusoes inventadas por tipo ou nome.",
                small);
            float listingHeight = Mathf.Max(83f,height-253f);
            if (count > 0)
            {
                float cellHeight = 65f;
                fusionScroll = GUI.BeginScrollView(
                    new Rect(x+18f,y+139f,width-36f,listingHeight),
                    fusionScroll,
                    new Rect(0f,0f,width-62f,count*cellHeight+7f));
                for (int i=0;i<count;i++)
                {
                    var option = fusionOptions[i];
                    float top = 4f+i*cellHeight;
                    if (i==selectedFusionOption)
                        Panel(new Rect(0f,top,width-67f,cellHeight-3f),
                            new Color(0.095f,0.17f,0.24f,1f));
                    string label = (i==selectedFusionOption ? "► " : "") +
                        FusionOptionLabel(option);
                    if (GUI.Button(new Rect(8f,top+5f,width-82f,51f),
                        label,button))
                    {
                        selectedFusionOption=i;
                        ConfirmFusionAssistantChoice();
                        GUI.EndScrollView();
                        return;
                    }
                }
                GUI.EndScrollView();
            }
            else
            {
                GUI.Label(new Rect(x+25f,y+151f,width-50f,
                    Mathf.Min(210f,listingHeight)),
                    match.FusionRecipeCount == 0 ?
                    "NENHUMA RECEITA FOI CARREGADA.\n\n" +
                    "As 722 cartas so formam o catalogo visual. " +
                    "Execute DIAGNOSTICAR_FUSOES.ps1 e reimporte a propria ISO " +
                    "com IMPORTAR_ORIGINAL.ps1 para obter fusoes verificadas." :
                    "Nenhuma fusao de duas cartas da mao ou de monstro " +
                    "do campo + mao pode ser jogada agora.\n\n" +
                    "Tente outra mao ou escolha seus materiais manualmente.",
                    body);
            }
            GUI.Label(new Rect(x+22f,y+height-99f,width-45f,35f),
                "Setas ↑/↓: navegar   Z: escolher   X/F: fechar. " +
                "Escolher inicia a selecao; nao consome carta.",small);
            float bw=(width-58f)*0.5f;
            GUI.enabled=count>0;
            if (GUI.Button(new Rect(x+22f,y+height-58f,bw,36f),
                "PREPARAR FUSAO [Z]",button)) ConfirmFusionAssistantChoice();
            GUI.enabled=true;
            if (GUI.Button(new Rect(x+36f+bw,y+height-58f,bw,36f),
                "VOLTAR AO DUELO [X]",button)) CloseFusionAssistant();
        }
    }
}
