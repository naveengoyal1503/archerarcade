using System;
using ArcherArcade.Core;
using ArcherArcade.Feel;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArcherArcade.UI
{
    /// <summary>
    /// 2-Player "Pass the phone" cover (design `pass`, state 04; SCREEN_INVENTORY 22): full-screen pink, a floating
    /// phone, "P2, your turn", "The last aim is hidden. No peeking." and "I'm ready" (a tap anywhere works too).
    /// Hides the board so the next player cannot see the last aim. Back pauses the match.
    /// </summary>
    public sealed class PassModal : UiModal
    {
        readonly string _name;
        readonly Action _ready, _back;
        RectTransform _phone;
        float _t;
        bool _done;

        public PassModal(string playerName, Action ready, Action back)
        {
            _name = playerName;
            _ready = ready;
            _back = back;
        }

        public override Color Dim => Color.clear;
        public override bool TapOutsideCloses => false;

        public override void Build(RectTransform root)
        {
            RectTransform full = (RectTransform)root.parent;
            RawImage bg = UiKit.Picture(full, "Pink", ShapeSprites.Radial(UiKit.Hex(0xFF7FB6), UiKit.Hex(0xE83E8C), 0.5f, 0.45f, 0.7f));
            UiKit.Stretch(bg.rectTransform);
            bg.transform.SetSiblingIndex(1);
            bg.raycastTarget = true;
            var tap = bg.gameObject.AddComponent<Button>();
            tap.transition = Selectable.Transition.None;
            tap.onClick.AddListener(Ready);

            RectTransform col = UiKit.Rect(root, "Col");
            UiKit.At(col, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(700f, 300f));
            UiKit.Column(col, 10f, TextAnchor.MiddleCenter, true, false);
            RectTransform phoneBox = UiKit.Rect(col, "Phone");
            UiKit.Size(phoneBox, -1, 60f);
            TextMeshProUGUI phone = UiKit.Glyph(phoneBox, Icons.PhoneAndroid, 52f, Color.white);
            _phone = (RectTransform)phone.transform;
            UiKit.Stretch(_phone);
            TextMeshProUGUI kicker = UiKit.Label(col, Loc.T("pass_title"), FontRole.BodyBold, 14f, new Color(1f, 1f, 1f, 0.95f), TextAlignmentOptions.Center);
            UiKit.Size(kicker, -1, 20f);
            TextMeshProUGUI title = UiKit.ShadowLabel(col, Loc.F("pass_turn", _name), FontRole.Display, 44f, Color.white, new Color(0f, 0f, 0f, 0.15f), 4f);
            UiKit.Size(title.transform.parent, -1, 54f);
            UiKit.Fit(title, 0.55f);
            TextMeshProUGUI hint = UiKit.Label(col, Loc.T("pass_hidden"), FontRole.BodyBold, 13f, new Color(1f, 1f, 1f, 0.95f), TextAlignmentOptions.Center);
            UiKit.Size(hint, -1, 20f);
            RectTransform holder = UiKit.Rect(col, "Holder");
            UiKit.Size(holder, -1, 68f);
            Button3D ready = Button3D.Create(holder, "Ready", Color.white, new Color(0f, 0f, 0f, 0.18f), 18f, 5f, 4f);
            UiKit.At((RectTransform)ready.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 4f), new Vector2(220f, 54f));
            ready.AddLabel(Loc.T("pass_ready"), FontRole.Display, 22f, UiKit.Hex(0xC92A73));
            ready.Sound = SoundId.UiConfirm;
            ready.OnClick(Ready);
            ready.PopIn(0.1f);
            ServiceLocator.Audio?.Play(SoundId.TurnStart);
        }

        void Ready()
        {
            if (_done) return;
            _done = true;
            Close();
            _ready?.Invoke();
        }

        public override void Tick(float dt)
        {
            _t += dt;
            if (_phone && (ServiceLocator.Settings == null || !ServiceLocator.Settings.ReduceMotion))
                _phone.anchoredPosition = new Vector2(_phone.anchoredPosition.x, Mathf.Sin(_t * Mathf.PI) * 6f);
        }

        public override bool OnBack()
        {
            // Back on the cover pauses (SCREEN_INVENTORY 22); the cover stays under the pause dialog.
            _back?.Invoke();
            return true;
        }
    }
}
