using System.Collections.Generic;
using ArcherArcade.Core;
using ArcherArcade.Logic;
using UnityEngine;

namespace ArcherArcade.Archers
{
    /// <summary>
    /// A 2.5D archer on screen: the design roster's part sprites (baked volume shading) assembled every frame by
    /// <see cref="RigSolver"/>, with depth-tinted back limbs and a soft ground shadow. Animation blends pose
    /// parameters (never detaching limbs): idle breathing, gameplay aim + draw, release, hit, victory, knockout.
    /// Injury looks and element marks from GAME_DESIGN §3.3.2 are overlay sprites on the head / torso.
    /// </summary>
    public sealed class ArcherView : MonoBehaviour
    {
        enum Mode { Idle, Aim, Release, Hit, Victory, KnockedOut }

        const float ArrowTip = 122f; // design arrow: nock → tip, units
        static readonly Color BackTint = new Color(0.8f, 0.8f, 0.86f, 1f);
        static readonly RigPart[] OrderNormal =
        {
            RigPart.UpperArmBack, RigPart.ForearmBack, RigPart.HandBack, RigPart.UpperLegBack, RigPart.LowerLegBack, RigPart.FootBack,
            RigPart.Cape, RigPart.Quiver, RigPart.Torso, RigPart.UpperLegFront, RigPart.LowerLegFront, RigPart.FootFront,
            RigPart.Head, RigPart.Gear, RigPart.Bow, RigPart.UpperArmFront, RigPart.ForearmFront, RigPart.HandFront
        };
        static readonly RigPart[] OrderFront =
        {
            RigPart.UpperLegBack, RigPart.LowerLegBack, RigPart.FootBack, RigPart.Cape, RigPart.Quiver, RigPart.Torso,
            RigPart.UpperLegFront, RigPart.LowerLegFront, RigPart.FootFront, RigPart.UpperArmBack, RigPart.ForearmBack,
            RigPart.HandBack, RigPart.Head, RigPart.Gear, RigPart.Bow, RigPart.UpperArmFront, RigPart.ForearmFront, RigPart.HandFront
        };

        RigAsset _rig;
        Transform _root, _body;
        readonly SpriteRenderer[] _parts = new SpriteRenderer[RigFrame.PartCount];
        readonly SpriteRenderer[] _string = new SpriteRenderer[6];
        SpriteRenderer _arrow, _shadow;
        readonly RigFrame _frame = new RigFrame(), _fa = new RigFrame(), _fb = new RigFrame();
        readonly RigPose _pose = new RigPose(), _from = new RigPose(), _target = new RigPose();
        float _blend = 1f, _blendSpeed = 8f;
        Mode _mode = Mode.Idle;
        float _modeTime, _t;
        int _facing = 1;
        int _baseOrder;
        bool _lastFront;
        RigExpression? _forceExpression;
        float _koAngle;
        float _drawSlow = 1f;

        // Looks (injury, marks, statuses).
        InjuryStage _injury;
        ElementMarks _marks;
        bool _burning, _poisoned, _bubble, _helmet;
        float _frozenUntil;
        readonly List<SpriteRenderer> _overlays = new List<SpriteRenderer>();
        SpriteRenderer _bandaid, _bandage, _sweat, _helmetSr, _bubbleSr, _cloud, _ice;
        readonly SpriteRenderer[] _stars = new SpriteRenderer[3];
        readonly SpriteRenderer[] _flames = new SpriteRenderer[3];
        readonly SpriteRenderer[] _zzz = new SpriteRenderer[3];
        readonly List<SpriteRenderer> _marksSr = new List<SpriteRenderer>();
        readonly List<GameObject> _stuck = new List<GameObject>();

        public RigAsset Rig => _rig;
        public int Facing => _facing;
        public bool IsKnockedOut => _mode == Mode.KnockedOut;
        /// <summary>Metres per design unit for this look.</summary>
        public float M => _rig != null ? _rig.Meters : 1f / 140f;

