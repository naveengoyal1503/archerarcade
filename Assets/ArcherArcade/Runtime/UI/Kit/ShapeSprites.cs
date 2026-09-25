using System.Collections.Generic;
using UnityEngine;

namespace ArcherArcade.UI
{
    /// <summary>
    /// Procedural, anti-aliased UI shapes made once at startup (no image files): 9-slice rounded rectangles for every
    /// radius in DESIGN_TOKENS §5, outline rings (borders), soft shadows, circles, a progress ring and gradient
    /// textures. Sprites are 2 texture pixels per dp so corners stay crisp on dense screens.
    /// </summary>
    public static class ShapeSprites
    {
        const float Ppu = 2f;

        static readonly Dictionary<int, Sprite> Rounded = new Dictionary<int, Sprite>();
        static readonly Dictionary<long, Sprite> Outlines = new Dictionary<long, Sprite>();
        static readonly Dictionary<long, Sprite> Shadows = new Dictionary<long, Sprite>();
        static readonly Dictionary<long, Texture2D> Gradients = new Dictionary<long, Texture2D>();
        static Sprite _circle, _ring, _white;

        /// <summary>Filled rounded rectangle, 9-sliced; use Image.Type.Sliced. radius in dp.</summary>
        public static Sprite RoundedRect(float radius)
        {
            int key = Mathf.Max(1, Mathf.RoundToInt(radius * Ppu));
            if (Rounded.TryGetValue(key, out Sprite s) && s) return s;
            s = Build(key, 0, 0, false);
            Rounded[key] = s;
            return s;
        }

        /// <summary>Pill / circle: radius big enough for every control (Unity shrinks the corners to fit).</summary>
        public static Sprite Pill => RoundedRect(64f);

        /// <summary>Border only (transparent middle): radius and line thickness in dp.</summary>
        public static Sprite Outline(float radius, float thickness)
        {
            int r = Mathf.Max(1, Mathf.RoundToInt(radius * Ppu));
            int t = Mathf.Max(1, Mathf.RoundToInt(thickness * Ppu));
            long key = r * 1000L + t;
            if (Outlines.TryGetValue(key, out Sprite s) && s) return s;
            s = Build(r, t, 0, false);
            Outlines[key] = s;
            return s;
        }

        /// <summary>Soft blurred rounded rectangle for drop shadows (box-shadow blur in dp).</summary>
        public static Sprite Shadow(float radius, float blur)
        {
            int r = Mathf.Max(1, Mathf.RoundToInt(radius * Ppu));
            int b = Mathf.Max(1, Mathf.RoundToInt(blur * Ppu));
            long key = r * 1000L + b;
            if (Shadows.TryGetValue(key, out Sprite s) && s) return s;
            s = Build(r, 0, b, true);
            Shadows[key] = s;
            return s;
        }

        static Sprite Build(int radiusPx, int ringPx, int blurPx, bool soft)
        {
            int pad = soft ? blurPx : 1;
            int half = radiusPx + pad + 1;
            int size = half * 2 + 1;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, name = "shape" };
            var px = new Color32[size * size];
            float c = half; // centre of the middle pixel
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Signed distance to a rounded square whose flat middle is the single centre pixel.
                    float dx = Mathf.Max(Mathf.Abs(x - c) - 0.5f, 0f);
                    float dy = Mathf.Max(Mathf.Abs(y - c) - 0.5f, 0f);
                    float d = Mathf.Sqrt(dx * dx + dy * dy) - radiusPx; // < 0 inside
                    float a;
                    if (soft)
                    {
                        float t = Mathf.Clamp01((d + blurPx * 0.5f) / Mathf.Max(1f, blurPx));
                        a = 1f - t * t * (3f - 2f * t);
                    }
                    else
                    {
                        a = Mathf.Clamp01(0.5f - d);
                        if (ringPx > 0) a = Mathf.Min(a, Mathf.Clamp01(d + ringPx + 0.5f));
                    }
                    px[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            float border = half;
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), Ppu, 0, SpriteMeshType.FullRect,
                new Vector4(border, border, border, border));
        }

        /// <summary>Anti-aliased filled circle (simple sprite).</summary>
        public static Sprite Circle
        {
            get
            {
                if (_circle) return _circle;
                _circle = Disc(128, 0f);
                return _circle;
            }
        }

        /// <summary>Ring for radial fills (turn timer): 14 % thick.</summary>
        public static Sprite Ring
        {
            get
            {
                if (_ring) return _ring;
                _ring = Disc(128, 0.14f);
                return _ring;
            }
        }

        /// <summary>1-pixel white sprite for lines and plain quads.</summary>
        public static Sprite White
        {
            get
            {
                if (_white) return _white;
                var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false) { name = "white" };
                var px = new Color32[16];
                for (int i = 0; i < 16; i++) px[i] = new Color32(255, 255, 255, 255);
                tex.SetPixels32(px);
                tex.Apply(false, true);
                _white = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
                return _white;
            }
        }

        static Sprite Disc(int size, float ring)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "disc" };
            var px = new Color32[size * size];
            float c = (size - 1) * 0.5f, r = size * 0.5f - 1f, inner = r * (1f - ring);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
                    float a = Mathf.Clamp01(r - d + 0.5f);
                    if (ring > 0f) a = Mathf.Min(a, Mathf.Clamp01(d - inner + 0.5f));
                    px[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        /// <summary>Vertical gradient texture (top → bottom) for RawImage backgrounds.</summary>
        public static Texture2D Vertical(Color top, Color bottom)
        {
            long key = ((long)ColorKey(top) << 32) ^ ColorKey(bottom);
            if (Gradients.TryGetValue(key, out Texture2D t) && t) return t;
            const int h = 64;
            t = new Texture2D(2, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "vgrad" };
            for (int y = 0; y < h; y++)
            {
                Color c = Color.Lerp(bottom, top, y / (h - 1f));
                t.SetPixel(0, y, c);
                t.SetPixel(1, y, c);
            }
            t.Apply(false, true);
            Gradients[key] = t;
            return t;
        }

        /// <summary>Radial gradient (CSS radial-gradient(circle at cx cy, inner, outer at reach)).</summary>
        public static Texture2D Radial(Color inner, Color outer, float cx = 0.5f, float cy = 0.4f, float reach = 0.7f)
        {
            long key = ((long)ColorKey(inner) << 32) ^ ColorKey(outer) ^ ((long)(cy * 100) << 20) ^ (long)(reach * 1000);
            if (Gradients.TryGetValue(key, out Texture2D t) && t) return t;
            const int w = 128, h = 64;
            t = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "rgrad" };
            // Screen aspect ≈ 844:390, so measure distance in a 2:1 space to keep the circle round.
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float dx = (x / (w - 1f) - cx) * 2.16f, dy = (1f - y / (h - 1f)) - cy;
                    float d = Mathf.Sqrt(dx * dx + dy * dy) / (reach * 2.16f);
                    t.SetPixel(x, y, Color.Lerp(inner, outer, Mathf.Clamp01(d)));
                }
            }
            t.Apply(false, true);
            Gradients[key] = t;
            return t;
        }

        static uint ColorKey(Color c)
        {
            Color32 k = c;
            return (uint)(k.r << 24 | k.g << 16 | k.b << 8 | k.a);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Rounded.Clear();
            Outlines.Clear();
            Shadows.Clear();
            Gradients.Clear();
            _circle = _ring = _white = null;
        }
    }
}
