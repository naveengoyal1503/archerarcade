using ArcherArcade.Arena;
using ArcherArcade.Core;
using ArcherArcade.Feel;
using ArcherArcade.Logic;
using UnityEngine;

namespace ArcherArcade.Match
{
    /// <summary>
    /// One flying arrow, replayed exactly from its Logic <see cref="ArrowPath"/> (Ballistics.PositionAt), with the
    /// equipped trail (Classic, Sparkle, Rainbow, Comet), a glowing element tip in flight (fire flicker, electric
    /// sparks, frost mist, bomb fuse, poison bubbles, GAME_DESIGN §3.3.1) and a quiver when it sticks. The visual
    /// start blends from the bow's drawn arrow to the Logic launch point in the first 0.15 s. Pooled.
    /// </summary>
    public sealed class ArrowView : MonoBehaviour
    {
        const float BlendSeconds = 0.15f;

        static Material _trailMaterial;

        SpriteRenderer _sr, _glow;
        TrailRenderer _trail;
        ArrowPath _path;
        Vector3 _offset;
        ArrowTip _tip;
        string _trailId;
        bool _flying, _stuck;
        float _stuckTime, _fxTimer, _stuckAngle;
        int _index;

        public bool Busy => _flying || _stuck;
        public bool Flying => _flying;
        public int PathIndex => _index;
        public ArrowPath Path => _path;
        public Sprite Sprite => _sr.sprite;

        public static ArrowView Create(Transform parent)
        {
            var go = new GameObject("Arrow");
            go.transform.SetParent(parent, false);
            var v = go.AddComponent<ArrowView>();
            v.Build();
            return v;
        }

        static Material TrailMaterial
        {
            get
            {
                if (_trailMaterial) return _trailMaterial;
                Shader sh = Shader.Find("Sprites/Default");
                _trailMaterial = new Material(sh) { name = "ArrowTrail" };
                return _trailMaterial;
            }
        }

        void Build()
        {
            _sr = WorldSprites.Make(transform, "Body", ArtLibrary.Get(ArtLibrary.Arrows, "arrow_normal"), WorldSprites.Flight);
            _glow = WorldSprites.Make(transform, ArtLibrary.Fx, "glow", WorldSprites.Flight - 1);
            _glow.transform.localPosition = new Vector3(-0.06f, 0f, 0f);
            _glow.enabled = false;
            _trail = gameObject.AddComponent<TrailRenderer>();
            _trail.sharedMaterial = TrailMaterial;
            _trail.sortingOrder = WorldSprites.Flight - 2;
            _trail.time = 0.22f;
            _trail.minVertexDistance = 0.08f;
            _trail.numCapVertices = 2;
            _trail.emitting = false;
            _trail.autodestruct = false;
            gameObject.SetActive(false);
        }

        public static string SpriteFor(ArrowTip tip, AbilityKind ability)
        {
            switch (ability)
            {
                case AbilityKind.MeteorArrow: return "arrow_fire";
                case AbilityKind.StormBolt: return "arrow_electric";
                case AbilityKind.ClusterBomb: return "arrow_bomb";
                case AbilityKind.ThornVolley: return "arrow_thorn";
                case AbilityKind.RainOfLeaves: return "arrow_leaf";
            }
            return "arrow_" + tip.ToString().ToLowerInvariant();
        }

        /// <summary>Starts replaying <paramref name="path"/>; <paramref name="visualOffset"/> is bow arrow − Logic launch point.</summary>
        public void Launch(int index, ArrowPath path, ArrowTip tip, string sprite, string trailId, Vector3 visualOffset, bool bigger)
        {
            _index = index;
            _path = path;
            _tip = tip;
            _trailId = trailId ?? "trail_classic";
            _offset = path.Parent < 0 ? visualOffset : Vector3.zero;
            _flying = true;
            _stuck = false;
            _fxTimer = 0f;
            _sr.sprite = ArtLibrary.Get(ArtLibrary.Arrows, sprite) ?? ArtLibrary.Get(ArtLibrary.Arrows, "arrow_normal");
            _sr.sortingOrder = WorldSprites.Flight;
            _sr.color = Color.white;
            transform.localScale = Vector3.one * (bigger ? 1.25f : 1f);
            gameObject.SetActive(false);
            StyleTrail();
            Color g = ElementGlow(tip, sprite);
            _glow.enabled = g.a > 0f;
            _glow.color = g;
            WorldSprites.FitWidth(_glow, 0.55f);
            _trail.Clear();
        }