        public static ArcherView Create(Transform parent, string look, int facing, int sortingOrder)
        {
            var go = new GameObject("Archer_" + look);
            go.transform.SetParent(parent, false);
            var v = go.AddComponent<ArcherView>();
            v.Build(ArtLibrary.Rig(look), facing, sortingOrder);
            return v;
        }

        void Build(RigAsset rig, int facing, int sortingOrder)
        {
            _rig = rig;
            _baseOrder = sortingOrder;
            _root = new GameObject("Root").transform;
            _root.SetParent(transform, false);
            _body = new GameObject("Body").transform;
            _body.SetParent(_root, false);
            _shadow = NewRenderer(transform, "Shadow", ArtLibrary.Get(ArtLibrary.Fx, "shadow"), sortingOrder - 1);
            FitWidth(_shadow, 1.1f * (rig != null ? rig.Scale : 1f));
            for (int i = 0; i < RigFrame.PartCount; i++)
            {
                _parts[i] = NewRenderer(_body, ((RigPart)i).ToString(), null, sortingOrder);
                if (i <= (int)RigPart.FootBack) _parts[i].color = BackTint;
            }
            for (int i = 0; i < _string.Length; i++) _string[i] = NewRenderer(_body, "String" + i, UI.ShapeSprites.White, sortingOrder);
            _arrow = NewRenderer(_body, "Arrow", ArtLibrary.Get(ArtLibrary.Arrows, "arrow_normal"), sortingOrder);
            SetFacing(facing);
            if (rig == null) return;
            AssignSprites(RigExpression.Normal);
            Color core = rig.StringColor, ink = new Color32(0x2A, 0x1B, 0x30, 0xFF);
            _string[0].color = _string[1].color = rig.StringGlow ? new Color(core.r, core.g, core.b, 0.3f) : Color.clear;
            _string[2].color = _string[3].color = ink;
            _string[4].color = _string[5].color = core;
            if (rig.Crossbow) for (int i = 0; i < _string.Length; i++) _string[i].enabled = false;
            _target.CopyFrom(RigPoses.Idle());
            _pose.CopyFrom(_target);
            _from.CopyFrom(_target);
            BuildOverlays();
            Solve();
            ApplyOrder();
        }

        SpriteRenderer NewRenderer(Transform parent, string name, Sprite sprite, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            return sr;
        }

        static void FitWidth(SpriteRenderer sr, float meters)
        {
            if (!sr || !sr.sprite) return;
            float w = sr.sprite.bounds.size.x;
            if (w > 0f) sr.transform.localScale = Vector3.one * (meters / w);
        }

        void AssignSprites(RigExpression expression)
        {
            if (_rig == null) return;
            string[] names =
            {
                "upper_arm", "forearm", "hand", "upper_leg", "lower_leg", "foot", "cape", "quiver", "torso",
                "upper_leg", "lower_leg", "foot", null, "gear", "bow", "upper_arm", "forearm", "hand"
            };
            for (int i = 0; i < names.Length; i++)
            {
                if (names[i] == null) continue;
                _parts[i].sprite = _rig.Part(names[i]);
            }
            _parts[(int)RigPart.Cape].enabled = _rig.HasCape;
            SetHead(expression);
        }

        RigExpression _headExpr = (RigExpression)(-1);

        void SetHead(RigExpression e)
        {
            if (e == _headExpr || _rig == null) return;
            _headExpr = e;
            _parts[(int)RigPart.Head].sprite = _rig.Part("head_" + e.ToString().ToLowerInvariant()) ?? _rig.Part("head_normal");
        }

        public void SetFacing(int facing)
        {
            _facing = facing >= 0 ? 1 : -1;
            if (_body) _body.localScale = new Vector3(_facing, 1f, 1f);
        }

        /// <summary>Sorting base for this archer's renderers (40 slots are used).</summary>
        public void SetSortingBase(int order)
        {
            _baseOrder = order;
            ApplyOrder();
        }

        public void SetArrowTip(ArrowTip tip)
        {
            string id = "arrow_" + tip.ToString().ToLowerInvariant();
            Sprite s = ArtLibrary.Get(ArtLibrary.Arrows, id) ?? ArtLibrary.Get(ArtLibrary.Arrows, "arrow_normal");
            _arrow.sprite = s;
        }

