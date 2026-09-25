using ArcherArcade.Core;
using UnityEngine;

namespace ArcherArcade.Arena
{
    /// <summary>
    /// Ambient life in the arena (GAME_DESIGN §12): drifting leaves and wind streaks that show the wind's direction
    /// and strength at a glance, and fireflies at dusk and night. Pooled renderers, no allocations per frame;
    /// "Reduce motion" keeps half of them.
    /// </summary>
    public sealed class WeatherFx : MonoBehaviour
    {
        const int Leaves = 12, Streaks = 10, Flies = 16;

        readonly SpriteRenderer[] _leaf = new SpriteRenderer[Leaves];
        readonly Vector3[] _leafPos = new Vector3[Leaves];
        readonly float[] _leafPhase = new float[Leaves];
        readonly SpriteRenderer[] _streak = new SpriteRenderer[Streaks];
        readonly Vector3[] _streakPos = new Vector3[Streaks];
        readonly float[] _streakLife = new float[Streaks];
        readonly SpriteRenderer[] _fly = new SpriteRenderer[Flies];
        readonly Vector3[] _flyHome = new Vector3[Flies];
        readonly float[] _flyPhase = new float[Flies];
        int _wind;
        float _t;
        uint _rng = 0x2545F491u;
        bool _fireflies;
        int _active = Leaves;

        public static WeatherFx Create(Transform parent, bool fireflies, float minX, float maxX)
        {
            var go = new GameObject("Weather");
            go.transform.SetParent(parent, false);
            var w = go.AddComponent<WeatherFx>();
            w.Build(fireflies, minX, maxX);
            return w;
        }

        float R()
        {
            _rng ^= _rng << 13;
            _rng ^= _rng >> 17;
            _rng ^= _rng << 5;
            return (_rng & 0xFFFFFF) / 16777216f;
        }

        void Build(bool fireflies, float minX, float maxX)
        {
            bool reduced = ServiceLocator.Settings != null && ServiceLocator.Settings.ReduceMotion;
            _active = reduced ? Leaves / 2 : Leaves;
            Sprite leaf = ArtLibrary.Get(ArtLibrary.Fx, "leaf"), dash = ArtLibrary.Get(ArtLibrary.Fx, "dash");
            for (int i = 0; i < Leaves; i++)
            {
                _leaf[i] = WorldSprites.Make(transform, "Leaf" + i, leaf, WorldSprites.Weather);
                WorldSprites.FitWidth(_leaf[i], 0.18f + R() * 0.1f);
                _leaf[i].color = Color.Lerp(new Color(0.55f, 0.85f, 0.35f), new Color(0.95f, 0.75f, 0.3f), R());
                _leafPhase[i] = R() * 10f;
                _leaf[i].enabled = i < _active;
            }
            for (int i = 0; i < Streaks; i++)
            {
                _streak[i] = WorldSprites.Make(transform, "Streak" + i, dash, WorldSprites.Weather - 1);
                _streak[i].color = new Color(1f, 1f, 1f, 0f);
                _streakLife[i] = R() * 2f;
            }
            _fireflies = fireflies;
            Sprite fly = ArtLibrary.Get(ArtLibrary.Scenery, "firefly");
            for (int i = 0; i < Flies && fireflies; i++)
            {
                _fly[i] = WorldSprites.Make(transform, "Firefly" + i, fly, WorldSprites.FrontDecor - 5);
                WorldSprites.FitWidth(_fly[i], 0.22f);
                _flyHome[i] = new Vector3(Mathf.Lerp(minX, maxX, R()), 0.4f + R() * 2.6f, 0f);
                _flyPhase[i] = R() * 20f;
            }
        }

        public void SetWind(int wind) => _wind = wind;

        public void Place(Vector3 cam, float size, float aspect, float dt)
        {
            _t += dt;
            float halfW = size * aspect;
            float windX = _wind * 0.55f;
            for (int i = 0; i < _active; i++)
            {
                Vector3 p = _leafPos[i];
                bool outside = p.x < cam.x - halfW - 1f || p.x > cam.x + halfW + 1f || p.y < cam.y - size - 1f || p.y > cam.y + size + 1f;
                if (outside || p == Vector3.zero)
                {
                    float side = _wind > 0 ? -1f : _wind < 0 ? 1f : (R() < 0.5f ? -1f : 1f);
                    p = _wind != 0 && p != Vector3.zero
                        ? new Vector3(cam.x + side * (halfW + 0.8f), cam.y + (R() * 2f - 1f) * size, 0f)
                        : new Vector3(cam.x + (R() * 2f - 1f) * halfW, cam.y + size + 0.5f, 0f);
                }
                float ph = _leafPhase[i] + _t;
                p += new Vector3((windX + Mathf.Sin(ph * 1.7f) * 0.45f) * dt, (-0.45f + Mathf.Cos(ph * 2.1f) * 0.25f) * dt, 0f);
                _leafPos[i] = p;
                _leaf[i].transform.position = p;
                _leaf[i].transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Sin(ph * 2.3f) * 70f);
            }

            int strength = Mathf.Abs(_wind);
            int streaks = strength == 0 ? 0 : Mathf.Min(Streaks, 2 + strength * 2);
            for (int i = 0; i < Streaks; i++)
            {
                SpriteRenderer sr = _streak[i];
                if (i >= streaks)
                {
                    sr.color = new Color(1f, 1f, 1f, 0f);
                    continue;
                }
                _streakLife[i] -= dt;
                if (_streakLife[i] <= 0f)
                {
                    _streakLife[i] = 0.9f + R() * 0.6f;
                    _streakPos[i] = new Vector3(cam.x + (R() * 2f - 1f) * halfW, cam.y - size * 0.3f + R() * size * 1.2f, 0f);
                }
                _streakPos[i].x += Mathf.Sign(_wind) * (3f + strength * 1.6f) * dt;
                float k = Mathf.Clamp01(_streakLife[i] / 1.4f);
                sr.transform.position = _streakPos[i];
                sr.transform.localScale = new Vector3((1.4f + strength * 0.35f) * Mathf.Sign(_wind) * size / 4.4f, 0.8f * size / 4.4f, 1f);
                sr.color = new Color(1f, 1f, 1f, Mathf.Sin(k * Mathf.PI) * 0.35f);
            }

            if (!_fireflies) return;
            for (int i = 0; i < Flies; i++)
            {
                float ph = _flyPhase[i] + _t * 0.6f;
                Vector3 home = _flyHome[i];
                _fly[i].transform.position = home + new Vector3(Mathf.Sin(ph * 1.3f) * 0.7f, Mathf.Sin(ph * 2.1f + 1f) * 0.35f, 0f);
                float glow = 0.35f + 0.65f * Mathf.Max(0f, Mathf.Sin(ph * 3.1f));
                _fly[i].color = new Color(1f, 1f, 0.7f, glow);
            }
        }
    }
}
