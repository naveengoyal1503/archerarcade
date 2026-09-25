using ArcherArcade.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace ArcherArcade.UI
{
    /// <summary>
    /// Bar with a rounded track and fill (HP bars, stat bars, streaks). Changes animate like the design's
    /// width transition (.35 s, ease-out). Fills from the left, or from the right for the opponent's HP bar.
    /// </summary>
    public sealed class ProgressBar : MonoBehaviour
    {
        RectTransform _fill;
        Image _fillImage, _ghost;
        RectTransform _ghostRt;
        float _value = -1f;
        bool _fromRight;
        TweenHandle _anim, _ghostAnim;

        public Image FillImage => _fillImage;

        public static ProgressBar Create(Transform parent, Color track, Color fill, float height, float radius, bool fromRight = false,
            bool damageGhost = false)
        {
            RectTransform root = UiKit.Rect(parent, "Bar");
            UiKit.Size(root, -1, height, 1f);
            var b = root.gameObject.AddComponent<ProgressBar>();
            Image t = UiKit.Box(root, "Track", track, radius);
            UiKit.Stretch(t.rectTransform);
            t.gameObject.AddComponent<RectMask2D>();
            if (damageGhost)
            {
                b._ghost = UiKit.Box(t.transform, "Ghost", new Color(1f, 1f, 1f, 0.85f), radius);
                b._ghostRt = b._ghost.rectTransform;
            }
            b._fillImage = UiKit.Box(t.transform, "Fill", fill, radius);
            b._fill = b._fillImage.rectTransform;
            b._fromRight = fromRight;
            b.Apply(b._fill, 1f);
            if (b._ghostRt) b.Apply(b._ghostRt, 1f);
            b._value = 1f;
            return b;
        }

        void Apply(RectTransform rt, float v)
        {
            v = Mathf.Clamp01(v);
            if (_fromRight)
            {
                rt.anchorMin = new Vector2(1f - v, 0f);
                rt.anchorMax = Vector2.one;
            }
            else
            {
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = new Vector2(v, 1f);
            }
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        public void SetValue(float v, bool animate = true)
        {
            v = Mathf.Clamp01(v);
            if (Mathf.Approximately(v, _value)) return;
            float from = _value < 0 ? v : _value;
            _value = v;
            _anim.Kill();
            if (!animate)
            {
                Apply(_fill, v);
                if (_ghostRt) Apply(_ghostRt, v);
                return;
            }
            _anim = Tween.Value(from, v, 0.35f, x => { if (_fill) Apply(_fill, x); }, EaseType.OutCubic, 0f, true, null, this);
            if (_ghostRt && v < from)
            {
                // Damage ghost: the lost part flashes white, then drains after a beat.
                _ghostAnim.Kill();
                Apply(_ghostRt, from);
                _ghostAnim = Tween.Value(from, v, 0.45f, x => { if (_ghostRt) Apply(_ghostRt, x); }, EaseType.InOutQuad, 0.35f, true, null, this);
            }
            else if (_ghostRt)
            {
                Apply(_ghostRt, v);
            }
        }

        public void SetColor(Color c) => _fillImage.color = c;

        void OnDestroy() => Tween.KillTarget(this);
    }
}
