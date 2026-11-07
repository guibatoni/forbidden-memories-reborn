using Reborn.Core;
using UnityEngine;

namespace Reborn.Presentation
{
    public sealed class CardView : MonoBehaviour
    {
        public int HandIndex { get; private set; } = -1;
        public CardDefinition Card { get; private set; }
        private Renderer cardRenderer;
        private Material baseMaterial;
        private Material selectedMaterial;
        private Vector3 homeLocalPosition;
        private Vector3 homeScale;
        private bool selected;
        private bool hovered;
        private bool dragging;

        public void Initialize(CardDefinition card, int handIndex, Material normal, Material highlighted)
        {
            Card = card;
            HandIndex = handIndex;
            cardRenderer = GetComponent<Renderer>();
            baseMaterial = normal;
            selectedMaterial = highlighted;
            homeLocalPosition = transform.localPosition;
            homeScale = transform.localScale;
            SetSelected(false);
        }

        public void SetSelected(bool value)
        {
            selected = value;
            if (cardRenderer != null)
                cardRenderer.sharedMaterial = selected ? selectedMaterial : baseMaterial;
        }

        public void SetHovered(bool value) { hovered = value; }

        public void SetDragWorldPosition(Vector3 worldPosition)
        {
            if (HandIndex < 0) return;
            dragging = true;
            transform.position = worldPosition;
        }

        public void StopDragging() { dragging = false; }

        private void Update()
        {
            if (HandIndex < 0) return; // A animacao de cartas no campo pertence ao CardMotion.
            float factor = 1f - Mathf.Exp(-14f * Time.deltaTime);
            float raise = (hovered || selected) ? 0.28f : 0f;
            float scale = dragging ? 1.13f : ((hovered || selected) ? 1.09f : 1f);
            if (!dragging)
                transform.localPosition = Vector3.Lerp(transform.localPosition,
                    homeLocalPosition + Vector3.up * raise, factor);
            transform.localScale = Vector3.Lerp(transform.localScale, homeScale * scale, factor);
        }
    }
}
