using ArcherArcade.Core;
using ArcherArcade.Logic;
using ArcherArcade.UI;
using UnityEngine;

namespace ArcherArcade.Arena
{
    /// <summary>
    /// One arena prop on screen (GAME_DESIGN §10), drawn from the props atlas and fitted to its Logic shape: walls,
    /// crates (cracked after one hit), TNT with a sparking fuse, barrels, bounce pads that squash, targets on posts
    /// or balloons, the apple dummy, the rope with the caged fox, shields (fall flat when knocked down), floating
    /// platforms with propellers and the boss's vine wall. It only shows state: breaking, flying off and settling
    /// are animations started by the match replay; <see cref="Sync"/> snaps or eases to the Logic state.
    /// </summary>
    public sealed class PropView : MonoBehaviour
    {
        MatchState _m;
        int _index;
        PropKind _kind;
        int _order;
        SpriteRenderer _main, _extra, _extra2, _line;
        Transform _body;
        bool _shown = true, _flying, _down;
        Vector3 _vel;
        float _spin, _flyTime;
        float _wobble, _squash, _grow = 1f, _t;
        Vector3 _from, _to;
        float _moveT = 1f, _moveDur = 0.35f;
        bool _moveBounce;
        float _fuse;
        float _cageFall = -1f, _cageGround, _foxHop = -1f;
        Vector3 _cageVel;
        float _downAngle;
        int _facing = 1;

        public int Index => _index;
        public PropKind Kind => _kind;
        public Transform Body => _body;

        /// <summary>Standing on screen (not broken, flying off or hidden).</summary>
        public bool Shown => _shown && !_flying && gameObject.activeSelf;

        /// <summary>Where effects for this prop go (centre of its current shape).</summary>
        public Vector3 Center => _body ? _body.position : transform.position;

        public static PropView Create(Transform parent, MatchState m, int index)
        {
            var go = new GameObject("Prop" + index + "_" + m.GetProp(index).Kind);
            go.transform.SetParent(parent, false);
            var v = go.AddComponent<PropView>();
            v.Build(m, index);
            return v;
        }

        Shape RestShape => _m.PropRestShape(_index);

