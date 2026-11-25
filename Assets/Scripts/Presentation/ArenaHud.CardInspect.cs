using Reborn.Core;
using UnityEngine;

namespace Reborn.Presentation
{
    /// <summary>
    /// Right-click inspection overlay. It displays imported metadata only;
    /// input code refuses hidden cards to avoid revealing opponent information.
    /// </summary>
    public sealed partial class ArenaHud
    {
        private CardDefinition inspectedCard;
        private Vector2 inspectionScroll;

        public bool CardInspectionOpen => inspectedCard != null;

        public void ToggleCardInspection(CardDefinition card)
        {
            if (card == null) return;
            if (inspectedCard == card) { CloseCardInspection(); return; }
            inspectedCard = card;
            inspectionScroll = Vector2.zero;
        }

        public void CloseCardInspection() { inspectedCard = null; }

        private void DrawPinnedCardInspection()
        {
            var card = inspectedCard;
            if (card == null) return;
            Panel(new Rect(0f, 0f, Screen.width, Screen.height),
                new Color(0.008f, 0.013f, 0.028f, 0.90f));

            float w = Mathf.Min(925f, Screen.width - 28f);
            float h = Mathf.Min(595f, Screen.height - 26f);
            float x = (Screen.width - w) * 0.5f;
            float y = (Screen.height - h) * 0.5f;
            float artW = w * 0.45f;
            Panel(new Rect(x, y, artW, h),
                new Color(0.06f, 0.075f, 0.13f, 0.99f));
            Panel(new Rect(x + artW + 7f, y, w - artW - 7f, h),
                new Color(0.025f, 0.042f, 0.085f, 0.99f));
            Panel(new Rect(x + 14f, y + 15f, artW - 28f, 4f),
                new Color(0.95f, 0.74f, 0.36f));

            var nameStyle = Style(21, Color.white, FontStyle.Bold);
            GUI.Label(new Rect(x + 19f, y + 28f, artW - 38f, 62f),
                card.Name, nameStyle);
            GUI.Label(new Rect(x + 19f, y + 96f, artW - 38f, 25f),
                "#" + card.Id + "  /  " + CardFaceBuilder.KindLabel(card.Kind), small);
            float imageX = x + 22f;
            float imageY = y + 133f;
            float imageW = artW - 44f;
            float imageH = Mathf.Max(100f, h - 213f);
            Panel(new Rect(imageX - 5f, imageY - 5f, imageW + 10f, imageH + 10f),
                new Color(0.67f, 0.48f, 0.23f));
            GUI.DrawTexture(new Rect(imageX, imageY, imageW, imageH),
                CardArtCatalog.Get(card), ScaleMode.ScaleToFit);
            GUI.Label(new Rect(x + 20f, y + h - 52f, artW - 40f, 40f),
                card.Kind == CardKind.Monster ?
                    "ATK " + card.Attack + "  |  DEF " + card.Defense :
                    "Carta de " + CardFaceBuilder.KindLabel(card.Kind), body);

            float infoX = x + artW + 28f;
            float infoW = w - artW - 53f;
            GUI.Label(new Rect(infoX, y + 26f, infoW - 40f, 40f),
                "INFORMACOES DA CARTA", heading);
            if (GUI.Button(new Rect(x + w - 51f, y + 19f, 34f, 33f), "X", button))
            {
                CloseCardInspection();
                return;
            }
            float cursorY = y + 85f;
            string category = CardFaceBuilder.KindLabel(card.Kind);
            if (!string.IsNullOrWhiteSpace(card.TypeLabel))
                category += " - " + card.TypeLabel;
            GUI.Label(new Rect(infoX, cursorY, infoW, 25f),
                "Tipo: " + category, body);
            cursorY += 37f;
            if (card.Kind == CardKind.Monster)
            {
                GUI.Label(new Rect(infoX, cursorY, infoW, 28f),
                    "Ataque: " + card.Attack + "    Defesa: " + card.Defense, body);
                cursorY += 37f;
                GUI.Label(new Rect(infoX, cursorY, infoW, 27f),
                    "Guardian Star I: " + card.StarA, body);
                cursorY += 32f;
                GUI.Label(new Rect(infoX, cursorY, infoW, 27f),
                    "Guardian Star II: " + card.StarB, body);
                cursorY += 39f;
            }
            GUI.Label(new Rect(infoX, cursorY, infoW, 27f),
                "DESCRICAO E INFORMACOES DE EFEITO", body);
            cursorY += 34f;
            float available = Mathf.Max(48f, y + h - cursorY - 68f);
            string description = string.IsNullOrWhiteSpace(card.Description) ?
                "Descricao original indisponivel neste perfil." : card.Description;
            string content = "DESCRICAO DA CARTA\n" + description;
            if (card.HasEffectText)
            {
                content += "\n\nTEXTO DE EFEITO (INFORMATIVO)\n" +
                    card.EffectText + "\n\nFonte: " + card.EffectSource +
                    "\nEstado: " + card.EffectStatus +
                    "\nExecucao no jogo: NAO VERIFICADA";
            }
            else if (card.Kind != CardKind.Monster)
            {
                content += "\n\nNenhum texto de efeito com procedencia foi importado.";
            }
            var infoStyle = Style(16, new Color(0.88f, 0.91f, 0.96f));
            float textHeight = Mathf.Max(available, infoStyle.CalcHeight(
                new GUIContent(content), infoW - 27f) + 10f);
            inspectionScroll = GUI.BeginScrollView(
                new Rect(infoX, cursorY, infoW, available),
                inspectionScroll, new Rect(0f, 0f, infoW - 18f, textHeight));
            GUI.Label(new Rect(0f, 0f, infoW - 28f, textHeight),
                content, infoStyle);
            GUI.EndScrollView();
            GUI.Label(new Rect(infoX, y + h - 49f, infoW, 37f),
                "Botao direito ou ESC: fechar", small);
        }
    }
}
