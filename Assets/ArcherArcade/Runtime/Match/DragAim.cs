using System.Collections.Generic;
using ArcherArcade.Core;
using ArcherArcade.Feel;
using ArcherArcade.Logic;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace ArcherArcade.Match
{
    /// <summary>
    /// Drag-to-pull aiming (GAME_DESIGN §3.1): touch anywhere on the play area (not on HUD buttons) and drag away
    /// from the target. The angle follows the drag (−10° … +80°), the pull sets power
    /// (Ballistics.PowerFromDrag, × aim sensitivity). Releasing within 12 dp of the start cancels without using the
    /// turn. Haptic tick every 10 % of power; the bow creak rises with power.
    /// </summary>
    public sealed class DragAim : MonoBehaviour
    {
        ShotConfig _cfg;
        PointerEventData _ped;
        EventSystem _pedSystem;
        readonly List<RaycastResult> _hits = new List<RaycastResult>(8);
        int _lastTick;

        public bool Enabled;
        public int Facing = 1;
        /// <summary>Canvas scale (px per dp) of the HUD, to measure drags in dp.</summary>
        public float PixelsPerDp = 1f;

        public bool Dragging { get; private set; }
        public Vector2 StartScreen { get; private set; }
        public Vector2 CurrentScreen { get; private set; }
        public float DragDp { get; private set; }
        public float AngleDeg { get; private set; } = 20f;
        public float Power { get; private set; }
        public float HeldSeconds { get; private set; }
        public bool InDeadZone => DragDp < _cfg.CancelDragDp;

        public System.Action Began;
        public System.Action<float, float> Released;
        public System.Action Cancelled;

        public static DragAim Create(Transform parent, ShotConfig cfg)
        {
            var go = new GameObject("DragAim");
            go.transform.SetParent(parent, false);
            var d = go.AddComponent<DragAim>();
            d._cfg = cfg;
            return d;
        }

        public void CancelDrag()
        {
            if (!Dragging) return;
            Dragging = false;
            ServiceLocator.Audio?.Creak(false);
            Cancelled?.Invoke();
        }

        void Update()
        {
            if (!ReadPointer(out bool pressed, out bool held, out bool released, out Vector2 pos)) return;
            if (!Enabled)
            {
                if (Dragging) CancelDrag();
                return;
            }
            if (pressed && !Dragging && !OverUi(pos))
            {
                Dragging = true;
                StartScreen = CurrentScreen = pos;
                DragDp = 0f;
                Power = 0f;
                HeldSeconds = 0f;
                _lastTick = 0;
                Began?.Invoke();
                ServiceLocator.Audio?.Play(SoundId.BowDraw, 0.8f);
                return;
            }
            if (!Dragging) return;
            HeldSeconds += Time.unscaledDeltaTime;
            if (held || released) Measure(pos);
            if (released || !held)
            {
                Dragging = false;
                ServiceLocator.Audio?.Creak(false);
                if (InDeadZone) Cancelled?.Invoke();
                else Released?.Invoke(AngleDeg, Power);
            }
        }

        void Measure(Vector2 pos)
        {
            CurrentScreen = pos;
            Vector2 pull = StartScreen - pos;
            float scale = Mathf.Max(0.01f, PixelsPerDp);
            DragDp = pull.magnitude / scale;
            float angle = Mathf.Atan2(pull.y, pull.x * Facing) * Mathf.Rad2Deg;
            // Pulling towards the target: pin to the nearest limit instead of flipping over.
            if (angle > 135f || angle < -135f) angle = pull.y >= 0f ? (float)_cfg.MaxAngleDeg : (float)_cfg.MinAngleDeg;
            AngleDeg = Mathf.Clamp(angle, (float)_cfg.MinAngleDeg, (float)_cfg.MaxAngleDeg);
            double sens = ServiceLocator.Settings != null ? ServiceLocator.Settings.Data.AimSensitivity : 1.0;
            float eff = InDeadZone ? 0f : DragDp * (float)sens;
            Power = (float)Ballistics.PowerFromDrag(eff, _cfg);
            int tick = Mathf.FloorToInt(Power * 10f + 0.0001f);
            if (tick != _lastTick)
            {
                if (tick > _lastTick) ServiceLocator.Haptics?.Play(HapticId.PowerTick);
                _lastTick = tick;
            }
            ServiceLocator.Audio?.Creak(Power > 0.02f, Power);
        }

        bool OverUi(Vector2 pos)
        {
            EventSystem es = EventSystem.current;
            if (!es) return false;
            if (_ped == null || _pedSystem != es)
            {
                _ped = new PointerEventData(es);
                _pedSystem = es;
            }
            _ped.position = pos;
            _hits.Clear();
            es.RaycastAll(_ped, _hits);
            return _hits.Count > 0;
        }

        static bool ReadPointer(out bool pressed, out bool held, out bool released, out Vector2 pos)
        {
#if ENABLE_INPUT_SYSTEM
            Pointer p = Pointer.current;
            if (p == null)
            {
                pressed = held = released = false;
                pos = Vector2.zero;
                return false;
            }
            pressed = p.press.wasPressedThisFrame;
            held = p.press.isPressed;
            released = p.press.wasReleasedThisFrame;
            pos = p.position.ReadValue();
            return true;
#else
            pressed = Input.GetMouseButtonDown(0);
            held = Input.GetMouseButton(0);
            released = Input.GetMouseButtonUp(0);
            pos = Input.mousePosition;
            return true;
#endif
        }
    }
}
