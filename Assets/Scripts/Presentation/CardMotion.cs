using UnityEngine;

namespace Reborn.Presentation
{
    /// <summary>
    /// Visual-only, fixed-camera summon motion. The core DuelMatch has already
    /// accepted the placement before this starts; motion cannot affect rules.
    /// Finishes on the exact intended position, orientation and scale.
    /// </summary>
    public sealed class CardMotion : MonoBehaviour
    {
        private Vector3 from;
        private Vector3 to;
        private Vector3 sideways;
        private Vector3 restingScale;
        private Quaternion restingRotation;
        private Quaternion floatingRotation;
        private float duration;
        private float elapsed;
        private bool active;

        public void Play(Vector3 fromWorld, Vector3 toWorld, float seconds = 0.58f)
        {
            from = fromWorld;
            to = toWorld;
            sideways = Vector3.Cross(to-from, Vector3.up).normalized;
            duration = Mathf.Max(0.01f, seconds);
            elapsed = 0f;
            restingScale = transform.localScale;
            restingRotation = transform.rotation;
            floatingRotation = restingRotation * Quaternion.Euler(-11f, 0f, 5f);
            transform.position = from;
            transform.rotation = floatingRotation;
            transform.localScale = restingScale * 0.89f;
            active = true;
        }

        private void Update()
        {
            if (!active) return;
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            // Travel quickly out of the hand, then ease toward the slot.
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            float arc = Mathf.Sin(t * Mathf.PI) * 0.96f;
            // Original soft S-curve: the card leaves the hand as if
            // guided by a magnetic ribbon rather than a straight slide.
            // Lateral movement always becomes zero exactly at the slot.
            float cross = Mathf.Sin(t*Mathf.PI) *
                Mathf.Sin(t*Mathf.PI*1.72f)*0.19f;
            // Very small final settle gives weight to the placement without
            // bouncing the card out of its legal field zone.
            float settle = t > 0.75f ?
                Mathf.Sin((t - 0.75f) * 4f * Mathf.PI) * 0.05f : 0f;
            transform.position = Vector3.Lerp(from, to, eased) +
                Vector3.up * (arc + settle) + sideways * cross;
            transform.rotation = Quaternion.Slerp(
                floatingRotation, restingRotation, eased);
            float zoom = Mathf.Lerp(0.89f, 1f, eased) +
                Mathf.Sin(t * Mathf.PI) * 0.045f;
            transform.localScale = restingScale * zoom;
            if (t >= 1f)
            {
                active = false;
                transform.position = to;
                transform.rotation = restingRotation;
                transform.localScale = restingScale;
                Destroy(this);
            }
        }
    }
}
