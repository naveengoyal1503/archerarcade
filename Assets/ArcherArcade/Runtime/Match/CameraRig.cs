using ArcherArcade.Arena;
using ArcherArcade.Feel;
using UnityEngine;

namespace ArcherArcade.Match
{
    /// <summary>
    /// Orthographic match camera in metres (GAME_DESIGN §3.2): frames the shooter while aiming, follows the arrow
    /// with a smooth zoom-out that fits shooter, arrow and target, then eases back (DESIGN_TOKENS §7: follow 0.35 s,
    /// return 0.5 s). The ground line sits where the design has it (y 316 of 390). Shake and zoom punches are
    /// added on top; the sky and parallax follow after every move.
    /// </summary>
    public sealed class CameraRig : MonoBehaviour
    {
        /// <summary>Share of the view below the ground line (design: 74 of 390 dp).</summary>
        public const float GroundShare = 74f / 390f;
        public const float MinHalfWidth = 9.2f, MaxHalfWidth = 24f;

        Camera _cam;
        ArenaView _arena;
        Vector2 _center, _goal, _vel;
        float _halfW = 10f, _goalHalfW = 10f, _halfWVel;
        float _smooth = 0.35f;
        float _punch;

        public Camera Camera => _cam;
        public float Aspect => _cam ? Mathf.Max(1f, _cam.aspect) : 2.16f;
        public float Size => _halfW / Aspect;
        public Vector2 Center => _center;

        public static CameraRig Create(Transform parent)
        {
            var go = new GameObject("MatchCamera");
            go.transform.SetParent(parent, false);
            go.tag = "MainCamera";
            var cam = go.AddComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color32(0x8F, 0xD3, 0xFF, 0xFF);
            cam.nearClipPlane = -50f;
            cam.farClipPlane = 50f;
            cam.depth = 0;
            go.transform.position = new Vector3(0f, 0f, -10f);
            var rig = go.AddComponent<CameraRig>();
            rig._cam = cam;
            return rig;
        }

        public void Bind(ArenaView arena) => _arena = arena;

        /// <summary>
        /// Frames the box (metres) with the ground line <paramref name="groundY"/> at the design height; wider
        /// boxes zoom out up to <see cref="MaxHalfWidth"/>, beyond that the view centres on <paramref name="focusX"/>.
        /// </summary>
        public void Frame(float minX, float maxX, float groundY, float topY, float smooth, float focusX = float.NaN, float margin = 1.2f)
        {
            float aspect = Aspect;
            float halfW = Mathf.Max(MinHalfWidth, (maxX - minX) * 0.5f + margin);
            // Height needed: ground share below the ground line plus the top of the box with headroom.
            float needH = (topY + 0.8f - groundY) / (1f - GroundShare);
            halfW = Mathf.Max(halfW, needH * 0.5f * aspect);
            float cx = (minX + maxX) * 0.5f;
            if (halfW > MaxHalfWidth)
            {
                halfW = MaxHalfWidth;
                if (!float.IsNaN(focusX)) cx = Mathf.Clamp(focusX, minX + halfW - margin, maxX - halfW + margin);
            }
            float size = halfW / aspect;
            float cy = groundY - size * 2f * GroundShare + size;
            _goal = new Vector2(cx, cy);
            _goalHalfW = halfW;
            _smooth = smooth;
        }

        public void Snap()
        {
            _center = _goal;
            _halfW = _goalHalfW;
            _vel = Vector2.zero;
            _halfWVel = 0f;
            Apply(0f);
        }

        /// <summary>Quick zoom-in punch (headshots, knockouts), 0..1.</summary>
        public void Punch(float amount)
        {
            if (Core.ServiceLocator.Settings != null && Core.ServiceLocator.Settings.ReduceMotion) return;
            _punch = Mathf.Max(_punch, amount);
        }

        void LateUpdate()
        {
            float dt = Time.deltaTime;
            if (dt > 0f)
            {
                _center = Vector2.SmoothDamp(_center, _goal, ref _vel, _smooth, Mathf.Infinity, dt);
                _halfW = Mathf.SmoothDamp(_halfW, _goalHalfW, ref _halfWVel, _smooth * 1.2f, Mathf.Infinity, dt);
            }
            _punch = Mathf.MoveTowards(_punch, 0f, Time.unscaledDeltaTime * 1.6f);
            Apply(Time.unscaledDeltaTime);
        }

        void Apply(float unscaledDt)
        {
            if (!_cam) return;
            float aspect = Aspect;
            float halfW = _halfW * (1f - _punch * 0.06f);
            float size = halfW / aspect;
            _cam.orthographicSize = size;
            Vector2 shakeDp = ScreenShake.Sample(unscaledDt);
            float dpToWorld = size * 2f / 390f;
            var pos = new Vector3(_center.x + shakeDp.x * dpToWorld, _center.y + shakeDp.y * dpToWorld, -10f);
            transform.position = pos;
            if (_arena) _arena.Place(new Vector3(_center.x, _center.y, 0f), size, aspect);
        }

        /// <summary>World point → screen pixels.</summary>
        public Vector2 ToScreen(Vector3 world) => _cam ? (Vector2)_cam.WorldToScreenPoint(world) : Vector2.zero;
    }
}
