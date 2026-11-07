using UnityEngine;

namespace Reborn.Presentation
{
    /// <summary>
    /// Projetil visual isolado; nunca altera LP, cartas ou estado de turno.
    /// Nao muda a camera e nao ocupa uma zona do campo.
    /// </summary>
    public sealed class AttackProjectile : MonoBehaviour
    {
        private Vector3 start;
        private Vector3 destination;
        private float duration;
        private float elapsed;
        private Color tint;
        private Transform core;
        private TrailRenderer trail;

        public void Launch(Vector3 origin, Vector3 target, Color color, float seconds = 0.64f)
        {
            start = origin;
            destination = target;
            duration = Mathf.Max(0.05f, seconds);
            tint = color;
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Projectile_Core";
            go.transform.SetParent(transform, false);
            go.transform.localScale = Vector3.one * 0.30f;
            var collider = go.GetComponent<Collider>();
            if (collider != null) collider.enabled = false;
            go.GetComponent<Renderer>().sharedMaterial = ArenaVisuals.Mat(color, true);
            core = go.transform;
            trail = go.AddComponent<TrailRenderer>();
            trail.time = 0.36f;
            trail.startWidth = 0.26f;
            trail.endWidth = 0.012f;
            trail.minVertexDistance = 0.05f;
            trail.numCornerVertices = 4;
            trail.material = ArenaVisuals.Mat(color, true);
            trail.startColor = color;
            trail.endColor = new Color(color.r, color.g, color.b, 0f);
            transform.position = start;
        }

        private void Update()
        {
            if (core == null) return;
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = t * t * (3f - 2f * t);
            transform.position = Vector3.Lerp(start, destination, eased) +
                Vector3.up * (0.6f * Mathf.Sin(t * Mathf.PI));
            core.localScale = Vector3.one * (0.24f + 0.12f * Mathf.Sin(t * Mathf.PI));
            if (t >= 1f)
            {
                CreateImpact();
                core.gameObject.SetActive(false);
                Destroy(gameObject, 0.6f);
                core = null;
            }
        }

        private void CreateImpact()
        {
            var fx = new GameObject("Attack_Impact");
            fx.transform.position = destination;
            var particles = fx.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 0.30f;
            main.startLifetime = 0.45f;
            main.startSpeed = 2.2f;
            main.startSize = 0.16f;
            main.startColor = tint;
            main.maxParticles = 48;
            var emission = particles.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new [] { new ParticleSystem.Burst(0f, 28) });
            particles.Play();
            Destroy(fx, 1.3f);
        }
    }
}