        // ---------- animation API ----------

        void Go(Mode mode, RigPose pose, float blendSeconds)
        {
            _mode = mode;
            _modeTime = 0f;
            _from.CopyFrom(_pose);
            _target.CopyFrom(pose);
            _blend = blendSeconds <= 0f ? 1f : 0f;
            _blendSpeed = blendSeconds <= 0f ? 1000f : 1f / blendSeconds;
        }

        public void Idle(float blend = 0.3f)
        {
            if (_mode == Mode.KnockedOut) return;
            Go(Mode.Idle, RigPoses.Idle(), blend);
        }

        /// <summary>Aim at an angle (degrees, up positive) with draw 0..1; call every frame while aiming.</summary>
        public void Aim(float angleDeg, float draw)
        {
            if (_mode == Mode.KnockedOut) return;
            if (_mode != Mode.Aim) Go(Mode.Aim, _target, 0.18f);
            RigPoses.Aim(_target, angleDeg, draw * _drawSlow + (1f - _drawSlow) * draw * draw);
        }

        /// <summary>Ice tip: the next draw animates slower (visual only; GAME_DESIGN §3.3.1).</summary>
        public void SetDrawSlow(float slow) => _drawSlow = Mathf.Clamp01(1f - slow);

        public void Release()
        {
            if (_mode == Mode.KnockedOut) return;
            RigPose p = RigPoses.Release();
            Go(Mode.Release, p, 0.06f);
        }

        public void Hit()
        {
            if (_mode == Mode.KnockedOut) return;
            Go(Mode.Hit, RigPoses.Hit(), 0.07f);
        }

        public void Victory()
        {
            if (_mode == Mode.KnockedOut) return;
            Go(Mode.Victory, RigPoses.Victory(), 0.25f);
        }

        public void KnockOut()
        {
            if (_mode == Mode.KnockedOut) return;
            Go(Mode.KnockedOut, RigPoses.Hit(), 0.08f);
            _koAngle = 0f;
            ClearStuckArrows();
        }

        /// <summary>Back on its feet (new round / rematch).</summary>
        public void Revive()
        {
            _mode = Mode.Idle;
            _koAngle = 0f;
            _root.localRotation = Quaternion.identity;
            _root.localPosition = Vector3.zero;
            for (int i = 0; i < _zzz.Length; i++) if (_zzz[i]) _zzz[i].enabled = false;
            Go(Mode.Idle, RigPoses.Idle(), 0.2f);
        }

        public void ForceExpression(RigExpression? e) => _forceExpression = e;

        // ---------- looks ----------

        public void SetLooks(InjuryStage injury, ElementMarks marks, bool burning, bool poisoned, bool bubble, bool helmet)
        {
            _injury = injury;
            _burning = burning;
            _poisoned = poisoned;
            _bubble = bubble;
            _helmet = helmet;
            if (marks != _marks)
            {
                _marks = marks;
                RebuildMarks();
            }
            if (_bandaid) _bandaid.enabled = injury >= InjuryStage.Scratched && injury < InjuryStage.Dizzy;
            if (_bandage) _bandage.enabled = injury >= InjuryStage.Dizzy && injury < InjuryStage.KnockedOut;
            Color tint = poisoned || (marks & ElementMarks.Poisoned) != 0 ? new Color(0.82f, 1f, 0.74f, 1f) : Color.white;
            tint *= _ambient;
            for (int i = 0; i < _parts.Length; i++) _parts[i].color = i <= (int)RigPart.FootBack ? BackTint * tint : tint;
        }

        public void Freeze(float seconds) => _frozenUntil = _t + seconds;

        Color _ambient = Color.white;

        /// <summary>Scene light (dusk / night tint) multiplied into the body parts.</summary>
        public void SetAmbient(Color c)
        {
            _ambient = c;
            SetLooks(_injury, _marks, _burning, _poisoned, _bubble, _helmet);
        }

