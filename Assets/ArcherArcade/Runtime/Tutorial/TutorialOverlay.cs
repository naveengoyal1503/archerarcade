using ArcherArcade.Core;
using ArcherArcade.Match;
using ArcherArcade.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArcherArcade.Tutorial
{
    /// <summary>
    /// First-launch tutorial over Level 1 (SCREEN_INVENTORY 2): a hand shows the drag-back-and-release, a bubble
    /// says what to do next ("Drag back from anywhere to aim" → "Longer drag = stronger shot" → "Let go to
    /// shoot!" → "Nice shot!" → "Hit all 3 targets"), and Skip ends it. Touches pass through except Skip; it ends
    /// for good when the level is over or skipped (SaveData.TutorialDone).
    /// </summary>
    public sealed class TutorialOverlay : MonoBehaviour
    {
        enum Step { Drag, Pull, Release, Shot }

        MatchSceneRoot _r;
        RectTransform _root, _hand, _bubble;
        TextMeshProUGUI _text;
        CanvasGroup _handGroup;
        Step _step = Step.Drag;
        float _t, _stepTime;
        int _shots;

        public static TutorialOverlay Create(RectTransform parent, MatchSceneRoot root)
        {
            RectTransform rt = UiKit.Rect(parent, "Tutorial");
            UiKit.Stretch(rt);
            var o = rt.gameObject.AddComponent<TutorialOverlay>();
            o._r = root;
            o._root = rt;
            o.Build();
            return o;
        }

        void Build()
        {
            Image bubble = UiKit.Box(_root, "Bubble", new Color(1f, 1f, 1f, 0.95f), 16f);
            _bubble = bubble.rectTransform;
            UiKit.At(_bubble, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -122f), new Vector2(380f, 44f));
            UiKit.SoftShadow(bubble.transform, new Color(0f, 0f, 0f, 0.18f), 16f, 12f, 4f);
            TextMeshProUGUI icon = UiKit.Glyph(bubble.transform, Icons.TouchApp, 22f, Widgets.Purple);
            UiKit.At((RectTransform)icon.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(12f, 0f), new Vector2(28f, 28f));
            _text = UiKit.Label(bubble.transform, "", FontRole.Display, 17f, Widgets.Navy, TextAlignmentOptions.Center);
            UiKit.Stretch((RectTransform)_text.transform, 44f, 0f, 14f, 0f);
            UiKit.Fit(_text, 0.6f);

            _hand = UiKit.Rect(_root, "Hand");
            _hand.sizeDelta = new Vector2(64f, 64f);
            _handGroup = _hand.gameObject.AddComponent<CanvasGroup>();
            _handGroup.blocksRaycasts = false;
            Image ring = UiKit.Box(_hand, "Ring", new Color(1f, 1f, 1f, 0.8f), 0f);
            ring.sprite = ShapeSprites.Ring;
            UiKit.At(ring.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-8f, 14f), new Vector2(30f, 30f));
            TextMeshProUGUI hand = UiKit.Glyph(_hand, Icons.TouchApp, 54f, Color.white);
            hand.outlineWidth = 0.2f;
            hand.outlineColor = new Color32(0x2A, 0x23, 0x50, 0xFF);
            UiKit.Stretch((RectTransform)hand.transform);

            Button3D skip = Button3D.Create(_root, "Skip", new Color(1f, 1f, 1f, 0.92f), new Color(0f, 0f, 0f, 0.18f), 14f, 4f, 3f);
            UiKit.At((RectTransform)skip.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-12f, -128f), new Vector2(90f, 36f));
            skip.AddLabel(Loc.T("hud_skip"), FontRole.Display, 15f, Widgets.Navy);
            skip.OnClick(() => _r.FinishTutorial());
            Set(Step.Drag);
        }

        void Set(Step step)
        {
            _step = step;
            _stepTime = 0f;
            switch (step)
            {
                case Step.Drag: _text.text = Loc.T("tut_drag"); break;
                case Step.Pull: _text.text = Loc.T("tut_power"); break;
                case Step.Release: _text.text = Loc.T("tut_release"); break;
                case Step.Shot: _text.text = _shots <= 1 ? Loc.T("tut_nice") : Loc.T("tut_hit_all"); break;
            }
            _bubble.sizeDelta = new Vector2(Mathf.Clamp(_text.preferredWidth + 70f, 220f, 520f), 44f);
            Tweening.Tween.Scale(_bubble, Vector3.one * 0.85f, Vector3.one, 0.25f, Tweening.EaseType.OutBack);
        }

        public void OnDragBegan()
        {
            if (_step == Step.Drag || _step == Step.Shot) Set(Step.Pull);
        }

        public void OnShot()
        {
            _shots++;
            Set(Step.Shot);
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            _t += dt;
            _stepTime += dt;
            DragAim d = _r.Drag;
            if (_step == Step.Pull && d != null && d.Dragging && d.Power > 0.55f) Set(Step.Release);
            if (_step == Step.Shot && _stepTime > 2.2f && _shots == 1) _text.text = Loc.T("tut_hit_all");

            bool showHand = _r.Aiming && (d == null || !d.Dragging) && _step != Step.Release;
            _handGroup.alpha = Mathf.MoveTowards(_handGroup.alpha, showHand ? 1f : 0f, dt * 4f);
            if (_handGroup.alpha <= 0.01f) return;
            // Demo: press near the archer, pull back and down, let go.
            Vector2 archer = _r.ShooterScreen(out Vector2 chest);
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_root, chest, null, out Vector2 start)) return;
            start += new Vector2(90f, -20f);
            float cycle = (_t % 2.4f) / 2.4f;
            float pull = cycle < 0.15f ? 0f : cycle < 0.7f ? Mathf.SmoothStep(0f, 1f, (cycle - 0.15f) / 0.55f) : 1f;
            Vector2 end = start + new Vector2(-150f, -70f);
            _hand.anchoredPosition = Vector2.Lerp(start, end, pull);
            float press = cycle < 0.15f ? cycle / 0.15f : cycle > 0.85f ? 1f - (cycle - 0.85f) / 0.15f : 1f;
            _hand.localScale = Vector3.one * (1.1f - press * 0.15f);
            if (archer == Vector2.zero) _handGroup.alpha = 0f;
        }
    }
}
