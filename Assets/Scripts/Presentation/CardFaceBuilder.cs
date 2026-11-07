using System.Collections.Generic;
using Reborn.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace Reborn.Presentation
{
    /// <summary>
    /// Reusable original 2.5D card prefab generated entirely at runtime. The outer
    /// transform stays unscaled so labels do not inherit the flattened cube scale.
    /// A single root collider provides robust selection/drag interactions.
    /// </summary>
    internal static class CardFaceBuilder
    {
        private static readonly Dictionary<string, Material> Materials = new Dictionary<string, Material>();
        private static readonly Dictionary<string, Material> ArtMaterials = new Dictionary<string, Material>();

        private static Material Solid(string key, Color color, bool emissive = false)
        {
            if (!Materials.TryGetValue(key, out var material))
            {
                material = ArenaVisuals.Mat(color, emissive);
                Materials[key] = material;
            }
            return material;
        }

        private static Color CategoryColor(CardKind kind)
        {
            switch (kind)
            {
                case CardKind.Spell: return new Color(0.12f, 0.55f, 0.56f);
                case CardKind.Trap: return new Color(0.55f, 0.26f, 0.68f);
                case CardKind.Equip: return new Color(0.20f, 0.54f, 0.37f);
                case CardKind.Ritual: return new Color(0.43f, 0.38f, 0.76f);
                default: return new Color(0.71f, 0.37f, 0.19f);
            }
        }

        public static string KindLabel(CardKind kind)
        {
            switch (kind)
            {
                case CardKind.Spell: return "MAGIA";
                case CardKind.Trap: return "ARMADILHA";
                case CardKind.Equip: return "EQUIPAMENTO";
                case CardKind.Ritual: return "RITUAL";
                default: return "MONSTRO";
            }
        }

        private static Material Artwork(CardDefinition card)
        {
            string id = card == null ? "BACK" : card.Id;
            if (ArtMaterials.TryGetValue(id, out var result)) return result;
            Shader shader = GraphicsSettings.currentRenderPipeline != null
                ? Shader.Find("Universal Render Pipeline/Unlit")
                : Shader.Find("Unlit/Texture");
            if (shader == null) shader = Shader.Find("Unlit/Texture");
            if (shader == null) shader = Shader.Find("Standard");
            result = new Material(shader);
            result.name = "Reborn_Art_" + id;
            Texture2D texture = CardArtCatalog.Get(card);
            result.mainTexture = texture;
            if (result.HasProperty("_BaseMap")) result.SetTexture("_BaseMap", texture);
            if (result.HasProperty("_BaseColor")) result.SetColor("_BaseColor", Color.white);
            if (result.HasProperty("_Color")) result.SetColor("_Color", Color.white);
            ArtMaterials[id] = result;
            return result;
        }

        private static GameObject Block(string name, Transform parent, Vector3 pos,
            Vector3 size, Material material)
        {
            var go = ArenaVisuals.Cube(name, parent, pos, size, material);
            var box = go.GetComponent<Collider>();
            if (box != null) box.enabled = false;
            return go;
        }

        private static void Text(string title, Transform parent, float z, float y,
            float size, Color color)
        {
            var label = new GameObject("Text_" + title);
            label.transform.SetParent(parent, false);
            label.transform.localPosition = new Vector3(0f, y, z);
            label.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var mesh = label.AddComponent<TextMesh>();
            mesh.text = title;
            mesh.fontSize = 52;
            mesh.characterSize = size;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.color = color;
        }

        private static string ShortName(string name, int max)
        {
            if (string.IsNullOrEmpty(name)) return "";
            return name.Length <= max ? name : name.Substring(0, max - 1) + "…";
        }

        public static CardView Create(string name, Transform parent, CardDefinition card,
            int handIndex, Vector3 position, Vector3 dimensions, bool facedown)
        {
            float width = dimensions.x;
            float height = dimensions.z;
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.localPosition = position;
            var collider = root.AddComponent<BoxCollider>();
            collider.size = new Vector3(width, 0.16f, height);
            collider.center = Vector3.zero;
            Color type = facedown || card == null ? new Color(0.24f, 0.23f, 0.40f) : CategoryColor(card.Kind);
            Material rim = Solid("rim", new Color(0.87f, 0.70f, 0.43f));
            Material surface = Solid("surface", new Color(0.055f, 0.072f, 0.11f));
            Material frame = Solid("frame_" + (card == null ? "Back" : card.Kind.ToString()), type);
            Material glow = Solid("selected", new Color(0.28f, 0.89f, 1.0f), true);
            // Meshes share cache materials, while the CardView can switch its own border.
            var border = Block("Border_Selectable", root.transform, Vector3.zero,
                new Vector3(width, 0.10f, height), frame).GetComponent<Renderer>();
            Block("Inner_Rim", root.transform, new Vector3(0f, 0.057f, 0f),
                new Vector3(width - 0.06f, 0.012f, height - 0.06f), rim);
            Block("Face", root.transform, new Vector3(0f, 0.066f, 0f),
                new Vector3(width - 0.11f, 0.012f, height - 0.11f), surface);
            if (facedown || card == null)
            {
                Block("Back_Art", root.transform, new Vector3(0f, 0.076f, 0f),
                    new Vector3(width - 0.20f, 0.012f, height - 0.22f), Artwork(null));
                Text("R E B O R N", root.transform, -height * 0.36f, 0.092f, 0.054f, Color.white);
            }
            else
            {
                Material header = Solid("header_" + card.Kind, Color.Lerp(type, Color.white, 0.20f));
                Block("Title_Ribbon", root.transform, new Vector3(0f, 0.077f, height * 0.405f),
                    new Vector3(width - 0.16f, 0.014f, height * 0.15f), header);
                Text(ShortName(card.Name, 10), root.transform, height * 0.405f, 0.091f,
                    0.045f, Color.white);
                Block("Artwork_Frame", root.transform, new Vector3(0f, 0.078f, height * 0.045f),
                    new Vector3(width - 0.16f, 0.012f, height * 0.52f), rim);
                Block("Artwork", root.transform, new Vector3(0f, 0.087f, height * 0.045f),
                    new Vector3(width - 0.20f, 0.012f, height * 0.48f), Artwork(card));
                Block("Category_Ribbon", root.transform, new Vector3(0f, 0.078f, -height * 0.295f),
                    new Vector3(width - 0.16f, 0.014f, height * 0.11f), header);
                Text(KindLabel(card.Kind), root.transform, -height * 0.295f, 0.094f,
                    0.042f, Color.white);
                string stats = card.Kind == CardKind.Monster
                    ? card.Attack + " / " + card.Defense
                    : "ID " + card.Id;
                Text(ShortName(stats, 15), root.transform, -height * 0.407f, 0.089f,
                    0.038f, new Color(1f, 0.93f, 0.70f));
            }
            var view = root.AddComponent<CardView>();
            view.Initialize(card, handIndex, border, frame, glow, facedown);
            return view;
        }
    }
}
