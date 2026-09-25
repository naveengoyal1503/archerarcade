using ArcherArcade.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArcherArcade.UI
{
    /// <summary>Toast from the design: top 70 dp, #2A2350 pill, white Nunito 800 13, pops in, gone after 1.9 s.</summary>
    public sealed class ToastLayer : MonoBehaviour
    {
        const float Seconds = 1.9f;
        RectTransform _box;
        TextMeshProUGUI _text;
        CanvasGroup _group;
        float _hideAt = -1f;

        public static ToastLayer Create(RectTransform parent)
        {
            RectTransform root = UiKit.Rect(parent, "Toasts");
            UiKit.Stretch(root);
            var t = root.gameObject.AddComponent<ToastLayer>();
            Image bg = UiKit.Box(root, "Toast", UiKit.Hex(0x2A2350), 14f);
            t._box = bg.rectTransform;
            UiKit.At(t._box, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(200f, 36f));
            UiKit.SoftShadow(bg.transform, new Color(0, 0, 0, 0.25f), 14f, 18f, 6f);
            t._text = UiKit.Label(bg.transform, "", FontRole.Body, 13f, Color.white, TextAlignmentOptions.Center);
            UiKit.Stretch((RectTransform)t._text.transform, 16, 0, 16, 0);
            t._group = bg.gameObject.AddComponent<CanvasGroup>();
            t._group.blocksRaycasts = false;
            t._group.alpha = 0f;
            return t;
        }

        public void Show(string message)
        {
            _text.text = message;
            float w = Mathf.Min(_text.preferredWidth + 32f, 600f);
            _box.sizeDelta = new Vector2(w, 36f);
            _group.alpha = 1f;
            Tween.KillTarget(_box);
            Tween.Scale(_box, Vector3.one * 0.3f, Vector3.one, 0.25f, EaseType.OutBack);
            _hideAt = Time.unscaledTime + Seconds;
        }

        void Update()
        {
            if (_hideAt < 0f || Time.unscaledTime < _hideAt) return;
            _hideAt = -1f;
            Tween.Fade(_group, 1f, 0f, 0.18f);
        }
    }
}
