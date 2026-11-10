using Reborn.Core;
using UnityEngine;

namespace Reborn.Presentation
{
    public sealed class CardView : MonoBehaviour
    {
        public int HandIndex { get; private set; } = -1;
        public CardDefinition Card { get; private set; }
        public bool Facedown { get; private set; }
        private Renderer borderRenderer;
        private Material baseMaterial;
        private Material selectedMaterial;
        private Vector3 homeLocalPosition;
        private Vector3 homeScale;
        private Quaternion homeLocalRotation;
        private GameObject fusionBadge;
        private TextMesh fusionBadgeText;
        private static Material badgeMaterial;
        private bool selected;
        private bool hovered;
        private bool dragging;

        public void Initialize(CardDefinition card, int handIndex, Renderer border,
            Material normal, Material highlighted, bool facedown)
        {
            Card = card;
            Facedown = facedown;
            HandIndex = handIndex;
            borderRenderer = border;
            baseMaterial = normal;
            selectedMaterial = highlighted;
            homeLocalPosition = transform.localPosition;
            homeLocalRotation = transform.localRotation;
            homeScale = transform.localScale;
            SetSelected(false);
            if (handIndex >= 0) BuildFusionBadge();
        }

        public void SetSelected(bool value)
        {
            selected = value;
            if (borderRenderer != null)
                borderRenderer.sharedMaterial = selected ? selectedMaterial : baseMaterial;
        }

        public void SetHovered(bool value) { hovered = value; }

        /// <summary>MTGA-like overlapping fan without moving the fixed camera.</summary>
        public void SetFanRotation(float angle)
        {
            if (HandIndex < 0) return;
            homeLocalRotation = Quaternion.Euler(0f, angle, 0f);
            transform.localRotation = homeLocalRotation;
        }

        public void SetFusionOrder(int order)
        {
            if (fusionBadge == null) return;
            fusionBadge.SetActive(order > 0);
            if (order > 0 && fusionBadgeText != null)
                fusionBadgeText.text = order.ToString();
        }

        private void BuildFusionBadge()
        {
            var size = GetComponent<BoxCollider>();
            if (size == null) return;
            fusionBadge = new GameObject("Fusion_Sequence_Badge");
            fusionBadge.transform.SetParent(transform, false);
            fusionBadge.transform.localPosition =
                new Vector3(size.size.x * 0.35f, 0.13f, size.size.z * 0.37f);
            var backing = GameObject.CreatePrimitive(PrimitiveType.Cube);
            backing.name = "Badge_Number_Background";
            backing.transform.SetParent(fusionBadge.transform, false);
            backing.transform.localScale = new Vector3(0.34f, 0.045f, 0.34f);
            var collider = backing.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
                Destroy(collider);
            }
            if (badgeMaterial == null)
                badgeMaterial = ArenaVisuals.Mat(new Color(0.13f, 0.48f, 0.67f), true);
            backing.GetComponent<Renderer>().sharedMaterial = badgeMaterial;
            var label = new GameObject("Badge_Number");
            label.transform.SetParent(fusionBadge.transform, false);
            label.transform.localPosition = new Vector3(0f, 0.045f, 0f);
            label.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            fusionBadgeText = label.AddComponent<TextMesh>();
            fusionBadgeText.text = "";
            fusionBadgeText.fontSize = 60;
            fusionBadgeText.characterSize = 0.1f;
            fusionBadgeText.anchor = TextAnchor.MiddleCenter;
            fusionBadgeText.alignment = TextAlignment.Center;
            fusionBadgeText.color = Color.white;
            fusionBadge.SetActive(false);
        }

        public void SetDragWorldPosition(Vector3 worldPosition)
        {
            if (HandIndex < 0) return;
            dragging = true;
            transform.position = worldPosition;
        }

        public void StopDragging() { dragging = false; }

        private void Update()
        {
            // Field cards remain in their fixed slots. The hand floats and scales
            // smoothly while keeping its collider attached to the card root.
            if (HandIndex < 0) return;
            float factor = 1f - Mathf.Exp(-14f * Time.deltaTime);
            float raise = (hovered || selected) ? 0.58f : 0f;
            float scale = dragging ? 1.18f : ((hovered || selected) ? 1.14f : 1f);
            if (!dragging)
                transform.localPosition = Vector3.Lerp(transform.localPosition,
                    homeLocalPosition + Vector3.up * raise, factor);
            if (!dragging)
                transform.localRotation = Quaternion.Slerp(transform.localRotation,
                    homeLocalRotation, factor);
            transform.localScale = Vector3.Lerp(transform.localScale, homeScale * scale, factor);
        }
    }
}
