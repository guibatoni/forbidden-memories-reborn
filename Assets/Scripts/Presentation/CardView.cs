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
        private float arrivalRemaining;

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

        /// <summary>
        /// Animated home pose. Do not move the object immediately: the Update
        /// loop also handles hover raise, selection and active drag gestures.
        /// The BoxCollider is on the root and follows the same pose.
        /// </summary>
        public void SetHandLayoutTarget(Vector3 position, Quaternion rotation)
        {
            if (HandIndex < 0) return;
            homeLocalPosition = position;
            homeLocalRotation = rotation;
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

        /// <summary>
        /// A new draw slides in from the deck and settles into the existing
        /// fan target. The approved spacing/order/hover geometry is untouched.
        /// </summary>
        public void AnimateDrawFrom(Vector3 localDeckPosition)
        {
            if (HandIndex < 0) return;
            transform.localPosition = localDeckPosition;
            transform.localScale = homeScale * 0.84f;
            transform.localRotation = Quaternion.Euler(-9f, -16f, 0f);
            arrivalRemaining = 0.44f;
        }

        public void SetDragWorldPosition(Vector3 worldPosition)
        {
            if (HandIndex < 0) return;
            dragging = true;
            arrivalRemaining = 0f;
            transform.position = worldPosition;
        }

        public void StopDragging() { dragging = false; }

        private void Update()
        {
            // Field cards remain in their fixed slots. The hand floats and scales
            // smoothly while keeping its collider attached to the card root.
            if (HandIndex < 0) return;
            float factor = 1f - Mathf.Exp(-14f * Time.deltaTime);
            if (arrivalRemaining > 0f)
                arrivalRemaining = Mathf.Max(0f, arrivalRemaining - Time.deltaTime);
            // Hover takes priority and lifts the focused card above its
            // neighbors; an off-hover fusion selection stays more subtle.
            float raise = hovered ? 0.80f : (selected ? 0.22f : 0f);
            float scale = dragging ? 1.18f :
                (hovered ? 1.18f : (selected ? 1.065f : 1f));
            if (!dragging)
                transform.localPosition = Vector3.Lerp(transform.localPosition,
                    homeLocalPosition + Vector3.up * raise, factor);
            if (!dragging)
            {
                // Distinctive soft tilt on hover, with no change to the fan yaw.
                var targetRotation = homeLocalRotation *
                    Quaternion.Euler(hovered ? -5.5f : 0f, 0f, 0f);
                transform.localRotation = Quaternion.Slerp(transform.localRotation,
                    targetRotation, factor);
            }
            transform.localScale = Vector3.Lerp(transform.localScale, homeScale * scale, factor);
        }
    }
}