        void Build(MatchState m, int index)
        {
            _m = m;
            _index = index;
            Prop p = m.GetProp(index);
            _kind = p.Kind;
            _order = WorldSprites.Props + index * 4;
            _body = new GameObject("Body").transform;
            _body.SetParent(transform, false);
            Shape s = RestShape;
            float w = (float)s.HalfSize.X * 2f, h = (float)s.HalfSize.Y * 2f, r = (float)s.Radius;
            switch (_kind)
            {
                case PropKind.Wall:
                    _main = WorldSprites.Sliced(_body, ArtLibrary.Props, h > 4f ? "stone_wall" : "wall", new Vector2(w + 0.06f, h + 0.08f), _order);
                    break;
                case PropKind.Crate:
                    _main = WorldSprites.Make(_body, ArtLibrary.Props, "crate", _order);
                    WorldSprites.FitWidth(_main, w * 1.08f);
                    break;
                case PropKind.TntCrate:
                    _main = WorldSprites.Make(_body, ArtLibrary.Props, "tnt", _order);
                    WorldSprites.FitWidth(_main, w * 1.08f);
                    break;
                case PropKind.ExplosiveBarrel:
                    _main = WorldSprites.Make(_body, ArtLibrary.Props, "barrel", _order);
                    WorldSprites.FitHeight(_main, r * 2.3f);
                    break;
                case PropKind.BouncePad:
                    if (h > w)
                    {
                        _main = WorldSprites.Sliced(_body, ArtLibrary.Props, "pad", new Vector2(h + 0.1f, Mathf.Max(0.36f, w + 0.16f)), _order);
                        _main.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                    }
                    else
                    {
                        _main = WorldSprites.Sliced(_body, ArtLibrary.Props, "pad", new Vector2(w + 0.1f, Mathf.Max(0.36f, h + 0.16f)), _order);
                    }
                    break;
                case PropKind.Target:
                    BuildTarget(p, r);
                    break;
                case PropKind.Apple:
                    _main = WorldSprites.Make(_body, ArtLibrary.Props, "apple", _order + 2);
                    WorldSprites.FitWidth(_main, r * 2.6f);
                    break;
                case PropKind.Dummy:
                    _main = WorldSprites.Make(_body, ArtLibrary.Props, "dummy", _order);
                    WorldSprites.FitHeight(_main, h * 1.06f);
                    _main.transform.localPosition = new Vector3(0f, -h * 0.5f, 0f);
                    break;
                case PropKind.Rope:
                    _main = WorldSprites.Tiled(_body, ArtLibrary.Props, "rope", new Vector2(0.16f, h), _order);
                    _extra = WorldSprites.Make(transform, ArtLibrary.Props, "cage", _order + 2);
                    WorldSprites.FitHeight(_extra, 1.3f);
                    _extra2 = WorldSprites.Make(transform, ArtLibrary.Props, "fox", _order + 1);
                    WorldSprites.FitHeight(_extra2, 0.78f);
                    break;
                case PropKind.Shield:
                    bool boss = p.Spec.RotatingSlot >= 0;
                    _main = WorldSprites.Make(_body, ArtLibrary.Props, boss ? "shield_boss" : "shield_tall", _order);
                    WorldSprites.FitHeight(_main, h * 1.08f);
                    if (p.Spec.ShieldOwner >= 0) _facing = m.GetFighter(p.Spec.ShieldOwner).Facing;
                    _main.flipX = _facing < 0;
                    _order = WorldSprites.Archers + 150 + index;
                    _main.sortingOrder = _order;
                    break;
                case PropKind.Platform:
                    _main = WorldSprites.Sliced(_body, ArtLibrary.Props, "platform", new Vector2(w + 0.12f, Mathf.Max(0.42f, h + 0.08f)), _order);
                    _extra = WorldSprites.Make(_body, ArtLibrary.Props, "propeller", _order - 1);
                    WorldSprites.FitWidth(_extra, 0.9f);
                    _extra.transform.localPosition = new Vector3(0f, -h * 0.5f - 0.2f, 0f);
                    _line = WorldSprites.Make(_body, "Mast", ShapeSprites.White, _order - 2);
                    _line.color = new Color32(0x6B, 0x4A, 0x2B, 0xFF);
                    _line.transform.localPosition = new Vector3(0f, -h * 0.5f - 0.1f, 0f);
                    _line.transform.localScale = new Vector3(0.06f / Mathf.Max(0.01f, _line.sprite.bounds.size.x), 0.22f / Mathf.Max(0.01f, _line.sprite.bounds.size.y), 1f);
                    break;
                case PropKind.VineWall:
                    _main = WorldSprites.Sliced(_body, ArtLibrary.Props, "vine_wall", new Vector2(w * 1.6f, h), _order);
                    break;
            }
            Sync(false, m.Clock);
        }

