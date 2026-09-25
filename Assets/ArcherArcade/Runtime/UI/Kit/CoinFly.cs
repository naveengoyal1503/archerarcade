using ArcherArcade.Core;
using ArcherArcade.Feel;
using ArcherArcade.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace ArcherArcade.UI
{
    /// <summary>
    /// Coin fly (DESIGN_TOKENS motion: 600 ms): coins burst out of a point and swoop to the top-right coin pill
    /// with a pop, a clink and a light haptic each. UI space, unscaled time.
    /// </summary>
    public static class CoinFly
    {
        public static void Burst(RectTransform layer, Vector2 from, int count, float delay)
        {
            if (!layer) return;
            Sprite coin = ArtLibrary.Get(ArtLibrary.Fx, "coin");
            Rect r = layer.rect;
            var target = new Vector2(r.xMax - 70f, r.yMax - 30f);
            uint seed = 0x2545F491u;
            for (int i = 0; i < count; i++)
            {
                seed ^= seed << 13; seed ^= seed >> 17; seed ^= seed << 5;
                float a = (seed % 360) * Mathf.Deg2Rad;
                float spread = 40f + (seed % 50);
                Vector2 mid = from + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * spread;
                Image img = UiKit.Box(layer, "Coin", Color.white, 0);
                img.sprite = coin;
                img.preserveAspect = true;
                RectTransform rt = img.rectTransform;
                UiKit.At(rt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), from, new Vector2(26f, 26f));
                rt.localScale = Vector3.zero;
                float d = delay + i * 0.05f;
                bool last = i == count - 1;
                Tween.Value(0f, 1f, 0.6f, k =>
                {
                    if (!rt) return;
                    Vector2 p1 = Vector2.Lerp(from, mid, Mathf.Clamp01(k * 2.2f));
                    Vector2 p = Vector2.Lerp(p1, target - r.center, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((k - 0.3f) / 0.7f)));
                    rt.anchoredPosition = p;
                    float s = k < 0.2f ? k / 0.2f : 1f - Mathf.Clamp01((k - 0.85f) / 0.15f) * 0.6f;
                    rt.localScale = Vector3.one * s;
                }, EaseType.Linear, d, true, () =>
                {
                    if (rt) Object.Destroy(rt.gameObject);
                    ServiceLocator.Audio?.Play(SoundId.Coin, 0.5f, 1f + (last ? 0.1f : 0f));
                    ServiceLocator.Haptics?.Play(HapticId.PowerTick);
                }, rt);
            }
        }
    }
}
