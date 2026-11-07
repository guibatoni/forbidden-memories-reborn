using UnityEngine;
using UnityEngine.Rendering;

namespace Reborn.Presentation
{
    internal static class ArenaVisuals
    {
        public static readonly Color Stone = new Color(0.22f, 0.22f, 0.28f);
        public static readonly Color Dark = new Color(0.055f, 0.075f, 0.13f);
        public static readonly Color Midnight = new Color(0.085f, 0.11f, 0.18f);
        public static readonly Color Gold = new Color(0.72f, 0.51f, 0.20f);
        public static readonly Color Cyan = new Color(0.16f, 0.83f, 1.0f);
        public static readonly Color Red = new Color(0.85f, 0.26f, 0.27f);

        public static Material Mat(Color color, bool emissive = false)
        {
            // URP sem Pipeline Asset ativo deixa shaders Lit rosa no modo Built-in.
            // Selecionamos o shader coerente com o pipeline realmente configurado.
            var shader = GraphicsSettings.currentRenderPipeline != null
                ? Shader.Find("Universal Render Pipeline/Lit")
                : Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            var material = new Material(shader) { color = color };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            if (emissive && material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * 0.7f);
            }
            return material;
        }

        public static GameObject Cube(string name, Transform parent, Vector3 pos, Vector3 size, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name; go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }

        public static GameObject Cylinder(string name, Transform parent, Vector3 pos, Vector3 size, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name; go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }

        public static void Label(string title, Transform parent, Vector3 localPos, Color color, float charSize = 0.14f)
        {
            var go = new GameObject("Label_" + title);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var tm = go.AddComponent<TextMesh>();
            tm.text = title; tm.fontSize = 44; tm.characterSize = charSize;
            tm.anchor = TextAnchor.MiddleCenter; tm.alignment = TextAlignment.Center;
            tm.color = color;
        }
    }
}
