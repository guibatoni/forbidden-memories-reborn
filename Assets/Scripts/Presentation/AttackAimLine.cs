using UnityEngine;
using UnityEngine.Rendering;

namespace Reborn.Presentation
{
    /// <summary>
    /// Read-only attack intent arc. The duel rules choose legal targets;
    /// this component never triggers an attack or modifies the camera.
    /// </summary>
    public sealed class AttackAimLine : MonoBehaviour
    {
        private const int SegmentCount = 20;
        private LineRenderer line;
        private Vector3 origin;
        private Vector3 destination;

        private void Awake()
        {
            line = gameObject.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = SegmentCount + 1;
            line.sharedMaterial = ArenaVisuals.Mat(ArenaVisuals.Gold, true);
            line.startWidth = 0.055f;
            line.endWidth = 0.025f;
            line.numCapVertices = 4;
            line.numCornerVertices = 3;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.enabled = false;
        }

        public void Show(Vector3 from, Vector3 to)
        {
            origin = from;
            destination = to;
            if (line != null) line.enabled = true;
        }

        public void Hide()
        {
            if (line != null) line.enabled = false;
        }

        private void LateUpdate()
        {
            if (line == null || !line.enabled) return;
            // Subtle breathing pulse; no flicker and no interaction colliders.
            line.widthMultiplier = 0.90f + 0.10f *
                (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 4f));
            for (int i = 0; i <= SegmentCount; i++)
            {
                float t = (float)i / SegmentCount;
                float arc = Mathf.Sin(t * Mathf.PI) * 0.42f;
                line.SetPosition(i, Vector3.Lerp(origin, destination, t) +
                    Vector3.up * arc);
            }
        }

        private void OnDestroy()
        {
            if (line != null && line.sharedMaterial != null)
                Destroy(line.sharedMaterial);
        }
    }
}
