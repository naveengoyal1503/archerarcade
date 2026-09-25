using ArcherArcade.Core;
using ArcherArcade.UI;
using UnityEngine;

namespace ArcherArcade.Arena
{
    /// <summary>
    /// Everything painted on the sky, glued to the camera: the gradient (prototype #8FD3FF → #E6F6FF, dark
    /// #1E1A45 → #4A3478), the sun with slow rays or the moon, twinkling stars at night and puffy clouds that drift
    /// with the wind (design match canvas: speed 8 + wind × 5).
    /// </summary>
    public sealed class SkyLayer : MonoBehaviour
    {
        const int CloudCount = 6, StarCount = 34;

        SpriteRenderer _sky, _sun, _sunGlow, _rays, _rays2;
        readonly SpriteRenderer[] _clouds = new SpriteRenderer[CloudCount];
        readonly Vector2[] _cloudPos = new Vector2[CloudCount];
        readonly float[] _cloudScale = new float[CloudCount];
        readonly SpriteRenderer[] _stars = new SpriteRenderer[StarCount];
        readonly Vector2[] _starPos = new Vector2[StarCount];
        readonly float[] _starPhase = new float[StarCount];
        float _t, _wind;
        bool _moon;

        public static SkyLayer Create(Transform parent, SkyPalette pal, bool dark, uint seed)
        {
            var go = new GameObject("Sky");
            go.transform.SetParent(parent, false);
            var sky = go.AddComponent<SkyLayer>();
            sky.Build(pal, dark, seed);
            return sky;
        }

        void Build(SkyPalette pal, bool dark, uint seed)
        {
            Texture2D grad = ShapeSprites.Vertical(pal.Top, pal.Bottom);
            Sprite s = Sprite.Create(grad, new Rect(0, 0, grad.width, grad.height), new Vector2(0.5f, 0.5f), grad.height);
            s.name = "sky";
            _sky = WorldSprites.Make(transform, "Gradient", s, WorldSprites.Sky);
            _moon = pal.Moon;
            _sunGlow = WorldSprites.Make(transform, ArtLibrary.Fx, "glow", WorldSprites.SkyDecor);
            _sunGlow.color = new Color(pal.Sun.r, pal.Sun.g, pal.Sun.b, _moon ? 0.35f : 0.55f);
            if (!_moon)
            {
                _rays = WorldSprites.Make(transform, ArtLibrary.Fx, "star", WorldSprites.SkyDecor);
                _rays.color = new Color(1f, 0.96f, 0.75f, 0.28f);
                _rays2 = WorldSprites.Make(transform, ArtLibrary.Fx, "star", WorldSprites.SkyDecor);
                _rays2.color = new Color(1f, 0.96f, 0.75f, 0.18f);
            }
            _sun = WorldSprites.Make(transform, ArtLibrary.Scenery, _moon ? "moon" : "sun", WorldSprites.SkyDecor + 1);

            uint r = seed | 1u;
            if (pal.Stars)
            {
                Sprite dot = ArtLibrary.Get(ArtLibrary.Fx, "dot"), star = ArtLibrary.Get(ArtLibrary.Fx, "star");
                for (int i = 0; i < StarCount; i++)
                {
                    _stars[i] = WorldSprites.Make(transform, "Star" + i, i % 5 == 0 ? star : dot, WorldSprites.SkyDecor);
                    _starPos[i] = new Vector2(Rand(ref r) * 2f - 1f, 0.05f + Rand(ref r) * 0.95f);
                    _starPhase[i] = Rand(ref r) * 6.28f;
                }
            }
            Sprite cloud = ArtLibrary.Get(ArtLibrary.Scenery, "cloud");
            for (int i = 0; i < CloudCount; i++)
            {
                _clouds[i] = WorldSprites.Make(transform, "Cloud" + i, cloud, WorldSprites.Clouds + (i % 2));
                _clouds[i].color = dark || pal.Moon ? new Color(0.8f, 0.8f, 1f, 0.16f) : new Color(1f, 1f, 1f, 0.92f);
                _cloudPos[i] = new Vector2(Rand(ref r) * 2.4f - 1.2f, 0.25f + Rand(ref r) * 0.6f);
                _cloudScale[i] = 0.5f + Rand(ref r) * 0.55f;
            }
        }

        static float Rand(ref uint s)
        {
            s ^= s << 13;
            s ^= s >> 17;
            s ^= s << 5;
            return (s & 0xFFFFFF) / 16777216f;
        }

        public void SetWind(int wind) => _wind = wind;

        /// <summary>Follows the camera; <paramref name="dt"/> is scaled time (clouds pause with the game).</summary>
        public void Place(Vector3 cam, float size, float aspect, float dt)
        {
            _t += dt;
            float halfW = size * aspect, zoom = size / 4.4f;
            _sky.transform.position = new Vector3(cam.x, cam.y, 0f);
            Vector3 b = _sky.sprite.bounds.size;
            _sky.transform.localScale = new Vector3((halfW * 2f + 2f) / b.x, (size * 2f + 2f) / b.y, 1f);

            // Sun / moon: top right of the view (design: x 690 of 844, y 92 of 390).
            var sunPos = new Vector3(cam.x + halfW * 0.63f, cam.y + size * 0.53f, 0f);
            _sun.transform.position = sunPos;
            WorldSprites.FitWidth(_sun, (_moon ? 1.25f : 1.45f) * zoom);
            _sunGlow.transform.position = sunPos;
            WorldSprites.FitWidth(_sunGlow, (3.4f + Mathf.Sin(_t * 1.2f) * 0.2f) * zoom);
            if (_rays)
            {
                _rays.transform.position = sunPos;
                _rays.transform.rotation = Quaternion.Euler(0f, 0f, _t * 6f);
                WorldSprites.FitWidth(_rays, 3.1f * zoom);
                _rays2.transform.position = sunPos;
                _rays2.transform.rotation = Quaternion.Euler(0f, 0f, -_t * 4f + 18f);
                WorldSprites.FitWidth(_rays2, 3.8f * zoom);
            }

            for (int i = 0; i < StarCount; i++)
            {
                if (!_stars[i]) break;
                Vector2 p = _starPos[i];
                _stars[i].transform.position = new Vector3(cam.x + p.x * halfW, cam.y - size * 0.1f + p.y * size * 1.1f, 0f);
                WorldSprites.FitWidth(_stars[i], (i % 5 == 0 ? 0.16f : 0.07f) * zoom);
                float a = 0.45f + 0.45f * Mathf.Sin(_t * (1.3f + (i % 4) * 0.4f) + _starPhase[i]);
                _stars[i].color = new Color(1f, 0.97f, 0.85f, a);
            }

            float speed = (0.06f + _wind * 0.035f) * dt;
            for (int i = 0; i < CloudCount; i++)
            {
                _cloudPos[i].x += speed * _cloudScale[i];
                if (_cloudPos[i].x > 1.25f) _cloudPos[i].x = -1.25f;
                if (_cloudPos[i].x < -1.25f) _cloudPos[i].x = 1.25f;
                _clouds[i].transform.position = new Vector3(cam.x + _cloudPos[i].x * halfW, cam.y - size + _cloudPos[i].y * size * 2f, 0f);
                WorldSprites.FitWidth(_clouds[i], 2.6f * _cloudScale[i] * zoom);
            }
        }
    }
}
