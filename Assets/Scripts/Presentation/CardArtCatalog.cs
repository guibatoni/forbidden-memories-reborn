using System;
using System.Collections.Generic;
using Reborn.Core;
using UnityEngine;

namespace Reborn.Presentation
{
    /// <summary>
    /// Artwork is optional and entirely local. Nothing here downloads images or ships
    /// game art. Place appropriately licensed PNGs at Assets/Resources/RebornCardArt/{ID}.png.
    /// A deterministic abstract texture is used whenever local artwork is absent.
    /// </summary>
    internal static class CardArtCatalog
    {
        private static readonly Dictionary<string, Texture2D> Cache = new Dictionary<string, Texture2D>();
        private const int Size = 128;

        public static Texture2D Get(CardDefinition card)
        {
            if (card == null) return GetFallback("BACK", CardKind.Trap);
            if (Cache.TryGetValue(card.Id, out var cached)) return cached;
            Texture2D loaded = null;
            if (SafeResourceName(card.Id))
                loaded = Resources.Load<Texture2D>("RebornCardArt/" + card.Id);
            var result = loaded != null ? loaded : MakeFallback(card.Id, card.Kind);
            Cache[card.Id] = result;
            return result;
        }

        private static Texture2D GetFallback(string id, CardKind kind)
        {
            if (!Cache.TryGetValue(id, out var result))
            {
                result = MakeFallback(id, kind);
                Cache[id] = result;
            }
            return result;
        }

        private static bool SafeResourceName(string id)
        {
            if (string.IsNullOrEmpty(id) || id.Length > 64) return false;
            foreach (char c in id)
                if (!char.IsLetterOrDigit(c) && c != '_' && c != '-') return false;
            return true;
        }

        private static int StableSeed(string value)
        {
            unchecked
            {
                uint hash = 2166136261;
                foreach (char c in value) { hash ^= c; hash *= 16777619; }
                return (int)(hash & 0x7fffffff);
            }
        }

        private static Texture2D MakeFallback(string id, CardKind kind)
        {
            var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
            texture.name = "Reborn_OriginalPlaceholder_" + id;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            Color32[] pixels = new Color32[Size * Size];
            int seed = StableSeed(id);
            Color low = kind == CardKind.Monster ? new Color(0.08f, 0.12f, 0.24f) :
                kind == CardKind.Trap ? new Color(0.19f, 0.07f, 0.25f) :
                kind == CardKind.Ritual ? new Color(0.17f, 0.14f, 0.26f) :
                new Color(0.06f, 0.21f, 0.22f);
            Color high = kind == CardKind.Monster ? new Color(0.50f, 0.31f, 0.19f) :
                kind == CardKind.Trap ? new Color(0.44f, 0.24f, 0.52f) :
                kind == CardKind.Ritual ? new Color(0.61f, 0.47f, 0.17f) :
                new Color(0.14f, 0.48f, 0.43f);
            int rays = 5 + seed % 5;
            for (int y = 0; y < Size; y++)
            for (int x = 0; x < Size; x++)
            {
                float dx = (x - 63.5f) / 64f;
                float dy = (y - 63.5f) / 64f;
                float radius = Mathf.Sqrt(dx * dx + dy * dy);
                float angle = Mathf.Atan2(dy, dx);
                float halo = Mathf.Clamp01(1f - radius);
                float spokes = Mathf.Pow(Mathf.Max(0f, Mathf.Cos(angle * rays + seed % 13)), 14f);
                float ring = Mathf.Abs(radius - 0.52f) < 0.018f ? 0.65f : 0f;
                float inner = Mathf.Abs(radius - 0.27f) < 0.015f ? 0.35f : 0f;
                float grid = (x % 16 == 0 || y % 16 == 0) ? 0.065f : 0f;
                float mix = Mathf.Clamp01(0.12f + 0.50f * halo + spokes * 0.16f + ring + inner + grid);
                Color c = Color.Lerp(low, high, mix);
                pixels[y * Size + x] = c;
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }
    }
}