        void BuildTarget(Prop p, float r)
        {
            _main = WorldSprites.Make(_body, ArtLibrary.Props, "target", _order + 2);
            WorldSprites.FitWidth(_main, r * 2.15f);
            PropMotion motion = p.Spec.Motion;
            Shape s = RestShape;
            if (motion.Kind == MotionKind.PingPong)
            {
                _extra = WorldSprites.Make(_body, ArtLibrary.Props, "balloon", _order + 1);
                WorldSprites.FitHeight(_extra, 0.95f);
                _extra.transform.localPosition = new Vector3(0f, r + 1.05f, 0f);
                _line = WorldSprites.Make(_body, "String", ShapeSprites.White, _order);
                _line.color = new Color(1f, 1f, 1f, 0.85f);
                SetLine(_line, new Vector3(0f, r, 0f), new Vector3(0f, r + 0.62f, 0f), 0.025f);
            }
            else if (motion.Kind == MotionKind.Swing)
            {
                _line = WorldSprites.Tiled(transform, ArtLibrary.Props, "rope", new Vector2(0.1f, 1f), _order);
            }
            else
            {
                // Post down to the ground below (or a balloon when it floats over the void).
                float ground = GroundBelow((float)s.A.X, (float)s.A.Y - r);
                if (ground > -50f)
                {
                    float len = (float)s.A.Y - ground;
                    _line = WorldSprites.Sliced(transform, ArtLibrary.Props, "target_post", new Vector2(0.2f, len), _order);
                    WorldSprites.StandOn(_line, new Vector3((float)s.A.X, ground, 0f), len);
                    _extra2 = WorldSprites.Make(transform, ArtLibrary.Props, "target_stand", _order + 1);
                    WorldSprites.FitWidth(_extra2, 0.95f);
                    _extra2.transform.position = new Vector3((float)s.A.X, ground + 0.04f, 0f);
                }
                else
                {
                    _extra = WorldSprites.Make(_body, ArtLibrary.Props, "balloon", _order + 1);
                    WorldSprites.FitHeight(_extra, 0.95f);
                    _extra.transform.localPosition = new Vector3(0f, r + 1.05f, 0f);
                }
            }
        }

        float GroundBelow(float x, float y)
        {
            ArenaLayout a = _m.Setup.Arena;
            float best = -99f;
            for (int i = 0; i < a.Grounds.Count; i++)
            {
                Shape g = a.Grounds[i];
                float top = (float)(g.A.Y + g.HalfSize.Y);
                if (Mathf.Abs(x - (float)g.A.X) <= (float)g.HalfSize.X && top <= y + 0.01f && top > best) best = top;
            }
            return best;
        }

        static void SetLine(SpriteRenderer sr, Vector3 a, Vector3 b, float width)
        {
            Vector3 d = b - a;
            float len = d.magnitude;
            Vector3 size = sr.sprite ? sr.sprite.bounds.size : Vector3.one;
            sr.transform.localPosition = (a + b) * 0.5f;
            sr.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
            sr.transform.localScale = new Vector3(len / Mathf.Max(0.001f, size.x), width / Mathf.Max(0.001f, size.y), 1f);
        }

        // ---------- state ----------

        /// <summary>Shows the Logic state at match clock <paramref name="clock"/>; <paramref name="animate"/> eases moves (settling towers, platforms).</summary>
        public void Sync(bool animate, double clock)
        {
            Prop p = _m.GetProp(_index);
            bool present = p.Alive;
            if (_kind == PropKind.Shield)
            {
                int owner = p.Spec.ShieldOwner;
                Fighter f = owner >= 0 ? _m.GetFighter(owner) : null;
                present = p.Alive && f != null && f.IsAlive && _m.ActiveFighter(f.Side) == owner;
                _down = p.ShieldDown;
            }
            if (_flying || _cageFall >= 0f) return;
            if (_kind == PropKind.Rope)
            {
                ShowRope(p.Alive);
                return;
            }
            if (present != _shown)
            {
                _shown = present;
                gameObject.SetActive(present);
                if (present && _kind == PropKind.VineWall && animate) _grow = 0f;
            }
            if (!present) return;
            if (_kind == PropKind.Crate && p.HitsLeft == 1 && p.Spec.Tower < 0) SetCracked();
            Vector3 target = ShapeCenter(clock);
            if (animate && (target - _body.position).sqrMagnitude > 0.0004f)
            {
                _from = _body.position;
                _to = target;
                _moveT = 0f;
                _moveBounce = _kind == PropKind.Crate || _kind == PropKind.TntCrate;
                _moveDur = _kind == PropKind.Platform ? 0.7f : 0.35f;
            }
            else if (_moveT >= 1f)
            {
                _body.position = target;
            }
        }

