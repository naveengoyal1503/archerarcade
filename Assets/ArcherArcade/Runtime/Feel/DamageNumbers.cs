using ArcherArcade.UI;
using TMPro;
using UnityEngine;

namespace ArcherArcade.Feel
{
    /// <summary>
    /// Floating combat text over the world (GAME_DESIGN §3.3): damage numbers (crits in gold and bigger),
    /// "HEADSHOT!", "DOUBLE HIT!", "POOF!", status words. Pooled TextMeshPro labels in the HUD canvas that follow
    /// their world point, pop in (aaPop), rise 34 dp/s and fade (design match canvas). Unscaled time so text keeps
    /// moving during hit-stop.
    /// </summary>
    public sealed class DamageNumbers : MonoBehaviour
    {
        const int Pool = 24;
        const float Rise = 34f;

        readonly TextMeshProUGUI[] _labels = new TextMeshProUGUI[Pool];
        readonly Vector3[] _world = new Vector3[Pool];
        readonly float[] _life = new float[Pool], _max = new float[Pool], _rise = new float[Pool], _size = new float[Pool];
        RectTransform _layer;
        Camera _cam;
        int _next;

        public static DamageNumbers Create(RectTransform parent)
        {
            RectTransform rt = UiKit.Rect(parent, "DamageNumbers");
            UiKit.Stretch(rt);
            var d = rt.gameObject.AddComponent<DamageNumbers>();
            d._layer = rt;
            for (int i = 0; i < Pool; i++)
            {
                TextMeshProUGUI t = UiKit.Label(rt, "", FontRole.Display, 22f, Color.white, TextAlignmentOptions.Center, "Text" + i);
                ((RectTransform)t.transform).sizeDelta = new Vector2(320f, 60f);
                t.outlineWidth = 0.28f;
                t.outlineColor = new Color32(0x2A, 0x23, 0x50, 0xFF);
                t.gameObject.SetActive(false);
                d._labels[i] = t;
            }
            return d;
        }

        public void SetCamera(Camera cam) => _cam = cam;

        /// <summary>Shows <paramref name="text"/> at a world point.</summary>
        public void Show(Vector3 world, string text, Color color, float size = 22f, float life = 1f)
        {
            int i = _next;
            _next = (_next + 1) % Pool;
            TextMeshProUGUI t = _labels[i];
            t.text = text;
            t.color = color;
            t.fontSize = size;
            t.gameObject.SetActive(true);
            t.transform.SetAsLastSibling();
            _world[i] = world;
            _life[i] = life;
            _max[i] = life;
            _rise[i] = 0f;
            _size[i] = size;
            Place(i);
        }

        public void Clear()
        {
            for (int i = 0; i < Pool; i++)
            {
                _life[i] = 0f;
                _labels[i].gameObject.SetActive(false);
            }
        }

        void Place(int i)
        {
            if (!_cam) return;
            Vector2 screen = _cam.WorldToScreenPoint(_world[i]);
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_layer, screen, null, out Vector2 local))
                ((RectTransform)_labels[i].transform).anchoredPosition = local + new Vector2(0f, _rise[i]);
            float age = _max[i] - _life[i];
            float pop = age < 0.3f ? Tweening.Ease.Evaluate(Tweening.EaseType.OutBack, age / 0.3f) : 1f;
            _labels[i].transform.localScale = Vector3.one * Mathf.Lerp(0.3f, 1f, pop);
            Color c = _labels[i].color;
            c.a = Mathf.Clamp01(_life[i] * 2f);
            _labels[i].color = c;
        }

        void LateUpdate()
        {
            float dt = Time.unscaledDeltaTime;
            for (int i = 0; i < Pool; i++)
            {
                if (_life[i] <= 0f) continue;
                _life[i] -= dt;
                if (_life[i] <= 0f)
                {
                    _labels[i].gameObject.SetActive(false);
                    continue;
                }
                _rise[i] += Rise * dt;
                Place(i);
            }
        }
    }
}
