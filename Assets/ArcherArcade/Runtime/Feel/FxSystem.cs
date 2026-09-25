using ArcherArcade.Core;
using UnityEngine;

namespace ArcherArcade.Feel
{
    /// <summary>
    /// Pooled sprite particles for all match juice (CLAUDE.md: pool particles, no per-frame allocations): bursts,
    /// splinters, smoke, sparks, leaves, confetti, flashes, rings and lightning bolts. Runs on scaled time so
    /// hit-stop and slow-mo freeze / slow it. "Reduce motion" halves the particle counts.
    /// </summary>
    public sealed class FxSystem : MonoBehaviour
    {
        const int Capacity = 480;

        struct P
        {
            public bool Alive;
            public Vector3 Pos, Vel;
            public float Life, Max, Gravity, Drag, Spin, Rot, Size0, Size1, Stretch;
            public Color C0, C1;
            public bool AlignToVelocity;
        }

        readonly P[] _p = new P[Capacity];
        readonly SpriteRenderer[] _sr = new SpriteRenderer[Capacity];
        int _next;
        uint _rng = 0x6C8E9CF5u;

        public static FxSystem Instance { get; private set; }

        public static FxSystem Create(Transform parent)
        {
            var go = new GameObject("Fx");
            go.transform.SetParent(parent, false);
            Instance = go.AddComponent<FxSystem>();
            Instance.Build();
            return Instance;
        }

