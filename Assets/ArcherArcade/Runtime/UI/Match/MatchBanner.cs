using ArcherArcade.Core;
using ArcherArcade.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArcherArcade.UI
{
    /// <summary>
    /// Big centre banner for match moments: level intro (name + goal), "FIGHT!", "Round 2", "Wave 3",
    /// "Ranger goes first!", "Next: Captain Thorn", "Wave 4 cleared!". Pops in (aaPop 300 ms, easeOutBack), holds,
    /// then fades; unscaled time so it plays through slow-mo. Never blocks touches.
    /// </summary>
    public sealed class MatchBanner : MonoBehaviour
    {
        RectTransform _box;
        CanvasGroup _group;
        TextMeshProUGUI _title, _sub;
        Image _ribbon;
        float _left;

        public static MatchBanner Create(RectTransform parent)
        {
            RectTransform rt = UiKit.Rect(parent, "Banner");
            UiKit.Stretch(rt);
            var b = rt.gameObject.AddComponent<MatchBanner>();
            b._box = UiKit.Rect(rt, "Box");
            UiKit.At(b._box, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(620f, 120f));
            b._group = b._box.gameObject.AddComponent<CanvasGroup>();
            b._group.blocksRaycasts = false;
            b._group.interactable = false;
            b._ribbon = UiKit.Box(b._box, "Ribbon", new Color(0.16f, 0.14f, 0.31f, 0.55f), 22f);
            UiKit.Stretch(b._ribbon.rectTransform, 40f, 18f, 40f, 10f);
            b._title = UiKit.ShadowLabel(b._box, "", FontRole.Display, 46f, Color.white, new Color(0f, 0f, 0f, 0.18f), 5f);
            RectTransform tr = (RectTransform)b._title.transform.parent;
            UiKit.At(tr, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -18f), new Vector2(600f, 60f));
            UiKit.Fit(b._title, 0.5f);
            b._sub = UiKit.Label(b._box, "", FontRole.BodyBold, 15f, Color.white, TextAlignmentOptions.Center);
            UiKit.At((RectTransform)b._sub.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -80f), new Vector2(560f, 24f));
            UiKit.Fit(b._sub, 0.6f);
            b._box.gameObject.SetActive(false);
            return b;
        }

        public void Show(string title, string subtitle, float seconds)
        {
            Tween.KillTarget(this);
            _box.gameObject.SetActive(true);
            _title.text = title;
            _sub.text = subtitle ?? "";
            _sub.gameObject.SetActive(!string.IsNullOrEmpty(subtitle));
            _ribbon.rectTransform.offsetMin = new Vector2(40f, string.IsNullOrEmpty(subtitle) ? 40f : 10f);
            _left = seconds;
            _group.alpha = 1f;
            bool calm = ServiceLocator.Settings != null && ServiceLocator.Settings.ReduceMotion;
            if (calm) _box.localScale = Vector3.one;
            else Tween.Value(0.3f, 1f, 0.3f, k => { if (_box) _box.localScale = Vector3.one * k; }, EaseType.OutBack, 0f, true, null, this);
        }

        public void Hide()
        {
            _left = 0f;
            _box.gameObject.SetActive(false);
        }

        void Update()
        {
            if (!_box.gameObject.activeSelf) return;
            _left -= Time.unscaledDeltaTime;
            if (_left > 0.3f) return;
            _group.alpha = Mathf.Clamp01(_left / 0.3f);
            if (_left <= 0f) _box.gameObject.SetActive(false);
        }

        void OnDestroy() => Tween.KillTarget(this);
    }
}