        Vector3 ShapeCenter(double clock)
        {
            Shape s = _m.PropShapeAt(_index, clock);
            return WorldSprites.V(s.Center);
        }

        void SetCracked()
        {
            Sprite cracked = ArtLibrary.Get(ArtLibrary.Props, "crate_cracked");
            if (cracked && _main.sprite != cracked) _main.sprite = cracked;
        }

        void ShowRope(bool intact)
        {
            Shape s = RestShape;
            float top = (float)(s.A.Y + s.HalfSize.Y), bottom = (float)(s.A.Y - s.HalfSize.Y);
            _body.position = new Vector3((float)s.A.X, (top + bottom) * 0.5f, 0f);
            _main.size = new Vector2(0.16f, top - bottom);
            if (!intact) return;
            if (_extra)
            {
                Vector3 b = _extra.sprite ? _extra.sprite.bounds.max : Vector3.zero;
                _extra.transform.position = new Vector3((float)s.A.X, bottom - b.y * _extra.transform.localScale.y, 0f);
            }
            PlaceFoxInCage();
        }

        // ---------- reactions (called by the replay) ----------

        /// <summary>Small knock: wobble (targets, walls, dummy, shields, pads squash).</summary>
        public void Hit()
        {
            _wobble = 1f;
            if (_kind == PropKind.BouncePad) _squash = 1f;
            if (_kind == PropKind.Crate && _m.GetProp(_index).HitsLeft == 1) SetCracked();
        }

        /// <summary>Flies off spinning (crate knocked off a tower, toppled tower, target popped, apple).</summary>
        public void FlyOff(Vector2 push, float up = 3f)
        {
            if (_flying) return;
            _flying = true;
            _flyTime = 0f;
            _vel = new Vector3(push.x, push.y + up, 0f);
            _spin = (push.x >= 0f ? -1f : 1f) * (240f + Mathf.Abs(push.x) * 40f);
            if (_kind == PropKind.Target)
            {
                Sprite half = ArtLibrary.Get(ArtLibrary.Props, "target_half");
                if (half) _main.sprite = half;
                if (_extra) _extra.enabled = false;
                if (_line) _line.enabled = false;
            }
            if (_kind == PropKind.Apple)
            {
                Sprite half = ArtLibrary.Get(ArtLibrary.Props, "apple_half");
                if (half) _main.sprite = half;
            }
        }

        /// <summary>Gone at once (crate smashed, barrel / TNT exploded, vine burned); the replay plays the effect.</summary>
        public void Vanish()
        {
            _shown = false;
            gameObject.SetActive(false);
        }

        /// <summary>Rope cut: the cage drops to the ground and opens, the fox cheers.</summary>
        public void CutRope()
        {
            if (_kind != PropKind.Rope || _cageFall >= 0f) return;
            Shape s = RestShape;
            float bottom = (float)(s.A.Y - s.HalfSize.Y);
            _cageGround = GroundBelow((float)s.A.X, bottom);
            _cageFall = 0f;
            _cageVel = Vector3.zero;
            _main.size = new Vector2(0.16f, (float)s.HalfSize.Y);
            _main.transform.localPosition = new Vector3(0f, (float)s.HalfSize.Y * 0.5f, 0f);
        }

        public void KnockDown() => _down = true;

