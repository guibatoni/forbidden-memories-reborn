using System;
using System.Collections.Generic;
using Reborn.Core;
using UnityEngine;

namespace Reborn.Presentation
{
    /// <summary>
    /// Four optional independently dealt starter decks, each from all seven
    /// locally verified weighted pools. The original PS1 randomly deals ONE;
    /// showing four alternatives is an explicit Reborn quality-of-life feature.
    /// </summary>
    public sealed partial class ArenaHud
    {
        private IReadOnlyList<IReadOnlyList<CardDefinition>> starterOffers;
        private Action<int> onStarterChosen;
        private string starterMessage;
        private bool starterMissing;
        private bool starterPracticeAllowed;

        public bool StarterSelectionRequired =>
            !starterPracticeAllowed && (starterMissing ||
                (starterOffers != null && onStarterChosen != null));

        public void ConfigureStarterDeckChoices(
            IReadOnlyList<IReadOnlyList<CardDefinition>> offers,
            Action<int> chosen, string statusMessage)
        {
            starterOffers = offers;
            onStarterChosen = chosen;
            starterMessage = statusMessage ?? "";
            starterMissing = offers == null || offers.Count != 4;
            starterPracticeAllowed = false;
        }

        private void DrawStarterDeckChoice()
        {
            if (!StarterSelectionRequired) return;
            Panel(new Rect(0,0,Screen.width,Screen.height),
                new Color(0.005f,0.012f,0.026f,0.98f));
            float w = Mathf.Min(1050f,Screen.width - 24f);
            float h = Mathf.Min(530f,Screen.height - 22f);
            float x = (Screen.width-w)*0.5f;
            float y = (Screen.height-h)*0.5f;
            Panel(new Rect(x,y,w,h),new Color(0.024f,0.038f,0.081f,1f));
            Panel(new Rect(x+14f,y+13f,w-28f,4f),
                new Color(0.95f,0.71f,0.35f));
            GUI.Label(new Rect(x+22f,y+24f,w-44f,38f),
                starterMissing ? "POOLS INICIAIS ORIGINAIS AUSENTES" :
                    "ESCOLHA SEU BARALHO INICIAL",heading);
            GUI.Label(new Rect(x+22f,y+69f,w-44f,53f),
                starterMissing ?
                    "O DUELTRACE forneceu as cartas, mas nao os sete pesos iniciais da ISO.\n"+
                    "Execute IMPORTAR_ORIGINAL.ps1 e reinicie o Play." :
                    "Quatro sorteios independentes dos 7 pools originais do PS1.\n"+
                    "No jogo original existe apenas um sorteio; escolher entre 4 e exclusivo do Reborn.",
                small);
            if (starterMissing)
            {
                GUI.Label(new Rect(x+22f,y+136f,w-44f,140f),
                    starterMessage +
                    "\n\nNenhum deck foi apresentado como original sem as tabelas verificadas.\n"+
                    "Para continuar testando interface e IA, ative o modo treino explicitamente.",
                    body);
                if (GUI.Button(new Rect(x+22f,y+h-70f,w-44f,45f),
                    "Continuar apenas em TREINO (deck experimental)",button))
                {
                    starterPracticeAllowed = true;
                    Announce("TREINO: deck aleatorio nao representa o starter PS1.");
                }
                return;
            }
            bool smallScreen = w < 795f;
            int columns = smallScreen ? 2 : 4;
            int rows = smallScreen ? 2 : 1;
            float gap = 13f;
            float cardW = (w - 44f - gap*(columns-1)) / columns;
            float gridHeight = h - 165f;
            float cardH = (gridHeight - gap*(rows-1)) / rows;
            for (int i = 0; i < 4; i++)
            {
                int col = i % columns;
                int row = i / columns;
                float bx = x + 22f + col * (cardW + gap);
                float by = y + 130f + row * (cardH + gap);
                Panel(new Rect(bx,by,cardW,cardH),new Color(0.048f,0.067f,0.125f,1f));
                GUI.Label(new Rect(bx+12f,by+9f,cardW-24f,29f),
                    "BARALHO " + (i+1) + " / 40 cartas",body);
                var cards = starterOffers[i];
                int monsters=0, spells=0, equips=0;
                CardDefinition strongest=null;
                foreach(var c in cards)
                {
                    if(c.Kind == CardKind.Monster)
                    {
                        monsters++;
                        if(strongest == null || strongest.Attack < c.Attack)
                            strongest = c;
                    }
                    else if(c.Kind == CardKind.Spell) spells++;
                    else if(c.Kind == CardKind.Equip) equips++;
                }
                GUI.Label(new Rect(bx+12f,by+43f,cardW-24f,58f),
                    "Monstros: " + monsters +
                    "  Magias: " + spells + "  Equips: " + equips,small);
                if (cardH > 165f && strongest != null)
                    GUI.Label(new Rect(bx+12f,by+95f,cardW-24f,65f),
                        "Maior ATK: " + strongest.Attack +
                        "\n" + strongest.Name,small);
                if(GUI.Button(new Rect(bx+9f,by+cardH-48f,cardW-18f,37f),
                    "Escolher este deck",button))
                {
                    var callback = onStarterChosen;
                    // Clear first so the game's ResetDuel does not re-open
                    // the picker or accidentally select a second starter.
                    onStarterChosen = null;
                    starterOffers = null;
                    starterMissing = false;
                    starterPracticeAllowed = true;
                    callback?.Invoke(i);
                    return;
                }
            }
            GUI.Label(new Rect(x+22f,y+h-27f,w-44f,21f),
                "Distribuicao autentica; sorteio reproduz pesos, nao o fluxo MIPS exato.",small);
        }
    }
}
