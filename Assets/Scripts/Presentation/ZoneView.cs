using Reborn.Core;
using UnityEngine;

namespace Reborn.Presentation
{
    public sealed class ZoneView : MonoBehaviour
    {
        public FieldLane Lane { get; private set; }
        public int Index { get; private set; }
        public bool Opponent { get; private set; }
        private Renderer[] trim;
        private Material trimNormal;
        private Material trimHover;
        private bool pointerHover;
        private bool legalTarget;

        public void Initialize(FieldLane lane, int index, bool opponent, Renderer[] edgeRenderers,
            Material normal, Material hover)
        {
            Lane = lane;
            Index = index;
            Opponent = opponent;
            trim = edgeRenderers;
            trimNormal = normal;
            trimHover = hover;
            UpdateHighlight();
        }

        public void SetLegal(bool legal)
        {
            legalTarget = !Opponent && legal;
            UpdateHighlight();
        }

        /// <summary>Enemy slots may be shown as targets only during experimental combat.</summary>
        public void SetCombatTarget(bool active)
        {
            legalTarget = active;
            UpdateHighlight();
        }

        public void SetHighlight(bool highlight)
        {
            pointerHover = !Opponent && highlight;
            UpdateHighlight();
        }

        private void UpdateHighlight()
        {
            if (trim == null) return;
            // Zonas validas recebem realce; um hover invalido nao induz o jogador ao erro.
            var active = legalTarget || (pointerHover && legalTarget);
            foreach (var edge in trim)
                if (edge != null) edge.sharedMaterial = active ? trimHover : trimNormal;
        }
    }
}