        void BuildOverlays()
        {
            Transform head = _parts[(int)RigPart.Head].transform;
            Transform torso = _parts[(int)RigPart.Torso].transform;
            _bandaid = Overlay(head, "bandaid", new Vector2(40f, -34f), 0.2f, -24f, 1);
            _bandage = Overlay(head, "bandage", new Vector2(6f, -92f), 0.86f, 6f, 2);
            _sweat = Overlay(head, "sweat", new Vector2(70f, -96f), 0.09f, 0f, 2);
            _helmetSr = Overlay(head, "helmet", new Vector2(6f, -112f), 0.8f, 0f, 3);
            _bubbleSr = Overlay(_root, "bubble", Vector2.zero, 2.3f, 0f, 30);
            _cloud = Overlay(torso, "poison_cloud", new Vector2(0f, -60f), 1.2f, 0f, 20);
            _ice = Overlay(_root, "ice_block", Vector2.zero, 1.2f, 0f, 31);
            for (int i = 0; i < 3; i++)
            {
                _stars[i] = Overlay(head, "star", new Vector2(6f, -130f), 0.16f, 0f, 3);
                _flames[i] = Overlay(i == 0 ? head : torso, "flame", i == 0 ? new Vector2(-10f, -110f) : new Vector2(i == 1 ? -20f : 18f, -70f + i * 10f), 0.32f, 0f, 3);
                _zzz[i] = Overlay(_root, "zzz", Vector2.zero, 0.2f, 0f, 32);
            }
            foreach (SpriteRenderer sr in _overlays) sr.enabled = false;
        }

        SpriteRenderer Overlay(Transform parent, string sprite, Vector2 units, float meters, float rot, int orderOffset)
        {
            SpriteRenderer sr = NewRenderer(parent, sprite, ArtLibrary.Get(ArtLibrary.Fx, sprite), 0);
            sr.transform.localPosition = new Vector3(units.x * M, -units.y * M, 0f);
            sr.transform.localRotation = Quaternion.Euler(0f, 0f, rot);
            FitWidth(sr, meters * (_rig != null ? _rig.Scale : 1f));
            sr.gameObject.AddComponent<OverlayOrder>().Offset = orderOffset;
            _overlays.Add(sr);
            return sr;
        }

        void RebuildMarks()
        {
            foreach (SpriteRenderer sr in _marksSr) if (sr) Destroy(sr.gameObject);
            _marksSr.Clear();
            Transform head = _parts[(int)RigPart.Head].transform;
            Transform torso = _parts[(int)RigPart.Torso].transform;
            if ((_marks & ElementMarks.Soot) != 0)
            {
                _marksSr.Add(Overlay(head, "soot", new Vector2(30f, -40f), 0.3f, 0f, 1));
                _marksSr.Add(Overlay(torso, "soot", new Vector2(4f, -50f), 0.36f, 20f, 1));
            }
            if ((_marks & ElementMarks.Frost) != 0)
            {
                _marksSr.Add(Overlay(head, "frost", new Vector2(-14f, -96f), 0.24f, 0f, 1));
                _marksSr.Add(Overlay(torso, "frost", new Vector2(12f, -30f), 0.26f, 30f, 1));
            }
            if ((_marks & ElementMarks.Bruised) != 0)
            {
                _marksSr.Add(Overlay(torso, "bruise", new Vector2(16f, -56f), 0.26f, 0f, 1));
                _marksSr.Add(Overlay(head, "bruise", new Vector2(30f, -74f), 0.2f, 0f, 1));
            }
            if ((_marks & ElementMarks.Sparks) != 0)
                _marksSr.Add(Overlay(head, "spark", new Vector2(-20f, -126f), 0.22f, 0f, 4));
            foreach (SpriteRenderer sr in _marksSr) sr.enabled = true;
            ApplyOrder();
        }