        void Update()
        {
            float dt = Time.deltaTime;
            _t += dt;
            if (_flying)
            {
                _flyTime += dt;
                _vel.y -= 18f * dt;
                _body.position += _vel * dt;
                _body.rotation = Quaternion.Euler(0f, 0f, _body.eulerAngles.z + _spin * dt);
                if (_main)
                {
                    Color c = _main.color;
                    c.a = Mathf.Clamp01(1.6f - _flyTime);
                    _main.color = c;
                }
                if (_flyTime > 1.6f)
                {
                    _flying = false;
                    Vanish();
                }
                return;
            }
            if (_moveT < 1f)
            {
                _moveT = Mathf.Min(1f, _moveT + dt / _moveDur);
                float k = _moveBounce ? Tweening.Ease.Evaluate(Tweening.EaseType.OutBounce, _moveT) : Tweening.Ease.Evaluate(Tweening.EaseType.InOutQuad, _moveT);
                _body.position = Vector3.LerpUnclamped(_from, _to, k);
            }
            if (_cageFall >= 0f && _extra)
            {
                _cageFall += dt;
                Vector3 pos = _extra.transform.position;
                float floor = _cageGround > -50f ? _cageGround + 0.05f - _extra.sprite.bounds.min.y * _extra.transform.localScale.y : -40f;
                if (pos.y > floor)
                {
                    _cageVel.y -= 18f * dt;
                    pos += _cageVel * dt;
                    if (pos.y <= floor)
                    {
                        pos.y = floor;
                        OpenCage();
                    }
                    _extra.transform.position = pos;
                    if (_foxHop < 0f) PlaceFoxInCage();
                }
            }
            if (_foxHop >= 0f && _extra2)
            {
                _foxHop += dt;
                float hop = Mathf.Abs(Mathf.Sin(_foxHop * 6f)) * 0.35f * Mathf.Clamp01(3f - _foxHop * 0.5f);
                Vector3 basePos = new Vector3(_extra.transform.position.x + Mathf.Min(1.1f, _foxHop * 0.8f), _cageGround + 0.02f, 0f);
                _extra2.transform.position = basePos + new Vector3(0f, hop - _extra2.sprite.bounds.min.y * _extra2.transform.localScale.y, 0f);
            }
            if (_kind == PropKind.Shield)
            {
                float target = _down ? 84f : 0f;
                _downAngle = Mathf.MoveTowards(_downAngle, target, 420f * dt);
                _main.transform.localRotation = Quaternion.Euler(0f, 0f, -_downAngle * _facing);
                _main.transform.localPosition = new Vector3(0f, -_downAngle / 84f * 0.6f, 0f);
            }
            if (_kind == PropKind.Platform && _extra)
            {
                float spin = Mathf.Sin(_t * 40f);
                _extra.transform.localScale = new Vector3(Mathf.Abs(_extra.transform.localScale.y) * (0.25f + 0.75f * Mathf.Abs(spin)), _extra.transform.localScale.y, 1f);
                _body.position += new Vector3(0f, Mathf.Sin(_t * 2.2f) * 0.0006f, 0f);
            }
            if (_kind == PropKind.TntCrate)
            {
                _fuse -= dt;
                if (_fuse <= 0f)
                {
                    Feel.FxSystem fx = Feel.FxSystem.Instance;
                    _fuse = fx ? 0.12f + fx.Rand01() * 0.2f : 0.3f;
                    Vector3 top = _body.position + new Vector3(0.12f, (float)RestShape.HalfSize.Y + 0.22f, 0f);
                    if (fx)
                        fx.Emit(ArtLibrary.Get(ArtLibrary.Fx, "spark"), top, new Vector3(fx.Range(-0.6f, 0.6f), 1.2f, 0f), 0.3f, 0.16f, 0.02f,
                            new Color(1f, 0.9f, 0.4f), new Color(1f, 0.4f, 0.1f, 0f), 3f, 0f, 200f, _order + 3);
                }
            }
            if (_kind == PropKind.VineWall && _grow < 1f)
            {
                _grow = Mathf.Min(1f, _grow + dt / 0.55f);
                float k = Tweening.Ease.Evaluate(Tweening.EaseType.OutBack, _grow);
                _main.transform.localScale = new Vector3(1f, k, 1f);
                _main.transform.localPosition = new Vector3(0f, (k - 1f) * (float)RestShape.HalfSize.Y, 0f);
            }
            if (_kind == PropKind.Target && _m.GetProp(_index).Spec.Motion.Kind == MotionKind.Swing && _line)
            {
                PropMotion mo = _m.GetProp(_index).Spec.Motion;
                Vector3 pivot = WorldSprites.V(mo.Pivot);
                Vector3 c = _body.position;
                Vector3 d = pivot - c;
                float r = (float)RestShape.Radius;
                Vector3 start = c + d.normalized * r;
                _line.transform.position = (start + pivot) * 0.5f;
                _line.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg - 90f);
                _line.size = new Vector2(0.1f, (pivot - start).magnitude);
                _main.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg - 90f);
            }
            if (_extra && _kind == PropKind.Target && _m.GetProp(_index).Spec.Motion.Kind != MotionKind.Swing)
                _extra.transform.localPosition = new Vector3(Mathf.Sin(_t * 1.7f + _index) * 0.05f, _extra.transform.localPosition.y, 0f);

