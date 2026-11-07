using UnityEngine;

namespace Reborn.Presentation
{
    /// <summary>Animacao puramente visual: nao altera zonas, regras ou camera.</summary>
    public sealed class CardMotion : MonoBehaviour
    {
        private Vector3 from;
        private Vector3 to;
        private Vector3 restingScale;
        private float duration;
        private float elapsed;
        private bool active;

        public void Play(Vector3 fromWorld, Vector3 toWorld, float seconds = 0.45f)
        {
            from = fromWorld;
            to = toWorld;
            duration = Mathf.Max(0.01f, seconds);
            elapsed = 0f;
            restingScale = transform.localScale;
            transform.position = from;
            transform.localScale = restingScale * 0.85f;
            active = true;
        }

        private void Update()
        {
            if (!active) return;
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = Mathf.SmoothStep(0f, 1f, t);
            Vector3 arc = Vector3.up * (Mathf.Sin(t * Mathf.PI) * 0.85f);
            transform.position = Vector3.Lerp(from, to, eased) + arc;
            transform.localScale = restingScale * Mathf.Lerp(0.85f, 1f, eased);
            if (t >= 1f)
            {
                active = false;
                transform.position = to;
                transform.localScale = restingScale;
                Destroy(this);
            }
        }
    }
}
