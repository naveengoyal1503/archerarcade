using System;
using ArcherArcade.Theme;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArcherArcade.UI
{
    /// <summary>Two-choice dialog (e.g. "Leave Archer Arcade?" Stay / Quit). Back = the safe choice.</summary>
    public sealed class ConfirmModal : UiModal
    {
        readonly string _title, _body, _safe, _risky, _icon;
        readonly Action _onRisky;
        readonly ButtonStyle _riskyStyle;

        public ConfirmModal(string icon, string title, string body, string safe, string risky, ButtonStyle riskyStyle, Action onRisky)
        {
            _icon = icon;
            _title = title;
            _body = body;
            _safe = safe;
            _risky = risky;
            _riskyStyle = riskyStyle;
            _onRisky = onRisky;
        }

        public override void Build(RectTransform root)
        {
            Palette p = UiKit.P;
            Image panel = UiKit.Box(root, "Panel", p.Solid, 26f);
            UiKit.At(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(340f, 0f));
            panel.raycastTarget = true;
            UiKit.SoftShadow(panel.transform, new Color(0, 0, 0, 0.25f), 26f, 30f, 10f);
            UiKit.Column(panel, 8f, TextAnchor.UpperCenter, true, false, new RectOffset(22, 22, 20, 20));
            var fit = panel.gameObject.AddComponent<ContentSizeFitter>();
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            if (!string.IsNullOrEmpty(_icon))
            {
                TextMeshProUGUI ic = UiKit.Glyph(panel.transform, _icon, 40f, p.Ink);
                UiKit.Size(ic, -1, 46f);
            }
            TextMeshProUGUI t = UiKit.Label(panel.transform, _title, FontRole.Display, 22f, p.Ink, TextAlignmentOptions.Center);
            UiKit.Size(t, -1, 30f);
            UiKit.Fit(t);
            if (!string.IsNullOrEmpty(_body))
            {
                TextMeshProUGUI b = UiKit.Paragraph(panel.transform, _body, FontRole.BodyBold, 13f, p.InkMuted, TextAlignmentOptions.Top);
                UiKit.Size(b, -1, 40f);
            }
            RectTransform row = UiKit.Rect(panel.transform, "Buttons");
            UiKit.Size(row, -1, 50f);
            UiKit.Row(row, 10f, TextAnchor.MiddleCenter, true, true, new RectOffset(0, 0, 4, 4));
            Button3D safe = Button3D.Styled(row, "Safe", ButtonStyle.Success, _safe, 17f);
            safe.OnClick(Close);
            Button3D risky = Button3D.Styled(row, "Risky", _riskyStyle, _risky, 17f);
            risky.OnClick(() =>
            {
                Close();
                _onRisky?.Invoke();
            });
            UIManager.PopPanel(panel.transform);
        }
    }
}