        void Build()
        {
            for (int i = 0; i < Capacity; i++)
            {
                var go = new GameObject("p");
                go.transform.SetParent(transform, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.enabled = false;
                sr.sortingOrder = 900;
                _sr[i] = sr;
            }
        }

        public float Rand01()
        {
            _rng ^= _rng << 13;
            _rng ^= _rng >> 17;
            _rng ^= _rng << 5;
            return (_rng & 0xFFFFFF) / 16777216f;
        }

        public float Range(float a, float b) => a + (b - a) * Rand01();

        static bool Reduced => ServiceLocator.Settings != null && ServiceLocator.Settings.ReduceMotion;

        /// <summary>One particle. Sizes in metres (sprite width), life in seconds.</summary>
        public void Emit(Sprite sprite, Vector3 pos, Vector3 vel, float life, float size0, float size1, Color c0, Color c1,
            float gravity = 0f, float drag = 0f, float spin = 0f, int order = 900, bool alignToVelocity = false, float stretch = 1f)
        {
            if (!sprite) return;
            int i = _next;
            _next = (_next + 1) % Capacity;
            SpriteRenderer sr = _sr[i];
            sr.sprite = sprite;
            sr.sortingOrder = order;
            sr.enabled = true;
            _p[i] = new P
            {
                Alive = true, Pos = pos, Vel = vel, Life = life, Max = life, Gravity = gravity, Drag = drag, Spin = spin,
                Rot = Range(0f, 360f), Size0 = size0, Size1 = size1, C0 = c0, C1 = c1, AlignToVelocity = alignToVelocity, Stretch = stretch
            };
            Apply(i, 0f);
        }

        /// <summary>Radial burst of particles with random speed / size, colours cycled from the list.</summary>
        public void Burst(Sprite sprite, Vector3 pos, int count, float speed, float life, Color[] colors, float size, float gravity = 9f,
            float upBias = 1.5f, int order = 900, float spin = 360f)
        {
            if (Reduced) count = Mathf.Max(1, count / 2);
            for (int i = 0; i < count; i++)
            {
                float a = Range(0f, Mathf.PI * 2f), v = speed * Range(0.4f, 1.2f);
                var vel = new Vector3(Mathf.Cos(a) * v, Mathf.Sin(a) * v + upBias, 0f);
                Color c = colors[i % colors.Length];
                float s = size * Range(0.6f, 1.3f);
                Emit(sprite, pos, vel, life * Range(0.7f, 1.2f), s, s * 0.3f, c, new Color(c.r, c.g, c.b, 0f), gravity, 1.2f,
                    Range(-spin, spin), order);
            }
        }

        public void Flash(Vector3 pos, Color color, float size, float life, int order = 950)
        {
            Emit(ArtLibrary.Get(ArtLibrary.Fx, "glow"), pos, Vector3.zero, life, size * 0.6f, size, color, new Color(color.r, color.g, color.b, 0f), 0f, 0f, 0f, order);
        }

        public void Ring(Vector3 pos, Color color, float radius, float life, int order = 940)
        {
            Emit(ArtLibrary.Get(ArtLibrary.Fx, "ring"), pos, Vector3.zero, life, radius * 0.3f, radius * 2f, color, new Color(color.r, color.g, color.b, 0f), 0f, 0f, 0f, order);
        }

        public void Smoke(Vector3 pos, int count, float size, Color color, int order = 880)
        {
            Sprite s = ArtLibrary.Get(ArtLibrary.Fx, "smoke");
            if (Reduced) count = Mathf.Max(1, count / 2);
            for (int i = 0; i < count; i++)
            {
                var vel = new Vector3(Range(-1.2f, 1.2f), Range(0.6f, 2.2f), 0f);
                Emit(s, pos + new Vector3(Range(-0.3f, 0.3f), Range(-0.2f, 0.3f), 0f), vel, Range(0.8f, 1.4f), size * 0.6f, size * 1.4f,
                    color, new Color(color.r, color.g, color.b, 0f), -0.6f, 1.5f, Range(-40f, 40f), order);
            }
        }

        /// <summary>A lightning bolt from the sky onto a point (Electric tip, GAME_DESIGN §3.3.1).</summary>
        public void Lightning(Vector3 target, float height)
        {
            Sprite bolt = ArtLibrary.Get(ArtLibrary.Fx, "bolt");
            if (!bolt) return;
            float h = bolt.bounds.size.y;
            float scale = height / Mathf.Max(0.01f, h);
            int i = _next;
            _next = (_next + 1) % Capacity;
            SpriteRenderer sr = _sr[i];
            sr.sprite = bolt;
            sr.sortingOrder = 960;
            sr.enabled = true;
            // Sprite pivot is near the bottom tip: stretch it up from the target.
            _p[i] = new P
            {
                Alive = true, Pos = target, Vel = Vector3.zero, Life = 0.35f, Max = 0.35f, Rot = Range(-6f, 6f),
                Size0 = bolt.bounds.size.x * scale, Size1 = bolt.bounds.size.x * scale, C0 = Color.white, C1 = new Color(1f, 0.95f, 0.6f, 0f),
                Stretch = 1f
            };
            Apply(i, 0f);
            Flash(target, new Color(1f, 0.95f, 0.7f, 0.9f), 3.2f, 0.3f);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            for (int i = 0; i < Capacity; i++)
            {
                if (!_p[i].Alive) continue;
                _p[i].Life -= dt;
                if (_p[i].Life <= 0f)
                {
                    _p[i].Alive = false;
                    _sr[i].enabled = false;
                    continue;
                }
                _p[i].Vel.y -= _p[i].Gravity * dt;
                if (_p[i].Drag > 0f) _p[i].Vel *= Mathf.Max(0f, 1f - _p[i].Drag * dt);
                _p[i].Pos += _p[i].Vel * dt;
                _p[i].Rot += _p[i].Spin * dt;
                Apply(i, 1f - _p[i].Life / _p[i].Max);
            }
        }

        void Apply(int i, float k)
        {
            SpriteRenderer sr = _sr[i];
            Transform t = sr.transform;
            t.position = _p[i].Pos;
            float size = Mathf.Lerp(_p[i].Size0, _p[i].Size1, k);
            float w = sr.sprite ? Mathf.Max(0.0001f, sr.sprite.bounds.size.x) : 1f;
            float s = size / w;
            if (_p[i].AlignToVelocity && _p[i].Vel.sqrMagnitude > 0.01f)
            {
                t.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(_p[i].Vel.y, _p[i].Vel.x) * Mathf.Rad2Deg);
                t.localScale = new Vector3(s * _p[i].Stretch, s, 1f);
            }
            else
            {
                t.rotation = Quaternion.Euler(0f, 0f, _p[i].Rot);
                t.localScale = new Vector3(s, s, 1f);
            }
            sr.color = Color.Lerp(_p[i].C0, _p[i].C1, k);
        }

        public void Clear()
        {
            for (int i = 0; i < Capacity; i++)
            {
                _p[i].Alive = false;
                if (_sr[i]) _sr[i].enabled = false;
            }
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
