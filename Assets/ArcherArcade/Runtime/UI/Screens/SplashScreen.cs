using System;
using ArcherArcade.Core;
using ArcherArcade.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArcherArcade.UI
{
    /// <summary>
    /// Splash (design `splash`, state 01): radial purple, floating gold logo tile, "Archer Arcade" with its hard
    /// shadow, the studio credit, a 1.6 s load bar and "Tap to start". Moves on after 1.9 s or on tap.
    /// </summary>
    public sealed class SplashScreen : UiScreen
    {
        readonly Action _done;
        RectTransform _logo;
        float _t;
        bool _finished;

        public SplashScreen(Action done) => _done = done;

        public override Texture Background => ShapeSprites.Radial(UiKit.Hex(0x8E6BFF), UiKit.Hex(0x5536D6), 0.5f, 0.6f, 0.7f);
        public override bool ShowCoins => false;

        public override void Build(RectTransform root)
        {
            Image hit = UiKit.HitArea(root);
            var b = root.gameObject.AddComponent<Button>();
            b.transition = Selectable.Transition.None;
            b.onClick.AddListener(Finish);

            RectTransform col = UiKit.Rect(root, "Column");
            UiKit.At(col, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(600f, 330f));
            UiKit.Column(col, 10f, TextAnchor.MiddleCenter, false, false);

            // Logo tile: 96 dp, radius 30, gold with a 7 dp edge, floating.
            RectTransform holder = UiKit.Rect(col, "LogoHolder");
            UiKit.Size(holder, 96f, 103f);
            _logo = UiKit.Rect(holder, "Logo");
            UiKit.At(_logo, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(96f, 96f));
            Image edge = UiKit.Box(_logo, "Edge", Widgets.GoldEdge, 30f);
            UiKit.Stretch(edge.rectTransform, 0, 7, 0, -7);
            Image face = UiKit.Box(_logo, "Face", Widgets.Gold, 30f);
            UiKit.Stretch(face.rectTransform);
            Image bow = UiKit.Box(_logo, "Bow", Color.white, 0);
            bow.sprite = ArtLibrary.Get(ArtLibrary.Ui, "logo_bow");
            bow.preserveAspect = true;
            UiKit.Stretch(bow.rectTransform, 12, 10, 12, 10);

            TextMeshProUGUI title = UiKit.ShadowLabel(col, Loc.T("app_name"), FontRole.Display, 60f, Color.white, UiKit.Hex(0x3A1FB0), 5f);
            UiKit.Size(title.transform.parent as RectTransform, 600f, 70f);
            TextMeshProUGUI studio = UiKit.Label(col, Loc.T("studio"), FontRole.Body, 14f, new Color(1, 1, 1, 0.9f), TextAlignmentOptions.Center);
            UiKit.Size(studio, 400f, 20f);

            RectTransform barHolder = UiKit.Rect(col, "LoadHolder");
            UiKit.Size(barHolder, 220f, 28f);
            Image track = UiKit.Box(barHolder, "Track", new Color(1, 1, 1, 0.2f), 5f);
            UiKit.At(track.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(220f, 10f));
            track.gameObject.AddComponent<RectMask2D>();
            Image fill = UiKit.Box(track.transform, "Fill", Widgets.Gold, 5f);
            RectTransform f = fill.rectTransform;
            f.anchorMin = Vector2.zero;
            f.anchorMax = new Vector2(0f, 1f);
            f.offsetMin = f.offsetMax = Vector2.zero;
            Tween.Value(0f, 1f, 1.6f, k => { if (f) f.anchorMax = new Vector2(k, 1f); }, EaseType.OutCubic, 0f, true, null, f);

            TextMeshProUGUI tap = UiKit.Label(col, Loc.T("tap_to_start"), FontRole.BodyBold, 12f, new Color(1, 1, 1, 0.8f), TextAlignmentOptions.Center);
            UiKit.Size(tap, 300f, 18f);
        }

        public override void OnShow() => ServiceLocator.Audio?.PlayMusic(Feel.SoundId.MusicHome);

        public override void Tick(float dt)
        {
            _t += dt;
            if (_logo) _logo.anchoredPosition = new Vector2(0f, Mathf.Sin(_t * Mathf.PI * 2f / 2.4f) * -3f - 3f);
            if (_t >= 1.9f) Finish();
        }

        public override bool OnBack()
        {
            Finish();
            return true;
        }

        void Finish()
        {
            if (_finished) return;
            _finished = true;
            _done?.Invoke();
        }
    }
}