        /// <summary>An arrow that hit this archer stays stuck in the part it hit until the turn ends (§3.2).</summary>
        public void AddStuckArrow(Vector3 worldPoint, float worldAngleDeg, HitZone zone, Sprite arrow)
        {
            if (_mode == Mode.KnockedOut || !arrow) return;
            if (_stuck.Count >= 4)
            {
                Destroy(_stuck[0]);
                _stuck.RemoveAt(0);
            }
            RigPart part = zone == HitZone.Head ? RigPart.Head : zone == HitZone.Legs ? RigPart.UpperLegFront : RigPart.Torso;
            Transform parent = _parts[(int)part].transform;
            SpriteRenderer sr = NewRenderer(parent, "Stuck", arrow, 0);
            sr.gameObject.AddComponent<OverlayOrder>().Offset = zone == HitZone.Head ? 4 : 1;
            // Push the tip 0.12 m into the body so the arrow looks embedded.
            float a = worldAngleDeg * Mathf.Deg2Rad;
            Vector3 embedded = worldPoint + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * 0.12f;
            sr.transform.position = embedded;
            sr.transform.rotation = Quaternion.Euler(0f, 0f, worldAngleDeg);
            sr.transform.localScale = new Vector3(Mathf.Sign(sr.transform.lossyScale.x) < 0 ? -1f : 1f, 1f, 1f);
            _stuck.Add(sr.gameObject);
            ApplyOrder();
        }

        public void ClearStuckArrows()
        {
            foreach (GameObject g in _stuck) if (g) Destroy(g);
            _stuck.Clear();
        }

        // ---------- anchors for effects (world space) ----------

        public Vector3 HeadWorld => _body ? _body.TransformPoint(ToLocal(_frame.HeadCenter)) : transform.position;
        public Vector3 ChestWorld => _body ? _body.TransformPoint(ToLocal(_frame.Chest)) : transform.position;
        public Vector3 GripWorld => _body ? _body.TransformPoint(ToLocal(_frame.Grip)) : transform.position;

        /// <summary>Tip of the nocked arrow in world space (where the flying arrow starts visually).</summary>
        public Vector3 ArrowTipWorld => _arrow ? _arrow.transform.position : transform.position;

        Vector3 ToLocal(Vector2 u) => new Vector3(u.x * M, (_frame.Lift - u.y) * M, 0f);

        // ---------- per frame ----------

        void Update()
        {
            if (_rig == null) return;
            float dt = Time.deltaTime;
            _t += dt;
            _modeTime += dt;
            switch (_mode)
            {
                case Mode.Release:
                    if (_modeTime > 0.32f) Go(Mode.Idle, RigPoses.Idle(), 0.35f);
                    break;
                case Mode.Hit:
                    if (_modeTime > 0.34f) Go(Mode.Idle, RigPoses.Idle(), 0.3f);
                    break;
                case Mode.KnockedOut:
                    _koAngle = Mathf.MoveTowards(_koAngle, 88f, dt * 260f);
                    break;
            }
            if (_blend < 1f) _blend = Mathf.Min(1f, _blend + dt * _blendSpeed);
            float k = _blend * _blend * (3f - 2f * _blend);
            RigPoses.Blend(_rig.Dims, _rig.Crossbow, _from, _target, k, _fa, _fb, _pose);
            AddLife();
            Solve();
            AnimateLooks(dt);
        }

        void AddLife()
        {
            float breath = Mathf.Sin(_t * Mathf.PI * 2f / 2.4f);
            float depth = _injury >= InjuryStage.Limping ? 2.6f : 1.6f;
            if (_mode == Mode.Idle || _mode == Mode.Victory)
            {
                _pose.Bob += (breath * 0.5f + 0.5f) * depth;
                if (_injury >= InjuryStage.Limping)
                {
                    _pose.Torso += -4f + Mathf.Sin(_t * 2.2f) * 1.5f;
                    _pose.FrontLeg += new Vector3(-6f, 10f, 0f);
                }
                if (_injury >= InjuryStage.Dizzy) _pose.Torso += Mathf.Sin(_t * 3.1f) * 4f;
                if (_mode == Mode.Victory)
                {
                    float hop = Mathf.Abs(Mathf.Sin(_t * Mathf.PI / 0.6f));
                    _root.localPosition = new Vector3(0f, hop * 0.12f, 0f);
                }
            }
            if (_mode != Mode.Victory && _mode != Mode.KnockedOut) _root.localPosition = Vector3.zero;
            if (_mode == Mode.KnockedOut)
            {
                _root.localRotation = Quaternion.Euler(0f, 0f, _facing * _koAngle);
                _root.localPosition = new Vector3(-_facing * 0.25f * (_koAngle / 88f), 0.05f, 0f);
            }
            if (_forceExpression.HasValue) _pose.Expression = _forceExpression.Value;
            else if (_injury >= InjuryStage.Dizzy && _mode == Mode.Idle) _pose.Expression = RigExpression.Hurt;
        }

