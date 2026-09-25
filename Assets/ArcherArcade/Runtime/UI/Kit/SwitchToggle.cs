using System;
using ArcherArcade.Core;
using ArcherArcade.Feel;
using ArcherArcade.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ArcherArcade.UI
{
    /// <summary>
    /// Settings switch from the design: 44 × 26 track (on #12A67A, off var(--track)), 20 dp white knob with a soft
    /// shadow sliding 3 → 21 dp with an overshoot (220 ms). Plays toggle on/off sounds.
    /// </summary>
    public sealed class SwitchToggle : MonoBehaviour, IPointerClickHandler
    {
        static readonly Color OnColor = new Color32(0x12, 0xA6, 0x7A, 0xFF);
        Image _track;
        RectTransform _knob;
        bool _on;
        Action<bool> _changed;
        TweenHandle _anim;

        public bool On => _on;

        public static SwitchToggle Create(Transform parent, bool on, Action<bool> changed)
        {
            RectTransform root = UiKit.Rect(parent, "Switch");
            root.sizeDelta = new Vector2(44f, 26f);
            UiKit.Size(root, 44f, 26f);
            var s = root.gameObject.AddComponent<SwitchToggle>();
            s._track = UiKit.Box(root, "Track", on ? OnColor : UiKit.P.Track, 13f);
            UiKit.Stretch(s._track.rectTransform);
            s._track.raycastTarget = true;
            Image shadow = UiKit.Box(root, "KnobShadow", new Color(0, 0, 0, 0.2f), 0f);
            shadow.sprite = ShapeSprites.Shadow(10f, 4f);
            shadow.type = Image.Type.Sliced;
            Image knob = UiKit.Disc(root, "Knob", Color.white);
            s._knob = knob.rectTransform;
            UiKit.At(s._knob, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(on ? 21f : 3f, 0f), new Vector2(20f, 20f));
            UiKit.At(shadow.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(on ? 17f : -1f, -2f), new Vector2(28f, 28f));
            s._on = on;
            s._changed = changed;
            s._shadow = shadow.rectTransform;
            return s;
        }

        RectTransform _shadow;

        public void OnPointerClick(PointerEventData e) => Set(!_on, true);

        public void Set(bool on, bool notify)
        {
            if (on == _on) return;
            _on = on;
            if (notify)
            {
                ServiceLocator.Audio?.Play(on ? SoundId.ToggleOn : SoundId.ToggleOff);
                ServiceLocator.Haptics?.Play(HapticId.UiTap);
            }
            _anim.Kill();
            float from = _knob.anchoredPosition.x, to = on ? 21f : 3f;
            Color c0 = _track.color, c1 = on ? OnColor : UiKit.P.Track;
            _anim = Tween.Value(0f, 1f, 0.22f, k =>
            {
                if (!_knob) return;
                float x = Mathf.LerpUnclamped(from, to, k);
                _knob.anchoredPosition = new Vector2(x, 0f);
                _shadow.anchoredPosition = new Vector2(x - 4f, -2f);
                _track.color = Color.Lerp(c0, c1, Mathf.Clamp01(k));
            }, EaseType.OutBack, 0f, true, null, this);
            if (notify) _changed?.Invoke(on);
        }

        void OnDestroy() => Tween.KillTarget(this);
    }
}
