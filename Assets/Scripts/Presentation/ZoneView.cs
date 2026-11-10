using Reborn.Core;
using UnityEngine;

namespace Reborn.Presentation
{
    /// <summary>
    /// Presentation-only state: an eligible slot glows gently; a hovered
    /// eligible slot brightens. Invalid zones never look actionable.
    /// Neither hover nor outlines change the actual duel rules.
    /// </summary>
    public sealed class ZoneView : MonoBehaviour
    {
        public FieldLane Lane { get; private set; }
        public int Index { get; private set; }
        public bool Opponent { get; private set; }

        private Renderer[] trim;
        private Material trimNormal;
        private Material trimLegal;
        private Material trimHover;
        private Material trimSource;
        private bool pointerHover;
        private bool legalTarget;
        private bool selectedAttacker;
        private bool displayedHovered;
        private bool displayedLegal;
        private bool displayedSource;
        private Vector3[] trimBaseScale;

        public bool IsLegalTarget => legalTarget;

        public void Initialize(FieldLane lane, int index, bool opponent,
            Renderer[] edgeRenderers, Material normal, Material legal,
            Material hovered, Material source)
        {
            Lane = lane;
            Index = index;
            Opponent = opponent;
            trim = edgeRenderers;
            trimNormal = normal;
            trimLegal = legal;
            trimHover = hovered;
            trimSource = source;
            trimBaseScale = new Vector3[trim.Length];
            for (int i = 0; i < trim.Length; i++)
                if (trim[i] != null) trimBaseScale[i] = trim[i].transform.localScale;
            UpdateHighlight(true);
        }

        public void SetLegal(bool legal)
        {
            legalTarget = !Opponent && legal;
            selectedAttacker = false;
            UpdateHighlight();
        }

        public void SetCombatTarget(bool active)
        {
            legalTarget = active;
            UpdateHighlight();
        }

        public void SetCombatSource(bool source)
        {
            selectedAttacker = source;
            UpdateHighlight();
        }

        public void SetHighlight(bool highlight)
        {
            pointerHover = highlight;
            UpdateHighlight();
        }

        private void UpdateHighlight(bool force = false)
        {
            if (trim == null) return;
            bool isHovered = pointerHover && legalTarget;
            if (!force && displayedHovered == isHovered &&
                displayedLegal == legalTarget &&
                displayedSource == selectedAttacker) return;

            displayedHovered = isHovered;
            displayedLegal = legalTarget;
            displayedSource = selectedAttacker;

            var material = selectedAttacker ? trimSource :
                isHovered ? trimHover : legalTarget ? trimLegal : trimNormal;
            for (int i = 0; i < trim.Length; i++)
            {
                var edge = trim[i];
                if (edge == null) continue;
                edge.sharedMaterial = material;
                // Selected target is thicker and easy to see even when
                // several adjacent slots are highlighted.
                var baseScale = trimBaseScale[i];
                edge.transform.localScale = isHovered || selectedAttacker ?
                    new Vector3(baseScale.x * 1.14f, baseScale.y * 1.35f,
                        baseScale.z * 1.14f) : baseScale;
            }
        }
    }
}