            float wob = 0f;
            if (_wobble > 0f)
            {
                _wobble = Mathf.Max(0f, _wobble - dt * 2.4f);
                wob = Mathf.Sin(_t * 38f) * 9f * _wobble;
            }
            if (_kind == PropKind.Target || _kind == PropKind.Dummy || _kind == PropKind.Wall || _kind == PropKind.Crate || _kind == PropKind.TntCrate ||
                _kind == PropKind.ExplosiveBarrel || _kind == PropKind.VineWall)
            {
                if (!(_kind == PropKind.Target && _m.GetProp(_index).Spec.Motion.Kind == MotionKind.Swing))
                    _main.transform.localRotation = Quaternion.Euler(0f, 0f, wob * (_kind == PropKind.Wall ? 0.3f : 1f));
            }
            if (_squash > 0f)
            {
                _squash = Mathf.Max(0f, _squash - dt * 3f);
                float sq = Mathf.Sin(_squash * Mathf.PI * 3f) * 0.25f * _squash;
                _body.localScale = new Vector3(1f + sq, 1f - sq, 1f);
            }
        }

        /// <summary>The fox sits on the cage floor.</summary>
        void PlaceFoxInCage()
        {
            if (!_extra2 || !_extra || !_extra.sprite || !_extra2.sprite) return;
            float cageBottom = _extra.transform.position.y + _extra.sprite.bounds.min.y * _extra.transform.localScale.y;
            float foxBottom = _extra2.sprite.bounds.min.y * _extra2.transform.localScale.y;
            _extra2.transform.position = new Vector3(_extra.transform.position.x - 0.03f, cageBottom + 0.1f - foxBottom, 0f);
        }

        void OpenCage()
        {
            Feel.FxSystem fx = Feel.FxSystem.Instance;
            Vector3 at = _extra.transform.position;
            if (fx)
            {
                fx.Smoke(at, 5, 0.9f, new Color(1f, 1f, 1f, 0.8f));
                fx.Burst(ArtLibrary.Get(ArtLibrary.Fx, "heart"), at + Vector3.up * 0.6f, 6, 3f, 1.1f,
                    new[] { new Color(1f, 0.37f, 0.64f), new Color(1f, 0.55f, 0.75f) }, 0.28f, 2f, 2.5f);
            }
            _extra.enabled = false;
            if (_extra2)
            {
                Sprite happy = ArtLibrary.Get(ArtLibrary.Props, "fox_happy");
                if (happy) _extra2.sprite = happy;
                _foxHop = 0f;
            }
        }

        /// <summary>Moving target / swing position during aiming and flight.</summary>
        public void TrackClock(double clock)
        {
            if (!_shown || _flying || _moveT < 1f) return;
            if (_kind != PropKind.Target) return;
            if (_m.GetProp(_index).Spec.Motion.Kind == MotionKind.None) return;
            _body.position = ShapeCenter(clock);
        }
    }
}
