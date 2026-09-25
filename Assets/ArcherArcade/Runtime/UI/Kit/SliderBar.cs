using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ArcherArcade.UI
{
    /// <summary>
    /// Range slider (design: input type=range with accent #6D4AFF): 6 dp track, purple fill, 18 dp white knob.
    /// Value 0..1; <c>changed</c> fires while dragging, <c>committed</c> on release (save then).
    /// </summary>
    public sealed class SliderBar : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        static readonly Color Accent = new Color32(0x6D, 0x4A, 0xFF, 0xFF);
        RectTransform _area, _fill, _knob;
        float _value;
        Action<float> _changed, _committed;

        public float Value => _value;

        public static SliderBar Create(Transform parent, float value, Action<float> changed, Action<float> committed)
        {
            RectTransform root = UiKit.Rect(parent, "Slider");
            UiKit.Size(root, -1, 24f, 1f);
            var s = root.gameObject.AddComponent<SliderBar>();
            UiKit.HitArea(root);
            s._area = UiKit.Rect(root, "Area");
            UiKit.Stretch(s._area, 9f, 0f, 9f, 0f);
            Image track = UiKit.Box(s._area, "Track", UiKit.P.Track, 3f);
            UiKit.At(track.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(0f, 6f));
            track.rectTransform.anchorMax = new Vector2(1f, 0.5f);
            track.rectTransform.sizeDelta = new Vector2(0f, 6f);
            Image fill = UiKit.Box(s._area, "Fill", Accent, 3f);
            s._fill = fill.rectTransform;
            s._fill.anchorMin = new Vector2(0f, 0.5f);
            s._fill.pivot = new Vector2(0f, 0.5f);
            Image shadow = UiKit.Box(s._area, "KnobShadow", new Color(0, 0, 0, 0.22f), 0f);
            shadow.sprite = ShapeSprites.Shadow(9f, 4f);
            shadow.type = Image.Type.Sliced;
            Image knob = UiKit.Disc(s._area, "Knob", Color.white);
            s._knob = knob.rectTransform;
            s._knob.sizeDelta = new Vector2(18f, 18f);
            shadow.transform.SetParent(s._knob, false);
            UiKit.Stretch(shadow.rectTransform, -4f, -2f, -4f, -6f);
            shadow.transform.SetAsFirstSibling();
            Image ring = UiKit.Box(s._knob, "Ring", Accent, 0f);
            ring.sprite = ShapeSprites.Outline(9f, 2f);
            ring.type = Image.Type.Sliced;
            UiKit.Stretch(ring.rectTransform);
            s._changed = changed;
            s._committed = committed;
            s.SetValue(value);
            return s;
        }

        public void SetValue(float v)
        {
            _value = Mathf.Clamp01(v);
            _fill.anchorMax = new Vector2(_value, 0.5f);
            _fill.sizeDelta = new Vector2(0f, 6f);
            _fill.anchoredPosition = Vector2.zero;
            _knob.anchorMin = _knob.anchorMax = new Vector2(_value, 0.5f);
            _knob.anchoredPosition = Vector2.zero;
        }

        void Drag(PointerEventData e)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_area, e.position, e.pressEventCamera, out Vector2 local)) return;
            Rect r = _area.rect;
            float v = Mathf.InverseLerp(r.xMin, r.xMax, local.x);
            if (Mathf.Abs(v - _value) < 0.0001f) return;
            SetValue(v);
            _changed?.Invoke(_value);
        }

        public void OnPointerDown(PointerEventData e) => Drag(e);
        public void OnDrag(PointerEventData e) => Drag(e);
        public void OnPointerUp(PointerEventData e) => _committed?.Invoke(_value);
    }
}