        static Color ElementGlow(ArrowTip tip, string sprite)
        {
            switch (tip)
            {
                case ArrowTip.Fire: return new Color(1f, 0.55f, 0.15f, 0.7f);
                case ArrowTip.Electric: return new Color(0.25f, 0.88f, 0.95f, 0.7f);
                case ArrowTip.Ice: return new Color(0.7f, 0.92f, 1f, 0.6f);
                case ArrowTip.Poison: return new Color(0.55f, 0.95f, 0.35f, 0.55f);
                case ArrowTip.Bomb: return new Color(1f, 0.85f, 0.3f, 0.45f);
            }
            if (sprite == "arrow_fire") return new Color(1f, 0.55f, 0.15f, 0.7f);
            if (sprite == "arrow_electric") return new Color(0.25f, 0.88f, 0.95f, 0.7f);
            return new Color(0f, 0f, 0f, 0f);
        }

        void StyleTrail()
        {
            var g = new Gradient();
            float width = 0.08f;
            switch (_trailId)
            {
                case "trail_sparkle":
                case "sparkle":
                    g.SetKeys(new[] { new GradientColorKey(new Color(1f, 0.92f, 0.45f), 0f), new GradientColorKey(new Color(1f, 0.82f, 0.25f), 1f) },
                        new[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0f, 1f) });
                    break;
                case "trail_rainbow":
                case "rainbow":
                    g.SetKeys(new[]
                        {
                            new GradientColorKey(new Color(1f, 0.37f, 0.64f), 0f), new GradientColorKey(new Color(1f, 0.82f, 0.25f), 0.3f),
                            new GradientColorKey(new Color(0.18f, 0.83f, 0.63f), 0.6f), new GradientColorKey(new Color(0.22f, 0.74f, 0.97f), 1f)
                        },
                        new[] { new GradientAlphaKey(0.95f, 0f), new GradientAlphaKey(0f, 1f) });
                    width = 0.12f;
                    break;
                case "trail_comet":
                case "comet":
                    g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(0.22f, 0.74f, 0.97f), 1f) },
                        new[] { new GradientAlphaKey(0.95f, 0f), new GradientAlphaKey(0f, 1f) });
                    width = 0.16f;
                    break;
                default:
                    g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                        new[] { new GradientAlphaKey(0.6f, 0f), new GradientAlphaKey(0f, 1f) });
                    break;
            }
            _trail.colorGradient = g;
            _trail.widthCurve = new AnimationCurve(new Keyframe(0f, width), new Keyframe(1f, 0f));
            _trail.time = _trailId.Contains("comet") ? 0.34f : 0.22f;
        }

        /// <summary>Positions the arrow for replay time <paramref name="t"/> (seconds after release).</summary>
        public void SetTime(float t)
        {
            if (!_flying) return;
            if (t < _path.StartTime)
            {
                if (gameObject.activeSelf) gameObject.SetActive(false);
                return;
            }
            double local = System.Math.Min(t, _path.EndTime) - _path.StartTime;
            Vec2 p = Ballistics.PositionAt(_path.StartPosition, _path.StartVelocity, _path.Acceleration, local);
            Vec2 v = Ballistics.VelocityAt(_path.StartVelocity, _path.Acceleration, local);
            float blend = 1f - Mathf.Clamp01((float)local / BlendSeconds);
            Vector3 pos = WorldSprites.V(p) + _offset * blend;
            float angle = Mathf.Atan2((float)v.Y, (float)v.X) * Mathf.Rad2Deg;
            if (!gameObject.activeSelf)
            {
                transform.position = pos;
                gameObject.SetActive(true);
                _trail.Clear();
                _trail.emitting = true;
            }
            transform.position = pos;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
            FlightFx(pos, v);
            if (t >= _path.EndTime) Land(angle);
        }

        void FlightFx(Vector3 pos, Vec2 v)
        {
            FxSystem fx = FxSystem.Instance;
            if (!fx) return;
            _fxTimer -= Time.deltaTime;
            if (_fxTimer > 0f) return;
            _fxTimer = 0.035f;
            var back = new Vector3(-(float)v.X, -(float)v.Y, 0f).normalized;
            Vector3 tail = pos + back * 0.35f;
            if (_glow.enabled) _glow.color = new Color(_glow.color.r, _glow.color.g, _glow.color.b, 0.45f + fx.Rand01() * 0.35f);
            switch (_tip)
            {
                case ArrowTip.Fire:
                    fx.Emit(ArtLibrary.Get(ArtLibrary.Fx, "flame"), pos + back * 0.05f, back * 0.6f + Vector3.up * 0.8f, 0.25f, 0.2f, 0.05f,
                        new Color(1f, 0.8f, 0.3f), new Color(1f, 0.3f, 0.1f, 0f), -1f, 1f, fx.Range(-90f, 90f), WorldSprites.Flight - 1);
                    break;
                case ArrowTip.Electric:
                    fx.Emit(ArtLibrary.Get(ArtLibrary.Fx, "spark"), pos + new Vector3(fx.Range(-0.1f, 0.1f), fx.Range(-0.1f, 0.1f), 0f),
                        new Vector3(fx.Range(-1f, 1f), fx.Range(-1f, 1f), 0f), 0.14f, 0.18f, 0.04f, new Color(0.8f, 1f, 1f), new Color(0.25f, 0.88f, 0.95f, 0f),
                        0f, 0f, 400f, WorldSprites.Flight + 1);
                    break;
                case ArrowTip.Ice:
                    fx.Emit(ArtLibrary.Get(ArtLibrary.Fx, "puff"), tail, back * 0.4f, 0.35f, 0.1f, 0.3f, new Color(0.85f, 0.96f, 1f, 0.55f),
                        new Color(0.85f, 0.96f, 1f, 0f), 0f, 1.2f, 30f, WorldSprites.Flight - 3);
                    break;
                case ArrowTip.Poison:
                    fx.Emit(ArtLibrary.Get(ArtLibrary.Fx, "bubble_small"), tail, new Vector3(fx.Range(-0.3f, 0.3f), 0.6f, 0f), 0.45f, 0.08f, 0.12f,
                        new Color(0.6f, 1f, 0.4f, 0.9f), new Color(0.6f, 1f, 0.4f, 0f), -0.5f, 0.5f, 0f, WorldSprites.Flight - 1);
                    break;
                case ArrowTip.Bomb:
                    fx.Emit(ArtLibrary.Get(ArtLibrary.Fx, "spark"), pos + back * 0.9f, back + Vector3.up * 0.5f, 0.2f, 0.14f, 0.02f,
                        new Color(1f, 0.9f, 0.4f), new Color(1f, 0.4f, 0.1f, 0f), 2f, 0f, 300f, WorldSprites.Flight + 1);
                    break;
            }
            if (_trailId.Contains("sparkle") && fx.Rand01() < 0.6f)
                fx.Emit(ArtLibrary.Get(ArtLibrary.Fx, "star"), tail + new Vector3(fx.Range(-0.08f, 0.08f), fx.Range(-0.08f, 0.08f), 0f), Vector3.down * 0.3f,
                    0.4f, 0.14f, 0.02f, new Color(1f, 0.92f, 0.45f), new Color(1f, 1f, 1f, 0f), 0f, 0f, 200f, WorldSprites.Flight - 1);
            if (_trailId.Contains("comet"))
                fx.Emit(ArtLibrary.Get(ArtLibrary.Fx, "glow"), tail, Vector3.zero, 0.25f, 0.5f, 0.1f, new Color(0.6f, 0.85f, 1f, 0.5f), new Color(0.22f, 0.74f, 0.97f, 0f),
                    0f, 0f, 0f, WorldSprites.Flight - 2);
        }

        void Land(float angle)
        {
            _flying = false;
            _trail.emitting = false;
            _glow.enabled = false;
            switch (_path.Contact)
            {
                case ContactKind.Ground:
                case ContactKind.Wall:
                case ContactKind.Prop:
                    _stuck = true;
                    _stuckTime = 0f;
                    _stuckAngle = angle;
                    // Push the tip a little into what it hit.
                    transform.position += new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad), 0f) * 0.12f;
                    _sr.sortingOrder = WorldSprites.StuckArrows;
                    break;
                default:
                    // Bounces continue as a child path; fighters take the arrow into their rig; the void swallows it.
                    gameObject.SetActive(false);
                    break;
            }
        }

        /// <summary>Stuck arrow rides along with what it is stuck in (crate, board).</summary>
        public void AttachTo(Transform parent)
        {
            if (_stuck && parent) transform.SetParent(parent, true);
        }

        void Update()
        {
            if (!_stuck) return;
            _stuckTime += Time.deltaTime;
            if (_stuckTime < 0.35f)
            {
                float q = Mathf.Sin(_stuckTime * 60f) * 7f * (1f - _stuckTime / 0.35f);
                transform.rotation = Quaternion.Euler(0f, 0f, _stuckAngle + q);
            }
        }

        public void Recycle(Transform pool)
        {
            _flying = false;
            _stuck = false;
            _trail.emitting = false;
            _trail.Clear();
            transform.SetParent(pool, false);
            gameObject.SetActive(false);
        }
    }
}
