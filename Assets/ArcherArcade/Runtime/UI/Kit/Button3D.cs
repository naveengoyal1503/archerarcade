using System;
using ArcherArcade.Core;
using ArcherArcade.Feel;
using ArcherArcade.Theme;
using ArcherArcade.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ArcherArcade.UI
{
    /// <summary>
    /// The design's chunky 3D button (DESIGN_TOKENS §2.1, §5): a face over a darker edge showing `depth` dp below;
    /// pressing moves the face down (pressed offset) so the edge shrinks, like CSS translateY + box-shadow.
    /// Content (labels, icons) lives on <see cref="Body"/>. Plays the UI tap sound + haptic on click.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class Button3D : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler, IPointerEnterHandler
    {
        Image _face, _edge;
        RectTransform _body;
        float _press;
        bool _down, _inside;
        Action _click;
        Button _button;
        TweenHandle _pop;

        public RectTransform Body => _body;
        public Image Face => _face;
        public Image Edge => _edge;
        public string Sound = SoundId.UiTap;
        public TextMeshProUGUI Text { get; private set; }
        public TextMeshProUGUI IconText { get; private set; }

        public static Button3D Create(Transform parent, string name, Color face, Color edge, float radius, float depth, float press)
        {
            RectTransform root = UiKit.Rect(parent, name);
            var b = root.gameObject.AddComponent<Button3D>();
            b._press = press;
            if (depth > 0f)
            {
                b._edge = UiKit.Box(root, "Edge", edge, radius);
                UiKit.Stretch(b._edge.rectTransform, 0, depth, 0, -depth);
            }
            b._body = UiKit.Rect(root, "Body");
            UiKit.Stretch(b._body);
            b._face = UiKit.Box(b._body, "Face", face, radius);
            UiKit.Stretch(b._face.rectTransform);
            b._face.raycastTarget = true;
            b._button = root.gameObject.AddComponent<Button>();
            b._button.transition = Selectable.Transition.None;
            b._button.targetGraphic = b._face;
            b._button.onClick.AddListener(b.Clicked);
            return b;
        }

        /// <summary>A design style button (gold, play, primary, success, info, danger, pvp, training, locked).</summary>
        public static Button3D Styled(Transform parent, string name, ButtonStyle style, string label, float fontSize, float radius = 14f,
            float depth = 4f, float press = 3f)
        {
            ButtonColors c = ButtonColors.For(style);
            Button3D b = Create(parent, name, c.Face, c.Edge, radius, depth, press);
            b.AddLabel(label, FontRole.Display, fontSize, c.Text);
            return b;
        }

        /// <summary>Flat "off" button of the design (var(--track) face, no edge, muted text).</summary>
        public static Button3D Off(Transform parent, string name, string label, float fontSize, float radius = 14f)
        {
            Palette p = UiKit.P;
            Button3D b = Create(parent, name, p.Track, Color.clear, radius, 0f, 2f);
            b.AddLabel(label, FontRole.Display, fontSize, p.InkMuted);
            return b;
        }

        public TextMeshProUGUI AddLabel(string text, FontRole role, float size, Color color, TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            TextMeshProUGUI t = UiKit.Label(_body, text, role, size, color, align);
            UiKit.Stretch((RectTransform)t.transform, 6, 0, 6, 0);
            UiKit.Fit(t, 0.6f);
            Text = t;
            return t;
        }

        public TextMeshProUGUI AddIcon(string glyph, float size, Color color)
        {
            TextMeshProUGUI t = UiKit.Glyph(_body, glyph, size, color);
            UiKit.At((RectTransform)t.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size * 1.3f, size * 1.3f));
            IconText = t;
            return t;
        }

        public Button3D OnClick(Action action)
        {
            _click = action;
            return this;
        }

        public void SetColors(Color face, Color edge)
        {
            _face.color = face;
            if (_edge) _edge.color = edge;
        }

        public bool Interactable
        {
            get => _button.interactable;
            set => _button.interactable = value;
        }

        void Clicked()
        {
            if (Sound != null) ServiceLocator.Audio?.Play(Sound);
            ServiceLocator.Haptics?.Play(HapticId.UiTap);
            _click?.Invoke();
        }

        public void OnPointerDown(PointerEventData e)
        {
            if (!_button.interactable) return;
            _down = true;
            _inside = true;
            SetPressed(true);
        }

        public void OnPointerUp(PointerEventData e)
        {
            _down = false;
            SetPressed(false);
        }

        public void OnPointerExit(PointerEventData e)
        {
            _inside = false;
            if (_down) SetPressed(false);
        }

        public void OnPointerEnter(PointerEventData e)
        {
            _inside = true;
            if (_down) SetPressed(true);
        }

        void SetPressed(bool pressed)
        {
            if (_body) _body.anchoredPosition = new Vector2(0f, pressed && _inside ? -_press : 0f);
        }

        /// <summary>Pop-in animation (aaPop: scale .3 → 1.15 → 1, 300 ms) after an optional delay.</summary>
        public void PopIn(float delay = 0f)
        {
            _pop.Kill();
            transform.localScale = Vector3.zero;
            _pop = Tween.Scale(transform, Vector3.one * 0.3f, Vector3.one, 0.3f, EaseType.OutBack, delay);
        }

        void OnDisable()
        {
            _down = false;
            SetPressed(false);
        }

        void OnDestroy() => Tween.KillTarget(transform);
    }
}
