using ArcherArcade.Core;
using ArcherArcade.UI;
using UnityEngine;
using UnityEngine.UI;

namespace ArcherArcade.Feel
{
    /// <summary>
    /// Result-screen confetti rain (design `result`: 9 × 14 dp pieces falling and tumbling, looping). Pooled UI
    /// images, unscaled time; "Reduce motion" drops a third of the pieces and the tumble.
    /// </summary>
    public sealed class Confetti : MonoBehaviour
    {
        static readonly Color[] Colors =
        {
            new Color32(0xFF, 0xD2, 0x3F, 0xFF), new Color32(0xFF, 0x5F, 0xA2, 0xFF), new Color32(0x38, 0xBD, 0xF8, 0xFF),
            new Color32(0x2E, 0xD3, 0xA0, 0xFF), new Color32(0xFF, 0x8A, 0x3D, 0xFF), Color.white
        };

        RectTransform[] _pieces;
        float[] _speed, _spin, _sway, _phase;
        RectTransform _rt;
        bool _calm;
        uint _rng = 0x9E3779B9u;

        public static Confetti Create(RectTransform parent, int count = 36)
        {
            RectTransform rt = UiKit.Rect(parent, "Confetti");
            UiKit.Stretch(rt);
            var c = rt.gameObject.AddComponent<Confetti>();
            c._rt = rt;
            c._calm = ServiceLocator.Settings != null && ServiceLocator.Settings.ReduceMotion;
            if (c._calm) count = count * 2 / 3;
            c._pieces = new RectTransform[count];
            c._speed = new float[count];
            c._spin = new float[count];
            c._sway = new float[count];
            c._phase = new float[count];
            for (int i = 0; i < count; i++)
            {
                Image img = UiKit.Box(rt, "Piece", Colors[i % Colors.Length], 3f);
                RectTransform p = img.rectTransform;
                p.sizeDelta = new Vector2(9f, 14f);
                c._pieces[i] = p;
                c.Reset(i, true);
            }
            return c;
        }

        float R()
        {
            _rng ^= _rng << 13;
            _rng ^= _rng >> 17;
            _rng ^= _rng << 5;
            return (_rng & 0xFFFFFF) / 16777216f;
        }

        void Reset(int i, bool anywhere)
        {
            Rect r = _rt.rect;
            float w = r.width > 0f ? r.width : 844f, h = r.height > 0f ? r.height : 390f;
            float y = anywhere ? h * 0.5f + R() * h : h * 0.5f + 20f;
            _pieces[i].anchoredPosition = new Vector2((R() - 0.5f) * w, y);
            _speed[i] = 90f + R() * 110f;
            _spin[i] = _calm ? 0f : (R() - 0.5f) * 540f;
            _sway[i] = 10f + R() * 26f;
            _phase[i] = R() * 6.28f;
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            float bottom = -_rt.rect.height * 0.5f - 20f;
            for (int i = 0; i < _pieces.Length; i++)
            {
                RectTransform p = _pieces[i];
                _phase[i] += dt * 2f;
                Vector2 pos = p.anchoredPosition;
                pos.y -= _speed[i] * dt;
                pos.x += Mathf.Cos(_phase[i]) * _sway[i] * dt;
                p.anchoredPosition = pos;
                p.localRotation = Quaternion.Euler(0f, 0f, p.localEulerAngles.z + _spin[i] * dt);
                if (!_calm) p.localScale = new Vector3(Mathf.Abs(Mathf.Cos(_phase[i] * 1.7f)) * 0.8f + 0.2f, 1f, 1f);
                if (pos.y < bottom) Reset(i, false);
            }
        }
    }
}
