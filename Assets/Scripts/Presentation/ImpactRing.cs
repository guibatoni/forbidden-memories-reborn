using UnityEngine;

namespace Reborn.Presentation
{
    /// <summary>
    /// Procedural shockwave without imported effects or an extra field slot.
    /// Presentation-only: no physics, no camera changes, no gameplay mutations.
    /// </summary>
    public sealed class ImpactRing : MonoBehaviour
    {
        private LineRenderer line;
        private Material ringMaterial;
        private float age;
        private const float Lifetime = 0.46f;
        private Color tint;
        private const int Segments = 48;

        public void Initialize(Color color)
        {
            tint = color;
            line = gameObject.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.positionCount = Segments + 1;
            line.loop = false;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            var shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            ringMaterial = new Material(shader);
            line.sharedMaterial = ringMaterial;
            line.widthMultiplier = 0.10f;
            Draw(0f);
        }

        private void Update()
        {
            age += Time.deltaTime;
            Draw(Mathf.Clamp01(age / Lifetime));
            if (age >= Lifetime) Destroy(gameObject);
        }

        private void Draw(float progress)
        {
            if (line == null) return;
            float radius = Mathf.Lerp(0.20f,1.25f,progress);
            for (int i=0;i<=Segments;i++)
            {
                float angle = (i / (float)Segments) * Mathf.PI * 2f;
                line.SetPosition(i,new Vector3(Mathf.Cos(angle)*radius,0.05f,
                    Mathf.Sin(angle)*radius));
            }
            float alpha = 1f-progress;
            Color fade = new Color(tint.r,tint.g,tint.b,alpha);
            line.startColor = fade;
            line.endColor = fade;
            line.widthMultiplier = Mathf.Lerp(0.10f,0.018f,progress);
        }

        private void OnDestroy()
        {
            if (ringMaterial != null) Destroy(ringMaterial);
        }
    }
}