        void Solve()
        {
            RigSolver.Solve(_rig.Dims, _pose, _rig.Crossbow, _frame);
            SetHead(_frame.Expression);
            float m = M;
            for (int i = 0; i < RigFrame.PartCount; i++)
            {
                Vector2 p = _frame.Position[i];
                Transform t = _parts[i].transform;
                t.localPosition = new Vector3(p.x * m, (_frame.Lift - p.y) * m, 0f);
                t.localRotation = Quaternion.Euler(0f, 0f, -_frame.Rotation[i]);
            }
            if (_frame.BackArmFront != _lastFront) ApplyOrder();
            UpdateString(m);
            UpdateArrow(m);
        }

        void ApplyOrder()
        {
            _lastFront = _frame.BackArmFront;
            RigPart[] order = _frame.BackArmFront ? OrderFront : OrderNormal;
            int slot = 0;
            for (int i = 0; i < order.Length; i++)
            {
                SpriteRenderer sr = _parts[(int)order[i]];
                sr.sortingOrder = _baseOrder + slot;
                slot += 2;
                if (order[i] == RigPart.Bow)
                {
                    for (int s = 0; s < _string.Length; s++) _string[s].sortingOrder = _baseOrder + slot + (s / 2);
                    slot += 3;
                    _arrow.sortingOrder = _baseOrder + slot;
                    slot += 2;
                }
            }
            if (_shadow) _shadow.sortingOrder = _baseOrder - 1;
            foreach (SpriteRenderer sr in GetComponentsInChildren<SpriteRenderer>(true))
            {
                var o = sr.GetComponent<OverlayOrder>();
                if (!o) continue;
                var parentSr = sr.transform.parent ? sr.transform.parent.GetComponent<SpriteRenderer>() : null;
                sr.sortingOrder = (parentSr ? parentSr.sortingOrder : _baseOrder + 40) + o.Offset;
            }
        }

        void UpdateString(float m)
        {
            if (_rig.Crossbow) return;
            Vector3 a = new Vector3(_frame.StringTop.x * m, (_frame.Lift - _frame.StringTop.y) * m, 0f);
            Vector3 b = new Vector3(_frame.StringMid.x * m, (_frame.Lift - _frame.StringMid.y) * m, 0f);
            Vector3 c = new Vector3(_frame.StringBottom.x * m, (_frame.Lift - _frame.StringBottom.y) * m, 0f);
            float sw = 5.217f / Mathf.Max(0.1f, _rig.Scale);
            float glow = 11f * m, ink = 6.5f / 6f * sw * m, core = 2.8f / 6f * sw * m;
            Segment(_string[0], a, b, glow);
            Segment(_string[1], b, c, glow);
            Segment(_string[2], a, b, ink);
            Segment(_string[3], b, c, ink);
            Segment(_string[4], a, b, core);
            Segment(_string[5], b, c, core);
        }

        static void Segment(SpriteRenderer sr, Vector3 a, Vector3 b, float width)
        {
            Vector3 d = b - a;
            float len = d.magnitude;
            Transform t = sr.transform;
            t.localPosition = (a + b) * 0.5f;
            t.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
            t.localScale = new Vector3(len + width, width, 1f);
        }

        void UpdateArrow(float m)
        {
            _arrow.enabled = _frame.ArrowVisible && _mode != Mode.KnockedOut;
            if (!_arrow.enabled) return;
            float a = -_frame.ArrowAngle * Mathf.Deg2Rad;
            var nock = new Vector3(_frame.ArrowNock.x * m, (_frame.Lift - _frame.ArrowNock.y) * m, 0f);
            float len = ArrowTip * m;
            Transform t = _arrow.transform;
            t.localPosition = nock + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * len;
            t.localRotation = Quaternion.Euler(0f, 0f, a * Mathf.Rad2Deg);
            t.localScale = Vector3.one * _rig.Scale;
        }

