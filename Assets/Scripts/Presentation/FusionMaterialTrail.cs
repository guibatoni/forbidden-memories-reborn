using UnityEngine;

namespace Reborn.Presentation
{
    /// <summary>
    /// Original constellation-style path per material. Pure presentation:
    /// the Core has already committed a verified fusion when this begins.
    /// No camera movement, texture copying or physics interaction.
    /// </summary>
    public sealed class FusionMaterialTrail : MonoBehaviour
    {
        private Vector3 from;
        private Vector3 to;
        private Vector3 sideways;
        private float duration;
        private float delay;
        private float elapsed;
        private bool finished;
        private TrailRenderer trail;

        public void Begin(Vector3 start, Vector3 destination, float startDelay,
            Material material)
        {
            from = start;
            to = destination;
            delay = Mathf.Max(0f,startDelay);
            duration = 0.66f;
            elapsed = 0f;
            var vector = to-from;
            sideways = Vector3.Cross(vector.normalized,Vector3.up).normalized;
            transform.position = from;
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "FusionLight";
            sphere.transform.SetParent(transform,false);
            sphere.transform.localScale = Vector3.one*0.15f;
            var collider = sphere.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            var renderer = sphere.GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterial = material;
            trail = gameObject.AddComponent<TrailRenderer>();
            trail.material = material;
            trail.time = 0.34f;
            trail.startWidth = 0.11f;
            trail.endWidth = 0.014f;
            trail.minVertexDistance = 0.025f;
            trail.numCornerVertices = 4;
            trail.numCapVertices = 3;
            trail.emitting = false;
        }

        private void Update()
        {
            if (finished) return;
            elapsed += Time.deltaTime;
            if (elapsed < delay) return;
            if (trail != null) trail.emitting = true;
            float t = Mathf.Clamp01((elapsed-delay)/duration);
            float eased = t*t*(3f-2f*t);
            float arc = Mathf.Sin(Mathf.PI*t)*0.95f;
            float helix = Mathf.Sin(Mathf.PI*t*2f)*
                Mathf.Sin(Mathf.PI*t)*0.34f;
            transform.position = Vector3.Lerp(from,to,eased) +
                Vector3.up*arc + sideways*helix;
            transform.localScale = Vector3.one *
                (0.72f+0.43f*Mathf.Sin(Mathf.PI*t));
            if (t >= 1f)
            {
                finished = true;
                if (trail != null) trail.emitting = false;
                Destroy(gameObject,0.50f);
            }
        }
    }
}
