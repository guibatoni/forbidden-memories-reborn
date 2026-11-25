using UnityEngine;
using UnityEngine.Rendering;

namespace Reborn.Presentation
{
    /// <summary>
    /// Original arcane helix: geometric runic rings, counter-rotating motes,
    /// and a timed reveal pillar. No stock particle prefabs, copied card art,
    /// camera moves, SFX, colliders, or gameplay state changes.
    /// </summary>
    public sealed class FusionCeremonyVfx : MonoBehaviour
    {
        public enum Ceremony { Fusion, Replacement, Ritual }
        private const int RingPoints = 64;
        private readonly LineRenderer[] rings = new LineRenderer[3];
        private readonly Transform[] motes = new Transform[6];
        private readonly Material[] materials = new Material[2];
        private LineRenderer pillar;
        private Ceremony mode;
        private float duration;
        private float elapsed;
        private bool active;

        public void Begin(Ceremony ceremony)
        {
            mode = ceremony;
            duration = mode == Ceremony.Ritual ? 1.48f :
                mode == Ceremony.Fusion ? 1.06f : 0.68f;
            elapsed = 0f;
            var primary = mode == Ceremony.Ritual ?
                new Color(0.69f, 0.23f, 0.91f) :
                mode == Ceremony.Fusion ? ArenaVisuals.Cyan : ArenaVisuals.Gold;
            var secondary = mode == Ceremony.Ritual ?
                ArenaVisuals.Gold : new Color(0.96f, 0.90f, 0.64f);
            materials[0] = ArenaVisuals.Mat(primary, true);
            materials[1] = ArenaVisuals.Mat(secondary, true);
            for (int i = 0; i < rings.Length; i++)
            {
                var ring = new GameObject("Arcane_Ring_" + i);
                ring.transform.SetParent(transform, false);
                rings[i] = ring.AddComponent<LineRenderer>();
                ConfigureLine(rings[i], materials[i % 2], RingPoints + 1,
                    0.036f + i * 0.006f);
            }

            var beamObject = new GameObject("Ritual_Or_Fusion_Reveal_Pillar");
            beamObject.transform.SetParent(transform, false);
            pillar = beamObject.AddComponent<LineRenderer>();
            ConfigureLine(pillar, materials[0], 2, 0.08f);
            for (int i = 0; i < motes.Length; i++)
            {
                var spark = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                spark.name = "Ceremony_Orbit_Mote_" + i;
                spark.transform.SetParent(transform, false);
                spark.transform.localScale = Vector3.one * 0.055f;
                var collider = spark.GetComponent<Collider>();
                if (collider != null) collider.enabled = false;
                var renderer = spark.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = materials[i % 2];
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                motes[i] = spark.transform;
            }
            active = true;
            Draw(0f);
        }

        private static void ConfigureLine(LineRenderer line, Material material,
            int points, float width)
        {
            line.useWorldSpace = false;
            line.positionCount = points;
            line.sharedMaterial = material;
            line.startWidth = width;
            line.endWidth = width * 0.52f;
            line.numCapVertices = 4;
            line.numCornerVertices = 2;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
        }

        private void Update()
        {
            if (!active) return;
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            Draw(t);
            if (t >= 1f)
            {
                active = false;
                Destroy(gameObject);
            }
        }

        private void Draw(float t)
        {
            float open = Mathf.SmoothStep(0f, 1f, Mathf.Min(1f, t * 4.2f));
            float fade = Mathf.Clamp01((1f - t) * 4.1f);
            float intensity = open * fade;
            float spin = 6.2831853f * t;
            float ritualScale = mode == Ceremony.Ritual ? 1.20f : 1f;
            for (int i = 0; i < rings.Length; i++)
            {
                var line = rings[i];
                float radius = (0.58f + i * 0.30f) * ritualScale *
                    (0.70f + 0.30f * open);
                float phase = spin * (i % 2 == 0 ? 1.5f : -1.2f) + i * 1.05f;
                float elevation = 0.06f + i * 0.22f +
                    Mathf.Sin(Mathf.PI * t) * (i + 1) * 0.18f;
                line.widthMultiplier = Mathf.Max(0.001f, intensity);
                for (int pt = 0; pt <= RingPoints; pt++)
                {
                    float angle = 6.2831853f * pt / RingPoints + phase;
                    float spiral = Mathf.Sin(angle * 5f + spin * 3f) * 0.035f;
                    line.SetPosition(pt, new Vector3(
                        Mathf.Cos(angle) * (radius + spiral),
                        elevation + Mathf.Sin(angle * 3f - spin) * 0.04f,
                        Mathf.Sin(angle) * (radius + spiral)));
                }
            }

            float height = mode == Ceremony.Ritual ? 2.6f : 1.8f;
            float beam = Mathf.Sin(Mathf.PI * t);
            pillar.widthMultiplier = Mathf.Max(0.001f, beam * intensity);
            pillar.SetPosition(0, new Vector3(0f, 0.10f, 0f));
            pillar.SetPosition(1, new Vector3(0f, height * beam, 0f));

            for (int i = 0; i < motes.Length; i++)
            {
                if (motes[i] == null) continue;
                float a = spin * (i % 2 == 0 ? 2.2f : -2.6f) +
                    i * 1.04719755f;
                float rad = (0.70f + 0.20f * Mathf.Sin(i * 2.4f + spin)) *
                    ritualScale;
                motes[i].localPosition = new Vector3(
                    Mathf.Cos(a) * rad,
                    0.25f + Mathf.Sin(a * 2f + spin) * 0.22f +
                    Mathf.Sin(Mathf.PI * t) * 0.80f,
                    Mathf.Sin(a) * rad);
                motes[i].localScale = Vector3.one *
                    (0.040f + 0.060f * intensity);
            }
        }

        private void OnDestroy()
        {
            foreach (var material in materials)
                if (material != null) Destroy(material);
        }
    }
}