        void AnimateLooks(float dt)
        {
            bool alive = _mode != Mode.KnockedOut;
            if (_sweat)
            {
                bool on = alive && _injury == InjuryStage.Limping;
                _sweat.enabled = on;
                if (on)
                {
                    float c = Mathf.Repeat(_t, 1.4f) / 1.4f;
                    _sweat.transform.localPosition = new Vector3(70f * M, (96f - c * 40f) * M, 0f);
                    _sweat.color = new Color(1f, 1f, 1f, 1f - c);
                }
            }
            bool dizzy = alive && _injury >= InjuryStage.Dizzy;
            for (int i = 0; i < 3; i++)
            {
                SpriteRenderer s = _stars[i];
                if (!s) continue;
                s.enabled = dizzy;
                if (!dizzy) continue;
                float ang = _t * 3f + i * Mathf.PI * 2f / 3f;
                s.transform.localPosition = new Vector3((6f + Mathf.Cos(ang) * 58f) * M, (136f + Mathf.Sin(ang) * 12f) * M, 0f);
                s.transform.localRotation = Quaternion.Euler(0f, 0f, _t * 180f);
            }
            for (int i = 0; i < 3; i++)
            {
                SpriteRenderer f = _flames[i];
                if (!f) continue;
                f.enabled = alive && _burning;
                if (!f.enabled) continue;
                float flick = 1f + Mathf.Sin(_t * 22f + i * 2f) * 0.12f;
                f.transform.localScale = new Vector3(f.transform.localScale.x, Mathf.Abs(f.transform.localScale.x) * flick, 1f);
            }
            if (_cloud)
            {
                _cloud.enabled = alive && _poisoned;
                if (_cloud.enabled) _cloud.color = new Color(1f, 1f, 1f, 0.55f + Mathf.Sin(_t * 2.4f) * 0.15f);
            }
            if (_helmetSr) _helmetSr.enabled = alive && _helmet;
            if (_bubbleSr)
            {
                _bubbleSr.enabled = alive && _bubble;
                if (_bubbleSr.enabled)
                {
                    float s = 1f + Mathf.Sin(_t * 2f) * 0.03f;
                    _bubbleSr.transform.localPosition = new Vector3(0f, 1.0f * _rig.Scale, 0f);
                    FitWidth(_bubbleSr, 2.3f * _rig.Scale * s);
                }
            }
            if (_ice)
            {
                _ice.enabled = alive && _t < _frozenUntil;
                if (_ice.enabled) _ice.transform.localPosition = new Vector3(0f, 0.95f * _rig.Scale, 0f);
            }
            for (int i = 0; i < 3; i++)
            {
                SpriteRenderer z = _zzz[i];
                if (!z) continue;
                z.enabled = !alive && _koAngle > 80f;
                if (!z.enabled) continue;
                float c = Mathf.Repeat(_t * 0.5f + i / 3f, 1f);
                Vector3 head = HeadWorld;
                z.transform.position = head + new Vector3(0.2f + c * 0.35f, 0.25f + c * 0.7f, 0f);
                z.color = new Color(1f, 1f, 1f, 1f - c);
            }
            if (_marksSr.Count > 0 && (_marks & ElementMarks.Sparks) != 0)
            {
                SpriteRenderer sp = _marksSr[_marksSr.Count - 1];
                if (sp) sp.enabled = alive && Mathf.Repeat(_t, 0.9f) < 0.18f;
            }
            if (_shadow)
            {
                _shadow.transform.localPosition = new Vector3(_mode == Mode.KnockedOut ? -_facing * 0.4f * _rig.Scale : 0f, 0.02f, 0f);
                _shadow.color = new Color(1f, 1f, 1f, _mode == Mode.Victory ? 0.75f : 1f);
            }
        }
    }
}
